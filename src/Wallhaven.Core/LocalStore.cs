using System.Text.Json;

namespace Wallhaven.Core;

public sealed class LocalStore
{
    public static string DefaultRoot => AppContext.BaseDirectory;
    public string Root { get; }
    public string CacheDirectory => Path.Combine(Root, "cache");
    public string? LoadWarning { get; private set; }
    private bool migrationIncomplete;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public LocalStore(string? root = null, string? legacyRoot = null)
    {
        Root = Path.GetFullPath(root ?? DefaultRoot);
        Directory.CreateDirectory(Root);
        if (root == null || legacyRoot != null)
            ImportLegacyData(legacyRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WallhavenDesktop"));
        Directory.CreateDirectory(CacheDirectory);
    }
    private void ImportLegacyData(string legacyRoot)
    {
        if (string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(legacyRoot)), Path.TrimEndingDirectorySeparator(Root), StringComparison.OrdinalIgnoreCase)
            || !Directory.Exists(legacyRoot)
            || File.Exists(Path.Combine(Root, "settings.json")) && !File.Exists(Path.Combine(Root, ".legacy-importing"))
            || File.Exists(Path.Combine(Root, ".legacy-imported"))) return;
        try
        {
            File.WriteAllText(Path.Combine(Root, ".legacy-importing"), "Legacy data import in progress.");
            foreach (string name in new[] { "settings.json", "favorites.json", "api-key.dpapi", "errors.log", "desktop.bmp" })
                CopyIfMissing(Path.Combine(legacyRoot, name), Path.Combine(Root, name));
            foreach (string directory in new[] { "cache", "desktop" })
            {
                string source = Path.Combine(legacyRoot, directory);
                if (Directory.Exists(source)) CopyDirectory(source, Path.Combine(Root, directory));
            }
            File.WriteAllText(Path.Combine(Root, ".legacy-imported"), "Legacy data copied; original files retained.");
            File.Delete(Path.Combine(Root, ".legacy-importing"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            migrationIncomplete = true;
            LoadWarning = "旧数据未能完整复制，原文件仍保留，暂不保存设置和收藏。请检查空间与权限，修复后重启应用将继续迁移。";
        }
    }
    private static void CopyIfMissing(string source, string target)
    {
        if (!File.Exists(source) || File.Exists(target)) return;
        string temp = target + ".migration-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.Copy(source, temp);
            File.Move(temp, target, false);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    private static void CopyDirectory(string source, string target)
    {
        // Do not follow directory links outside the legacy data folder.
        if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0) return;
        Directory.CreateDirectory(target);
        foreach (string file in Directory.EnumerateFiles(source)) CopyIfMissing(file, Path.Combine(target, Path.GetFileName(file)));
        foreach (string directory in Directory.EnumerateDirectories(source)) CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
    }
    public AppSettings LoadSettings()
    {
        var value = Read("settings.json", new AppSettings());
        value.IntervalMinutes = Math.Clamp(value.IntervalMinutes, 1, 1440);
        value.Query ??= "";
        value.Sorting ??= "random";
        if (!value.RandomSortingMigrated)
        {
            value.Sorting = "random";
            value.RandomSortingMigrated = true;
        }
        value.Categories ??= "111";
        value.AtLeast ??= "";
        value.Ratios ??= "";
        value.TopRange ??= "1M";
        value.DesktopHistory ??= [];
        value.DesktopFilePath ??= "";
        return value;
    }
    public List<Wallpaper> LoadFavorites() => Read<List<Wallpaper>>("favorites.json", []).Where(w => w is not null && WallhavenClient.ValidId(w.Id)).DistinctBy(w => w.Id).ToList();
    public void SaveSettings(AppSettings settings) => Write("settings.json", settings);
    public void SaveFavorites(IEnumerable<Wallpaper> wallpapers) => Write("favorites.json", wallpapers.DistinctBy(w => w.Id).ToList());
    private T Read<T>(string name, T fallback)
    {
        string file = Path.Combine(Root, name);
        if (!File.Exists(file)) return fallback;
        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(file)) ?? fallback; }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            LoadWarning = "部分本地数据无法读取，已使用默认设置。";
            if (ex is JsonException)
            {
                try
                {
                    File.Copy(file, file + $".corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}.bak", true);
                    LoadWarning += "原始内容已备份在数据目录。";
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return fallback;
        }
    }
    private void Write<T>(string name, T value)
    {
        if (migrationIncomplete) throw new IOException("旧数据迁移未完成，暂不保存设置和收藏。修复目录权限或磁盘空间后请重启应用继续迁移。");
        string file = Path.Combine(Root, name), temp = file + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, JsonOptions));
        File.Move(temp, file, true);
    }
}
