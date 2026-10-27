using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;
using System.Windows.Threading;
using Wallhaven.Core;

namespace Wallhaven.Desktop;

internal sealed class NativeWindowBounds : IDisposable
{
    private readonly Window window;
    private readonly Func<bool> fullscreen;
    private HwndSource? source;
    private IntPtr handle;
    private bool pending, disposed;
    public NativeWindowBounds(Window window, Func<bool> fullscreen)
    {
        this.window = window; this.fullscreen = fullscreen;
        window.SourceInitialized += SourceInitialized;
        window.StateChanged += StateChanged;
        window.Loaded += Loaded;
    }
    private void SourceInitialized(object? sender, EventArgs e)
    {
        handle = new WindowInteropHelper(window).Handle;
        source = HwndSource.FromHwnd(handle);
        source?.AddHook(WindowProc);
        ClampLogicalMinimum();
        var info = Monitor();
        var size = WindowGeometry.LogicalSize(info.Work.ToPixels(), Dpi);
        if (!double.IsNaN(window.Width)) window.Width = Math.Min(window.Width, size.Width);
        if (!double.IsNaN(window.Height)) window.Height = Math.Min(window.Height, size.Height);
    }
    private void Loaded(object sender, RoutedEventArgs e) => RefreshPlacement();
    private void StateChanged(object? sender, EventArgs e)
    {
        var chrome = WindowChrome.GetWindowChrome(window);
        if (chrome != null)
        {
            if (chrome.IsFrozen) { chrome = (WindowChrome)chrome.Clone(); WindowChrome.SetWindowChrome(window, chrome); }
            chrome.ResizeBorderThickness = new Thickness(window.WindowState == WindowState.Maximized ? 0 : 6);
        }
        RefreshPlacement();
    }
    private IntPtr WindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == 0x0024 && lParam != IntPtr.Zero) // WM_GETMINMAXINFO
        {
            var monitor = Monitor();
            var target = WindowGeometry.Maximize(monitor.Monitor.ToPixels(), monitor.Work.ToPixels(), fullscreen());
            var limits = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            limits.MaxPosition = new(target.X, target.Y);
            limits.MaxSize = new(target.Width, target.Height);
            limits.MinTrackSize.X = Math.Min(limits.MinTrackSize.X, target.Width);
            limits.MinTrackSize.Y = Math.Min(limits.MinTrackSize.Y, target.Height);
            limits.MaxTrackSize.X = Math.Max(limits.MaxTrackSize.X, target.Width);
            limits.MaxTrackSize.Y = Math.Max(limits.MaxTrackSize.Y, target.Height);
            Marshal.StructureToPtr(limits, lParam, false);
            handled = true;
        }
        else if (message is 0x02E0 or 0x007E or 0x001A) // DPI / display / work-area change
            RefreshPlacement();
        return IntPtr.Zero;
    }
    public void RefreshPlacement()
    {
        if (pending || disposed || handle == IntPtr.Zero) return;
        pending = true;
        window.Dispatcher.BeginInvoke(() =>
        {
            pending = false;
            if (disposed || window.WindowState == WindowState.Minimized) return;
            ClampLogicalMinimum();
            var info = Monitor();
            PixelRect target;
            if (window.WindowState == WindowState.Maximized)
                target = WindowGeometry.Target(info.Monitor.ToPixels(), info.Work.ToPixels(), fullscreen());
            else
            {
                if (!GetWindowRect(handle, out var rect)) return;
                target = WindowGeometry.FitNormal(rect.ToPixels(), info.Work.ToPixels());
            }
            if (GetWindowRect(handle, out var current) && current.ToPixels() == target) return;
            SetWindowPos(handle, IntPtr.Zero, target.Left, target.Top, target.Width, target.Height, 0x0014); // no z-order or activation
        }, DispatcherPriority.Loaded);
    }
    private void ClampLogicalMinimum()
    {
        var size = WindowGeometry.LogicalSize(Monitor().Work.ToPixels(), Dpi);
        window.MinWidth = Math.Min(604, size.Width);
        window.MinHeight = Math.Min(540, size.Height);
    }
    private MonitorInfo Monitor()
    {
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(handle, 2), ref info)) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        return info;
    }
    public uint Dpi => handle == IntPtr.Zero ? 96 : GetDpiForWindow(handle);
    public (PixelRect Window, PixelRect Client, PixelRect Monitor, PixelRect Work) Snapshot()
    {
        var monitor = Monitor();
        if (!GetWindowRect(handle, out var rect) || !GetClientRect(handle, out var client)) throw new InvalidOperationException("Cannot read window bounds.");
        var origin = new NativePoint(0, 0); ClientToScreen(handle, ref origin);
        return (rect.ToPixels(), new(origin.X, origin.Y, origin.X + client.Right, origin.Y + client.Bottom), monitor.Monitor.ToPixels(), monitor.Work.ToPixels());
    }
    public void Dispose()
    {
        disposed = true; source?.RemoveHook(WindowProc);
        window.SourceInitialized -= SourceInitialized; window.StateChanged -= StateChanged; window.Loaded -= Loaded;
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint(int x, int y) { public int X = x; public int Y = y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
        public readonly PixelRect ToPixels() => new(Left, Top, Right, Bottom);
    }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [StructLayout(LayoutKind.Sequential)] private struct MinMaxInfo { public NativePoint Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetClientRect(IntPtr hwnd, out NativeRect rect);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ClientToScreen(IntPtr hwnd, ref NativePoint point);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
}
