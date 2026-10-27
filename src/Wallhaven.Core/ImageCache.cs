namespace Wallhaven.Core;

public sealed class ImageCache(HttpClient http, string directory, TimeSpan? downloadTimeout = null,
    TimeSpan? idleTimeout = null, Action<string>? validateImage = null, Action<string, string>? fileCommitted = null)
{
    private readonly SemaphoreSlim downloads = new(3);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, FileStamp> validated = new(StringComparer.OrdinalIgnoreCase);
    private readonly record struct FileStamp(long Length, long Modified, long Created)
    {
        public static FileStamp Read(string path)
        {
            var info = new FileInfo(path);
            return new(info.Length, info.LastWriteTimeUtc.Ticks, info.CreationTimeUtc.Ticks);
        }
    }
    public static Uri ValidateImageUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !(uri.Host.Equals("wallhaven.cc", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".wallhaven.cc", StringComparison.OrdinalIgnoreCase)) || !uri.IsDefaultPort)
            throw new WallhavenException("图片地址不属于 Wallhaven。");
        return uri;
    }
    public string GetPath(Wallpaper wallpaper, bool thumbnail = false)
    {
        if (!WallhavenClient.ValidId(wallpaper.Id)) throw new ArgumentException("壁纸编号无效。");
        var uri = ValidateImageUrl(thumbnail ? wallpaper.Thumbs.Large : wallpaper.Path);
        string extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg" or ".webp")) throw new WallhavenException("不支持的图片格式。");
        return Path.Combine(directory, $"{(thumbnail ? "thumb-" : "wallhaven-")}{wallpaper.Id}{extension}");
    }
    public async Task<string> GetAsync(Wallpaper wallpaper, bool thumbnail = false, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        string path = GetPath(wallpaper, thumbnail);
        await downloads.WaitAsync(ct);
        using var transfer = CancellationTokenSource.CreateLinkedTokenSource(ct);
        transfer.CancelAfter(downloadTimeout ?? TimeSpan.FromMinutes(2));
        var token = transfer.Token;
        string temp = path + "." + Guid.NewGuid().ToString("N") + ".part";
        try
        {
            if (File.Exists(path))
            {
                try { await ValidateAsync(path, token); return path; }
                catch (InvalidDataException) { File.Delete(path); }
            }
            Directory.CreateDirectory(directory);
            var uri = ValidateImageUrl(thumbnail ? wallpaper.Thumbs.Large : wallpaper.Path);
            using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            long limit = thumbnail ? 8 * 1024 * 1024 : 80 * 1024 * 1024;
            long? length = response.Content.Headers.ContentLength;
            if (length > limit) throw new WallhavenException("图片过大，暂不下载超过 80 MB 的原图。");
            await using (var source = await response.Content.ReadAsStreamAsync(token))
            await using (var target = File.Create(temp))
            {
                using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                var buffer = new byte[65536];
                long total = 0;
                int count;
                while (true)
                {
                    readTimeout.CancelAfter(idleTimeout ?? TimeSpan.FromSeconds(20));
                    count = await source.ReadAsync(buffer, readTimeout.Token);
                    readTimeout.CancelAfter(Timeout.InfiniteTimeSpan);
                    if (count == 0) break;
                    total += count;
                    if (total > limit) throw new WallhavenException("图片超过下载大小限制。");
                    await target.WriteAsync(buffer.AsMemory(0, count), token);
                    if (length > 0) progress?.Report((double)total / length.Value * 100);
                }
                if (total == 0) throw new WallhavenException("图片内容为空。");
                if (length.HasValue && total != length.Value) throw new WallhavenException("图片下载不完整，请重试。");
            }
            try { await ValidateAsync(temp, token); }
            catch (InvalidDataException) { throw new WallhavenException("服务器返回的图片损坏或格式无效，请重试。"); }
            File.Move(temp, path, true);
            fileCommitted?.Invoke(temp, path);
            RememberValidation(path, FileStamp.Read(path));
            return path;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new WallhavenException("图片下载超时或长时间没有收到数据，请重试。");
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); }
            finally { downloads.Release(); }
        }
    }
    private Task ValidateAsync(string path, CancellationToken ct) => Task.Run(() =>
    {
        ct.ThrowIfCancellationRequested();
        var stamp = FileStamp.Read(path);
        if (validated.TryGetValue(path, out var known) && known == stamp) return;
        var header = new byte[12];
        using (var file = File.OpenRead(path))
        {
            int count = (int)Math.Min(file.Length, header.Length);
            if (count < 12) throw new InvalidDataException("图片文件不完整。");
            file.ReadExactly(header);
        }
        bool jpeg = header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff;
        bool png = header.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10});
        bool webp = System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP";
        if (!jpeg && !png && !webp) throw new InvalidDataException("图片格式无效。");
        validateImage?.Invoke(path);
        ct.ThrowIfCancellationRequested();
        if (FileStamp.Read(path) != stamp) throw new IOException("图片在读取过程中发生变化，请重试。");
        RememberValidation(path, stamp);
    }, ct);
    private void RememberValidation(string path, FileStamp stamp)
    {
        if (validated.Count >= 1024) validated.Clear();
        validated[path] = stamp;
    }
}
