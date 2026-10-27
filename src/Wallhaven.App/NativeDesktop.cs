using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace Wallhaven.Desktop;

internal static partial class NativeDesktop
{
    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadWallpaper(uint action, uint parameter, StringBuilder value, uint flags);
    public static string GetWallpaperPath()
    {
        var path = new StringBuilder(32768);
        if (ReadWallpaper(0x0073, (uint)path.Capacity, path, 0)) return path.ToString();
        using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
        return key?.GetValue("WallPaper") as string ?? "";
    }
    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SystemParametersInfo(uint action, uint parameter, string value, uint flags);

    public static void SetWallpaper(string path, bool fill)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop");
        key.SetValue("WallpaperStyle", fill ? "10" : "6");
        key.SetValue("TileWallpaper", "0");
        if (!SystemParametersInfo(20, 0, path, 3)) throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows 未能设置桌面壁纸。");
    }
}
