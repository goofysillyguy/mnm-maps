using System;
using System.IO;
using System.Windows;

namespace MnMMaps;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args is ["--smoke-test", var output, var image])
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try { await SmokeTest.RunAsync(output, image); Shutdown(0); }
            catch (Exception exception)
            {
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "error.txt"), exception.ToString());
                Shutdown(1);
            }
            return;
        }

        try
        {
            var window = new MainWindow(new MapStore());
            MainWindow = window;
            window.Show();
            await window.InitializeAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "MnM Maps could not start", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
