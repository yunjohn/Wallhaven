using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Wallhaven.Desktop;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var exceptions = new List<Exception>();
        application.DispatcherUnhandledException += (_, e) => { exceptions.Add(e.Exception); e.Handled = true; };
        Window? window = null;
        int passed = 0;
        void Check(bool condition, string description)
        {
            if (exceptions.Count > 0) throw new AggregateException("Unhandled WPF layout exception", exceptions);
            if (!condition) throw new InvalidOperationException(description);
            Console.WriteLine("PASS " + description); passed++;
        }
        void Settle()
        {
            window!.UpdateLayout();
            application.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
        }
        try
        {
            var items = new ItemsControl
            {
                ItemsSource = Records(1000),
                ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingTilePanel)))
            };
            var scroll = new ScrollViewer { Content = items, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            window = new Window { Content = scroll, Width = 480, Height = 640, Opacity = 0, ShowActivated = false, ShowInTaskbar = false };
            window.Show(); Settle();
            var panel = FindPanel(items)!;
            Check(panel != null && panel.RealizedCount > 0 && panel.RealizedCount < 60, "a fresh items host renders a prepopulated filtered collection");
            for (int iteration = 0; iteration < 12; iteration++)
            {
                scroll.ScrollToEnd(); Settle();
                scroll.Visibility = Visibility.Collapsed;
                items.ItemsSource = Records(iteration % 3 == 0 ? 0 : iteration % 3 == 1 ? 24 : 500);
                // Applying filters also reapplies theme resources and can recreate the items host.
                var border = new FrameworkElementFactory(typeof(Border));
                border.AppendChild(new FrameworkElementFactory(typeof(ItemsPresenter)));
                items.Template = new ControlTemplate(typeof(ItemsControl)) { VisualTree = border };
                scroll.Visibility = Visibility.Visible;
                Settle();
                panel = FindPanel(items)!;
                Check(panel != null && (items.Items.Count == 0 ? panel.RealizedCount == 0 : panel.RealizedCount > 0 && panel.RealizedCount < 60), "filter replacement and template recreation " + iteration);
                ((ObservableCollection<string>)items.ItemsSource).Clear();
                Settle();
                Check(FindPanel(items)!.RealizedCount == 0, "clearing the filtered collection resets generated controls " + iteration);
            }
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { window?.Close(); application.Shutdown(); Console.WriteLine($"UI checks passed: {passed}"); }
    }
    private static ObservableCollection<string> Records(int count) => new(Enumerable.Range(0, count).Select(i => "Wallpaper " + i));
    private static VirtualizingTilePanel? FindPanel(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is VirtualizingTilePanel panel) return panel;
            if (FindPanel(child) is { } nested) return nested;
        }
        return null;
    }
}
