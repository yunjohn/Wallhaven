namespace Wallhaven.Core;

public readonly record struct PixelRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;
    public bool Contains(PixelRect other, int tolerance = 0) => other.Left >= Left - tolerance && other.Top >= Top - tolerance && other.Right <= Right + tolerance && other.Bottom <= Bottom + tolerance;
}
public readonly record struct MaximizedPlacement(int X, int Y, int Width, int Height);
public static class WindowGeometry
{
    public static PixelRect Target(PixelRect monitor, PixelRect work, bool fullscreen)
    {
        if (monitor.Width <= 0 || monitor.Height <= 0) throw new ArgumentException("Invalid monitor bounds.");
        if (fullscreen) return monitor;
        var intersection = new PixelRect(Math.Max(monitor.Left, work.Left), Math.Max(monitor.Top, work.Top), Math.Min(monitor.Right, work.Right), Math.Min(monitor.Bottom, work.Bottom));
        return intersection.Width > 0 && intersection.Height > 0 ? intersection : monitor;
    }
    public static MaximizedPlacement Maximize(PixelRect monitor, PixelRect work, bool fullscreen)
    {
        var target = Target(monitor, work, fullscreen);
        // WM_GETMINMAXINFO uses monitor-relative offsets and physical pixels, not WPF DIPs.
        return new(target.Left - monitor.Left, target.Top - monitor.Top, target.Width, target.Height);
    }
    public static (double Width, double Height) LogicalSize(PixelRect physicalBounds, uint dpi)
    {
        double scale = dpi > 0 ? dpi / 96d : 1;
        return (physicalBounds.Width / scale, physicalBounds.Height / scale);
    }
    public static PixelRect FitNormal(PixelRect current, PixelRect work)
    {
        int width = Math.Clamp(current.Width, 1, work.Width), height = Math.Clamp(current.Height, 1, work.Height);
        int left = Math.Clamp(current.Left, work.Left, work.Right - width), top = Math.Clamp(current.Top, work.Top, work.Bottom - height);
        return new(left, top, left + width, top + height);
    }
}
