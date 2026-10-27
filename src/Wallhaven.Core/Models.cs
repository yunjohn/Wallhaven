using System.Text.Json.Serialization;

namespace Wallhaven.Core;

public sealed record Wallpaper
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("url")] public string Url { get; init; } = "";
    [JsonPropertyName("path")] public string Path { get; init; } = "";
    [JsonPropertyName("resolution")] public string Resolution { get; init; } = "";
    [JsonPropertyName("category")] public string Category { get; init; } = "";
    [JsonPropertyName("purity")] public string Purity { get; init; } = "";
    [JsonPropertyName("file_size")] public long FileSize { get; init; }
    [JsonPropertyName("file_type")] public string FileType { get; init; } = "";
    [JsonPropertyName("views")] public int Views { get; init; }
    [JsonPropertyName("favorites")] public int Favorites { get; init; }
    [JsonPropertyName("thumbs")] public Thumbnails Thumbs { get; init; } = new();
    [JsonPropertyName("tags")] public List<WallpaperTag> Tags { get; init; } = [];
    public string LocalPath { get; init; } = "";
    [JsonIgnore] public bool IsLocal => LocalPath.Length > 0;
    [JsonIgnore] public string CategoryLabel => Category switch { "general" => "风景与创意", "anime" => "动漫", "people" => "人物", "local" => "本地图片", _ => Category };
    [JsonIgnore] public string SizeLabel => $"{FileSize / 1048576d:0.0} MB";
    [JsonIgnore] public string Title => IsLocal && !WallhavenClient.ValidId(Id) ? "当前桌面壁纸" : Tags.Count > 0 ? string.Join(" · ", Tags.Take(4).Select(t => t.Name)) : $"Wallhaven / {Id.ToUpperInvariant()}";
}
public sealed record Thumbnails
{
    [JsonPropertyName("large")] public string Large { get; init; } = "";
    [JsonPropertyName("small")] public string Small { get; init; } = "";
}
public sealed record WallpaperTag
{
    [JsonPropertyName("name")] public string Name { get; init; } = "";
}
public sealed record SearchOptions(string Query = "", string Sorting = "random", string Categories = "111", string AtLeast = "", string Ratios = "", string TopRange = "1M", string Purity = "100");
public sealed record SearchResult(List<Wallpaper> Wallpapers, int CurrentPage, int LastPage, int Total, string? Seed);
public sealed class AppSettings
{
    public string Query { get; set; } = "";
    public string Sorting { get; set; } = "random";
    public bool RandomSortingMigrated { get; set; }
    public string Categories { get; set; } = "111";
    public string AtLeast { get; set; } = "";
    public string Ratios { get; set; } = "";
    public string TopRange { get; set; } = "1M";
    public int IntervalMinutes { get; set; } = 60;
    public bool AutoChange { get; set; }
    public bool CloseToTray { get; set; } = true;
    public bool Fill { get; set; } = true;
    public string Theme { get; set; } = "system";
    public string Purity { get; set; } = "100";
    public string WallhavenUsername { get; set; } = "";
    public DateTimeOffset? NextChange { get; set; }
    public Wallpaper? LastWallpaper { get; set; }
    public Wallpaper? DesktopWallpaper { get; set; }
    public string DesktopFilePath { get; set; } = "";
    public List<DesktopRecord> DesktopHistory { get; set; } = [];
    public Wallpaper? NextDesktopWallpaper { get; set; }
    public SearchOptions? NextDesktopOptions { get; set; }
    public SearchOptions ToSearch() => new(Query, Sorting, Categories, AtLeast, Ratios, TopRange, Purity);
}
public sealed record DesktopRecord(Wallpaper Wallpaper, string FilePath);
public sealed record AccountPreferences
{
    [JsonPropertyName("purity")] public List<string> Purity { get; init; } = [];
    [JsonPropertyName("categories")] public List<string> Categories { get; init; } = [];
    [JsonPropertyName("aspect_ratios")] public List<string> AspectRatios { get; init; } = [];
    [JsonPropertyName("toplist_range")] public string TopRange { get; init; } = "1M";
}
public sealed record SiteCollection
{
    [JsonPropertyName("id")] public int Id { get; init; }
    [JsonPropertyName("label")] public string Label { get; init; } = "";
    [JsonPropertyName("count")] public int Count { get; init; }
    [JsonPropertyName("public")] public int Public { get; init; }
    [JsonIgnore] public string DisplayLabel => $"{Label} · {Count} 张{(Public == 0 ? " · 私有" : "")}";
}
