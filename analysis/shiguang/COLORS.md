# 配色核对与映射

2026-10-26，从拾光壁纸实际依赖的本机 `Microsoft.UI.Xaml.2.8_8.2501.31001.0_x64__8wekyb3d8bbwe/resources.pri` 提取 26 份 XAML。其 `Themes/21h1_themeresources.xaml` 中的颜色用于本应用的主题映射；背景模糊与光度混合没有等同实现。

| WinUI 资源 | 浅色 | 深色 |
| --- | --- | --- |
| TextFillColorPrimary | #E4000000 | #FFFFFFFF |
| TextFillColorSecondary | #9E000000 | #C5FFFFFF |
| TextFillColorTertiary | #72000000 | #87FFFFFF |
| ControlFillColorDefault | #B3FFFFFF | #0FFFFFFF |
| ControlFillColorSecondary | #80F9F9F9 | #15FFFFFF |
| CardBackgroundFillColorDefault | #B3FFFFFF | #0DFFFFFF |
| ControlStrokeColorDefault | #0F000000 | #12FFFFFF |
| AcrylicInAppFillColorDefaultBrush TintColor | #FFFCFCFC | #FF2C2C2C |
| AccentFillColorDefaultBrush | SystemAccentColorDark1 | SystemAccentColorLight2 |
| AccentTextFillColorPrimaryBrush | SystemAccentColorDark2 | SystemAccentColorLight3 |

`ThemeManager.cs` 按此映射动态资源。亚克力面板使用上述中性底色加半透明填充；其他控件使用源资源中的颜色和 alpha。Windows 强调色从当前用户 AccentPalette 读取；默认按 AppsUseLightTheme 跟随系统，应用内也可指定浅色或深色。

参考应用的设置资源本来就有 Light、Dark、System 三项。本次未能恢复其用户设置数据库中的实际选择，因此不把系统偏好当作已确认的原应用当前选项。新应用默认跟随系统，让用户可自行选择匹配主题。
