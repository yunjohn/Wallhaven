using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Wallhaven.Core;

namespace Wallhaven.Desktop;

public partial class App : Application
{
    private Mutex? mutex;
    private EventWaitHandle? wake;
    private RegisteredWaitHandle? wait;
    private void App_Startup(object sender, StartupEventArgs e)
    {
        string? diagnostics = null;
        int captureIndex = Array.IndexOf(e.Args, "--diagnostics");
        if (captureIndex >= 0 && captureIndex + 1 < e.Args.Length) diagnostics = Path.GetFullPath(e.Args[captureIndex + 1]);
        bool startInTray = diagnostics == null && e.Args.Contains(WindowsStartup.Argument);
        string instanceSuffix = diagnostics == null ? "" : ".Diagnostics";
        mutex = new Mutex(true, "Local\\WallhavenDesktop.App" + instanceSuffix, out bool first);
        wake = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\WallhavenDesktop.Wake" + instanceSuffix);
        if (!first) { if (!startInTray) wake.Set(); Shutdown(); return; }
        DispatcherUnhandledException += OnUnhandled;
        var window = new MainWindow(diagnostics == null ? null : Path.Combine(diagnostics, "test-data"), diagnostics);
        MainWindow = window;
        wait = ThreadPool.RegisterWaitForSingleObject(wake, (_, _) => Dispatcher.BeginInvoke(window.RestoreWindow), null, -1, false);
        if (startInTray)
        {
            window.StartInTray();
        }
        else window.Show();
    }
    private void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            var root = LocalStore.DefaultRoot;
            Directory.CreateDirectory(root);
            File.AppendAllText(Path.Combine(root, "errors.log"), $"{DateTimeOffset.Now:u} {e.Exception}\n");
        }
        catch { }
        MessageBox.Show("操作未完成：" + e.Exception.Message, "Wallhaven", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }
    protected override void OnExit(ExitEventArgs e)
    {
        wait?.Unregister(null);
        wake?.Dispose();
        mutex?.Dispose();
        base.OnExit(e);
    }
}
