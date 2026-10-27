using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Wallhaven.Core;

public sealed class WallhavenException(string message) : Exception(message);

public sealed partial class WallhavenClient(HttpClient http, TimeSpan? requestSpacing = null) : IDisposable
{
    private readonly SemaphoreSlim gate = new(1);
    private DateTimeOffset lastRequest = DateTimeOffset.MinValue;
    private readonly TimeSpan spacing = requestSpacing ?? TimeSpan.FromMilliseconds(1500);
    private string apiKey = "";
    public bool HasApiKey => apiKey.Length > 0;
    public void SetApiKey(string? value)
    {
        value = value?.Trim() ?? "";
        if (value.Length > 0 && (value.Length is < 16 or > 128 || value.Any(c => !char.IsAsciiLetterOrDigit(c))))
            throw new ArgumentException("API Key 格式不正确，请复制完整的字母数字密钥。");
        apiKey = value;
    }
    public string Redact(string text) => apiKey.Length > 0 ? text.Replace(apiKey, "[API KEY REDACTED]", StringComparison.Ordinal) : text;
    public static bool ValidMask(string? mask) => mask is { Length: 3 } && mask.Any(c => c == '1') && mask.All(c => c is '0' or '1');
    public static bool ValidId(string? id) => id is not null && IdRegex().IsMatch(id);
    [GeneratedRegex("^[a-zA-Z0-9]{6}$")] private static partial Regex IdRegex();

    public static Uri BuildSearchUri(SearchOptions options, int page, string? seed = null)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (!ValidMask(options.Purity)) throw new ArgumentException("请至少选择一种内容级别。");
        if (options.Categories.Length != 3 || options.Categories.Any(c => c is not ('0' or '1')) || options.Categories == "000")
            throw new ArgumentException("请至少选择一种壁纸分类。");
        if (!new[] { "toplist", "date_added", "random", "favorites", "views", "relevance" }.Contains(options.Sorting))
            throw new ArgumentException("不支持的排序方式。");
        var query = new Dictionary<string, string>
        {
            ["q"] = options.Query.Trim(), ["sorting"] = options.Sorting, ["categories"] = options.Categories,
            ["purity"] = options.Purity, ["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture), ["order"] = "desc"
        };
        if (options.Sorting == "toplist") query["topRange"] = options.TopRange;
        if (options.AtLeast.Length > 0) query["atleast"] = options.AtLeast;
        if (options.Ratios.Length > 0) query["ratios"] = options.Ratios;
        if (!string.IsNullOrEmpty(seed) && options.Sorting == "random") query["seed"] = seed;
        return new Uri("https://wallhaven.cc/api/v1/search?" + string.Join("&", query.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}")));
    }

    public async Task<SearchResult> SearchAsync(SearchOptions options, int page, string? seed = null, CancellationToken ct = default)
    {
        RequirePurityAuthorization(options.Purity);
        if (options.Sorting == "random" && string.IsNullOrEmpty(seed))
            seed = System.Security.Cryptography.RandomNumberGenerator.GetString("abcdefghijklmnopqrstuvwxyz0123456789", 6);
        using var doc = await GetJsonAsync(BuildSearchUri(options, page, seed), ct);
        var result = ReadSearch(doc.RootElement, options.Purity);
        return options.Sorting == "random" ? result with { Seed = result.Seed ?? seed } : result;
    }
    private SearchResult ReadSearch(JsonElement root, string purity)
    {
        var data = root.GetProperty("data").Deserialize<List<Wallpaper>>() ?? [];
        var meta = root.GetProperty("meta");
        return new(data.Where(w => w != null && ValidId(w.Id) && MatchesPurity(w.Purity, purity)).ToList(), meta.GetProperty("current_page").GetInt32(),
            meta.GetProperty("last_page").GetInt32(), meta.GetProperty("total").GetInt32(),
            meta.TryGetProperty("seed", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() : null);
    }

    public async Task<Wallpaper> GetWallpaperAsync(string id, CancellationToken ct = default)
    {
        if (!ValidId(id)) throw new ArgumentException("壁纸编号无效。");
        using var doc = await GetJsonAsync(new Uri($"https://wallhaven.cc/api/v1/w/{id}"), ct);
        var wallpaper = doc.RootElement.GetProperty("data").Deserialize<Wallpaper>() ?? throw new WallhavenException("壁纸数据为空。");
        if (wallpaper.Purity == "nsfw" && !HasApiKey) throw new WallhavenException("该壁纸需要有效的 API Key。");
        return wallpaper;
    }
    public bool MatchesPurity(string value, string mask) => !ValidMask(mask) ? false : value switch
    {
        "sfw" => mask[0] == '1', "sketchy" => mask[1] == '1', "nsfw" => mask[2] == '1' && HasApiKey, _ => false
    };
    private void RequirePurityAuthorization(string purity)
    {
        if (!ValidMask(purity)) throw new ArgumentException("请至少选择一种内容级别。");
        if (purity[2] == '1' && !HasApiKey) throw new WallhavenException("NSFW 内容需要先在设置中连接有效的 API Key。");
    }
    public async Task<AccountPreferences> GetSettingsAsync(CancellationToken ct = default)
    {
        if (!HasApiKey) throw new WallhavenException("请先填写 API Key。");
        using var doc = await GetJsonAsync(new Uri("https://wallhaven.cc/api/v1/settings"), ct);
        return doc.RootElement.GetProperty("data").Deserialize<AccountPreferences>() ?? throw new WallhavenException("账户设置为空。");
    }
    public async Task<List<SiteCollection>> GetCollectionsAsync(CancellationToken ct = default)
    {
        if (!HasApiKey) throw new WallhavenException("请先连接 API Key。");
        using var doc = await GetJsonAsync(new Uri("https://wallhaven.cc/api/v1/collections"), ct);
        return doc.RootElement.GetProperty("data").Deserialize<List<SiteCollection>>() ?? [];
    }
    public async Task<SearchResult> GetCollectionAsync(string username, int collectionId, string purity, int page, CancellationToken ct = default)
    {
        if (!HasApiKey) throw new WallhavenException("请先连接 API Key。");
        if (string.IsNullOrWhiteSpace(username) || username.Length > 80 || username.Any(char.IsControl)) throw new ArgumentException("请填写 Wallhaven 用户名。");
        if (collectionId <= 0 || page <= 0) throw new ArgumentException("收藏夹或页码无效。");
        RequirePurityAuthorization(purity);
        var uri = new Uri($"https://wallhaven.cc/api/v1/collections/{Uri.EscapeDataString(username.Trim())}/{collectionId}?purity={purity}&page={page}");
        using var doc = await GetJsonAsync(uri, ct);
        return ReadSearch(doc.RootElement, purity);
    }

    private async Task<JsonDocument> GetJsonAsync(Uri uri, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var delay = spacing - (DateTimeOffset.UtcNow - lastRequest);
            if (delay > TimeSpan.Zero) await Task.Delay(delay, ct);
            lastRequest = DateTimeOffset.UtcNow;
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("WallhavenDesktop/1.0");
            if (HasApiKey) request.Headers.Add("X-API-Key", apiKey);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode == HttpStatusCode.TooManyRequests) throw new WallhavenException("请求过于频繁，请稍后再试（Wallhaven 限流）。");
            if (response.StatusCode == HttpStatusCode.Unauthorized) throw new WallhavenException("API Key 无效或已失效，请在设置中重新连接；也可移除 Key 使用公开壁纸。");
            if (response.StatusCode == HttpStatusCode.Forbidden) throw new WallhavenException("Wallhaven 拒绝访问，请检查账户权限或网络后重试。");
            if (response.StatusCode == HttpStatusCode.NotFound) throw new WallhavenException("这张壁纸已不存在，请选择其他壁纸。");
            response.EnsureSuccessStatusCode();
            var text = await response.Content.ReadAsStringAsync(ct);
            try { return JsonDocument.Parse(text); }
            catch (JsonException) { throw new WallhavenException("服务器没有返回有效数据，请稍后重试。"); }
        }
        finally { gate.Release(); }
    }
    public void Dispose() => gate.Dispose();
}
