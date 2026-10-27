using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Wallhaven.Desktop;

/// <summary>Two fixed-height columns; the outer pixel scroll viewer keeps its existing behavior.</summary>
public sealed class VirtualizingTilePanel : VirtualizingPanel
{
    public const double RowHeight = 144;
    private ScrollViewer? scroll;
    public int RealizedCount => InternalChildren.Count;
    public VirtualizingTilePanel()
    {
        Loaded += (_, _) =>
        {
            for (DependencyObject? parent = VisualTreeHelper.GetParent(this); parent != null; parent = VisualTreeHelper.GetParent(parent))
            {
                if (parent is not ScrollViewer owner) continue;
                scroll = owner;
                scroll.ScrollChanged += ScrollChanged;
                InvalidateMeasure();
                break;
            }
        };
        Unloaded += (_, _) => { if (scroll != null) scroll.ScrollChanged -= ScrollChanged; scroll = null; };
    }
    private void ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0 || e.ViewportHeightChange != 0 || e.ExtentHeightChange != 0) InvalidateMeasure();
    }
    public double ItemOffset(int index)
    {
        double top = 0;
        if (scroll?.Content is UIElement content) top = TranslatePoint(new Point(), content).Y;
        return top + index / 2 * RowHeight;
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var owner = ItemsControl.GetItemsOwner(this);
        int count = owner?.Items.Count ?? 0;
        double width = double.IsInfinity(availableSize.Width) ? scroll?.ViewportWidth ?? 400 : availableSize.Width;
        double viewport = scroll?.ViewportHeight > 0 ? scroll.ViewportHeight : 720;
        double offset = Math.Max(0, (scroll?.VerticalOffset ?? 0) - ItemOffset(0));
        int first = Math.Min(Math.Max(0, count - 1) / 2, Math.Max(0, (int)(offset / RowHeight) - 1)) * 2;
        int last = Math.Min(count - 1, ((int)Math.Ceiling((offset + viewport) / RowHeight) + 1) * 2 + 1);
        var generator = ItemContainerGenerator;
        // Release offscreen controls before creating the new viewport.
        for (int i = InternalChildren.Count - 1; i >= 0; i--)
        {
            var position = new GeneratorPosition(i, 0);
            int item = generator.IndexFromGeneratorPosition(position);
            if (item >= first && item <= last) continue;
            generator.Remove(position, 1);
            RemoveInternalChildRange(i, 1);
        }
        if (count > 0)
        {
            var position = generator.GeneratorPositionFromIndex(first);
            int childIndex = position.Offset == 0 ? position.Index : position.Index + 1;
            using (generator.StartAt(position, GeneratorDirection.Forward, true))
            {
                for (int index = first; index <= last; index++, childIndex++)
                {
                    var child = (UIElement)generator.GenerateNext(out bool created);
                    if (created)
                    {
                        InsertInternalChild(childIndex, child);
                        generator.PrepareItemContainer(child);
                    }
                    child.Measure(new Size(width / 2, RowHeight));
                }
            }
        }
        return new Size(width, Math.Ceiling(count / 2d) * RowHeight);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        for (int i = 0; i < InternalChildren.Count; i++)
        {
            int index = ItemContainerGenerator.IndexFromGeneratorPosition(new GeneratorPosition(i, 0));
            InternalChildren[i].Arrange(new Rect(index % 2 * finalSize.Width / 2, index / 2 * RowHeight, finalSize.Width / 2, RowHeight));
        }
        return finalSize;
    }
    protected override void OnItemsChanged(object sender, ItemsChangedEventArgs args)
    {
        base.OnItemsChanged(sender, args);
        ItemContainerGenerator.RemoveAll();
        if (InternalChildren.Count > 0) RemoveInternalChildRange(0, InternalChildren.Count);
        InvalidateMeasure();
    }
}
