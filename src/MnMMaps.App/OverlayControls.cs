using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace MnMMaps;

// These APIs register our shortcuts and change our own window's styles only.
public sealed class OverlayControls : IDisposable
{
    private const int ToggleId = 0x4D31, LockId = 0x4D32, ExtendedStyle = -20;
    private const long TransparentStyle = 0x20;
    private readonly Window window;
    private readonly Action toggle;
    private readonly Action toggleLock;
    private readonly IntPtr handle;
    private readonly HwndSource source;
    private readonly Forms.NotifyIcon tray;
    private readonly System.Drawing.Icon trayIcon;
    private readonly Forms.ContextMenuStrip menu;
    private bool disposed;
    public bool CanToggle { get; }
    public bool CanLock { get; }
    public bool Locked { get; private set; }

    public OverlayControls(Window window, Action toggle, Action toggleLock, Action showUnlocked)
    {
        this.window = window;
        this.toggle = toggle;
        this.toggleLock = toggleLock;
        var iconResource = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/MnMMaps.ico", UriKind.Absolute))
            ?? throw new InvalidOperationException("The application icon was not found.");
        using (var iconStream = iconResource.Stream)
        using (var icon = new System.Drawing.Icon(iconStream, Forms.SystemInformation.SmallIconSize))
            trayIcon = (System.Drawing.Icon)icon.Clone();
        handle = new WindowInteropHelper(window).Handle;
        source = HwndSource.FromHwnd(handle) ?? throw new InvalidOperationException("The overlay window was not created.");
        source.AddHook(WindowMessage);
        CanToggle = RegisterHotKey(handle, ToggleId, 0x4006, 0x4D); // Ctrl+Shift+M, no repeat.
        CanLock = RegisterHotKey(handle, LockId, 0x4006, 0x4C); // Ctrl+Shift+L, no repeat.
        menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Show / hide (Ctrl+Shift+M)", null, (_, _) => Dispatch(toggle));
        menu.Items.Add("Unlock mouse and show", null, (_, _) => Dispatch(showUnlocked));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quit MnM Maps", null, (_, _) => Dispatch(window.Close));
        tray = new Forms.NotifyIcon { Icon = trayIcon, Text = "MnM Maps · Ctrl+Shift+M", ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (_, _) => Dispatch(toggle);
    }

    private void Dispatch(Action action) => window.Dispatcher.BeginInvoke(action);

    private IntPtr WindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == 0x0312)
        {
            if (wParam.ToInt32() == ToggleId) { toggle(); handled = true; }
            if (wParam.ToInt32() == LockId) { toggleLock(); handled = true; }
        }
        return IntPtr.Zero;
    }

    public void SetLocked(bool locked)
    {
        if (locked && !CanLock) return;
        var current = GetWindowLongPtr(handle, ExtendedStyle).ToInt64();
        SetWindowLongPtr(handle, ExtendedStyle, new IntPtr(locked ? current | TransparentStyle : current & ~TransparentStyle));
        Locked = locked;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (CanToggle) UnregisterHotKey(handle, ToggleId);
        if (CanLock) UnregisterHotKey(handle, LockId);
        source.RemoveHook(WindowMessage);
        tray.Visible = false;
        tray.Dispose();
        trayIcon.Dispose();
        menu.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr window, int id);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);
}
