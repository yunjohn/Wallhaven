using System.Windows;
using System.Windows.Media;

namespace Wallhaven.Desktop;

public static class ButtonPalette
{
    public static readonly DependencyProperty HoverBackgroundProperty = DependencyProperty.RegisterAttached("HoverBackground", typeof(Brush), typeof(ButtonPalette));
    public static readonly DependencyProperty PressedBackgroundProperty = DependencyProperty.RegisterAttached("PressedBackground", typeof(Brush), typeof(ButtonPalette));
    public static readonly DependencyProperty DisabledBackgroundProperty = DependencyProperty.RegisterAttached("DisabledBackground", typeof(Brush), typeof(ButtonPalette));
    public static readonly DependencyProperty DisabledForegroundProperty = DependencyProperty.RegisterAttached("DisabledForeground", typeof(Brush), typeof(ButtonPalette));
    public static Brush? GetHoverBackground(DependencyObject target) => (Brush?)target.GetValue(HoverBackgroundProperty);
    public static void SetHoverBackground(DependencyObject target, Brush value) => target.SetValue(HoverBackgroundProperty, value);
    public static Brush? GetPressedBackground(DependencyObject target) => (Brush?)target.GetValue(PressedBackgroundProperty);
    public static void SetPressedBackground(DependencyObject target, Brush value) => target.SetValue(PressedBackgroundProperty, value);
    public static Brush? GetDisabledBackground(DependencyObject target) => (Brush?)target.GetValue(DisabledBackgroundProperty);
    public static void SetDisabledBackground(DependencyObject target, Brush value) => target.SetValue(DisabledBackgroundProperty, value);
    public static Brush? GetDisabledForeground(DependencyObject target) => (Brush?)target.GetValue(DisabledForegroundProperty);
    public static void SetDisabledForeground(DependencyObject target, Brush value) => target.SetValue(DisabledForegroundProperty, value);
}
