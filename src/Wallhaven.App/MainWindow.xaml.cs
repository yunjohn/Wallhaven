using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Wallhaven.Core;
using Forms = System.Windows.Forms;

namespace Wallhaven.Desktop;

public sealed class WallpaperItem(Wallpaper wallpaper, bool favorite) : INotifyPropertyChanged
{
    public Wallpaper Wallpaper { get; } = wallpaper;
    private ImageSource? thumbnail;
    private string error = "";
    private bool isFavorite = favorite;
    private bool isCurrent;
    private bool isDesktop;
    private bool isNextDesktop;
    public ImageSource? Thumbnail { get => thumbnail; set { if (ReferenceEquals(thumbnail, value)) return; thumbnail = value; Changed(); } }
    public string ThumbnailError { get => error; set { if (error == value) return; error = value; Changed(); } }
    public string FavoriteMark => isFavorite ? "♥" : "";
    public bool IsFavorite { get => isFavorite; set { if (isFavorite == value) return; isFavorite = value; Changed(nameof(FavoriteMark)); } }
    public bool IsCurrent { get => isCurrent; set { if (isCurrent == value) return; isCurrent = value; Changed(); MarkerChanged(); } }
    public bool IsDesktop { get => isDesktop; set { if (isDesktop == value) return; isDesktop = value; MarkerChanged(); } }
    public bool IsNextDesktop { get => isNextDesktop; set { if (isNextDesktop == value) return; isNextDesktop = value; MarkerChanged(); } }
    public bool IsMarked => IsCurrent || IsDesktop || IsNextDesktop;
    public string MarkerText => IsDesktop ? "桌面使用中" : IsNextDesktop ? "下次自动切换" : IsCurrent ? "当前预览" : "";
    private void MarkerChanged() { Changed(nameof(IsMarked)); Changed(nameof(MarkerText)); }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly LocalStore store;
    private readonly HttpClient http = NetworkClient.Create();
    private readonly WallhavenClient client;
    private readonly ImageCache cache;
    private readonly List<Wallpaper> favorites;
    private readonly DispatcherTimer scheduler = new() { Interval = TimeSpan.FromSeconds(20) };
    private readonly DispatcherTimer statusTimer = new() { Interval = TimeSpan.FromSeconds(7) };
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? searchCts, imageCts, thumbsCts, preloadCts;
    private Forms.NotifyIcon? tray;
    private bool searchBusy, imageBusy, actionBusy, autoBusy, exiting, favoriteView, fullscreen;
    private bool trayHintShown, initialized;
    private int page, lastPage, total, selectedIndex = -1;
    private string? seed, originalPath;
    private readonly BitmapMemoryCache bitmapMemory = new();
    private Func<string, int, Task<BitmapImage>> previewLoader;
    private readonly DispatcherTimer previewSaveTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
    private readonly DispatcherTimer thumbnailViewportTimer = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private bool thumbnailsLoading, thumbnailsRefreshRequested;
    private readonly Dictionary<string, object?> publishedValues = new();
    private Wallpaper? markedDesktop, markedNext;
    private bool markersDirty = true;
    private readonly TranslateTransform drawerTranslation = new();
    private bool drawerClosing;
    private int drawerAnimation;
    private string status = "", busyText = "正在连接 Wallhaven…", searchText = "";
    private SearchOptions activeSearch = new();
    private Wallpaper? current;
    private WindowState beforeFullscreen;
    private readonly NativeWindowBounds windowBounds;
    private readonly SemaphoreSlim desktopGate = new(1);
    private readonly string? diagnosticsDirectory;
    private readonly AutoWallpaperSchedule autoSchedule = new();
    private readonly DesktopRotationQueue rotation = new();
    private readonly SemaphoreSlim nextPreparationGate = new(1);
    private readonly SemaphoreSlim desktopIdentityGate = new(1);
    private bool nextBusy, nextReady;
    private ImageSource? desktopThumbnail, nextDesktopThumbnail;
    private string desktopStatus = "尚未读取当前桌面", nextDesktopStatus = "尚未准备下一张";
    private string observedDesktopPath = "";
    private DateTime observedDesktopStamp;
    public ImageSource? DesktopThumbnail { get => desktopThumbnail; private set { desktopThumbnail = value; Changed(); } }
    public ImageSource? NextDesktopThumbnail { get => nextDesktopThumbnail; private set { nextDesktopThumbnail = value; Changed(); } }
    public string DesktopDescription => Settings.DesktopWallpaper?.Title ?? desktopStatus;
    public string NextDesktopDescription => NextDesktopReady ? Settings.NextDesktopWallpaper!.Title : nextDesktopStatus;
    public string NextDesktopDetails => NextDesktopReady ? $"{Settings.NextDesktopWallpaper!.Resolution} · {Settings.NextDesktopWallpaper.Id}" : "提前准备完成后，这张图片就是实际切换的下一张。";
    public bool NextDesktopReady => nextReady && Settings.NextDesktopWallpaper != null && Settings.NextDesktopOptions == (Settings.ToSearch() with { Sorting = "random" }) && Settings.NextDesktopWallpaper.Id != Settings.DesktopWallpaper?.Id;
    public bool CanSwitchDesktop => !actionBusy && !autoBusy && !nextBusy;
    private readonly ApiKeyVault keyVault;
    private string activeApiKey = "", apiStatus = "未连接 · 可浏览公开壁纸";
    private bool apiBusy;
    private SiteCollection? activeCollection;
    private string collectionUsername = "", collectionPurity = "100";
    private DateTimeOffset lastScrollLoad = DateTimeOffset.MinValue;
    private bool locatingCurrent;
    private bool scrollLoadArmed, scrollLoadInProgress;
    public ObservableCollection<WallpaperItem> PinnedItems { get; } = [];
    public bool HasPinnedWallpaper => PinnedItems.Count > 0;
    public ObservableCollection<SiteCollection> SiteCollections { get; } = [];
    public string ApiStatus { get => apiStatus; private set { apiStatus = value; Changed(); } }
    public bool CanUseApi => !apiBusy && !searchBusy;
    public AppSettings Settings { get; }
    public ObservableCollection<WallpaperItem> Items { get; } = [];
    public Wallpaper? Current
    {
        get => current;
        private set
        {
            if (ReferenceEquals(current, value)) return;
            if (current == null || value == null || !SameWallpaper(current, value)) { PinnedItems.Clear(); Changed(nameof(HasPinnedWallpaper)); }
            current = value;
            foreach (var item in Items) item.IsCurrent = item.Wallpaper.Id == current?.Id;
            Changed(); Refresh();
        }
    }
    public string SearchText { get => searchText; set { searchText = value; Changed(); } }
    public string Status { get => status; private set { status = value; Changed(); Changed(nameof(HasStatus)); } }
    public bool HasStatus => Status.Length > 0;
    public bool HasWallpaper => Current != null;
    public bool ShowWelcome => !HasWallpaper;
    public bool IsBusy => searchBusy || imageBusy || actionBusy;
    public bool CanSearch => !searchBusy && !apiBusy;
    public bool CanAct => HasWallpaper && !imageBusy && !actionBusy && originalPath != null;
    public bool CanLoadMore => !favoriteView && !searchBusy && !apiBusy && page < lastPage;
    public string LoadMoreLabel => favoriteView ? "本地收藏" : page < lastPage ? "加载更多壁纸" : "已显示全部结果";
    public string BusyText { get => busyText; private set { busyText = value; Changed(); } }
    public string FavoriteLabel => Current != null && favorites.Any(w => w.Id == Current.Id) ? "♥ 已收藏" : "♡ 收藏";
    public string PositionText => selectedIndex >= 0 ? $"{selectedIndex + 1} / {Items.Count}" : "发现新风景";
    public string WallpaperMeta => Current == null ? "" : $"{Current.Resolution}   ·   {Current.CategoryLabel}   ·   {Current.SizeLabel}";
    public string DetailsText => Current == null ? "" : $"编号 {Current.Id}  ·  {Current.Views:N0} 次浏览  ·  {Current.Favorites:N0} 次收藏\n原图与作者信息可在 Wallhaven 来源页查看。";
    public string GallerySummary => favoriteView ? $"{Items.Count} 张本地收藏 · 点击查看原图" : activeCollection != null ? $"站内收藏 · {activeCollection.Label} · 共 {total:N0} 张" : $"共 {total:N0} 张结果 · 已加载 {Items.Count} 张";
    public string ScheduleText => !Settings.AutoChange ? "自动更换已关闭" : Settings.NextChange is { } next ? $"下次更换：{next.LocalDateTime:MM-dd HH:mm}（应用运行时）" : "保存设置后开始计时";
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    private void Refresh()
    {
        foreach (var value in new (string Name, object? Value)[]
        {
            (nameof(HasWallpaper), HasWallpaper), (nameof(ShowWelcome), ShowWelcome), (nameof(IsBusy), IsBusy),
            (nameof(CanSearch), CanSearch), (nameof(CanAct), CanAct), (nameof(CanLoadMore), CanLoadMore),
            (nameof(LoadMoreLabel), LoadMoreLabel), (nameof(FavoriteLabel), FavoriteLabel), (nameof(PositionText), PositionText),
            (nameof(WallpaperMeta), WallpaperMeta), (nameof(DetailsText), DetailsText), (nameof(GallerySummary), GallerySummary),
            (nameof(ScheduleText), ScheduleText), (nameof(CanUseApi), CanUseApi), (nameof(DesktopDescription), DesktopDescription),
            (nameof(NextDesktopDescription), NextDesktopDescription), (nameof(NextDesktopDetails), NextDesktopDetails),
            (nameof(NextDesktopReady), NextDesktopReady), (nameof(CanSwitchDesktop), CanSwitchDesktop)
        })
        {
            if (publishedValues.TryGetValue(value.Name, out var previous) && Equals(previous, value.Value)) continue;
            publishedValues[value.Name] = value.Value;
            Changed(value.Name);
        }
        bool ready = NextDesktopReady;
        if (!ready && !nextBusy && NextDesktopThumbnail != null) NextDesktopThumbnail = null;
        var next = ready ? Settings.NextDesktopWallpaper : null;
        if (!markersDirty && ReferenceEquals(markedDesktop, Settings.DesktopWallpaper) && ReferenceEquals(markedNext, next)) return;
        markersDirty = false; markedDesktop = Settings.DesktopWallpaper; markedNext = next;
        foreach (var item in Items.Concat(PinnedItems))
        {
            item.IsDesktop = SameWallpaper(item.Wallpaper, Settings.DesktopWallpaper);
            item.IsNextDesktop = ready && SameWallpaper(item.Wallpaper, next);
        }
    }
    private static bool SameWallpaper(Wallpaper first, Wallpaper? second) => second != null &&
        (WallhavenClient.ValidId(first.Id) && first.Id == second.Id || first.IsLocal && second.IsLocal && string.Equals(first.LocalPath, second.LocalPath, StringComparison.OrdinalIgnoreCase));

    public MainWindow(string? dataRoot = null, string? diagnostics = null)
    {
        previewLoader = LoadBitmapAsync;
        Items.CollectionChanged += (_, _) => markersDirty = true;
        PinnedItems.CollectionChanged += (_, _) => markersDirty = true;
        previewSaveTimer.Tick += (_, _) => SaveLocal();
        thumbnailViewportTimer.Tick += async (_, _) =>
        {
            thumbnailViewportTimer.Stop();
            thumbsCts?.Cancel();
            await GuardAsync(LoadThumbnailsAsync);
        };
        store = new(dataRoot);
        diagnosticsDirectory = diagnostics;
        Settings = store.LoadSettings();
        ThemeManager.Apply(Settings.Theme);
        favorites = store.LoadFavorites();
        client = new(http);
        keyVault = new(store.Root);
        try
        {
            activeApiKey = keyVault.Load(); client.SetApiKey(activeApiKey);
            if (client.HasApiKey) ApiStatus = "已载入加密密钥 · 可点击验证连接";
        }
        catch { ApiStatus = "保存的密钥无法解密，请重新填写连接"; }
        cache = new(http, store.CacheDirectory, validateImage: ValidateCachedImage, fileCommitted: bitmapMemory.FileCommitted);
        InitializeComponent();
        Drawer.RenderTransform = drawerTranslation;
        if (diagnosticsDirectory == null) ReadStartupSetting();
        else StartupCheck.IsEnabled = false;
        windowBounds = new NativeWindowBounds(this, () => fullscreen);
        RootGrid.SizeChanged += (_, _) => { if (initialized) UpdateLayoutMode(); };
        DataContext = this;
        chromeTimer.Tick += (_, _) =>
        {
            chromeTimer.Stop();
            if (NavigationPanel.IsMouseOver || WallpaperMenu.IsOpen) return;
            NavigationPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)));
            NavigationPanel.IsHitTestVisible = false;
            chromeVisible = false;
        };
        SearchText = Settings.Query;
        string cats = Settings.Categories;
        if (cats.Length != 3 || cats.Any(c => c is not ('0' or '1')) || cats == "000") cats = Settings.Categories = "111";
        GeneralCheck.IsChecked = cats[0] == '1'; AnimeCheck.IsChecked = cats[1] == '1'; PeopleCheck.IsChecked = cats[2] == '1';
        if (!WallhavenClient.ValidMask(Settings.Purity) || !client.HasApiKey && Settings.Purity[2] == '1') Settings.Purity = "100";
        UpdatePurityChecks();
        UpdateStretch();
        statusTimer.Tick += (_, _) => { Status = ""; statusTimer.Stop(); };
        scheduler.Tick += async (_, _) => await GuardAsync(CheckScheduleAsync);
        SetupTray();
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += SystemThemeChanged;
    }
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        initialized = true;
        UpdateLayoutMode();
        if (store.LoadWarning != null) Notify(store.LoadWarning, true);
        await GuardAsync(async () =>
        {
            if (Settings.LastWallpaper is { } last && client.MatchesPurity(last.Purity, Settings.Purity))
            {
                try
                {
                    string cached = last.IsLocal ? last.LocalPath : cache.GetPath(last);
                    if (File.Exists(cached))
                    {
                        Current = last; originalPath = cached;
                        ShowImage(await LoadBitmapAsync(cached, 2560));
                    }
                }
                catch (Exception ex) { WriteLog(ex); }
            }
            await SearchAsync(true);
        });
        if (Settings.AutoChange && Settings.NextChange == null)
        {
            Settings.NextChange = DateTimeOffset.Now.AddMinutes(Settings.IntervalMinutes);
            SaveLocal();
        }
        scheduler.Start();
        Refresh();
        if (diagnosticsDirectory != null) await GuardAsync(CaptureDiagnosticsAsync);
        else await GuardAsync(async () => { await RefreshDesktopStateAsync(); if (Settings.AutoChange || Settings.NextDesktopWallpaper != null) await PrepareNextDesktopAsync(); });
    }
    private async Task GuardAsync(Func<Task> action)
    {
        try { await action(); }
        catch (OperationCanceledException) { if (!lifetime.IsCancellationRequested) Notify("请求已取消或超时，请检查网络后重试。", true); }
        catch (Exception ex) { WriteLog(ex); Notify(Friendly(ex), true); }
    }
    private static string Friendly(Exception ex) => ex switch
    {
        WallhavenException or ArgumentException => ex.Message,
        OperationCanceledException => "请求已超时或取消，请稍后重试。",
        HttpRequestException => "连接 Wallhaven 失败，请检查网络或系统代理后重试。",
        IOException => "文件读写失败，请检查磁盘空间与目录权限。",
        UnauthorizedAccessException => "无法写入应用数据或图片文件，请检查目录访问权限。",
        _ => "操作未完成：" + ex.Message
    };
    private void Notify(string message, bool persistent = false)
    {
        Status = message;
        statusTimer.Stop();
        if (!persistent) statusTimer.Start();
        if (!IsVisible && tray != null)
        {
            tray.BalloonTipTitle = "Wallhaven";
            tray.BalloonTipText = message;
            tray.ShowBalloonTip(4000);
        }
    }
    private void WriteLog(Exception ex)
    {
        try { File.AppendAllText(Path.Combine(store.Root, "errors.log"), client.Redact($"{DateTimeOffset.Now:u} {ex}\n")); } catch { }
    }

    private async Task SearchAsync(bool initial = false)
    {
        if (searchBusy) return;
        var options = Settings.ToSearch();
        _ = WallhavenClient.BuildSearchUri(options, 1);
        searchCts?.Cancel(); searchCts?.Dispose();
        var cts = searchCts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        cts.CancelAfter(TimeSpan.FromSeconds(35));
        searchBusy = true; BusyText = "正在寻找新的风景…"; Refresh();
        try
        {
            var result = await client.SearchAsync(options, 1, ct: cts.Token);
            imageCts?.Cancel(); thumbsCts?.Cancel(); thumbsCts?.Dispose();
            thumbsCts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            favoriteView = false; activeCollection = null; activeSearch = options;
            page = result.CurrentPage; lastPage = result.LastPage; total = result.Total; seed = result.Seed;
            Items.Clear();
            selectedIndex = -1;
            AddItems(result.Wallpapers);
            if (GalleryPanel.Visibility == Visibility.Visible) SetGalleryHeading();
            if (Items.Count == 0)
            {
                Notify("没有找到匹配的壁纸，试试更短的英文关键词或放宽筛选条件。", true);
                OpenDrawer("gallery");
            }
            else
            {
                Status = "";
                int index = initial && options.Sorting != "random" && Current != null ? Items.ToList().FindIndex(x => x.Wallpaper.Id == Current.Id) : 0;
                await SelectAsync(Math.Max(0, index));
            }
        }
        finally { searchBusy = false; Refresh(); }
    }
    private void AddItems(IEnumerable<Wallpaper> wallpapers)
    {
        foreach (var wallpaper in wallpapers)
        {
            if (!client.MatchesPurity(wallpaper.Purity, Settings.Purity)) continue;
            if (Items.Any(i => i.Wallpaper.Id == wallpaper.Id)) continue;
            var item = new WallpaperItem(wallpaper, favorites.Any(f => f.Id == wallpaper.Id)) { IsCurrent = wallpaper.Id == Current?.Id };
            Items.Add(item);
        }
        if (PinnedItems.Count > 0 && Items.Any(item => item.Wallpaper.Id == Current?.Id))
        {
            PinnedItems.Clear(); Changed(nameof(HasPinnedWallpaper));
        }
        Refresh();
    }
    private async Task LoadThumbnailsAsync()
    {
        if (GalleryPanel.Visibility != Visibility.Visible) return;
        if (thumbnailsLoading) { thumbnailsRefreshRequested = true; return; }
        thumbnailsLoading = true;
        try
        {
            do
            {
                thumbnailsRefreshRequested = false;
                if (thumbsCts == null || thumbsCts.IsCancellationRequested)
                {
                    thumbsCts?.Dispose();
                    thumbsCts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                }
                var ct = thumbsCts.Token;
                int first = Items.Count <= 24 ? 0 : Math.Max(0, (int)(GalleryScroll.VerticalOffset / VirtualizingTilePanel.RowHeight) - 4) * 2;
                int count = Math.Max(24, ((int)Math.Ceiling(GalleryScroll.ViewportHeight / VirtualizingTilePanel.RowHeight) + 8) * 2);
                for (int i = 0; i < Items.Count; i++) if (i < first - 12 || i >= first + count + 12) Items[i].Thumbnail = null;
                await Task.WhenAll(Items.Skip(first).Take(count).Where(i => i.Thumbnail == null).ToArray().Select(async item =>
                {
                    try
                    {
                        string path = item.Wallpaper.IsLocal ? item.Wallpaper.LocalPath : await cache.GetAsync(item.Wallpaper, true, ct: ct);
                        var image = await LoadBitmapAsync(path, 400);
                        if (!ct.IsCancellationRequested) { item.Thumbnail = image; item.ThumbnailError = ""; }
                    }
                    catch (OperationCanceledException) { }
                    catch { item.ThumbnailError = "预览暂不可用"; }
                }));
            } while (thumbnailsRefreshRequested && GalleryPanel.Visibility == Visibility.Visible && !lifetime.IsCancellationRequested);
        }
        finally { thumbnailsLoading = false; }
    }
    private async Task LoadMoreAsync()
    {
        if (!CanLoadMore) return;
        searchBusy = true; BusyText = "正在加载更多壁纸…"; Refresh();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(35));
            var result = activeCollection != null
                ? await client.GetCollectionAsync(collectionUsername, activeCollection.Id, collectionPurity, page + 1, cts.Token)
                : await client.SearchAsync(activeSearch, page + 1, seed, cts.Token);
            page = result.CurrentPage; lastPage = result.LastPage; total = result.Total;
            AddItems(result.Wallpapers);
        }
        finally { searchBusy = false; Refresh(); }
        await LoadThumbnailsAsync();
    }
    private async Task SelectAsync(int index)
    {
        if (index < 0 || index >= Items.Count) return;
        preloadCts?.Cancel();
        imageCts?.Cancel();
        imageCts?.Dispose();
        var cts = imageCts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        cts.CancelAfter(TimeSpan.FromSeconds(100));
        var ct = cts.Token;
        var wallpaper = Items[index].Wallpaper;
        selectedIndex = index;
        Current = wallpaper; originalPath = null;
        imageBusy = true; BusyText = "正在加载高清壁纸…"; Refresh();
        DetailsPanel.Visibility = Visibility.Collapsed;
        try
        {
            // Avoid displaying a previous image under a newly selected wallpaper's metadata.
            string cachedPath = wallpaper.IsLocal ? wallpaper.LocalPath : cache.GetPath(wallpaper);
            if (bitmapMemory.TryGet(cachedPath, 2560) is { } full) ShowImage(full);
            else if (Items[index].Thumbnail is { } thumb) ShowImage(thumb);
            else { WallpaperImage.Source = null; PreviousImage.Source = null; }
            string path = wallpaper.IsLocal ? wallpaper.LocalPath : await cache.GetAsync(wallpaper, ct: ct);
            var bitmap = await previewLoader(path, 2560);
            ct.ThrowIfCancellationRequested();
            if (imageCts != cts) return;
            ShowImage(bitmap); originalPath = path;
            Settings.LastWallpaper = wallpaper; QueuePreviewSave();
            WarmAdjacentImages(index);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested && imageCts != cts) { }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (imageCts == cts) { WriteLog(ex); Notify(Friendly(ex) + " 点击下一张或重试当前图片。", true); }
        }
        finally { if (imageCts == cts) { imageBusy = false; Refresh(); } }
    }
    private Task<BitmapImage> LoadBitmapAsync(string path, int width) => bitmapMemory.LoadAsync(path, width);
    private void ValidateCachedImage(string path) => bitmapMemory.Validate(path);
    private void WarmAdjacentImages(int index)
    {
        if (diagnosticsDirectory != null) return;
        preloadCts?.Cancel(); preloadCts?.Dispose();
        var cts = preloadCts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        var neighbors = new[] { index + 1, index - 1 }.Where(i => i >= 0 && i < Items.Count).Select(i => Items[i].Wallpaper).ToArray();
        _ = WarmAsync();
        async Task WarmAsync()
        {
            foreach (var neighbor in neighbors)
            {
                if (cts.IsCancellationRequested) return;
                try
                {
                    string path = neighbor.IsLocal ? neighbor.LocalPath : cache.GetPath(neighbor);
                    if (File.Exists(path)) await LoadBitmapAsync(path, 2560);
                }
                catch { /* Speculative local preloading must never interrupt browsing. */ }
            }
        }
    }
    private void ShowImage(ImageSource image)
    {
        if (ReferenceEquals(WallpaperImage.Source, image)) return;
        PreviousImage.Source = WallpaperImage.Source;
        WallpaperImage.Source = image;
        Changed(nameof(WallpaperBackdropImage));
        WallpaperImage.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.55)));
    }
    private async Task MoveAsync(int direction)
    {
        if (Items.Count == 0) { await SearchAsync(); return; }
        int index = selectedIndex + direction;
        if (index >= Items.Count && CanLoadMore) await LoadMoreAsync();
        index = (index + Items.Count) % Items.Count;
        await SelectAsync(index);
    }
    private void Favorite_Click(object? sender, RoutedEventArgs e)
    {
        if (Current == null) return;
        if (!WallhavenClient.ValidId(Current.Id)) { Notify("这张本地桌面图没有 Wallhaven 编号，可以使用下载保存。"); return; }
        bool remove = favorites.Any(w => w.Id == Current.Id);
        var updated = favorites.Where(w => w.Id != Current.Id).ToList();
        if (!remove) updated.Insert(0, Current);
        try { store.SaveFavorites(updated); }
        catch (Exception ex) { WriteLog(ex); Notify(Friendly(ex), true); return; }
        favorites.Clear(); favorites.AddRange(updated);
        if (favoriteView)
        {
            var row = Items.FirstOrDefault(w => w.Wallpaper.Id == Current.Id);
            if (remove && row != null) Items.Remove(row);
            else if (!remove && row == null) Items.Insert(0, new WallpaperItem(Current, true) { IsCurrent = true });
            selectedIndex = Items.ToList().FindIndex(w => w.Wallpaper.Id == Current.Id);
        }
        foreach (var item in Items) item.IsFavorite = favorites.Any(w => w.Id == item.Wallpaper.Id);
        foreach (var item in PinnedItems) item.IsFavorite = favorites.Any(w => w.Id == item.Wallpaper.Id);
        Refresh();
        Notify(remove ? "已取消收藏" : "已收藏，随时可以在收藏中找到它");
    }
    private async void SetWallpaper_Click(object? sender, RoutedEventArgs e) => await GuardAsync(SetCurrentWallpaperAsync);
    private async Task SetCurrentWallpaperAsync()
    {
        if (!CanAct || Current == null || originalPath == null) return;
        string path = originalPath;
        var wallpaper = Current;
        actionBusy = true; BusyText = "正在设置桌面壁纸…"; Refresh();
        try
        {
            await rotation.ApplyManualAsync(Settings, wallpaper, (_, ct) => ApplyDesktopAsync(path, Settings.Fill, ct), lifetime.Token);
            SaveLocal(); await RefreshDesktopStateAsync();
            Notify("已设置为桌面壁纸");
        }
        finally { actionBusy = false; Refresh(); }
        if (Settings.AutoChange && diagnosticsDirectory == null) _ = GuardAsync(PrepareNextDesktopAsync);
    }
    private async Task<string> ApplyDesktopAsync(string path, bool fill, CancellationToken ct)
    {
        await desktopGate.WaitAsync(ct);
        try
        {
            string bmp;
            try { bmp = await DesktopImagePreparer.PrepareAsync(path, store.Root, ct); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                WriteLog(ex);
                throw new WallhavenException("准备桌面图片失败，请检查应用数据目录的可写权限。无需更改下载保存位置。");
            }
            if (diagnosticsDirectory == null) NativeDesktop.SetWallpaper(bmp, fill);
            return bmp;
        }
        finally { desktopGate.Release(); }
    }
    private async void Download_Click(object? sender, RoutedEventArgs e) => await GuardAsync(async () =>
    {
        if (!CanAct || Current == null || originalPath == null) return;
        string source = originalPath;
        var dialog = new Microsoft.Win32.SaveFileDialog { FileName = Path.GetFileName(source), Filter = "原始图片|*" + Path.GetExtension(source), DefaultExt = Path.GetExtension(source), AddExtension = true };
        if (dialog.ShowDialog(this) != true) return;
        actionBusy = true; BusyText = "正在保存原图…"; Refresh();
        try { await Task.Run(() => { if (!string.Equals(source, dialog.FileName, StringComparison.OrdinalIgnoreCase)) File.Copy(source, dialog.FileName, true); }); Notify("原图已保存到所选位置"); }
        finally { actionBusy = false; Refresh(); }
    });
    private void Source_Click(object? sender, RoutedEventArgs e)
    {
        if (Current == null || !WallhavenClient.ValidId(Current.Id)) return;
        OpenPath($"https://wallhaven.cc/w/{Current.Id}");
    }
    private async void LocateCurrent_Click(object? sender, RoutedEventArgs e) => await GuardAsync(LocateDesktopAsync);
    private async void LocatePreview_Click(object? sender, RoutedEventArgs e) => await GuardAsync(LocateCurrentAsync);
    private async Task LocateCurrentAsync()
    {
        if (Current == null) { Notify("请先选择一张壁纸"); return; }
        if (searchBusy) { Notify("正在更新列表，请稍后再定位"); return; }
        locatingCurrent = true;
        try
        {
            var wallpaper = Current;
            var row = Items.FirstOrDefault(item => item.Wallpaper.Id == wallpaper.Id);
            PinnedItems.Clear();
            if (row == null)
                PinnedItems.Add(new WallpaperItem(wallpaper, favorites.Any(item => item.Id == wallpaper.Id)) { IsCurrent = true, Thumbnail = WallpaperImage.Source });
            Changed(nameof(HasPinnedWallpaper));
            OpenDrawer("gallery");
            foreach (var item in Items) item.IsCurrent = item.Wallpaper.Id == wallpaper.Id;
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);
            GalleryItems.UpdateLayout();
            if (row != null)
            {
                var panel = FindVisualChild<VirtualizingTilePanel>(GalleryItems);
                GalleryScroll.ScrollToVerticalOffset(panel?.ItemOffset(Items.IndexOf(row)) ?? Items.IndexOf(row) / 2 * VirtualizingTilePanel.RowHeight);
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);
                GalleryItems.UpdateLayout();
                if (GalleryItems.ItemContainerGenerator.ContainerFromItem(row) is FrameworkElement container) container.BringIntoView();
            }
            else GalleryScroll.ScrollToTop();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            // Thumbnail work can run after positioning; it does not change the active image or list source.
            _ = LoadThumbnailsAsync();
        }
        finally { locatingCurrent = false; }
    }
    private string desktopOriginalPath = "";
    private async Task RefreshDesktopStateAsync()
    {
        await desktopIdentityGate.WaitAsync(lifetime.Token);
        try
        {
            string path = diagnosticsDirectory != null ? Settings.DesktopFilePath : NativeDesktop.GetWallpaperPath();
            path = Environment.ExpandEnvironmentVariables(path);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                Settings.DesktopWallpaper = null; DesktopThumbnail = null;
                desktopStatus = "Windows 当前没有提供可读取的静态壁纸文件";
                Refresh(); return;
            }
            var stamp = File.GetLastWriteTimeUtc(path);
            if (string.Equals(path, observedDesktopPath, StringComparison.OrdinalIgnoreCase) && stamp == observedDesktopStamp && DesktopThumbnail != null)
            { Refresh(); return; }
            Wallpaper? wallpaper = string.Equals(path, Settings.DesktopFilePath, StringComparison.OrdinalIgnoreCase) ? Settings.DesktopWallpaper : null;
            wallpaper ??= Settings.DesktopHistory.FirstOrDefault(record => string.Equals(record.FilePath, path, StringComparison.OrdinalIgnoreCase))?.Wallpaper;
            string? source = null;
            if (wallpaper != null)
            {
                try { string cached = wallpaper.IsLocal ? wallpaper.LocalPath : cache.GetPath(wallpaper); if (File.Exists(cached)) source = cached; } catch { }
            }
            if (wallpaper == null)
            {
                desktopStatus = "正在识别 Windows 当前桌面…"; Refresh();
                source = await DesktopIdentityResolver.FindCachedSourceAsync(path, store.CacheDirectory, lifetime.Token);
                string id = source == null ? "" : Path.GetFileNameWithoutExtension(source).Replace("wallhaven-", "", StringComparison.OrdinalIgnoreCase);
                if (WallhavenClient.ValidId(id))
                {
                    wallpaper = Items.Select(item => item.Wallpaper).Concat(favorites).Concat(Settings.DesktopHistory.Select(record => record.Wallpaper)).FirstOrDefault(item => item.Id == id);
                    wallpaper ??= Settings.LastWallpaper?.Id == id ? Settings.LastWallpaper : null;
                    if (wallpaper == null)
                    {
                        try { wallpaper = await client.GetWallpaperAsync(id, lifetime.Token); }
                        catch (Exception ex) when (ex is not OperationCanceledException) { WriteLog(ex); }
                    }
                    wallpaper ??= new Wallpaper { Id = id, Purity = "sfw", LocalPath = source!, Category = "local" };
                }
            }
            var image = await LoadBitmapAsync(path, 512);
            wallpaper ??= new Wallpaper { LocalPath = path, Category = "local", Purity = "sfw", FileSize = new FileInfo(path).Length, Resolution = "Windows 当前桌面" };
            Settings.DesktopWallpaper = wallpaper; Settings.DesktopFilePath = path;
            desktopOriginalPath = source ?? path;
            observedDesktopPath = path; observedDesktopStamp = stamp;
            DesktopThumbnail = image; desktopStatus = "与 Windows 当前桌面一致";
            SaveLocal(); Refresh();
        }
        finally { desktopIdentityGate.Release(); }
    }
    private async Task<bool> ShowStandaloneAsync(Wallpaper wallpaper, string path, string? downloadPath = null)
    {
        imageCts?.Cancel();
        imageCts?.Dispose();
        var cts = imageCts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        cts.CancelAfter(TimeSpan.FromSeconds(100));
        var ct = cts.Token;
        Current = wallpaper;
        selectedIndex = Items.ToList().FindIndex(item => SameWallpaper(item.Wallpaper, wallpaper));
        originalPath = null;
        imageBusy = true; BusyText = "正在加载高清壁纸…"; Refresh();
        WallpaperImage.Source = null; PreviousImage.Source = null;
        try
        {
            var bitmap = await previewLoader(path, 2560);
            ct.ThrowIfCancellationRequested();
            if (imageCts != cts) return false;
            ShowImage(bitmap); originalPath = downloadPath ?? path;
            Settings.LastWallpaper = wallpaper; QueuePreviewSave();
            return true;
        }
        catch (OperationCanceledException) when (imageCts != cts) { return false; }
        catch (Exception) when (imageCts != cts) { return false; }
        finally { if (imageCts == cts) { imageBusy = false; Refresh(); } }
    }
    private async Task LocateDesktopAsync()
    {
        await RefreshDesktopStateAsync();
        if (Settings.DesktopWallpaper == null || !File.Exists(desktopOriginalPath)) { Notify(desktopStatus, true); return; }
        if (!await ShowStandaloneAsync(Settings.DesktopWallpaper, Settings.DesktopFilePath, desktopOriginalPath)) return;
        await LocateCurrentAsync();
        Notify("已定位 Windows 当前桌面使用的壁纸");
    }
    private async void DesktopStatus_Click(object? sender, RoutedEventArgs e) => await GuardAsync(async () =>
    {
        OpenDrawer("desktop");
        await RefreshDesktopStateAsync();
        if (Settings.AutoChange || Settings.NextDesktopWallpaper != null) await PrepareNextDesktopAsync();
    });
    private DateTimeOffset nextPrepareRetry;
    private Task PrepareNextDesktopAsync() => PrepareNextDesktopAsync(false);
    private async Task PrepareNextDesktopAsync(bool force)
    {
        if (NextDesktopReady && File.Exists(cache.GetPath(Settings.NextDesktopWallpaper!))) return;
        nextReady = false;
        if (!force && nextPrepareRetry > DateTimeOffset.Now) return;
        await nextPreparationGate.WaitAsync(lifetime.Token);
        try
        {
            if (NextDesktopReady) return;
            nextBusy = true; nextDesktopStatus = "正在准备下一张自动切换壁纸…"; Refresh();
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var options = Settings.ToSearch() with { Sorting = "random" };
                var next = await rotation.PrepareAsync(Settings, options,
                    async (search, ct) => (await client.SearchAsync(search, 1, ct: ct)).Wallpapers,
                    async (wallpaper, ct) =>
                    {
                        string file = await cache.GetAsync(wallpaper, ct: ct);
                        await DesktopImagePreparer.PrepareAsync(file, store.Root, ct);
                    }, lifetime.Token);
                if (options != (Settings.ToSearch() with { Sorting = "random" })) continue;
                NextDesktopThumbnail = await LoadBitmapAsync(cache.GetPath(next), 512);
                nextReady = true; nextPrepareRetry = DateTimeOffset.MinValue;
                nextDesktopStatus = "已准备好";
                SaveLocal(); return;
            }
            nextDesktopStatus = "筛选条件已变化，请重新准备下一张";
        }
        catch
        {
            nextDesktopStatus = "下一张准备失败，可点击重试";
            nextPrepareRetry = DateTimeOffset.Now.AddMinutes(5);
            throw;
        }
        finally { nextBusy = false; Refresh(); nextPreparationGate.Release(); }
    }
    private async void PreviewNextDesktop_Click(object sender, RoutedEventArgs e) => await GuardAsync(PreviewNextDesktopAsync);
    private async Task PreviewNextDesktopAsync()
    {
        OpenDrawer("desktop");
        await RefreshDesktopStateAsync();
        await PrepareNextDesktopAsync(true);
        if (NextDesktopReady) await ShowStandaloneAsync(Settings.NextDesktopWallpaper!, cache.GetPath(Settings.NextDesktopWallpaper!));
    }
    private async Task SwitchNextDesktopAsync()
    {
        await RefreshDesktopStateAsync();
        await PrepareNextDesktopAsync(true);
        var applied = await rotation.ApplyNextAsync(Settings, async (wallpaper, ct) =>
        {
            string path = await cache.GetAsync(wallpaper, ct: ct);
            return await ApplyDesktopAsync(path, Settings.Fill, ct);
        }, lifetime.Token);
        nextReady = false; NextDesktopThumbnail = null; nextDesktopStatus = "正在准备下一张…";
        SaveLocal(); await RefreshDesktopStateAsync();
        Notify($"已切换桌面：{applied.Title}");
    }
    private async void SwitchNextDesktop_Click(object sender, RoutedEventArgs e) => await GuardAsync(async () =>
    {
        if (!CanSwitchDesktop) return;
        actionBusy = true; BusyText = "正在切换到已预览的下一张…"; Refresh();
        try
        {
            await SwitchNextDesktopAsync();
            if (Settings.AutoChange) Settings.NextChange = DateTimeOffset.Now.AddMinutes(Settings.IntervalMinutes);
            SaveLocal();
        }
        finally { actionBusy = false; Refresh(); }
        await PrepareNextDesktopAsync(true);
    });
    private void OpenFileLocation_Click(object? sender, RoutedEventArgs e)
    {
        if (originalPath == null || !File.Exists(originalPath)) { Notify("当前图片的本地原图尚未加载，或缓存文件已被移走。", true); return; }
        try
        {
            var start = new ProcessStartInfo("explorer.exe") { UseShellExecute = true };
            start.ArgumentList.Add("/select,");
            start.ArgumentList.Add(Path.GetFullPath(originalPath));
            Process.Start(start);
        }
        catch (Exception ex) { Notify(Friendly(ex), true); }
    }
    private void OpenPath(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { Notify(Friendly(ex), true); }
    }
    private void OpenDrawer(string kind)
    {
        bool wasOpen = Drawer.Visibility == Visibility.Visible && !drawerClosing;
        double from = Drawer.Visibility == Visibility.Visible ? drawerTranslation.X : -462;
        ++drawerAnimation;
        drawerClosing = false;
        drawerTranslation.BeginAnimation(TranslateTransform.XProperty, null);
        drawerTranslation.X = 0;
        Drawer.IsHitTestVisible = true;
        bool settings = kind == "settings";
        bool desktop = kind == "desktop";
        Drawer.Visibility = Visibility.Visible;
        SettingsScroll.Visibility = settings ? Visibility.Visible : Visibility.Collapsed;
        GalleryPanel.Visibility = !settings && !desktop ? Visibility.Visible : Visibility.Collapsed;
        DesktopPanel.Visibility = desktop ? Visibility.Visible : Visibility.Collapsed;
        if (settings) { DrawerTitle.Text = "设置"; DrawerSubtitle.Text = "为下一次心动，留一点空间。"; }
        else if (desktop) { DrawerTitle.Text = "桌面壁纸"; DrawerSubtitle.Text = "正在使用的桌面，和即将切换的下一张。"; }
        else SetGalleryHeading();
        UpdateLayoutMode();
        if (!wasOpen && SystemParameters.ClientAreaAnimation)
        {
            int generation = drawerAnimation;
            var transition = new DoubleAnimation(from, 0, TimeSpan.FromMilliseconds(180)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            transition.Completed += (_, _) =>
            {
                if (generation != drawerAnimation) return;
                drawerTranslation.BeginAnimation(TranslateTransform.XProperty, null);
                DrawerMaterial.RefreshSample();
            };
            drawerTranslation.BeginAnimation(TranslateTransform.XProperty, transition);
        }
    }
    private void SetGalleryHeading()
    {
        DrawerTitle.Text = favoriteView ? "我的收藏" : activeCollection != null ? "站内收藏" : "发现壁纸";
        DrawerSubtitle.Text = favoriteView ? "喜欢的风景，随时回来。" : activeCollection != null ? activeCollection.Label : "点击一张图片，打开新的风景。";
    }
    private void CloseDrawer()
    {
        if (Drawer.Visibility != Visibility.Visible || drawerClosing) return;
        int generation = ++drawerAnimation;
        drawerClosing = true;
        Drawer.IsHitTestVisible = false;
        DrawerScrim.Visibility = Visibility.Collapsed;
        UpdateLayoutMode();
        if (!IsActive || !SystemParameters.ClientAreaAnimation) { Finish(); return; }
        var transition = new DoubleAnimation(drawerTranslation.X, -Drawer.Width, TimeSpan.FromMilliseconds(150)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } };
        transition.Completed += (_, _) => Finish();
        drawerTranslation.BeginAnimation(TranslateTransform.XProperty, transition);
        void Finish()
        {
            if (generation != drawerAnimation) return;
            Drawer.Visibility = Visibility.Collapsed;
            drawerTranslation.BeginAnimation(TranslateTransform.XProperty, null);
            drawerTranslation.X = 0;
        }
    }
    private bool SettingsDrawerOpen => Drawer.Visibility == Visibility.Visible && !drawerClosing && SettingsScroll.Visibility == Visibility.Visible;
    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (SettingsDrawerOpen) CloseDrawer();
    }
    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (SettingsDrawerOpen && !IsDrawerInteraction(e.OriginalSource as DependencyObject)) CloseDrawer();
    }
    private void Drawer_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        int generation = drawerAnimation;
        // Popups can move focus through a transient null; evaluate the settled focus.
        Dispatcher.BeginInvoke(() =>
        {
            if (generation == drawerAnimation && SettingsDrawerOpen && Keyboard.FocusedElement is DependencyObject focused && !IsDrawerInteraction(focused)) CloseDrawer();
        }, DispatcherPriority.Input);
    }
    private bool IsDrawerInteraction(DependencyObject? target)
    {
        while (target != null)
        {
            if (ReferenceEquals(target, Drawer)) return true;
            target = target switch
            {
                ContextMenu menu => menu.PlacementTarget,
                System.Windows.Controls.Primitives.Popup popup => popup.PlacementTarget,
                ComboBoxItem item => ItemsControl.ItemsControlFromItemContainer(item),
                MenuItem item when ItemsControl.ItemsControlFromItemContainer(item) is { } owner => owner,
                FrameworkElement element when element.Parent != null => element.Parent,
                FrameworkElement element when element.TemplatedParent != null => element.TemplatedParent,
                FrameworkContentElement content => content.Parent,
                Visual or System.Windows.Media.Media3D.Visual3D => VisualTreeHelper.GetParent(target),
                _ => LogicalTreeHelper.GetParent(target)
            };
        }
        return false;
    }
    public FrameworkElement WallpaperBackdrop => Backdrop;
    public ImageSource? WallpaperBackdropImage => WallpaperImage.Source;
    public string AutoMenuLabel => "启用定时更换";
    private readonly DispatcherTimer chromeTimer = new() { Interval = TimeSpan.FromSeconds(2.5) };
    private bool chromeVisible;
    private long lastChromeActivity;
    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!chromeVisible)
        {
            chromeVisible = true;
            NavigationPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(150)));
            NavigationPanel.IsHitTestVisible = true;
        }
        if (Environment.TickCount64 - lastChromeActivity < 150) return;
        lastChromeActivity = Environment.TickCount64;
        chromeTimer.Stop(); chromeTimer.Start();
    }
    private void InfoTitle_Click(object sender, MouseButtonEventArgs e) => Details_Click(sender, new RoutedEventArgs());
    private void NarrowWindow_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Normal; Width = 680; }
    private void WideWindow_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Normal; Width = 1360; }
    private void FocusSearch_Click(object sender, RoutedEventArgs e) { OpenDrawer("gallery"); SearchBox.Focus(); SearchBox.SelectAll(); }
    private void ApiMenu_Click(object sender, RoutedEventArgs e) { OpenDrawer("settings"); ApiExpander.IsExpanded = true; ApiExpander.BringIntoView(); }
    private void AutoMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menu) AutoCheck.IsChecked = menu.IsChecked;
        SaveSettings_Click(sender, e);
    }
    private void CopySource_Click(object sender, RoutedEventArgs e)
    {
        if (Current != null) { Clipboard.SetText("https://wallhaven.cc/w/" + Current.Id); Notify("已复制来源链接。"); }
    }
    private void UpdateLayoutMode()
    {
        if (!initialized && !IsLoaded) return;
        bool open = Drawer.Visibility == Visibility.Visible && !drawerClosing;
        double viewportWidth = RootGrid.ActualWidth;
        Drawer.Width = Math.Min(462, Math.Max(300, viewportWidth - 20));
        bool inline = viewportWidth >= 924;
        MainContent.Margin = open && inline ? new Thickness(Drawer.Width, 0, 0, 0) : new Thickness(0);
        DrawerScrim.Visibility = open && !inline ? Visibility.Visible : Visibility.Collapsed;
        NavigationPanel.Visibility = viewportWidth < 960 && open ? Visibility.Collapsed : Visibility.Visible;
        double width = viewportWidth - (open && inline ? Drawer.Width : 0);
        InfoCard.MaxWidth = Math.Max(280, Math.Min(640, width - 60));
        NavigationPanel.Margin = width < 850 ? new Thickness(0, 0, 30, 180) : new Thickness(0, 0, 30, 30);
        InfoMaterial.RefreshSample(); DrawerMaterial.RefreshSample();
    }
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) return match;
            if (FindVisualChild<T>(child) is { } nested) return nested;
        }
        return null;
    }
    private async void Browse_Click(object? sender, RoutedEventArgs e) => await GuardAsync(async () =>
    {
        if (favoriteView) await SearchAsync();
        OpenDrawer("gallery"); await LoadThumbnailsAsync();
    });
    private async void Favorites_Click(object? sender, RoutedEventArgs e) => await GuardAsync(async () =>
    {
        if (searchBusy) return;
        imageCts?.Cancel(); thumbsCts?.Cancel();
        favoriteView = true; activeCollection = null; page = lastPage = 1; total = favorites.Count;
        Items.Clear(); selectedIndex = -1; AddItems(favorites);
        OpenDrawer("gallery");
        if (Items.Count == 0) Notify("还没有收藏。在喜欢的壁纸上点击“♡ 收藏”即可。", true);
        await LoadThumbnailsAsync();
    });
    private async void Thumbnail_Click(object sender, RoutedEventArgs e) => await GuardAsync(async () =>
    {
        if (sender is Button { Tag: WallpaperItem item })
        {
            int index = Items.IndexOf(item);
            if (index >= 0) await SelectAsync(index);
            if (RootGrid.ActualWidth < 924) CloseDrawer();
        }
    });
    private void Settings_Click(object? sender, RoutedEventArgs e) => OpenDrawer("settings");
    private void ReadStartupSetting()
    {
        try { StartupCheck.IsChecked = new WindowsStartup(Environment.ProcessPath!).IsEnabled(); }
        catch (Exception ex)
        {
            WriteLog(ex);
            StartupCheck.IsEnabled = false;
            StartupCheck.ToolTip = "无法读取开机启动设置：" + ex.Message;
        }
    }
    private void CloseDrawer_Click(object sender, RoutedEventArgs e) => CloseDrawer();
    private void Scrim_Click(object sender, MouseButtonEventArgs e) => CloseDrawer();
    private void Backdrop_Click(object sender, MouseButtonEventArgs e) { if (e.ClickCount == 2) ToggleFullscreen(); }
    private async void Details_Click(object sender, RoutedEventArgs e)
    {
        DetailsPanel.Visibility = DetailsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        if (DetailsPanel.Visibility != Visibility.Visible || Current == null || Current.Tags.Count > 0 || !WallhavenClient.ValidId(Current.Id)) return;
        var id = Current.Id;
        await GuardAsync(async () =>
        {
            var detail = await client.GetWallpaperAsync(id, lifetime.Token);
            if (Current?.Id == id) Current = detail;
        });
    }
    private async void Next_Click(object? sender, RoutedEventArgs e) => await GuardAsync(() => MoveAsync(1));
    private async void Previous_Click(object? sender, RoutedEventArgs e) => await GuardAsync(() => MoveAsync(-1));
    private async void LoadMore_Click(object sender, RoutedEventArgs e) => await GuardAsync(LoadMoreAsync);
    private void CollectSettings()
    {
        Settings.Categories = $"{(GeneralCheck.IsChecked == true ? '1' : '0')}{(AnimeCheck.IsChecked == true ? '1' : '0')}{(PeopleCheck.IsChecked == true ? '1' : '0')}";
        Settings.Purity = $"{(SfwCheck.IsChecked == true ? '1' : '0')}{(SketchyCheck.IsChecked == true ? '1' : '0')}{(NsfwCheck.IsChecked == true ? '1' : '0')}";
        _ = WallhavenClient.BuildSearchUri(Settings.ToSearch(), 1);
        if (Settings.Purity[2] == '1' && !client.HasApiKey) throw new WallhavenException("请先在“账户与 API Key”中连接密钥，再启用 NSFW。");
        Settings.IntervalMinutes = Math.Clamp(Settings.IntervalMinutes, 1, 1440);
        if (Settings.NextDesktopOptions != (Settings.ToSearch() with { Sorting = "random" })) nextPrepareRetry = DateTimeOffset.MinValue;
        UpdateStretch();
        ThemeManager.Apply(Settings.Theme);
    }
    private void UpdateStretch()
    {
        if (WallpaperImage == null) return;
        WallpaperImage.Stretch = PreviousImage.Stretch = Settings.Fill ? Stretch.UniformToFill : Stretch.Uniform;
    }
    private void SaveLocal()
    {
        previewSaveTimer.Stop();
        try { store.SaveSettings(Settings); }
        catch (Exception ex) { WriteLog(ex); Notify("设置暂时无法保存：" + Friendly(ex), true); }
    }
    private void QueuePreviewSave()
    {
        previewSaveTimer.Stop();
        previewSaveTimer.Start();
    }
    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CollectSettings();
            if (diagnosticsDirectory == null && StartupCheck.IsEnabled)
            {
                try { new WindowsStartup(Environment.ProcessPath!).SetEnabled(StartupCheck.IsChecked == true); }
                catch (Exception ex)
                {
                    ReadStartupSetting();
                    throw new IOException("开机启动设置未保存：" + ex.Message, ex);
                }
            }
            Settings.NextChange = Settings.AutoChange ? DateTimeOffset.Now.AddMinutes(Settings.IntervalMinutes) : null;
            previewSaveTimer.Stop();
            store.SaveSettings(Settings); SearchText = Settings.Query;
            Refresh(); Notify("设置已保存");
            if (Settings.AutoChange) _ = GuardAsync(PrepareNextDesktopAsync);
        }
        catch (Exception ex) { Notify(Friendly(ex), true); }
    }
    private async void ApplyFilters_Click(object sender, RoutedEventArgs e) => await GuardAsync(async () =>
    {
        CollectSettings(); SearchText = Settings.Query; SaveLocal();
        await SearchAsync(); OpenDrawer("gallery"); await LoadThumbnailsAsync();
        if (Settings.AutoChange) _ = GuardAsync(PrepareNextDesktopAsync);
    });
    private async Task SubmitSearchAsync()
    {
        if (searchBusy) return;
        Settings.Query = SearchText; SaveLocal();
        await SearchAsync(); OpenDrawer("gallery"); await LoadThumbnailsAsync();
    }
    private async void Search_Click(object sender, RoutedEventArgs e) => await GuardAsync(SubmitSearchAsync);
    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;
        if (sender == SettingsQuery) { SearchText = Settings.Query; }
        await GuardAsync(SubmitSearchAsync);
    }
    private async Task CheckScheduleAsync()
    {
        if (Settings.AutoChange && !NextDesktopReady && !nextBusy && !IsBusy && !autoBusy && DateTimeOffset.Now >= nextPrepareRetry)
        {
            await RefreshDesktopStateAsync();
            await PrepareNextDesktopAsync();
        }
        if (!Settings.AutoChange || autoBusy || IsBusy || Settings.NextChange is not { } due || due > DateTimeOffset.Now) return;
        autoBusy = true;
        try
        {
            var outcome = await autoSchedule.TickAsync(Settings, async ct =>
            {
                if (!Settings.AutoChange) return;
                await SwitchNextDesktopAsync();
            }, lifetime.Token);
            if (outcome.State != ScheduleState.NotDue) SaveLocal();
            if (outcome.Error is { } ex) { WriteLog(ex); Notify("自动更换失败，5 分钟后重试。" + Friendly(ex), true); }
            if (tray != null && Settings.NextChange is { } next) tray.Text = $"Wallhaven · 下次 {next.LocalDateTime:HH:mm}";
        }
        finally { autoBusy = false; Refresh(); }
        if (Settings.AutoChange && !NextDesktopReady) await PrepareNextDesktopAsync();
    }
    private void SetupTray()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("打开 Wallhaven", null, (_, _) => Dispatcher.BeginInvoke(RestoreWindow));
        menu.Items.Add("定位当前桌面壁纸", null, (_, _) => Dispatcher.BeginInvoke(async () => { RestoreWindow(); await GuardAsync(LocateDesktopAsync); }));
        menu.Items.Add("预览下一张自动壁纸", null, (_, _) => Dispatcher.BeginInvoke(() => { RestoreWindow(); PreviewNextDesktop_Click(this, new RoutedEventArgs()); }));
        menu.Items.Add("下一张", null, (_, _) => Dispatcher.BeginInvoke(async () => await GuardAsync(() => MoveAsync(1))));
        menu.Items.Add("设置当前壁纸为桌面", null, (_, _) => Dispatcher.BeginInvoke(async () => await GuardAsync(SetCurrentWallpaperAsync)));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => Dispatcher.BeginInvoke(ExitApp));
        using var executableIcon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!);
        tray = new Forms.NotifyIcon { Text = "Wallhaven · 高清壁纸", Icon = executableIcon == null ? System.Drawing.SystemIcons.Application : (System.Drawing.Icon)executableIcon.Clone(), ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (_, _) => Dispatcher.BeginInvoke(RestoreWindow);
    }
    public void RestoreWindow()
    {
        ShowActivated = true;
        Show(); if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
    }
    private void ExitApp()
    {
        exiting = true;
        Close();
    }
    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!exiting && Settings.CloseToTray)
        {
            e.Cancel = true; Hide(); SaveLocal();
            if (!trayHintShown) { trayHintShown = true; Notify("已留在系统托盘。双击图标恢复，右键可完全退出。"); }
            return;
        }
        lifetime.Cancel(); searchCts?.Cancel(); imageCts?.Cancel(); thumbsCts?.Cancel(); preloadCts?.Cancel();
        thumbnailViewportTimer.Stop();
        scheduler.Stop(); statusTimer.Stop(); chromeTimer.Stop(); SaveLocal();
        tray?.Dispose(); http.Dispose();
        windowBounds.Dispose();
        Microsoft.Win32.SystemEvents.UserPreferenceChanged -= SystemThemeChanged;
        Application.Current.Shutdown();
    }
    private void DismissStatus_Click(object sender, RoutedEventArgs e) { Status = ""; statusTimer.Stop(); }
    private void DataFolder_Click(object sender, RoutedEventArgs e) => OpenPath(store.Root);
    private void ApiSettingsPage_Click(object sender, RoutedEventArgs e) => OpenPath("https://wallhaven.cc/settings/account");
    private void UpdatePurityChecks()
    {
        SfwCheck.IsChecked = Settings.Purity[0] == '1';
        SketchyCheck.IsChecked = Settings.Purity[1] == '1';
        NsfwCheck.IsChecked = Settings.Purity[2] == '1';
    }
    private async Task RunApiAsync(Func<Task> operation)
    {
        if (!CanUseApi) return;
        apiBusy = true; Refresh();
        try { await operation(); }
        catch (Exception ex)
        {
            WriteLog(ex);
            ApiStatus = "操作未完成 · " + Friendly(ex);
            Notify(Friendly(ex), true);
        }
        finally { apiBusy = false; Refresh(); }
    }
    private async void ConnectApi_Click(object sender, RoutedEventArgs e) => await RunApiAsync(async () =>
    {
        string key = ApiKeyBox.Password.Trim();
        if (key.Length == 0) key = activeApiKey;
        if (key.Length == 0) throw new WallhavenException("请从 Wallhaven 账户设置复制 API Key 后填写。");
        ApiStatus = "正在验证连接…";
        using var candidate = new WallhavenClient(http);
        candidate.SetApiKey(key);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        cts.CancelAfter(TimeSpan.FromSeconds(35));
        _ = await candidate.GetSettingsAsync(cts.Token);
        keyVault.Save(key);
        activeApiKey = key; client.SetApiKey(key); ApiKeyBox.Clear();
        ApiStatus = "已连接 · 密钥已在本机加密保存";
        Notify("API Key 验证成功，可以读取账户偏好和站内收藏");
    });
    private async void ClearApi_Click(object sender, RoutedEventArgs e) => await RunApiAsync(() =>
    {
        keyVault.Clear(); activeApiKey = ""; client.SetApiKey(null); ApiKeyBox.Clear();
        ApiStatus = "未连接 · 已移除本机密钥";
        SiteCollections.Clear(); activeCollection = null;
        Settings.Purity = "100"; UpdatePurityChecks();
        imageCts?.Cancel();
        if (Current?.Purity is "nsfw" or "sketchy")
        {
            Current = null; Settings.LastWallpaper = null; originalPath = null;
            WallpaperImage.Source = PreviousImage.Source = null;
        }
        foreach (var item in Items.Where(x => x.Wallpaper.Purity != "sfw").ToArray()) Items.Remove(item);
        page = lastPage = 1; total = Items.Count;
        selectedIndex = Current == null ? -1 : Items.ToList().FindIndex(x => x.Wallpaper.Id == Current.Id);
        if (GalleryPanel.Visibility == Visibility.Visible) SetGalleryHeading();
        SaveLocal(); Refresh(); Notify("密钥已移除，内容筛选恢复为 SFW");
        return Task.CompletedTask;
    });
    private async void ImportPreferences_Click(object sender, RoutedEventArgs e) => await RunApiAsync(async () =>
    {
        var preferences = await client.GetSettingsAsync(lifetime.Token);
        string cats = MakeMask(preferences.Categories, "general", "anime", "people");
        string purity = MakeMask(preferences.Purity, "sfw", "sketchy", "nsfw");
        Settings.Categories = WallhavenClient.ValidMask(cats) ? cats : "111";
        Settings.Purity = WallhavenClient.ValidMask(purity) ? purity : "100";
        GeneralCheck.IsChecked = Settings.Categories[0] == '1'; AnimeCheck.IsChecked = Settings.Categories[1] == '1'; PeopleCheck.IsChecked = Settings.Categories[2] == '1';
        UpdatePurityChecks();
        if (new[] { "1d", "3d", "1w", "1M", "3M", "6M", "1y", "all" }.Contains(preferences.TopRange)) Settings.TopRange = preferences.TopRange;
        Settings.Ratios = preferences.AspectRatios.FirstOrDefault(r => new[] { "16x9", "21x9", "9x16", "1x1" }.Contains(r)) ?? "";
        DataContext = null; DataContext = this;
        SaveLocal();
        ApiStatus = "已导入账户分类、内容级别、比例与热门范围";
        Notify("账户偏好已导入，点击“应用筛选并浏览”更新壁纸");
    });
    private static string MakeMask(IEnumerable<string> values, params string[] names) => string.Concat(names.Select(n => values.Contains(n) ? '1' : '0'));
    private async void ReadCollections_Click(object sender, RoutedEventArgs e) => await RunApiAsync(async () =>
    {
        var collections = await client.GetCollectionsAsync(lifetime.Token);
        SiteCollections.Clear(); foreach (var collection in collections) SiteCollections.Add(collection);
        if (SiteCollections.Count > 0) CollectionsBox.SelectedIndex = 0;
        ApiStatus = $"已读取 {SiteCollections.Count} 个站内收藏夹";
        SaveLocal();
    });
    private async void OpenCollection_Click(object sender, RoutedEventArgs e)
    {
        if (!CanUseApi) return;
        await GuardAsync(async () =>
        {
            if (CollectionsBox.SelectedItem is not SiteCollection collection) throw new WallhavenException("请先读取收藏夹并选择一个。");
            CollectSettings();
            string username = Settings.WallhavenUsername.Trim();
            searchBusy = true; BusyText = "正在读取站内收藏…"; Refresh();
            try
            {
                var result = await client.GetCollectionAsync(username, collection.Id, Settings.Purity, 1, lifetime.Token);
                imageCts?.Cancel(); thumbsCts?.Cancel();
                favoriteView = false; activeCollection = collection; collectionUsername = username; collectionPurity = Settings.Purity;
                page = result.CurrentPage; lastPage = result.LastPage; total = result.Total; seed = null;
                Items.Clear(); selectedIndex = -1; AddItems(result.Wallpapers); SaveLocal();
                OpenDrawer("gallery");
                if (Items.Count > 0) await SelectAsync(0);
                else Notify("收藏夹在当前内容筛选下为空，请调整筛选后重试。", true);
            }
            finally { searchBusy = false; Refresh(); }
            await LoadThumbnailsAsync();
        });
    }
    private void SystemThemeChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
    {
        if (Settings.Theme is "system" or "" or null && !exiting)
            Dispatcher.BeginInvoke(() => ThemeManager.Apply(Settings.Theme));
    }
    private void Theme_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (initialized && sender is ComboBox { SelectedValue: string value })
        {
            Settings.Theme = value;
            ThemeManager.Apply(value);
        }
    }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private async void Window_Activated(object sender, EventArgs e)
    {
        if (initialized && diagnosticsDirectory == null) await GuardAsync(RefreshDesktopStateAsync);
    }
    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        if (fullscreen) { ToggleFullscreen(); WindowState = WindowState.Maximized; }
        else WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        windowBounds.RefreshPlacement();
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void Fullscreen_Click(object? sender, RoutedEventArgs e) => ToggleFullscreen();
    private void ToggleFullscreen()
    {
        if (!fullscreen)
        {
            beforeFullscreen = WindowState;
            fullscreen = true;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Normal; WindowState = WindowState.Maximized;
        }
        else
        {
            fullscreen = false;
            WindowState = WindowState.Normal; ResizeMode = ResizeMode.CanResize; WindowState = beforeFullscreen;
        }
        windowBounds.RefreshPlacement();
    }
    private async void GalleryScroll_Changed(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0 && Items.Count > 24)
        {
            thumbnailViewportTimer.Stop(); thumbnailViewportTimer.Start();
        }
        if (scrollLoadArmed && e.VerticalChange > 0 && e.ExtentHeightChange == 0 && e.ViewportHeightChange == 0)
            await MaybeLoadMoreOnScrollAsync();
    }
    private async void GalleryScroll_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta < 0) { scrollLoadArmed = true; await MaybeLoadMoreOnScrollAsync(); }
    }
    private void GalleryScroll_MouseDown(object sender, MouseButtonEventArgs e)
    {
        for (DependencyObject? node = e.OriginalSource as DependencyObject; node != null && node != GalleryScroll;
             node = node is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node))
            if (node is System.Windows.Controls.Primitives.ScrollBar) { scrollLoadArmed = true; break; }
    }
    private void GalleryScroll_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.PageDown or Key.End or Key.Down) scrollLoadArmed = true;
    }
    private async Task MaybeLoadMoreOnScrollAsync()
    {
        if (!scrollLoadArmed || scrollLoadInProgress || locatingCurrent || GalleryPanel.Visibility != Visibility.Visible || !CanLoadMore ||
            GalleryScroll.ScrollableHeight - GalleryScroll.VerticalOffset > 220 ||
            DateTimeOffset.UtcNow - lastScrollLoad < TimeSpan.FromMilliseconds(600)) return;
        lastScrollLoad = DateTimeOffset.UtcNow;
        scrollLoadArmed = false; scrollLoadInProgress = true;
        try { await GuardAsync(LoadMoreAsync); }
        finally { scrollLoadArmed = false; scrollLoadInProgress = false; }
    }
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e) { if (initialized) UpdateLayoutMode(); }
    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBox || Keyboard.FocusedElement is ComboBox || Keyboard.FocusedElement is ComboBoxItem) return;
        if (e.Key == Key.Escape) { if (Drawer.Visibility == Visibility.Visible) CloseDrawer(); else if (fullscreen) ToggleFullscreen(); e.Handled = true; return; }
        if (e.Key == Key.F11) { ToggleFullscreen(); e.Handled = true; return; }
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Key == Key.S) { Download_Click(this, e); e.Handled = true; }
            else if (e.Key == Key.D || e.Key == Key.B) { SetWallpaper_Click(this, e); e.Handled = true; }
            else if (e.Key == Key.F) { FocusSearch_Click(this, e); e.Handled = true; }
            else if (e.Key == Key.L) { LocateCurrent_Click(this, e); e.Handled = true; }
            return;
        }
        if (Keyboard.Modifiers != ModifierKeys.None) return;
        if (e.Key == Key.F1) { Settings_Click(this, e); e.Handled = true; return; }
        if (e.Key == Key.F8) { if (ActualWidth < 924) WideWindow_Click(this, e); else NarrowWindow_Click(this, e); e.Handled = true; return; }
        if (e.Key == Key.Right || e.Key == Key.Space) { e.Handled = true; await GuardAsync(() => MoveAsync(1)); }
        else if (e.Key == Key.Left) { e.Handled = true; await GuardAsync(() => MoveAsync(-1)); }
        else if (e.Key == Key.F) { Favorite_Click(this, e); e.Handled = true; }
        else if (e.Key == Key.B) { Browse_Click(this, e); e.Handled = true; }
    }

    private async Task CaptureDiagnosticsAsync()
    {
        if (diagnosticsDirectory == null) return;
        Directory.CreateDirectory(diagnosticsDirectory);
        var checks = new Dictionary<string, object>();
        try
        {
            string nativeDesktopPath = NativeDesktop.GetWallpaperPath();
            checks["windowsDesktopPathReadable"] = string.IsNullOrWhiteSpace(nativeDesktopPath) || File.Exists(nativeDesktopPath);
            checks["realImageLoaded"] = originalPath != null && WallpaperImage.Source != null;
            checks["firstPageCount"] = Items.Count;
            await CaptureAsync("01-main.png");
            checks["compactInfoCard"] = InfoCard.ActualHeight < 160 && InfoCard.ActualWidth <= 640 && InfoCard.ActualHeight > 20;
            OpenDrawer("settings");
            await CaptureAsync("02-settings.png");
            checks["referencePaneWidth"] = Drawer.ActualWidth == 462;
            Width = 1000;
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            checks["referenceInlineThreshold"] = MainContent.Margin.Left == 462 && DrawerScrim.Visibility == Visibility.Collapsed;
            Width = 1360;
            bool wasFavorite = Current != null && favorites.Any(w => w.Id == Current.Id);
            Favorite_Click(this, new RoutedEventArgs());
            checks["favoritePersisted"] = Current != null && store.LoadFavorites().Any(w => w.Id == Current.Id) != wasFavorite;
            Favorite_Click(this, new RoutedEventArgs());
            checks["favoriteRestored"] = Current != null && store.LoadFavorites().Any(w => w.Id == Current.Id) == wasFavorite;
            Status = "";
            SaveSettings_Click(this, new RoutedEventArgs());
            checks["settingsPersisted"] = store.LoadSettings().Categories == Settings.Categories;
            Status = "";
            OpenDrawer("gallery");
            await LoadThumbnailsAsync();
            checks["loadedThumbnails"] = Items.Count(w => w.Thumbnail != null);
            await CaptureAsync("03-gallery.png");
            int firstCount = Items.Count;
            scrollLoadArmed = true;
            GalleryScroll.ScrollToEnd();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            for (int wait = 0; (searchBusy || scrollLoadInProgress) && wait < 200; wait++) await Task.Delay(150);
            if (Items.Count == firstCount) await MaybeLoadMoreOnScrollAsync();
            for (int wait = 0; (searchBusy || scrollLoadInProgress) && wait < 200; wait++) await Task.Delay(150);
            checks["pagination"] = Items.Count > firstCount;
            checks["scrollLoadsNextPage"] = Items.Count > firstCount;
            checks["scrollDoesNotChainRequests"] = Items.Count <= firstCount + 24;
            checks["nativeTitlebarDrag"] = System.Windows.Shell.WindowChrome.GetWindowChrome(this).CaptionHeight == 32 && System.Windows.Shell.WindowChrome.GetIsHitTestVisibleInChrome(TitleActions);
            int locatedCount = Items.Count;
            string? locatedId = Current?.Id;
            var displayedImage = WallpaperImage.Source;
            await LocateCurrentAsync();
            checks["locateKeepsImageAndList"] = Current?.Id == locatedId && WallpaperImage.Source == displayedImage && Items.Count == locatedCount;
            var currentRow = Items.Single(item => item.IsCurrent);
            var currentContainer = (FrameworkElement)GalleryItems.ItemContainerGenerator.ContainerFromItem(currentRow);
            var visibleBounds = currentContainer.TransformToAncestor(GalleryScroll).TransformBounds(new Rect(currentContainer.RenderSize));
            checks["currentThumbnailVisible"] = currentRow.Wallpaper.Id == locatedId && visibleBounds.Top >= 0 && visibleBounds.Bottom <= GalleryScroll.ActualHeight;
            await CaptureAsync("09-locate-current.png");
            int currentRowIndex = Items.IndexOf(currentRow);
            Items.Remove(currentRow);
            await LocateCurrentAsync();
            checks["locateOutsideList"] = HasPinnedWallpaper && PinnedItems[0].Wallpaper.Id == locatedId && Items.Count == locatedCount - 1;
            Items.Insert(currentRowIndex, currentRow);
            await LocateCurrentAsync();
            checks["locateRestoresListHighlight"] = !HasPinnedWallpaper && currentRow.IsCurrent;
            CloseDrawer();
            await MoveAsync(1);
            checks["nextWallpaper"] = selectedIndex == 1 && originalPath != null;
            await MoveAsync(-1);
            checks["previousWallpaper"] = selectedIndex == 0 && originalPath != null;
            await SetCurrentWallpaperAsync();
            string desktopImage = await DesktopImagePreparer.PrepareAsync(originalPath!, store.Root);
            checks["desktopImagePrepared"] = File.Exists(desktopImage);
            using (var locked = new FileStream(desktopImage, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                string reused = await DesktopImagePreparer.PrepareAsync(originalPath!, store.Root);
                checks["lockedDesktopReused"] = reused == desktopImage && locked.Length > 0;
                string other = await DesktopImagePreparer.PrepareAsync(cache.GetPath(Items[1].Wallpaper), store.Root);
                checks["lockedDesktopDifferentImage"] = other != desktopImage && File.Exists(other);
            }
            Status = "";
            await CaptureAsync("04-browsing.png");
            var actualDesktop = Items[1].Wallpaper;
            await rotation.ApplyManualAsync(Settings, actualDesktop, (_, ct) => ApplyDesktopAsync(cache.GetPath(actualDesktop), Settings.Fill, ct));
            observedDesktopPath = "";
            await RefreshDesktopStateAsync();
            checks["desktopSeparateFromPreview"] = Current?.Id != actualDesktop.Id && Settings.DesktopWallpaper?.Id == actualDesktop.Id;
            await LocateDesktopAsync();
            checks["locateActualDesktop"] = Current?.Id == actualDesktop.Id && Items.Single(item => item.IsCurrent).IsDesktop;
            await CaptureAsync("11-locate-desktop.png");
            var queued = Items[0].Wallpaper;
            Settings.NextDesktopWallpaper = queued;
            Settings.NextDesktopOptions = Settings.ToSearch() with { Sorting = "random" };
            nextReady = false;
            await PreviewNextDesktopAsync();
            checks["previewNextDoesNotChangeDesktop"] = Current?.Id == queued.Id && Settings.DesktopWallpaper?.Id == actualDesktop.Id && NextDesktopReady;
            await PreviewNextDesktopAsync();
            checks["nextPreviewStable"] = Settings.NextDesktopWallpaper?.Id == queued.Id;
            await CaptureAsync("10-desktop-and-next.png");
            await SwitchNextDesktopAsync();
            checks["desktopSwitchUsesPreviewedNext"] = Settings.DesktopWallpaper?.Id == queued.Id && Settings.NextDesktopWallpaper == null;
            Settings.DesktopWallpaper = null; Settings.DesktopHistory.Clear(); observedDesktopPath = "";
            await RefreshDesktopStateAsync();
            checks["recoverDesktopFromCache"] = Settings.DesktopWallpaper?.Id == queued.Id;
            Status = "";
            OpenDrawer("settings");
            string previousTheme = Settings.Theme;
            ThemeBox.SelectedValue = "dark";
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            checks["darkThemeSelector"] = Settings.Theme == "dark" && ((SolidColorBrush)Application.Current.Resources["TextBrush"]).Color == Colors.White;
            await CaptureAsync("06-dark-theme.png");
            ThemeBox.SelectedValue = "light";
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            checks["lightThemeSelector"] = Settings.Theme == "light" && ((SolidColorBrush)Application.Current.Resources["TextBrush"]).Color.R == 0;
            checks["buttonThemePalette"] = SetDesktopButton.Background == Application.Current.Resources["AccentBrush"] && ButtonPalette.GetHoverBackground(SetDesktopButton) == Application.Current.Resources["AccentHoverBrush"];
            await CaptureAsync("07-light-theme.png");
            ThemeBox.SelectedValue = previousTheme;
            const string testKey = "testkey0123456789testkey0123456789";
            keyVault.Save(testKey);
            checks["apiKeyEncryptedRoundTrip"] = keyVault.Load() == testKey && !System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(Path.Combine(store.Root, "api-key.dpapi"))).Contains(testKey);
            keyVault.Clear();
            checks["apiKeyRemoved"] = !File.Exists(Path.Combine(store.Root, "api-key.dpapi"));
            ApiExpander.IsExpanded = true;
            ApiExpander.BringIntoView();
            await CaptureAsync("08-api-settings.png");
            ApiExpander.IsExpanded = false;
            SettingsScroll.ScrollToTop();
            Width = 760;
            RootGrid.Width = 760;
            UpdateLayout();
            OpenDrawer("settings");
            await CaptureAsync("05-narrow.png");
            checks["narrowOverlay"] = MainContent.Margin.Left == 0 && DrawerScrim.Visibility == Visibility.Visible;
            RootGrid.Width = double.NaN;
            CloseDrawer();
            Maximize_Click(this, new RoutedEventArgs());
            await SettleWindowBoundsAsync();
            var maximized = windowBounds.Snapshot();
            checks["monitorDpi"] = windowBounds.Dpi;
            checks["maximizedWindowInsideWorkArea"] = maximized.Work.Contains(maximized.Window, 1);
            checks["maximizedClientInsideWorkArea"] = maximized.Work.Contains(maximized.Client, 1);
            var closeOrigin = CloseWindowButton.PointToScreen(new Point(0, 0));
            var closeEnd = CloseWindowButton.PointToScreen(new Point(CloseWindowButton.ActualWidth, CloseWindowButton.ActualHeight));
            checks["maximizedCloseButtonVisible"] = maximized.Work.Contains(new PixelRect((int)closeOrigin.X, (int)closeOrigin.Y, (int)Math.Ceiling(closeEnd.X), (int)Math.Ceiling(closeEnd.Y)), 1);
            checks["maximizedBounds"] = maximized.Window;
            checks["workAreaBounds"] = maximized.Work;
            await CaptureAsync("12-maximized.png");
            ToggleFullscreen();
            await SettleWindowBoundsAsync();
            var full = windowBounds.Snapshot();
            checks["fullscreenUsesMonitorBounds"] = full.Window == full.Monitor;
            ToggleFullscreen();
            await SettleWindowBoundsAsync();
            var exitedFull = windowBounds.Snapshot();
            checks["exitFullscreenRestoresWorkArea"] = WindowState == WindowState.Maximized && exitedFull.Work.Contains(exitedFull.Window, 1);
            Maximize_Click(this, new RoutedEventArgs());
            await SettleWindowBoundsAsync();
            var restored = windowBounds.Snapshot();
            checks["restoredWindowInsideWorkArea"] = WindowState == WindowState.Normal && restored.Work.Contains(restored.Window, 1);
            checks["desktopNotModified"] = true;
            await CheckPreviewRaceAsync(checks);
            await CheckPerformanceDiagnosticsAsync(checks);
            checks["status"] = "complete";
        }
        catch (Exception ex) { checks["error"] = ex.ToString(); }
        finally
        {
            File.WriteAllText(Path.Combine(diagnosticsDirectory, "results.json"), System.Text.Json.JsonSerializer.Serialize(checks, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            ExitApp();
        }
    }
    private async Task CheckPreviewRaceAsync(Dictionary<string, object> checks)
    {
        string slowPath = Path.Combine(store.Root, "race-preview.png");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, new byte[] { 0, 0, 255, 255 }, 4)));
        using (var file = File.Create(slowPath)) encoder.Save(file);
        var first = new Wallpaper { Id = "race01", LocalPath = slowPath, Purity = "sfw" };
        var latest = first with { Id = "race02" };
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int requests = 0;
        previewLoader = async (path, width) =>
        {
            if (Interlocked.Increment(ref requests) == 1) await release.Task;
            return await LoadBitmapAsync(path, width);
        };
        try
        {
            var stale = ShowStandaloneAsync(first, slowPath);
            await ShowStandaloneAsync(latest, slowPath);
            var image = WallpaperImage.Source;
            release.SetResult();
            checks["standalonePreviewLatestWins"] = !await stale && Current == latest && Settings.LastWallpaper == latest
                && ReferenceEquals(image, WallpaperImage.Source) && originalPath == slowPath && !imageBusy;
        }
        finally { release.TrySetResult(); previewLoader = LoadBitmapAsync; }

        int index = Items.ToList().FindIndex(item => File.Exists(cache.GetPath(item.Wallpaper)));
        if (index < 0) throw new InvalidOperationException("Race diagnostics need a cached list image.");
        var listWallpaper = Items[index].Wallpaper;
        release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        previewLoader = async (path, width) =>
        {
            if (path == slowPath) await release.Task;
            return await LoadBitmapAsync(path, width);
        };
        try
        {
            var stale = ShowStandaloneAsync(first, slowPath);
            checks["standalonePreviewDisablesActionsUntilLoaded"] = imageBusy && originalPath == null && !CanAct;
            await SelectAsync(index);
            var image = WallpaperImage.Source;
            release.SetResult();
            checks["listSelectionSupersedesStandalone"] = !await stale && Current == listWallpaper && Settings.LastWallpaper == listWallpaper
                && ReferenceEquals(image, WallpaperImage.Source) && originalPath == cache.GetPath(listWallpaper) && !imageBusy;
        }
        finally { release.TrySetResult(); previewLoader = LoadBitmapAsync; }
        string corrupt = Path.Combine(store.Root, "corrupt-preview.png");
        File.WriteAllBytes(corrupt, new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0 });
        checks["wpfRejectsTruncatedImage"] = false;
        try { ValidateCachedImage(corrupt); }
        catch (InvalidDataException) { checks["wpfRejectsTruncatedImage"] = true; }
        File.Delete(corrupt);
    }
    private async Task CheckPerformanceDiagnosticsAsync(Dictionary<string, object> checks)
    {
        string source = originalPath!;
        string path = Path.Combine(store.Root, "perf-image.png");
        File.Copy(source, path, true);
        long before = bitmapMemory.DecodeCount;
        ValidateCachedImage(path);
        var first = await LoadBitmapAsync(path, 2560);
        checks["validationAndPreviewShareDecode"] = bitmapMemory.DecodeCount == before + 1;
        long decoded = bitmapMemory.DecodeCount;
        var warmWatch = Stopwatch.StartNew();
        var second = await LoadBitmapAsync(path, 2560);
        checks["warmPreviewMilliseconds"] = warmWatch.Elapsed.TotalMilliseconds;
        checks["warmPreviewReusesBitmap"] = ReferenceEquals(first, second) && bitmapMemory.DecodeCount == decoded;
        File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddSeconds(2));
        var changed = await LoadBitmapAsync(path, 2560);
        checks["changedFileInvalidatesBitmap"] = !ReferenceEquals(first, changed) && bitmapMemory.DecodeCount == decoded + 1;

        string pending = Path.Combine(store.Root, "perf-transfer.part"), committed = Path.Combine(store.Root, "perf-committed.png");
        File.Copy(source, pending, true);
        ValidateCachedImage(pending);
        decoded = bitmapMemory.DecodeCount;
        File.Move(pending, committed, true);
        bitmapMemory.FileCommitted(pending, committed);
        await LoadBitmapAsync(committed, 2560);
        checks["downloadCommitRetainsDecodedBitmap"] = bitmapMemory.DecodeCount == decoded;

        var sizeProbe = await LoadBitmapAsync(path, 256);
        long bytes = (long)sizeProbe.PixelWidth * sizeProbe.PixelHeight * Math.Max(4, (sizeProbe.Format.BitsPerPixel + 7) / 8);
        var bounded = new BitmapMemoryCache(bytes * 2);
        await bounded.LoadAsync(path, 256);
        await bounded.LoadAsync(committed, 256);
        string third = Path.Combine(store.Root, "perf-third.png"); File.Copy(source, third, true);
        await bounded.LoadAsync(third, 256);
        checks["bitmapCacheEvictsLeastRecentlyUsed"] = bounded.TryGet(path, 256) == null && bounded.CachedBytes <= bytes * 2;
        checks["bitmapCacheBytes"] = bitmapMemory.CachedBytes;
        checks["bitmapCacheWithin96MiB"] = bitmapMemory.CachedBytes <= 96L * 1024 * 1024;

        var originalItems = Items.ToArray();
        var originalCurrent = Current;
        bool oldFavorites = favoriteView;
        try
        {
            favoriteView = true; scrollLoadArmed = false;
            Items.Clear();
            for (int i = 0; i < 1000; i++)
                Items.Add(new WallpaperItem(new Wallpaper { Id = i.ToString("x6"), LocalPath = source, Purity = "sfw", Resolution = "测试图片" }, false) { Thumbnail = sizeProbe });
            OpenDrawer("gallery");
            GalleryScroll.ScrollToTop();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            GalleryItems.UpdateLayout();
            var panel = FindVisualChild<VirtualizingTilePanel>(GalleryItems)!;
            checks["thousandItemsRealizedControls"] = panel.RealizedCount;
            checks["galleryVirtualizesThousandItems"] = panel.RealizedCount > 0 && panel.RealizedCount < 60;
            GalleryScroll.ScrollToEnd();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            checks["galleryVirtualizesAfterScroll"] = panel.RealizedCount > 0 && panel.RealizedCount < 60 && GalleryScroll.VerticalOffset > 10000;
            Current = Items[750].Wallpaper;
            await LocateCurrentAsync();
            var container = GalleryItems.ItemContainerGenerator.ContainerFromIndex(750) as FrameworkElement;
            var bounds = container?.TransformToAncestor(GalleryScroll).TransformBounds(new Rect(container.RenderSize));
            checks["virtualizedLocateRealizesOffscreenItem"] = bounds.HasValue && bounds.Value.Bottom > 0 && bounds.Value.Top < GalleryScroll.ViewportHeight;
            await CaptureAsync("13-virtualized-gallery.png");
        }
        finally
        {
            thumbnailViewportTimer.Stop();
            Items.Clear(); foreach (var item in originalItems) Items.Add(item);
            favoriteView = oldFavorites; Current = originalCurrent;
            GalleryScroll.ScrollToTop(); CloseDrawer(); Refresh();
        }
        long notifications = 0;
        PropertyChangedEventHandler observer = (_, _) => notifications++;
        Refresh(); PropertyChanged += observer;
        try { for (int i = 0; i < 100; i++) Refresh(); }
        finally { PropertyChanged -= observer; }
        checks["unchangedRefreshNotifications"] = notifications;
        checks["unchangedRefreshDoesNotRebind"] = notifications == 0;
    }
    private async Task CaptureAsync(string name)
    {
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        await Task.Delay(650);
        UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(RootGrid.ActualWidth), (int)Math.Ceiling(RootGrid.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(RootGrid);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(diagnosticsDirectory!, name));
        encoder.Save(file);
    }
    private async Task SettleWindowBoundsAsync()
    {
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        windowBounds.RefreshPlacement();
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        await Task.Delay(250);
        UpdateLayout();
    }
}
