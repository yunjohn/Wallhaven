using System.Net;
using System.Text;
using Wallhaven.Core;

int passed = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("PASS " + message); Interlocked.Increment(ref passed); }
async Task Throws<T>(Func<Task> action, string message) where T : Exception
{
    try { await action(); } catch (T) { Check(true, message); return; }
    throw new Exception("Expected " + typeof(T).Name + ": " + message);
}
if (OperatingSystem.IsWindows())
{
    // Exercise real registry operations in an isolated key, never the actual Run key.
    string startupTestKey = @"Software\WallhavenDesktop.Tests\" + Guid.NewGuid().ToString("N");
    string startupTestDirectory = Path.Combine(Path.GetTempPath(), "Wallhaven startup " + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(startupTestDirectory);
    string startupTestExe = Path.Combine(startupTestDirectory, "Wallhaven.exe");
    File.WriteAllText(startupTestExe, "test placeholder");
    try
    {
        var startup = new Wallhaven.Desktop.WindowsStartup(startupTestExe, startupTestKey);
        Check(!startup.IsEnabled(), "startup is disabled when registration is missing");
        startup.SetEnabled(false);
        startup.SetEnabled(true);
        using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(startupTestKey))
        {
            Check((string?)key?.GetValue("WallhavenDesktop") == $"\"{startupTestExe}\" --startup", "startup command quotes paths with spaces and requests tray launch");
        }
        Check(startup.IsEnabled(), "startup registration can be read back");
        startup.SetEnabled(true);
        Check(startup.IsEnabled(), "enabling startup repeatedly is idempotent");
        var moved = new Wallhaven.Desktop.WindowsStartup(Path.Combine(startupTestDirectory, "moved.exe"), startupTestKey);
        Check(!moved.IsEnabled(), "startup registration for an old executable path is not shown as enabled");
        await Throws<FileNotFoundException>(() => { if (OperatingSystem.IsWindows()) moved.SetEnabled(true); return Task.CompletedTask; }, "missing executable cannot replace startup registration");
        Check(startup.IsEnabled(), "failed registration preserves existing startup command");
        startup.SetEnabled(false);
        startup.SetEnabled(false);
        Check(!startup.IsEnabled(), "disabling startup removes registration and tolerates repeated calls");
    }
    finally
    {
        Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(startupTestKey, throwOnMissingSubKey: false);
        File.Delete(startupTestExe);
        Directory.Delete(startupTestDirectory);
    }
}
var options = new SearchOptions("mountains & sky #蓝", "random", "101", "3840x2160", "16x9");
var uri = WallhavenClient.BuildSearchUri(options, 2, "abc123");
Check(uri.Host == "wallhaven.cc" && uri.Query.Contains("purity=100") && uri.Query.Contains("seed=abc123") && uri.Query.Contains("page=2"), "search URL preserves filters, SFW, pagination and random seed");
Check(Uri.UnescapeDataString(uri.Query).Contains("mountains & sky #蓝"), "search safely encodes special and Unicode characters");
Check(!WallhavenClient.BuildSearchUri(options with { Sorting = "toplist" }, 1, "abc123").Query.Contains("seed="), "random seed does not leak into other sorts");
await Throws<ArgumentException>(() => Task.FromResult(WallhavenClient.BuildSearchUri(options with { Categories = "000" }, 1)), "empty categories rejected");
await Throws<WallhavenException>(() => Task.FromResult(ImageCache.ValidateImageUrl("https://wallhaven.cc.evil.test/p.jpg")), "lookalike image domains rejected");
await Throws<WallhavenException>(() => Task.FromResult(ImageCache.ValidateImageUrl("http://w.wallhaven.cc/p.jpg")), "insecure image URLs rejected");
Check(ImageCache.ValidateImageUrl("https://w.wallhaven.cc/full/ab/wallhaven-abc123.jpg").Host == "w.wallhaven.cc", "official image subdomain accepted");
var primaryMonitor = new PixelRect(0, 0, 1920, 1080);
var bottomTaskbarWork = new PixelRect(0, 0, 1920, 1032);
Check(WindowGeometry.Maximize(primaryMonitor, bottomTaskbarWork, false) == new MaximizedPlacement(0, 0, 1920, 1032), "maximized window reserves bottom taskbar without invisible border overhang");
Check(WindowGeometry.Maximize(primaryMonitor, new PixelRect(0, 48, 1920, 1080), false) == new MaximizedPlacement(0, 48, 1920, 1032), "top taskbar produces a monitor-relative maximum Y offset");
Check(WindowGeometry.Maximize(new PixelRect(-2560, -1440, 0, 0), new PixelRect(-2520, -1440, 0, -48), false) == new MaximizedPlacement(40, 0, 2520, 1392), "negative-coordinate secondary monitor and left taskbar maximize correctly");
Check(WindowGeometry.Maximize(primaryMonitor, bottomTaskbarWork, true) == new MaximizedPlacement(0, 0, 1920, 1080), "fullscreen uses full monitor rather than work area");
foreach (var dimensions in new[] { (Dpi: 96u, Width: 1920d, Height: 1032d), (Dpi: 120u, Width: 1536d, Height: 825.6), (Dpi: 144u, Width: 1280d, Height: 688d), (Dpi: 192u, Width: 960d, Height: 516d) })
{
    var logical = WindowGeometry.LogicalSize(bottomTaskbarWork, dimensions.Dpi);
    Check(Math.Abs(logical.Width - dimensions.Width) < 0.001 && Math.Abs(logical.Height - dimensions.Height) < 0.001 && WindowGeometry.Maximize(primaryMonitor, bottomTaskbarWork, false).Width == 1920,
        $"DPI {dimensions.Dpi}: WPF logical sizing converts once while maximize keeps physical pixels");
}
Check(WindowGeometry.FitNormal(new PixelRect(-200, -80, 2200, 1500), bottomTaskbarWork) == bottomTaskbarWork, "oversized initial or restored window fits the visible work area");
Check(WindowGeometry.FitNormal(new PixelRect(-3000, -200, -1000, 1700), new PixelRect(-2520, 0, 0, 1400)) == new PixelRect(-2520, 0, -520, 1400), "normal bounds clamp correctly on a negative-coordinate monitor");
Check(WindowGeometry.Target(primaryMonitor, new PixelRect(0, 0, 0, 0), false) == primaryMonitor, "invalid work area safely falls back to monitor bounds");
var clock = DateTimeOffset.Parse("2026-10-26T12:00:00+08:00");
var schedule = new AutoWallpaperSchedule(() => clock);
var scheduleSettings = new AppSettings { AutoChange = false, IntervalMinutes = 30, NextChange = clock.AddMinutes(-1) };
int changes = 0;
Task Change(CancellationToken ct) { changes++; return Task.CompletedTask; }
Check((await schedule.TickAsync(scheduleSettings, Change)).State == ScheduleState.NotDue && changes == 0, "disabled scheduler never changes wallpaper");
scheduleSettings.AutoChange = true; scheduleSettings.NextChange = clock.AddMinutes(1);
Check((await schedule.TickAsync(scheduleSettings, Change)).State == ScheduleState.NotDue, "scheduler waits until due time");
scheduleSettings.NextChange = clock.AddMinutes(-1);
Check((await schedule.TickAsync(scheduleSettings, Change)).State == ScheduleState.Changed && changes == 1 && scheduleSettings.NextChange == clock.AddMinutes(30), "successful scheduled change advances next run");
scheduleSettings.NextChange = clock.AddMinutes(-1);
var failedSchedule = await schedule.TickAsync(scheduleSettings, _ => throw new IOException("offline"));
Check(failedSchedule.State == ScheduleState.Failed && failedSchedule.Error is IOException && scheduleSettings.NextChange == clock.AddMinutes(5), "failed scheduled change retries after five minutes");
scheduleSettings.NextChange = clock.AddMinutes(-1);
var entered = new TaskCompletionSource(); var release = new TaskCompletionSource();
var running = schedule.TickAsync(scheduleSettings, async _ => { entered.SetResult(); await release.Task; });
await entered.Task;
Check((await schedule.TickAsync(scheduleSettings, Change)).State == ScheduleState.NotDue, "overlapping scheduler ticks cannot apply duplicate wallpapers");
release.SetResult(); await running;
var wallpaper = new Wallpaper { Id = "abc123", Path = "https://w.wallhaven.cc/full/ab/wallhaven-abc123.png", Purity = "sfw", Resolution = "1920x1080", Thumbs = new() { Large = "https://th.wallhaven.cc/lg/ab/abc123.jpg" } };
string resultJson = System.Text.Json.JsonSerializer.Serialize(new { data = new[] { wallpaper }, meta = new { current_page = 2, last_page = 4, total = 73, seed = "abc123" } });
var nextCandidate = wallpaper with { Id = "def456" };
var thirdCandidate = wallpaper with { Id = "ghi789" };
var rotationSettings = new AppSettings { DesktopWallpaper = wallpaper };
var rotation = new DesktopRotationQueue();
int rotationFetches = 0, rotationPrepares = 0;
Task<IReadOnlyList<Wallpaper>> FetchQueue(SearchOptions search, CancellationToken ct)
{
    Check(search.Sorting == "random", "rotation always requests random results with current filters");
    rotationFetches++;
    return Task.FromResult<IReadOnlyList<Wallpaper>>([wallpaper, nextCandidate, thirdCandidate]);
}
Task PrepareQueue(Wallpaper item, CancellationToken ct) { rotationPrepares++; return Task.CompletedTask; }
var prepared = await rotation.PrepareAsync(rotationSettings, rotationSettings.ToSearch(), FetchQueue, PrepareQueue);
Check(prepared.Id == nextCandidate.Id && rotationSettings.NextDesktopWallpaper?.Id == nextCandidate.Id, "next queue excludes actual desktop rather than application preview");
await rotation.PrepareAsync(rotationSettings, rotationSettings.ToSearch(), FetchQueue, PrepareQueue);
Check(rotationFetches == 1 && rotationPrepares == 2, "repeated next preview retains the same queued wallpaper");
string queuedJson = System.Text.Json.JsonSerializer.Serialize(rotationSettings);
var restoredQueue = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(queuedJson)!;
await new DesktopRotationQueue().PrepareAsync(restoredQueue, restoredQueue.ToSearch(), FetchQueue, PrepareQueue);
Check(rotationFetches == 1 && restoredQueue.NextDesktopWallpaper?.Id == nextCandidate.Id, "queued next wallpaper survives application restart");
await Throws<IOException>(() => rotation.ApplyNextAsync(rotationSettings, (_, _) => throw new IOException("busy")), "failed desktop apply reports failure");
Check(rotationSettings.NextDesktopWallpaper?.Id == nextCandidate.Id && rotationSettings.DesktopWallpaper?.Id == wallpaper.Id, "failed apply preserves next queue and actual desktop record");
string? appliedId = null;
var appliedWallpaper = await rotation.ApplyNextAsync(rotationSettings, (item, _) => { appliedId = item.Id; return Task.FromResult("prepared-desktop.bmp"); });
Check(appliedId == prepared.Id && appliedWallpaper.Id == prepared.Id && rotationSettings.DesktopWallpaper?.Id == prepared.Id && rotationSettings.NextDesktopWallpaper == null, "automatic switch applies exactly the wallpaper shown in next preview");
Check(rotationSettings.DesktopHistory.Single().Wallpaper.Id == prepared.Id, "applied desktop identity and file path persist separately from preview");
await rotation.PrepareAsync(rotationSettings, rotationSettings.ToSearch(), FetchQueue, PrepareQueue);
rotationSettings.Query = "new filter";
await Throws<WallhavenException>(() => rotation.ApplyNextAsync(rotationSettings, (_, _) => Task.FromResult("unused")), "changed filters cannot accidentally apply an old queued wallpaper");
await rotation.PrepareAsync(rotationSettings, rotationSettings.ToSearch(), FetchQueue, PrepareQueue);
Check(rotationSettings.NextDesktopOptions?.Query == "new filter", "changing filters rebuilds the queued next wallpaper");
var manualId = rotationSettings.NextDesktopWallpaper!.Id;
await rotation.ApplyManualAsync(rotationSettings, rotationSettings.NextDesktopWallpaper!, (_, _) => Task.FromResult("manual.bmp"));
Check(rotationSettings.DesktopWallpaper?.Id == manualId && rotationSettings.NextDesktopWallpaper == null, "manually applying queued image consumes it and records actual desktop");
using (var http = new HttpClient(new FakeHandler(_ => new(HttpStatusCode.OK) { Content = new StringContent(resultJson) })))
using (var api = new WallhavenClient(http, TimeSpan.Zero))
{
    var result = await api.SearchAsync(options, 2);
    Check(result.CurrentPage == 2 && result.LastPage == 4 && result.Total == 73 && result.Seed == "abc123" && result.Wallpapers.Single().Id == "abc123", "API response maps pagination and wallpaper metadata");
}
using (var http = new HttpClient(new FakeHandler(_ => new(HttpStatusCode.TooManyRequests))))
using (var api = new WallhavenClient(http, TimeSpan.Zero))
    await Throws<WallhavenException>(() => api.SearchAsync(options, 1), "rate limit yields actionable error");
using (var http = new HttpClient(new FakeHandler(_ => new(HttpStatusCode.OK) { Content = new StringContent("<html>challenge</html>") })))
using (var api = new WallhavenClient(http, TimeSpan.Zero))
    await Throws<WallhavenException>(() => api.SearchAsync(options, 1), "non-JSON server responses handled");
const string fakeKey = "testkey0123456789testkey0123456789";
bool authorizedHeader = false, keyInUrl = false, escapedUsername = false;
using (var http = new HttpClient(new FakeHandler(request =>
{
    authorizedHeader = request.Headers.TryGetValues("X-API-Key", out var headers) && headers.Single() == fakeKey;
    keyInUrl |= request.RequestUri!.ToString().Contains(fakeKey);
    string path = request.RequestUri!.AbsolutePath;
    string json;
    if (path.EndsWith("/settings")) json = """{"data":{"purity":["sfw","nsfw"],"categories":["anime"],"aspect_ratios":["16x9"],"toplist_range":"6M"}}""";
    else if (path == "/api/v1/collections") json = """{"data":[{"id":17,"label":"My collection","count":32,"public":0}]}""";
    else
    {
        escapedUsername = request.RequestUri.OriginalString.Contains("Alice%20A");
        json = System.Text.Json.JsonSerializer.Serialize(new { data = new[] { wallpaper, wallpaper with { Id = "def456", Purity = "nsfw" } }, meta = new { current_page = 2, last_page = 3, total = 32 } });
    }
    return new(HttpStatusCode.OK) { Content = new StringContent(json) };
})))
using (var api = new WallhavenClient(http, TimeSpan.Zero))
{
    await Throws<WallhavenException>(() => api.GetSettingsAsync(), "account preferences require a key before any request");
    await Throws<WallhavenException>(() => api.SearchAsync(options with { Purity = "111" }, 1), "NSFW search requires a key before any request");
    api.SetApiKey(fakeKey);
    var preferences = await api.GetSettingsAsync();
    Check(authorizedHeader && !keyInUrl && preferences.Categories.Single() == "anime" && preferences.TopRange == "6M", "API key uses request header and account preferences parse correctly");
    var collections = await api.GetCollectionsAsync();
    Check(collections.Single().Id == 17 && collections[0].Public == 0 && authorizedHeader, "own private collection metadata parses with authentication");
    var collection = await api.GetCollectionAsync("Alice A", 17, "111", 2);
    Check(escapedUsername && collection.Wallpapers.Count == 2 && collection.CurrentPage == 2, "collection request encodes username, keeps authorized content and pagination");
    Check((await api.GetCollectionAsync("Alice A", 17, "100", 2)).Wallpapers.All(w => w.Purity == "sfw"), "collection results respect selected content mask");
    Check(!api.Redact("prefix " + fakeKey).Contains(fakeKey), "API key redacted from diagnostic text");
    api.SetApiKey(null);
    Check(!api.HasApiKey, "removing a key restores anonymous mode");
    await Throws<ArgumentException>(() => Task.Run(() => api.SetApiKey("invalid\r\nheader")), "invalid key format cannot inject headers");
}
using (var http = new HttpClient(new FakeHandler(_ => new(HttpStatusCode.Unauthorized))))
using (var api = new WallhavenClient(http, TimeSpan.Zero))
{
    api.SetApiKey(fakeKey);
    await Throws<WallhavenException>(() => api.GetSettingsAsync(), "invalid remote key returns actionable authorization error");
}
string testRoot = Path.Combine(Path.GetTempPath(), "WallhavenTests-" + Guid.NewGuid().ToString("N"));
var randomQueries = new List<string>();
using (var randomHttp = new HttpClient(new FakeHandler(request =>
{
    randomQueries.Add(request.RequestUri!.Query);
    return new(HttpStatusCode.OK) { Content = new StringContent("{\"data\":[],\"meta\":{\"current_page\":1,\"last_page\":3,\"total\":72}}") };
})))
using (var randomApi = new WallhavenClient(randomHttp, TimeSpan.Zero))
{
    var firstRandom = await randomApi.SearchAsync(new SearchOptions(), 1);
    var secondRandom = await randomApi.SearchAsync(new SearchOptions(), 1);
    await randomApi.SearchAsync(new SearchOptions(), 2, firstRandom.Seed);
    Check(firstRandom.Seed?.Length == 6 && secondRandom.Seed?.Length == 6 && firstRandom.Seed != secondRandom.Seed && randomQueries[0] != randomQueries[1], "fresh random searches send different seeds and retain seed without server metadata");
    Check(randomQueries[2].Contains("seed=" + firstRandom.Seed) && randomQueries[2].Contains("page=2"), "random pagination reuses the first page seed");
}
try
{
    Check(Path.TrimEndingDirectorySeparator(LocalStore.DefaultRoot) == Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory), "default data root is the application directory rather than the working directory");
    string legacyRoot = Path.Combine(testRoot, "legacy"), portableRoot = Path.Combine(testRoot, "portable");
    var legacyStore = new LocalStore(legacyRoot);
    legacyStore.SaveSettings(new AppSettings { Query = "legacy", AutoChange = true });
    legacyStore.SaveFavorites([wallpaper]);
    File.WriteAllText(Path.Combine(legacyStore.CacheDirectory, "cached.jpg"), "cached wallpaper");
    Directory.CreateDirectory(Path.Combine(legacyRoot, "desktop"));
    File.WriteAllText(Path.Combine(legacyRoot, "desktop", "wallpaper.bmp"), "desktop wallpaper");
    File.WriteAllBytes(Path.Combine(legacyRoot, "api-key.dpapi"), [1, 2, 3]);
    var portableStore = new LocalStore(portableRoot, legacyRoot);
    Check(portableStore.LoadSettings().Query == "legacy" && portableStore.LoadSettings().AutoChange && portableStore.LoadFavorites().Count == 1, "first portable launch imports legacy settings and favorites");
    Check(File.ReadAllText(Path.Combine(portableStore.CacheDirectory, "cached.jpg")) == "cached wallpaper" && File.Exists(Path.Combine(portableRoot, "desktop", "wallpaper.bmp")), "legacy cached originals and prepared desktop images copy into the program data directory");
    Check(File.ReadAllBytes(Path.Combine(portableRoot, "api-key.dpapi")).SequenceEqual(new byte[] { 1, 2, 3 }), "legacy encrypted credentials copy without transformation");
    Check(File.Exists(Path.Combine(legacyRoot, "settings.json")) && File.Exists(Path.Combine(legacyStore.CacheDirectory, "cached.jpg")), "migration preserves original data and active desktop files");
    portableStore.SaveSettings(new AppSettings { Query = "portable" });
    Check(new LocalStore(portableRoot, legacyRoot).LoadSettings().Query == "portable", "existing program directory settings take precedence over legacy data");
    string partialRoot = Path.Combine(testRoot, "partial-import");
    Directory.CreateDirectory(Path.Combine(partialRoot, "cache"));
    File.WriteAllText(Path.Combine(partialRoot, "cache", "cached.jpg"), "existing image");
    File.WriteAllText(Path.Combine(partialRoot, "favorites.json"), "[]");
    var partialStore = new LocalStore(partialRoot, legacyRoot);
    Check(partialStore.LoadSettings().Query == "legacy" && partialStore.LoadFavorites().Count == 0 && File.ReadAllText(Path.Combine(partialStore.CacheDirectory, "cached.jpg")) == "existing image", "resumed migration retains existing files while importing missing data");
    string isolatedRoot = Path.Combine(testRoot, "diagnostics-isolated");
    Check(new LocalStore(isolatedRoot).LoadSettings().Query == "" && !File.Exists(Path.Combine(isolatedRoot, "api-key.dpapi")), "explicit data directories remain isolated from legacy user data");
    Check(new LocalStore(legacyRoot, legacyRoot).LoadSettings().Query == "legacy", "migration safely skips the same source and destination");
    string failedImportRoot = Path.Combine(testRoot, "failed-import");
    Directory.CreateDirectory(Path.Combine(failedImportRoot, "cache", "cached.jpg"));
    var failedImport = new LocalStore(failedImportRoot, legacyRoot);
    Check(failedImport.LoadWarning != null && File.Exists(Path.Combine(failedImportRoot, ".legacy-importing")), "failed migration retains a pending marker even after settings have been copied");
    await Throws<IOException>(() => { failedImport.SaveSettings(new AppSettings()); return Task.CompletedTask; }, "failed migration blocks automatic settings writes");
    await Throws<IOException>(() => { failedImport.SaveFavorites([]); return Task.CompletedTask; }, "failed migration blocks favorite writes");
    Directory.Delete(Path.Combine(failedImportRoot, "cache", "cached.jpg"));
    var resumedImport = new LocalStore(failedImportRoot, legacyRoot);
    Check(resumedImport.LoadWarning == null && resumedImport.LoadSettings().Query == "legacy" && File.Exists(Path.Combine(resumedImport.CacheDirectory, "cached.jpg")), "restart resumes failed migration despite an existing settings file");
    Check(!File.Exists(Path.Combine(failedImportRoot, ".legacy-importing")) && File.Exists(Path.Combine(failedImportRoot, ".legacy-imported")) && !Directory.EnumerateFiles(failedImportRoot, "*.tmp", SearchOption.AllDirectories).Any(), "successful migration clears pending state and temporary files");
    resumedImport.SaveSettings(new AppSettings { Query = "after recovery" });
    Check(new LocalStore(failedImportRoot, legacyRoot).LoadSettings().Query == "after recovery", "settings writes resume after migration recovery");
    var store = new LocalStore(testRoot);
    store.SaveSettings(new AppSettings { Sorting = "toplist" });
    var migrated = store.LoadSettings();
    Check(migrated.Sorting == "random" && migrated.RandomSortingMigrated, "old toplist settings migrate to random");
    migrated.Sorting = "date_added";
    store.SaveSettings(migrated);
    Check(store.LoadSettings().Sorting == "date_added", "migration preserves later explicit sorting choices");
    var settings = new AppSettings { Query = "nature", AutoChange = true, IntervalMinutes = 30, LastWallpaper = wallpaper };
    store.SaveSettings(settings);
    var loaded = new LocalStore(testRoot).LoadSettings();
    Check(loaded.Query == "nature" && loaded.AutoChange && loaded.IntervalMinutes == 30 && loaded.LastWallpaper?.Id == wallpaper.Id, "settings and restore wallpaper persist across store instances");
    store.SaveFavorites([wallpaper, wallpaper]);
    Check(new LocalStore(testRoot).LoadFavorites().Count == 1, "favorites persist without duplicates");
    File.WriteAllText(Path.Combine(testRoot, "favorites.json"), "[null,{\"id\":null}]");
    Check(store.LoadFavorites().Count == 0, "invalid favorite records cannot crash startup");
    File.WriteAllText(Path.Combine(testRoot, "settings.json"), "broken");
    Check(store.LoadSettings().Sorting == "random" && store.LoadWarning != null && File.ReadAllText(Path.Combine(testRoot, "settings.json")) == "broken", "corrupt settings preserved with fallback and warning");
    int requests = 0;
    byte[] png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jK1kAAAAASUVORK5CYII=");
    using var http = new HttpClient(new FakeHandler(_ => { requests++; return new(HttpStatusCode.OK) { Content = new ByteArrayContent(png) }; }));
    var cache = new ImageCache(http, store.CacheDirectory);
    var path = await cache.GetAsync(wallpaper);
    Check(File.ReadAllBytes(path).SequenceEqual(png), "image download retains original bytes");
    await cache.GetAsync(wallpaper);
    Check(requests == 1, "cached originals avoid repeated network requests");
    File.WriteAllText(path, "corrupt cached wallpaper");
    await cache.GetAsync(wallpaper);
    Check(requests == 2 && File.ReadAllBytes(path).SequenceEqual(png), "corrupt existing cache is replaced with a fresh original");
    int validatedDownloads = 0, validationCalls = 0, committedFiles = 0;
    using var validatedHttp = new HttpClient(new FakeHandler(_ => { validatedDownloads++; return new(HttpStatusCode.OK) { Content = new ByteArrayContent(png) }; }));
    var validatedCache = new ImageCache(validatedHttp, Path.Combine(testRoot, "decode-cache"), validateImage: file =>
    {
        validationCalls++;
        if (!File.ReadAllBytes(file).SequenceEqual(png)) throw new InvalidDataException("decoder rejected truncated pixels");
    }, fileCommitted: (temporary, final) => { if (!File.Exists(temporary) && File.Exists(final)) committedFiles++; });
    Directory.CreateDirectory(Path.Combine(testRoot, "decode-cache"));
    File.WriteAllBytes(validatedCache.GetPath(wallpaper), png[..16]);
    await validatedCache.GetAsync(wallpaper);
    Check(validatedDownloads == 1 && File.ReadAllBytes(validatedCache.GetPath(wallpaper)).SequenceEqual(png), "decoder validation recovers cached data with a valid header but truncated pixels");
    int knownValidationCalls = validationCalls;
    await validatedCache.GetAsync(wallpaper);
    Check(validationCalls == knownValidationCalls && validatedDownloads == 1, "unchanged committed cache reuses validation instead of decoding again");
    Check(committedFiles == 1, "commit callback can transfer warmed pixels after the temporary file is renamed");
    File.SetLastWriteTimeUtc(validatedCache.GetPath(wallpaper), File.GetLastWriteTimeUtc(validatedCache.GetPath(wallpaper)).AddSeconds(2));
    await validatedCache.GetAsync(wallpaper);
    Check(validationCalls == knownValidationCalls + 1 && validatedDownloads == 1, "modified cache is revalidated without an unnecessary network request");
    File.WriteAllBytes(validatedCache.GetPath(wallpaper), new byte[png.Length]);
    await validatedCache.GetAsync(wallpaper);
    Check(validatedDownloads == 2 && File.ReadAllBytes(validatedCache.GetPath(wallpaper)).SequenceEqual(png), "same-length corruption invalidates validation metadata and redownloads");
    using var truncatedHttp = new HttpClient(new FakeHandler(_ => new(HttpStatusCode.OK) { Content = new ByteArrayContent(png[..16]) }));
    var truncatedCache = new ImageCache(truncatedHttp, Path.Combine(testRoot, "truncated"), validateImage: file => { if (new FileInfo(file).Length < png.Length) throw new InvalidDataException(); });
    await Throws<WallhavenException>(() => truncatedCache.GetAsync(wallpaper), "decoder rejects corrupt downloads before committing to cache");
    Check(!Directory.EnumerateFiles(Path.Combine(testRoot, "truncated")).Any(), "corrupt downloads leave no final or partial files");
    int stalledRequests = 0;
    using var stalledHttp = new HttpClient(new FakeHandler(_ => new(HttpStatusCode.OK)
    {
        Content = Interlocked.Increment(ref stalledRequests) <= 3
            ? new StreamContent(new SlowReadStream(png, Timeout.InfiniteTimeSpan)) : new ByteArrayContent(png)
    }));
    var stalledCache = new ImageCache(stalledHttp, Path.Combine(testRoot, "stalled"), downloadTimeout: TimeSpan.FromSeconds(3), idleTimeout: TimeSpan.FromMilliseconds(100));
    await Task.WhenAll(new[] { "abc123", "def456", "ghi789" }.Select(id => Throws<WallhavenException>(() => stalledCache.GetAsync(wallpaper with { Id = id }), "stalled response body times out: " + id))).WaitAsync(TimeSpan.FromSeconds(5));
    Check(!Directory.EnumerateFiles(Path.Combine(testRoot, "stalled")).Any(), "stalled downloads clean up partial files");
    await stalledCache.GetAsync(wallpaper).WaitAsync(TimeSpan.FromSeconds(5));
    Check(File.ReadAllBytes(stalledCache.GetPath(wallpaper)).SequenceEqual(png), "all download slots are released after timeouts");
    using var trickleHttp = new HttpClient(new FakeHandler(_ => new(HttpStatusCode.OK) { Content = new StreamContent(new SlowReadStream(png, TimeSpan.FromMilliseconds(20))) }));
    var trickleCache = new ImageCache(trickleHttp, Path.Combine(testRoot, "trickle"), downloadTimeout: TimeSpan.FromMilliseconds(120), idleTimeout: TimeSpan.FromSeconds(2));
    await Throws<WallhavenException>(() => trickleCache.GetAsync(wallpaper).WaitAsync(TimeSpan.FromSeconds(5)), "total transfer deadline stops endless trickling despite ongoing data");
    using var cancelDownload = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
    await Throws<OperationCanceledException>(() => trickleCache.GetAsync(wallpaper, ct: cancelDownload.Token), "caller cancellation remains distinct from download timeout");
    await Throws<ArgumentException>(() => cache.GetAsync(wallpaper with { Id = "../bad" }), "path traversal IDs rejected");
    using var badHttp = new HttpClient(new FakeHandler(_ => new(HttpStatusCode.OK) { Content = new StringContent("this is not an image") }));
    var badCache = new ImageCache(badHttp, Path.Combine(testRoot, "bad"));
    await Throws<WallhavenException>(() => badCache.GetAsync(wallpaper), "HTML or other invalid image payload rejected");
    Check(!Directory.EnumerateFiles(Path.Combine(testRoot, "bad")).Any(), "failed downloads leave no partial cache file");
    using var cts = new CancellationTokenSource(); cts.Cancel();
    await Throws<OperationCanceledException>(() => cache.GetAsync(wallpaper, ct: cts.Token), "download cancellation honored even with cached files");
    if (args.Contains("--live"))
    {
        using var liveHttp = NetworkClient.Create();
        using var api = new WallhavenClient(liveHttp);
        var result = await api.SearchAsync(new SearchOptions(), 1);
        Check(result.Wallpapers.Count > 0 && result.Total > 0, "LIVE Wallhaven search returns wallpapers");
        var freshResult = await api.SearchAsync(new SearchOptions(), 1);
        Check(result.Seed != freshResult.Seed && !result.Wallpapers.Select(w => w.Id).SequenceEqual(freshResult.Wallpapers.Select(w => w.Id)), "LIVE fresh random search returns a different seed and wallpaper list");
        var first = result.Wallpapers[0];
        var details = await api.GetWallpaperAsync(first.Id);
        Check(details.Id == first.Id && details.Tags.Count > 0, "LIVE wallpaper detail endpoint returns tags");
        var images = new ImageCache(liveHttp, store.CacheDirectory);
        var thumb = await images.GetAsync(first, true);
        var original = await images.GetAsync(first);
        Check(new FileInfo(thumb).Length > 100 && new FileInfo(original).Length > 1000, "LIVE thumbnail and full original downloaded");
        Console.WriteLine($"LIVE id={first.Id} resolution={first.Resolution} originalBytes={new FileInfo(original).Length}");
    }
}
finally
{
    string resolved = Path.GetFullPath(testRoot);
    string tempPrefix = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    if (!resolved.StartsWith(tempPrefix, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(resolved).StartsWith("WallhavenTests-", StringComparison.Ordinal))
        throw new InvalidOperationException("Refusing to clean an unexpected test path.");
    if (Directory.Exists(resolved)) Directory.Delete(resolved, true);
}
Console.WriteLine($"All {passed} checks passed.");

sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(handler(request));
}

sealed class SlowReadStream(byte[] bytes, TimeSpan delay) : Stream
{
    private int position;
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => position; set => throw new NotSupportedException(); }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await Task.Delay(delay, cancellationToken);
        if (position >= bytes.Length) return 0;
        buffer.Span[0] = bytes[position++];
        return 1;
    }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override void Flush() { }
}
