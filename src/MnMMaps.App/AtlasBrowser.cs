using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace MnMMaps;

// Hosts the published site. The only page customization hides its controls while locked.
public sealed class AtlasBrowser : Grid, IDisposable
{
    public const string HomeUrl = "https://www.mnmatlas.com/";
    public const string InitialUrl = "https://www.mnmatlas.com/?level=surface&x=1382.95&y=1272&z=2.01";
    public const string RuntimeUrl = "https://developer.microsoft.com/en-us/microsoft-edge/webview2/consumer/";
    private WebView2CompositionControl? browser;
    private CoreWebView2Environment? environment;
    private Task? initialization;
    private readonly List<Window> popups = [];
    private readonly Border notice;
    private readonly TextBlock noticeTitle;
    private readonly TextBlock noticeDetail;
    private readonly StackPanel noticeActions;
    private string profileDirectory = "";
    private bool disposed;
    private bool locked;
    private bool signingIn;
    private bool hostVisible = true;
    public string Address { get; private set; } = InitialUrl;
    public bool HasBrowser => browser?.CoreWebView2 is not null;
    public bool CanGoBack => browser?.CanGoBack == true;
    public bool CanGoForward => browser?.CanGoForward == true;
    public event Action<string>? AddressChanged;
    public event Action<string>? StatusChanged;
    public event Action? HistoryChanged;
    public event Action<bool>? FullScreenChanged;

    public AtlasBrowser()
    {
        Background = Brushes.Transparent;
        noticeTitle = new TextBlock { FontSize = 19, Foreground = Brushes.Wheat, TextAlignment = TextAlignment.Center };
        noticeDetail = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 14), TextAlignment = TextAlignment.Center };
        noticeActions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        var retry = new Button { Content = "Retry Atlas" };
        retry.Click += async (_, _) => await OpenAsync(Address, profileDirectory);
        var runtime = new Button { Content = "Get WebView2 Runtime ↗" };
        runtime.Click += (_, _) => OpenExternal(RuntimeUrl);
        noticeActions.Children.Add(retry);
        noticeActions.Children.Add(runtime);
        var content = new StackPanel();
        content.Children.Add(noticeTitle);
        content.Children.Add(noticeDetail);
        content.Children.Add(noticeActions);
        notice = new Border { Child = content, Padding = new Thickness(25), MaxWidth = 480,
            Background = new SolidColorBrush(Color.FromRgb(29, 35, 41)), CornerRadius = new CornerRadius(8),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        Children.Add(notice);
        ShowNotice("Opening MnM Atlas", "Loading the interactive community atlas…", false);
    }

    public static bool TryAddress(string? text, out Uri uri)
    {
        uri = null!;
        if (!Uri.TryCreate(text?.Trim(), UriKind.Absolute, out var parsed) || parsed.Scheme != "https" ||
            !parsed.IsDefaultPort || parsed.UserInfo.Length != 0 ||
            (parsed.Host != "www.mnmatlas.com" && parsed.Host != "mnmatlas.com")) return false;
        var builder = new UriBuilder(parsed) { Host = "www.mnmatlas.com" };
        var query = HttpUtility.ParseQueryString(builder.Query);
        // Load the complete site, including its own Share/Embed menus. Authentication
        // callbacks stay inside WebView2; temporary auth fields never become settings.
        foreach (var key in new[] { "embed", "code", "access_token", "refresh_token", "error", "error_code", "error_description" }) query.Remove(key);
        builder.Query = query.ToString();
        if (builder.Fragment.Contains("access_token", StringComparison.OrdinalIgnoreCase) ||
            builder.Fragment.Contains("refresh_token", StringComparison.OrdinalIgnoreCase)) builder.Fragment = "";
        uri = builder.Uri;
        return true;
    }

    public async Task OpenAsync(string address, string profile)
    {
        if (disposed) return;
        if (!TryAddress(address, out var uri))
        {
            StatusChanged?.Invoke("Paste an HTTPS map or place link from www.mnmatlas.com.");
            return;
        }
        Address = uri.AbsoluteUri;
        profileDirectory = profile;
        if (initialization is not null) { await initialization; return; }
        if (browser?.CoreWebView2 is not null) { Command(core => core.Navigate(Address)); return; }
        initialization = InitializeBrowserAsync();
        try { await initialization; }
        finally { initialization = null; }
    }

    private async Task InitializeBrowserAsync()
    {
        ShowNotice("Opening MnM Atlas", "Loading the interactive community atlas…", false);
        try
        {
            Directory.CreateDirectory(profileDirectory);
            environment ??= await CoreWebView2Environment.CreateAsync(userDataFolder: profileDirectory);
            if (disposed) return;
            browser = CreateControl();
            Children.Insert(0, browser);
            await browser.EnsureCoreWebView2Async(environment);
            if (disposed) return;
            var core = browser.CoreWebView2;
            Configure(core, () => Window.GetWindow(this));
            core.NavigationStarting += (_sender, e) =>
            {
                if (!TryAddress(e.Uri, out _))
                {
                    if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var target) && target.Scheme == "https" &&
                        (signingIn || (target.Host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase) && target.AbsolutePath == "/auth/v1/authorize")))
                    {
                        signingIn = true;
                        notice.Visibility = Visibility.Collapsed;
                        StatusChanged?.Invoke("Atlas sign-in: " + target.Host + " · Alt+Left returns to the map.");
                        return;
                    }
                    e.Cancel = true;
                    if (e.IsUserInitiated) OpenExternal(e.Uri);
                    return;
                }
                signingIn = false;
                ShowNotice("Opening MnM Atlas", "Loading the selected page…", false);
                StatusChanged?.Invoke("Loading Atlas…");
            };
            core.SourceChanged += (_, _) =>
            {
                if (!TryAddress(core.Source, out var uri)) return;
                Address = uri.AbsoluteUri;
                AddressChanged?.Invoke(Address);
            };
            core.HistoryChanged += (_, _) => HistoryChanged?.Invoke();
            core.DOMContentLoaded += async (_, _) => await ApplyLockAsync();
            core.NavigationCompleted += async (_, e) =>
            {
                if (disposed) return;
                if (signingIn) return;
                if (e.IsSuccess)
                {
                    notice.Visibility = Visibility.Collapsed;
                    await ApplyLockAsync();
                    StatusChanged?.Invoke("Atlas ready · Ctrl+F: search · Ctrl+L: map address · F5: reload");
                }
                else if (e.WebErrorStatus != CoreWebView2WebErrorStatus.OperationCanceled)
                {
                    ShowNotice("Atlas could not load", "Check your internet connection and retry. Wiki maps remain available for cached or imported maps.", true);
                    StatusChanged?.Invoke("Atlas navigation error: " + e.WebErrorStatus);
                }
            };
            core.ContainsFullScreenElementChanged += (_, _) => FullScreenChanged?.Invoke(core.ContainsFullScreenElement);
            core.ProcessFailed += (_, e) => Dispatcher.BeginInvoke(() =>
            {
                if (disposed) return;
                ResetBrowser();
                ShowNotice("Atlas browser stopped", "Select Retry Atlas to reopen the page. Saved browser notes remain in the profile folder.", true);
                StatusChanged?.Invoke("Atlas browser error: " + e.ProcessFailedKind);
            });
            await core.AddScriptToExecuteOnDocumentCreatedAsync(LockStyles);
            if (!disposed) core.Navigate(Address);
        }
        catch (Exception exception)
        {
            if (disposed) return;
            ResetBrowser();
            ShowNotice("Atlas browser unavailable", "Install Microsoft Edge WebView2 Runtime if it is missing, then retry. You can also use Wiki maps.", true);
            notice.ToolTip = exception.Message;
            StatusChanged?.Invoke("Could not start Atlas: " + exception.Message);
        }
    }

    private static WebView2CompositionControl CreateControl() => new()
    {
        DefaultBackgroundColor = System.Drawing.Color.Transparent,
        AllowExternalDrop = true,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch
    };

    private void Configure(CoreWebView2 core, Func<Window?> owner)
    {
        core.Settings.AreDefaultContextMenusEnabled = true;
        core.Settings.AreBrowserAcceleratorKeysEnabled = true;
        core.Settings.IsStatusBarEnabled = false;
        core.NewWindowRequested += async (_sender, e) =>
        {
            var deferral = e.GetDeferral();
            e.Handled = true;
            try
            {
                if (disposed) return;
                // Keep same-origin windows in this profile so the site's sign-in popup can
                // retain its opener and finish normally. Other links use the default browser.
                if (e.Uri != "about:blank" && e.Uri.Length != 0 && !TryAddress(e.Uri, out _))
                {
                    if (e.IsUserInitiated) OpenExternal(e.Uri);
                    return;
                }
                var parent = owner();
                var childBrowser = CreateControl();
                var popup = new Window { Title = "MnM Atlas", Width = 860, Height = 700, MinWidth = 400, MinHeight = 350,
                    Content = childBrowser, Background = new SolidColorBrush(Color.FromRgb(25, 24, 23)), Icon = parent?.Icon, Topmost = parent?.Topmost == true,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false };
                if (parent is { IsVisible: true }) popup.Owner = parent;
                popups.Add(popup);
                popup.Closed += (_, _) => { popups.Remove(popup); childBrowser.Dispose(); };
                popup.Show();
                if (locked || !hostVisible) popup.Hide();
                await childBrowser.EnsureCoreWebView2Async(environment);
                if (disposed || !popups.Contains(popup)) return;
                Configure(childBrowser.CoreWebView2, () => popup);
                childBrowser.CoreWebView2.NavigationStarting += (_, navigation) =>
                {
                    if (navigation.Uri != "about:blank" && (!Uri.TryCreate(navigation.Uri, UriKind.Absolute, out var target) || target.Scheme != "https"))
                        navigation.Cancel = true;
                };
                childBrowser.CoreWebView2.DocumentTitleChanged += (_, _) => popup.Title = childBrowser.CoreWebView2.DocumentTitle;
                childBrowser.CoreWebView2.WindowCloseRequested += (_, _) => popup.Dispatcher.BeginInvoke(() => { if (popups.Contains(popup)) popup.Close(); });
                e.NewWindow = childBrowser.CoreWebView2;
                if (popup.IsVisible) childBrowser.Focus();
            }
            catch (Exception exception)
            { if (!disposed) StatusChanged?.Invoke("Could not open Atlas window: " + exception.Message); }
            finally { deferral.Complete(); }
        };
        core.DownloadStarting += async (_, e) =>
        {
            var deferral = e.GetDeferral();
            e.Handled = true;
            try
            {
                await Dispatcher.Yield(DispatcherPriority.Background);
                if (disposed) { e.Cancel = true; return; }
                var filename = Path.GetFileName(e.ResultFilePath);
                var dialog = new Microsoft.Win32.SaveFileDialog { Title = "Save Atlas export", FileName = string.IsNullOrWhiteSpace(filename) ? "atlas-export.json" : filename,
                    Filter = "All files|*.*", OverwritePrompt = true };
                var parent = owner();
                if ((parent is null ? dialog.ShowDialog() : dialog.ShowDialog(parent)) != true) { e.Cancel = true; return; }
                e.ResultFilePath = dialog.FileName;
                var download = e.DownloadOperation;
                download.StateChanged += DownloadChanged;
                StatusChanged?.Invoke("Saving Atlas export: " + Path.GetFileName(dialog.FileName));
                void DownloadChanged(object? sender, object args)
                {
                    if (download.State == CoreWebView2DownloadState.InProgress) return;
                    download.StateChanged -= DownloadChanged;
                    if (!disposed) StatusChanged?.Invoke(download.State == CoreWebView2DownloadState.Completed
                        ? "Saved Atlas export: " + Path.GetFileName(download.ResultFilePath) : "Atlas export interrupted: " + download.InterruptReason);
                }
            }
            catch (Exception exception)
            { if (!disposed) { e.Cancel = true; StatusChanged?.Invoke("Could not save Atlas export: " + exception.Message); } }
            finally { deferral.Complete(); }
        };
    }

    private void ShowNotice(string title, string detail, bool actions)
    {
        noticeTitle.Text = title;
        noticeDetail.Text = detail;
        noticeActions.Visibility = actions ? Visibility.Visible : Visibility.Collapsed;
        notice.Visibility = Visibility.Visible;
    }

    private void Command(Action<CoreWebView2> action)
    {
        if (disposed || browser?.CoreWebView2 is not { } core) return;
        try { action(core); }
        catch (Exception exception) when (exception is InvalidOperationException or COMException)
        { if (!disposed) StatusChanged?.Invoke("Atlas command unavailable: " + exception.Message); }
    }
    public void Back() => Command(core => { if (core.CanGoBack) core.GoBack(); });
    public void Forward() => Command(core => { if (core.CanGoForward) core.GoForward(); });
    public async Task ReloadAsync()
    {
        if (browser?.CoreWebView2 is not null) Command(core => core.Reload());
        else if (profileDirectory.Length != 0) await OpenAsync(Address, profileDirectory);
    }
    public async Task SearchAsync()
    {
        FocusPage();
        // The site's own Ctrl+F handler opens the search menu, including compact mode.
        await ScriptAsync("document.dispatchEvent(new KeyboardEvent('keydown', {key:'f', ctrlKey:true, bubbles:true, cancelable:true}));");
    }
    public void FocusPage() { if (!locked) browser?.Focus(); }
    public void SetHostVisible(bool visible)
    {
        hostVisible = visible;
        UpdatePopupVisibility();
    }
    public void SetTopmost(bool topmost) { foreach (var popup in popups.ToArray()) popup.Topmost = topmost; }
    private void UpdatePopupVisibility()
    {
        foreach (var popup in popups.ToArray())
        {
            if (hostVisible && !locked) { if (!popup.IsVisible) popup.Show(); }
            else popup.Hide();
        }
    }
    public async Task ExitFullScreenAsync() => await ScriptAsync("if (document.fullscreenElement) document.exitFullscreen();");
    public async Task SetLockedAsync(bool value)
    {
        locked = value;
        IsHitTestVisible = !locked;
        UpdatePopupVisibility();
        await ApplyLockAsync();
    }
    private Task ApplyLockAsync() => ScriptAsync("document.documentElement.classList.toggle('mnm-overlay-locked', " + (locked ? "true" : "false") + ");");
    private async Task ScriptAsync(string script)
    {
        if (disposed || browser?.CoreWebView2 is not { } core || !TryAddress(core.Source, out _)) return;
        try { await core.ExecuteScriptAsync(script); }
        catch (Exception exception) when (exception is InvalidOperationException or COMException)
        { if (!disposed) StatusChanged?.Invoke("Atlas control unavailable: " + exception.Message); }
    }

    public void OpenExternal(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != "https") return;
        try { Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception exception) { StatusChanged?.Invoke("Could not open your browser: " + exception.Message); }
    }

    private void ResetBrowser()
    {
        foreach (var popup in popups.ToArray()) if (popups.Contains(popup)) popup.Close();
        if (browser is not null) { Children.Remove(browser); browser.Dispose(); browser = null; }
        HistoryChanged?.Invoke();
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        ResetBrowser();
    }

    private const string LockStyles = """
        (() => {
          if (location.origin !== 'https://www.mnmatlas.com') return;
          const install = () => {
            if (document.getElementById('mnm-overlay-lock-style')) return;
            const style = document.createElement('style'); style.id = 'mnm-overlay-lock-style';
            style.textContent = `
              html.mnm-overlay-locked,
              html.mnm-overlay-locked body,
              html.mnm-overlay-locked .atlas { background: transparent !important; }
              html.mnm-overlay-locked dialog::backdrop { background: transparent !important; }
              html.mnm-overlay-locked .masthead,
              html.mnm-overlay-locked #journal,
              html.mnm-overlay-locked .map-tools,
              html.mnm-overlay-locked .level-controls,
              html.mnm-overlay-locked .map-title,
              html.mnm-overlay-locked .map-caption,
              html.mnm-overlay-locked .compass,
              html.mnm-overlay-locked .frame-border,
              html.mnm-overlay-locked .leaflet-top,
              html.mnm-overlay-locked .leaflet-popup,
              html.mnm-overlay-locked dialog,
              html.mnm-overlay-locked #edit-bar,
              html.mnm-overlay-locked #update-notice { visibility: hidden !important; pointer-events: none !important; }
            `;
            document.head.appendChild(style);
          };
          if (document.head) install(); else document.addEventListener('DOMContentLoaded', install, {once:true});
        })();
        """;
}
