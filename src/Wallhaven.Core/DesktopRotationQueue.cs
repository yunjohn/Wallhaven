namespace Wallhaven.Core;

public sealed class DesktopRotationQueue
{
    private readonly SemaphoreSlim gate = new(1);
    public async Task<Wallpaper> PrepareAsync(AppSettings settings, SearchOptions options,
        Func<SearchOptions, CancellationToken, Task<IReadOnlyList<Wallpaper>>> fetch,
        Func<Wallpaper, CancellationToken, Task> prepare, CancellationToken ct = default)
    {
        options = options with { Sorting = "random" };
        await gate.WaitAsync(ct);
        try
        {
            Wallpaper? candidate = settings.NextDesktopOptions == options ? settings.NextDesktopWallpaper : null;
            if (candidate == null || !WallhavenClient.ValidId(candidate.Id) || candidate.Id == settings.DesktopWallpaper?.Id)
            {
                var candidates = await fetch(options, ct);
                candidate = candidates.FirstOrDefault(w => w.Id != settings.DesktopWallpaper?.Id)
                    ?? throw new WallhavenException("没有找到不同于当前桌面的壁纸，请调整筛选条件。");
            }
            await prepare(candidate, ct);
            ct.ThrowIfCancellationRequested();
            settings.NextDesktopWallpaper = candidate;
            settings.NextDesktopOptions = options;
            return candidate;
        }
        finally { gate.Release(); }
    }
    public async Task<Wallpaper> ApplyNextAsync(AppSettings settings,
        Func<Wallpaper, CancellationToken, Task<string>> apply, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            var next = settings.NextDesktopWallpaper ?? throw new WallhavenException("下一张尚未准备好，请稍后重试。");
            if (settings.NextDesktopOptions != (settings.ToSearch() with { Sorting = "random" }))
                throw new WallhavenException("筛选条件已变化，请重新准备下一张壁纸。");
            string path = await apply(next, ct);
            Record(settings, next, path);
            settings.NextDesktopWallpaper = null; settings.NextDesktopOptions = null;
            return next;
        }
        finally { gate.Release(); }
    }
    public async Task ApplyManualAsync(AppSettings settings, Wallpaper wallpaper,
        Func<Wallpaper, CancellationToken, Task<string>> apply, CancellationToken ct = default)
    {
        await gate.WaitAsync(ct);
        try
        {
            string path = await apply(wallpaper, ct);
            Record(settings, wallpaper, path);
            if (settings.NextDesktopWallpaper?.Id == wallpaper.Id)
            { settings.NextDesktopWallpaper = null; settings.NextDesktopOptions = null; }
        }
        finally { gate.Release(); }
    }
    private static void Record(AppSettings settings, Wallpaper wallpaper, string path)
    {
        settings.DesktopWallpaper = wallpaper;
        settings.DesktopFilePath = path;
        settings.DesktopHistory.RemoveAll(record => string.Equals(record.FilePath, path, StringComparison.OrdinalIgnoreCase));
        settings.DesktopHistory.Insert(0, new(wallpaper, path));
        if (settings.DesktopHistory.Count > 50) settings.DesktopHistory.RemoveRange(50, settings.DesktopHistory.Count - 50);
    }
}
