using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MnMMaps;

public partial class MainWindow : Window
{
    private static readonly DependencyPropertyKey IsMapOnlyPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsMapOnly), typeof(bool), typeof(MainWindow), new PropertyMetadata(false));
    public static readonly DependencyProperty IsMapOnlyProperty = IsMapOnlyPropertyKey.DependencyProperty;
    public bool IsMapOnly => (bool)GetValue(IsMapOnlyProperty);

    private const string Shortcuts = "Ctrl+Shift+M: show / hide   ·   Ctrl+Shift+L: click-through";
    private readonly MapStore store;
    private readonly MapImages images = new();
    private readonly List<MapEntry> catalog;
    private readonly System.Windows.Threading.DispatcherTimer saveTimer = new() { Interval = TimeSpan.FromMilliseconds(700) };
    private OverlayControls? controls;
    private CancellationTokenSource? imageCancellation;
    private MapEntry? current;
    private bool ready;
    private bool closing;
    private bool sidebarVisible;

    public MainWindow(MapStore store)
    {
        this.store = store;
        catalog = store.LoadCatalog();
        InitializeComponent();
        var settings = store.Settings;
        Width = double.IsFinite(settings.Width) ? Math.Clamp(settings.Width, MinWidth, 2400) : 880;
        Height = double.IsFinite(settings.Height) ? Math.Clamp(settings.Height, MinHeight, 1600) : 700;
        if (settings.Left is double left && settings.Top is double top && double.IsFinite(left) && double.IsFinite(top))
        {
            // Use current monitor bounds so a disconnected display cannot strand the overlay.
            var area = SystemParameters.WorkArea;
            Left = Math.Clamp(left, area.Left, Math.Max(area.Left, area.Right - Math.Min(Width, area.Width)));
            Top = Math.Clamp(top, area.Top, Math.Max(area.Top, area.Bottom - Math.Min(Height, area.Height)));
            WindowStartupLocation = WindowStartupLocation.Manual;
        }
        OpacitySlider.Value = double.IsFinite(settings.Opacity) ? Math.Clamp(settings.Opacity, 0.4, 1) : 0.95;
        Opacity = OpacitySlider.Value;
        TopmostBox.IsChecked = Topmost = settings.Topmost;
        MapPicker.ItemsSource = catalog;
        MapCanvas.ViewChanged += () => { ZoomText.Text = $"{MapCanvas.Zoom:P0} · scroll to zoom / drag to pan"; QueueSave(); };
        MapCanvas.PinRequested += AddPinAt;
        MapCanvas.PinSelected += pin => { ShowSidebar(true); PinList.SelectedItem = pin; };
        saveTimer.Tick += (_, _) => { saveTimer.Stop(); Save(); };
        LocationChanged += (_, _) => QueueSave();
        SizeChanged += (_, _) => QueueSave();
        SourceInitialized += (_, _) =>
        {
            controls = new OverlayControls(this, ToggleVisibility, ToggleLock, ShowUnlocked);
            LockButton.IsEnabled = controls.CanLock;
            if (!controls.CanToggle || !controls.CanLock)
                StatusText.Text = "A shortcut is already in use. The tray icon can show or hide the overlay; Lock needs Ctrl+Shift+L.";
        };
        Closing += Window_Closing;
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && MapCanvas.PinMode) { SetPinMode(false); e.Handled = true; }
        };
    }

    public async Task InitializeAsync()
    {
        var selected = catalog.FirstOrDefault(map => map.Id == store.Settings.SelectedMap) ?? catalog.First();
        MapPicker.SelectedItem = selected;
        ready = true;
        await LoadMapAsync(selected);
    }

    private async void Map_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ready && MapPicker.SelectedItem is MapEntry map) await LoadMapAsync(map);
    }

    private async Task LoadMapAsync(MapEntry map, bool refresh = false)
    {
        SaveView();
        imageCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        imageCancellation = cancellation;
        current = map;
        store.Settings.SelectedMap = map.Id;
        SourceButton.IsEnabled = !string.IsNullOrWhiteSpace(map.SourceUrl);
        CreditText.Text = map.Credit;
        CreditText.ToolTip = map.Credit;
        MapCanvas.SetMap(null, store.PinsFor(map.Id));
        AddPinButton.IsEnabled = false;
        AddPinButton.Content = "Add pin";
        UpdatePinList();
        EmptyTitle.Text = "Opening " + map.Name;
        EmptyDetail.Text = map.LocalFile is null ? "Loading the wiki image or its saved offline copy…" : "Loading your imported map…";
        EmptyPanel.Visibility = Visibility.Visible;
        RetryButton.Visibility = Visibility.Collapsed;
        try
        {
            var bitmap = await images.LoadAsync(map, store, refresh, cancellation.Token);
            if (cancellation.IsCancellationRequested || closing) return;
            store.Settings.Views.TryGetValue(map.Id, out var view);
            MapCanvas.SetMap(bitmap, store.PinsFor(map.Id), view);
            EmptyPanel.Visibility = Visibility.Collapsed;
            AddPinButton.IsEnabled = true;
            StatusText.Text = controls is { CanToggle: false } or { CanLock: false }
                ? "A shortcut is unavailable. Use the tray icon to show or hide MnM Maps." : Shortcuts;
            QueueSave();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception exception) when (!closing)
        {
            if (cancellation.IsCancellationRequested) return;
            EmptyTitle.Text = "Map unavailable";
            EmptyDetail.Text = "The image could not be opened. Retry when online, or use Import image to open a map saved on your PC.";
            EmptyDetail.ToolTip = exception.Message;
            RetryButton.Visibility = map.LocalFile is null ? Visibility.Visible : Visibility.Collapsed;
            StatusText.Text = "Download or image error: " + exception.Message;
        }
        finally
        {
            if (ReferenceEquals(imageCancellation, cancellation)) imageCancellation = null;
            cancellation.Dispose();
        }
    }

    private void QueueSave()
    {
        if (!ready || closing) return;
        saveTimer.Stop();
        saveTimer.Start();
    }

    private void SaveView()
    {
        if (current is not null && MapCanvas.HasImage) store.Settings.Views[current.Id] = MapCanvas.CaptureView();
    }

    private void Save()
    {
        SaveView();
        store.Settings.Width = Width;
        store.Settings.Height = Height;
        store.Settings.Left = double.IsFinite(Left) ? Left : null;
        store.Settings.Top = double.IsFinite(Top) ? Top : null;
        store.Settings.Opacity = OpacitySlider.Value;
        store.Settings.Topmost = Topmost;
        try { store.Save(); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { StatusText.Text = "Could not save settings: " + exception.Message; }
    }

    private void UpdatePinList()
    {
        var pins = current is null ? new List<MapPin>() : store.PinsFor(current.Id);
        PinList.ItemsSource = null;
        PinList.ItemsSource = pins;
        PinsButton.Content = $"Pins ({pins.Count})";
        PinNotes.Text = pins.Count == 0 ? "Use Add pin, then click a location on the map." : "";
        MapCanvas.InvalidateVisual();
    }

    private void ShowSidebar(bool show)
    {
        sidebarVisible = show;
        PinsColumn.Width = new GridLength(show ? 205 : 0);
    }

    private void SetPinMode(bool enabled)
    {
        MapCanvas.PinMode = enabled;
        AddPinButton.Content = enabled ? "Cancel pin" : "Add pin";
        StatusText.Text = enabled ? "Click a location on the map to add a pin. Escape cancels." : Shortcuts;
    }

    private void AddPinAt(Point position)
    {
        SetPinMode(false);
        if (current is null) return;
        var dialog = new PinEditor(this);
        if (dialog.ShowDialog() != true) return;
        var pin = new MapPin { Name = dialog.PinName, Notes = dialog.PinNotes, X = position.X, Y = position.Y };
        store.PinsFor(current.Id).Add(pin);
        UpdatePinList();
        ShowSidebar(true);
        PinList.SelectedItem = pin;
        Save();
    }

    private void Pin_Changed(object sender, SelectionChangedEventArgs e)
    {
        var pin = PinList.SelectedItem as MapPin;
        MapCanvas.SelectedPin = pin;
        PinNotes.Text = pin?.Notes ?? "";
        if (pin is not null) MapCanvas.CenterOn(pin);
        MapCanvas.InvalidateVisual();
    }

    private void EditPin_Click(object sender, RoutedEventArgs e)
    {
        if (PinList.SelectedItem is not MapPin pin) return;
        var dialog = new PinEditor(this, pin);
        if (dialog.ShowDialog() != true) return;
        pin.Name = dialog.PinName;
        pin.Notes = dialog.PinNotes;
        UpdatePinList();
        PinList.SelectedItem = pin;
        Save();
    }

    private void RemovePin_Click(object sender, RoutedEventArgs e)
    {
        if (current is null || PinList.SelectedItem is not MapPin pin) return;
        var result = MessageBox.Show(this, $"Remove the pin ‘{pin.Name}’?", "Remove personal pin", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;
        store.PinsFor(current.Id).Remove(pin);
        MapCanvas.SelectedPin = null;
        UpdatePinList();
        Save();
    }

    private void ToggleVisibility()
    {
        if (IsVisible) { Save(); Hide(); }
        else ShowUnlocked();
    }

    private void ShowUnlocked()
    {
        SetLocked(false);
        Show();
        Activate();
    }

    private void ToggleLock()
    {
        if (controls is null || !controls.CanLock) return;
        if (!IsVisible) Show();
        SetLocked(!controls.Locked);
    }

    private void SetLocked(bool locked)
    {
        if (controls is null || (locked && !controls.CanLock)) return;
        if (MapCanvas.PinMode) SetPinMode(false);
        SetValue(IsMapOnlyPropertyKey, locked);
        controls.SetLocked(locked);
    }

    private void Title_Drag(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source)
        {
            for (var element = source; element is not null; element = VisualTreeHelper.GetParent(element))
                if (element is Button) return;
        }
        if (e.ClickCount == 2) { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; return; }
        DragMove();
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e) => MapCanvas.ZoomBy(1.25);
    private void ZoomOut_Click(object sender, RoutedEventArgs e) => MapCanvas.ZoomBy(0.8);
    private void Fit_Click(object sender, RoutedEventArgs e) => MapCanvas.Fit();
    private void AddPin_Click(object sender, RoutedEventArgs e) => SetPinMode(!MapCanvas.PinMode);
    private void Pins_Click(object sender, RoutedEventArgs e) => ShowSidebar(!sidebarVisible);
    private void Lock_Click(object sender, RoutedEventArgs e) => ToggleLock();
    private void Hide_Click(object sender, RoutedEventArgs e) { Save(); Hide(); }
    private void Quit_Click(object sender, RoutedEventArgs e) => Close();
    private void Topmost_Click(object sender, RoutedEventArgs e) { Topmost = TopmostBox.IsChecked == true; QueueSave(); }
    private void Opacity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    { if (ready) { Opacity = e.NewValue; QueueSave(); } }
    private async void Retry_Click(object sender, RoutedEventArgs e) { if (current is not null) await LoadMapAsync(current, refresh: true); }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Import a map image", Filter = "Map images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.tif;*.tiff" };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            _ = MapImages.Read(dialog.FileName); // Validate before copying into our own storage.
            var map = store.Import(dialog.FileName);
            catalog.Add(map);
            MapPicker.Items.Refresh();
            // SelectionChanged starts the load; do not issue a second competing download.
            MapPicker.SelectedItem = map;
        }
        catch (Exception exception)
        { MessageBox.Show(this, exception.Message, "Could not import map", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void Source_Click(object sender, RoutedEventArgs e)
    {
        if (current is null || !Uri.TryCreate(current.SourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https") return;
        try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception exception) { StatusText.Text = "Could not open your browser: " + exception.Message; }
    }

    private void Help_Click(object sender, RoutedEventArgs e) => MessageBox.Show(this,
        "Select a map manually. Scroll to zoom; drag to pan; Fit resets the view.\n\n" +
        "Add pin, then click the map. Pins and notes belong to this app and are saved per map.\n\n" +
        "Ctrl+Shift+M: show / hide\nCtrl+Shift+L: map-only lock / restore controls\nLock hides the window controls and lets mouse clicks pass through. Your map position, zoom, and personal pins stay visible.\nThe tray menu's Unlock mouse and show also restores the controls. Closing the × quits; Hide keeps the app available.\n\n" +
        "Use the game in windowed or borderless mode. Exclusive fullscreen may cover the overlay.\n\n" +
        "Maps are downloaded from the community wiki's image server and cached for offline use. Wiki source opens each image's attribution and license page. Map images are not bundled with the executable.\n\n" +
        "This app does not access the game process, game files, screen contents, or game network traffic. No automatic location or zone tracking.\n\n" +
        "Interface ideas: Amy Lindley's Mo Betta Quests and Mo Betta Crafts. Independent implementation.\n\n" +
        "Settings, map cache and personal pins: " + store.Root,
        "MnM Maps · controls and sources", MessageBoxButton.OK, MessageBoxImage.Information);

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        Save();
        closing = true;
        ready = false;
        saveTimer.Stop();
        imageCancellation?.Cancel();
        controls?.Dispose();
        images.Dispose();
    }

    internal void PreparePreview(MapEntry map, System.Windows.Media.Imaging.BitmapSource bitmap)
    {
        current = map;
        MapPicker.SelectedItem = catalog.First(entry => entry.Id == map.Id);
        MapCanvas.SetMap(bitmap, store.PinsFor(map.Id));
        EmptyPanel.Visibility = Visibility.Collapsed;
        CreditText.Text = map.Credit;
        UpdatePinList();
        ShowSidebar(true);
    }
}
