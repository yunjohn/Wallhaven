using System;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Wallhaven.Desktop;

[SupportedOSPlatform("windows")]
internal sealed class WindowsStartup(string executablePath, string runKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run")
{
    private const string ValueName = "WallhavenDesktop";
    public const string Argument = "--startup";
    private string Command => $"\"{Path.GetFullPath(executablePath)}\" {Argument}";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(runKeyPath);
        return string.Equals(key?.GetValue(ValueName) as string, Command, StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            if (!File.Exists(executablePath)) throw new FileNotFoundException("找不到应用程序，请从固定位置运行后再启用开机启动。", executablePath);
            using var key = Registry.CurrentUser.CreateSubKey(runKeyPath, writable: true);
            key.SetValue(ValueName, Command, RegistryValueKind.String);
        }
        else
        {
            using var key = Registry.CurrentUser.OpenSubKey(runKeyPath, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
