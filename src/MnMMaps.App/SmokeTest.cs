using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MnMMaps;

internal static class SmokeTest
{
    // Render this app's visual tree to a file; never capture the desktop or show a window.
    public static async Task RunAsync(string output, string imageFile)
    {
        Directory.CreateDirectory(output);
        var store = new MapStore(Path.Combine(output, "app-data"));
        var maps = store.LoadCatalog();
        if (maps.Count < 12 || maps.Select(map => map.Id).Distinct().Count() != maps.Count)
            throw new InvalidOperationException("The map catalog is incomplete or has duplicate IDs.");
        var map = maps.Single(entry => entry.Id == "night-harbor");
        File.Copy(imageFile, store.CachedImagePath(map), overwrite: true);
        using var imageLoader = new MapImages();
        var image = await imageLoader.LoadAsync(map, store, refresh: false, CancellationToken.None);
        var pins = store.PinsFor(map.Id);
        pins.Clear();
        pins.Add(new MapPin { Name = "Meet at West Gate", Notes = "Example of a manually placed pin", X = 0.22, Y = 0.53 });
        pins.Add(new MapPin { Name = "Night Market", Notes = "Remember to buy supplies", X = 0.35, Y = 0.59 });
        var window = new MainWindow(store);
        window.PreparePreview(map, image);
        var content = (FrameworkElement)window.Content;
        content.Measure(new Size(880, 700));
        content.Arrange(new Rect(0, 0, 880, 700));
        content.UpdateLayout();
        var surface = (MapSurface)window.FindName("MapCanvas");
        var original = new Point(0.36, 0.47);
        var roundTrip = surface.ToMap(surface.ToScreen(original));
        if ((roundTrip - original).Length > 0.000001) throw new InvalidOperationException("Pin projection does not round-trip.");
        surface.ZoomBy(2);
        if (Math.Abs(surface.Zoom - 2) > 0.001) throw new InvalidOperationException("Zoom did not change.");
        surface.CenterOn(pins[0]);
        var center = surface.ToMap(new Point(surface.ActualWidth / 2, surface.ActualHeight / 2));
        if ((center - new Point(pins[0].X, pins[0].Y)).Length > 0.000001)
            throw new InvalidOperationException("Centering a pin did not preserve its image coordinates.");
        store.Settings.Views[map.Id] = surface.CaptureView();
        store.Save();
        var restored = new MapStore(store.Root);
        if (restored.PinsFor(map.Id).Count != 2 || restored.Settings.Views[map.Id].Zoom != store.Settings.Views[map.Id].Zoom)
            throw new InvalidOperationException("Pins or view did not survive a settings reload.");
        var imported = restored.Import(imageFile);
        if (MapImages.Read(imported.LocalFile!).PixelWidth != image.PixelWidth)
            throw new InvalidOperationException("The imported image changed.");
        surface.Fit();
        content.UpdateLayout();
        var render = new RenderTargetBitmap(880, 700, 96, 96, PixelFormats.Pbgra32);
        render.Render(content);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(render));
        using (var file = File.Create(Path.Combine(output, "overlay.png"))) encoder.Save(file);
        window.Close();
        File.WriteAllText(Path.Combine(output, "result.txt"),
            $"PASS: {maps.Count} map entries; raster image decoding; offline cache loading; pin projection; zoom; centering; settings and pin persistence; local image import; WPF rendering.\n" +
            "No visible window, game access, desktop capture, or writes to real app settings. Native hotkeys and mouse passthrough require an interactive check.\n");
    }
}
