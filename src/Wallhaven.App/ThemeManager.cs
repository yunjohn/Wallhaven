using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace Wallhaven.Desktop;

internal static class ThemeManager
{
    // WinUI 2.8's 21h1_themeresources.xaml, used by the installed reference app.
    public static bool Apply(string? preference)
    {
        bool dark = preference == "dark" || preference != "light" && !AppsUseLightTheme();
        var colors = new Dictionary<string, string>
        {
            ["TextBrush"] = dark ? "#FFFFFFFF" : "#E4000000",
            ["MutedBrush"] = dark ? "#C5FFFFFF" : "#9E000000",
            ["TertiaryBrush"] = dark ? "#87FFFFFF" : "#72000000",
            ["PanelBrush"] = dark ? "#F22C2C2C" : "#F2FCFCFC",
            ["AcrylicTintBrush"] = dark ? "#B32C2C2C" : "#BFFCFCFC",
            ["CardBrush"] = dark ? "#0DFFFFFF" : "#B3FFFFFF",
            ["ControlBrush"] = dark ? "#0FFFFFFF" : "#B3FFFFFF",
            ["ControlHoverBrush"] = dark ? "#15FFFFFF" : "#80F9F9F9",
            ["ControlPressedBrush"] = dark ? "#08FFFFFF" : "#4DF9F9F9",
            ["ControlDisabledBrush"] = dark ? "#0BFFFFFF" : "#4DF9F9F9",
            ["TextDisabledBrush"] = dark ? "#5DFFFFFF" : "#5C000000",
            ["AccentDisabledBrush"] = dark ? "#28FFFFFF" : "#37000000",
            ["OnAccentDisabledBrush"] = dark ? "#87FFFFFF" : "#FFFFFFFF",
            ["InputBrush"] = dark ? "#B31E1E1E" : "#B3FFFFFF",
            ["BorderBrush"] = dark ? "#12FFFFFF" : "#0F000000",
            ["PopupBrush"] = dark ? "#FF2C2C2C" : "#FFF9F9F9",
            ["BaseBrush"] = dark ? "#FF202020" : "#FFF3F3F3",
            ["OnAccentBrush"] = dark ? "#FF000000" : "#FFFFFFFF",
            ["BackdropShadeBrush"] = "#00000000"
        };
        foreach (var pair in colors) Set(pair.Key, (Color)ColorConverter.ConvertFromString(pair.Value));
        Set("AccentBrush", Accent(dark ? 1 : 4, dark ? "#99EBFF" : "#005A9E"));
        Set("AccentHoverBrush", Accent(dark ? 1 : 4, dark ? "#99EBFF" : "#005A9E"), 0.9);
        Set("AccentPressedBrush", Accent(dark ? 1 : 4, dark ? "#99EBFF" : "#005A9E"), 0.8);
        Set("AccentTextBrush", Accent(dark ? 0 : 5, dark ? "#B3F0FF" : "#003E73"));
        return dark;
    }
    private static void Set(string key, Color color, double opacity = 1)
    {
        var brush = new SolidColorBrush(color) { Opacity = opacity }; brush.Freeze();
        Application.Current.Resources[key] = brush;
    }
    private static bool AppsUseLightTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is not int value || value != 0;
    }
    private static Color Accent(int index, string fallback)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent");
        if (key?.GetValue("AccentPalette") is byte[] bytes && bytes.Length >= (index + 1) * 4)
            return Color.FromRgb(bytes[index * 4], bytes[index * 4 + 1], bytes[index * 4 + 2]);
        return (Color)ColorConverter.ConvertFromString(fallback);
    }
}
