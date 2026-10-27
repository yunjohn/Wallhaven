using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Wallhaven.Desktop;

/// <summary>Uses a small, frozen wallpaper snapshot so image transitions do not redraw live blur.</summary>
public sealed class BackdropMaterial : Grid
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(FrameworkElement), typeof(BackdropMaterial), new PropertyMetadata(null, SourceChanged));
    public static readonly DependencyProperty TintProperty = DependencyProperty.Register(nameof(Tint), typeof(Brush), typeof(BackdropMaterial), new PropertyMetadata(Brushes.Transparent, TintChanged));
    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(nameof(Image), typeof(ImageSource), typeof(BackdropMaterial), new PropertyMetadata(null, ImageChanged));
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(nameof(CornerRadius), typeof(double), typeof(BackdropMaterial), new PropertyMetadata(0d));
    private readonly Border blur = new() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, CacheMode = new BitmapCache(0.5), Effect = new BlurEffect { Radius = 28, RenderingBias = RenderingBias.Performance } };
    private readonly Border tint = new();
    private ImageBrush? wallpaper;
    private bool samplePending;
    private Rect lastViewbox = Rect.Empty;
    private Size lastSize;
    public FrameworkElement? Source { get => (FrameworkElement?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public Brush Tint { get => (Brush)GetValue(TintProperty); set => SetValue(TintProperty, value); }
    public ImageSource? Image { get => (ImageSource?)GetValue(ImageProperty); set => SetValue(ImageProperty, value); }
    public double CornerRadius { get => (double)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public bool HasWallpaperSample => wallpaper != null && !lastViewbox.IsEmpty;
    // A background must not contribute its sampled image size to the content's layout.
    protected override Size MeasureOverride(Size constraint)
    {
        base.MeasureOverride(constraint);
        return new Size(0, 0);
    }
    public BackdropMaterial()
    {
        IsHitTestVisible = false;
        Children.Add(blur); Children.Add(tint);
        Loaded += (_, _) => RefreshSample();
        SizeChanged += (_, _) => RefreshSample();
        IsVisibleChanged += (_, _) => { if (IsVisible) RefreshSample(); };
    }
    private static void TintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((BackdropMaterial)d).tint.Background = (Brush)e.NewValue;
    private static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var material = (BackdropMaterial)d;
        if (e.OldValue is FrameworkElement previous) previous.SizeChanged -= material.Source_SizeChanged;
        if (e.NewValue is FrameworkElement element) element.SizeChanged += material.Source_SizeChanged;
        material.RefreshSample();
    }
    private void Source_SizeChanged(object sender, SizeChangedEventArgs e) => RefreshSample();
    private static void ImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var material = (BackdropMaterial)d;
        var image = e.NewValue as ImageSource;
        if (image is BitmapSource bitmap && bitmap.PixelWidth > 480)
        {
            var reduced = new TransformedBitmap(bitmap, new ScaleTransform(480d / bitmap.PixelWidth, 480d / bitmap.PixelWidth));
            reduced.Freeze();
            image = reduced;
        }
        material.wallpaper = image == null ? null : new ImageBrush(image) { ViewboxUnits = BrushMappingMode.RelativeToBoundingBox, Stretch = Stretch.Fill };
        material.blur.Background = material.wallpaper;
        material.lastViewbox = Rect.Empty;
        material.RefreshSample();
    }
    public void RefreshSample()
    {
        if (samplePending) return;
        samplePending = true;
        Dispatcher.BeginInvoke(() => { samplePending = false; if (IsVisible) UpdateSample(); }, DispatcherPriority.Loaded);
    }
    private void UpdateSample()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        if (lastSize != RenderSize)
        {
            lastSize = RenderSize;
            Clip = new RectangleGeometry(new Rect(RenderSize), CornerRadius, CornerRadius);
            blur.Width = ActualWidth + 80; blur.Height = ActualHeight + 80; blur.Margin = new Thickness(-40, -40, 0, 0);
        }
        if (wallpaper == null || Source == null || PresentationSource.FromVisual(this) == null || PresentationSource.FromVisual(Source) == null) return;
        try
        {
            if (Image == null || Image.Width <= 0 || Image.Height <= 0 || Source.ActualWidth <= 0 || Source.ActualHeight <= 0) return;
            Point position = TranslatePoint(new Point(0, 0), Source);
            double scale = Math.Max(Source.ActualWidth / Image.Width, Source.ActualHeight / Image.Height);
            double scaledWidth = Image.Width * scale, scaledHeight = Image.Height * scale;
            var crop = new Rect((position.X - 40 + (scaledWidth - Source.ActualWidth) / 2) / scaledWidth,
                (position.Y - 40 + (scaledHeight - Source.ActualHeight) / 2) / scaledHeight,
                (ActualWidth + 80) / scaledWidth, (ActualHeight + 80) / scaledHeight);
            if (lastViewbox != crop) { lastViewbox = crop; wallpaper.Viewbox = crop; }
        }
        catch (InvalidOperationException) { /* A popup can be between presentation sources while opening. */ }
    }
}
