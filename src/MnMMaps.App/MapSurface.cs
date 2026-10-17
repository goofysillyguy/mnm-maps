using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MnMMaps;

public sealed class MapSurface : FrameworkElement
{
    public static readonly DependencyProperty MapOnlyProperty = DependencyProperty.Register(
        nameof(MapOnly), typeof(bool), typeof(MapSurface), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public bool MapOnly { get => (bool)GetValue(MapOnlyProperty); set => SetValue(MapOnlyProperty, value); }

    private BitmapSource? image;
    private List<MapPin> pins = [];
    private Point offset;
    private Point lastMouse;
    private double scale = 1;
    private bool dragging;
    private MapView? pendingView;
    private bool pinMode;
    private static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(15, 20, 25));
    private static readonly Brush PinBrush = new SolidColorBrush(Color.FromRgb(255, 209, 117));
    private static readonly Brush LabelBrush = new SolidColorBrush(Color.FromArgb(225, 16, 20, 25));

    public event Action<Point>? PinRequested;
    public event Action<MapPin>? PinSelected;
    public event Action? ViewChanged;
    public MapPin? SelectedPin { get; set; }
    public bool HasImage => image is not null;
    public double Zoom => image is null ? 1 : scale / FitScale(RenderSize);
    public bool PinMode
    {
        get => pinMode;
        set { pinMode = value; Cursor = value ? Cursors.Cross : Cursors.Hand; }
    }

    public MapSurface()
    {
        ClipToBounds = true;
        Focusable = true;
        Cursor = Cursors.Hand;
        MouseLeftButtonDown += BeginDragOrPin;
        MouseMove += Drag;
        MouseLeftButtonUp += (_, _) => { dragging = false; ReleaseMouseCapture(); };
        LostMouseCapture += (_, _) => dragging = false;
        MouseWheel += (_, e) => { if (HasImage) ZoomAt(Math.Pow(1.2, e.Delta / 120.0), e.GetPosition(this)); e.Handled = true; };
    }

    public void SetMap(BitmapSource? bitmap, List<MapPin> mapPins, MapView? view = null)
    {
        image = bitmap;
        pins = mapPins;
        SelectedPin = null;
        PinMode = false;
        pendingView = view ?? new();
        ApplyPendingView();
        InvalidateVisual();
    }

    private double FitScale(Size size) => image is null ? 1 : Math.Max(0.00001, Math.Min(size.Width / image.PixelWidth, size.Height / image.PixelHeight));

    private void ApplyPendingView()
    {
        if (pendingView is null || image is null || ActualWidth <= 0 || ActualHeight <= 0) return;
        var view = pendingView;
        pendingView = null;
        var zoom = double.IsFinite(view.Zoom) ? Math.Clamp(view.Zoom, 0.2, 20) : 1;
        var x = double.IsFinite(view.CenterX) ? Math.Clamp(view.CenterX, 0, 1) : 0.5;
        var y = double.IsFinite(view.CenterY) ? Math.Clamp(view.CenterY, 0, 1) : 0.5;
        scale = FitScale(RenderSize) * zoom;
        offset = new Point(ActualWidth / 2 - x * image.PixelWidth * scale, ActualHeight / 2 - y * image.PixelHeight * scale);
        ClampOffset();
        ViewChanged?.Invoke();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        if (image is not null && pendingView is null && sizeInfo.PreviousSize.Width > 0 && sizeInfo.PreviousSize.Height > 0)
        {
            var oldCenter = ToMap(new Point(sizeInfo.PreviousSize.Width / 2, sizeInfo.PreviousSize.Height / 2));
            pendingView = new MapView { Zoom = scale / FitScale(sizeInfo.PreviousSize), CenterX = oldCenter.X, CenterY = oldCenter.Y };
        }
        base.OnRenderSizeChanged(sizeInfo);
        ApplyPendingView();
    }

    public void Fit()
    {
        pendingView = new();
        ApplyPendingView();
        InvalidateVisual();
    }

    public void ZoomBy(double factor) => ZoomAt(factor, new Point(ActualWidth / 2, ActualHeight / 2));

    private void ZoomAt(double factor, Point anchor)
    {
        if (image is null) return;
        var before = ToMap(anchor);
        scale = FitScale(RenderSize) * Math.Clamp(Zoom * factor, 0.2, 20);
        offset = new Point(anchor.X - before.X * image.PixelWidth * scale, anchor.Y - before.Y * image.PixelHeight * scale);
        ClampOffset();
        InvalidateVisual();
        ViewChanged?.Invoke();
    }

    public MapView CaptureView()
    {
        var center = ToMap(new Point(ActualWidth / 2, ActualHeight / 2));
        return new MapView { Zoom = Zoom, CenterX = center.X, CenterY = center.Y };
    }

    public Point ToMap(Point screen) => image is null ? new Point(0.5, 0.5)
        : new Point((screen.X - offset.X) / (image.PixelWidth * scale), (screen.Y - offset.Y) / (image.PixelHeight * scale));

    public Point ToScreen(Point normalized) => image is null ? new Point()
        : new Point(offset.X + normalized.X * image.PixelWidth * scale, offset.Y + normalized.Y * image.PixelHeight * scale);

    public void CenterOn(MapPin pin)
    {
        if (image is null) return;
        offset = new Point(ActualWidth / 2 - pin.X * image.PixelWidth * scale, ActualHeight / 2 - pin.Y * image.PixelHeight * scale);
        ClampOffset();
        InvalidateVisual();
        ViewChanged?.Invoke();
    }

    private void ClampOffset()
    {
        if (image is null) return;
        offset.X = Math.Clamp(offset.X, 32 - image.PixelWidth * scale, Math.Max(32, ActualWidth - 32));
        offset.Y = Math.Clamp(offset.Y, 32 - image.PixelHeight * scale, Math.Max(32, ActualHeight - 32));
    }

    private void BeginDragOrPin(object sender, MouseButtonEventArgs e)
    {
        if (image is null) return;
        var point = e.GetPosition(this);
        if (PinMode)
        {
            var normalized = ToMap(point);
            if (normalized.X is >= 0 and <= 1 && normalized.Y is >= 0 and <= 1) PinRequested?.Invoke(normalized);
            e.Handled = true;
            return;
        }
        var hit = pins.LastOrDefault(pin => (ToScreen(new Point(pin.X, pin.Y)) - point).Length < 13);
        if (hit is not null)
        {
            SelectedPin = hit;
            PinSelected?.Invoke(hit);
            InvalidateVisual();
            return;
        }
        dragging = true;
        lastMouse = point;
        CaptureMouse();
        e.Handled = true;
    }

    private void Drag(object sender, MouseEventArgs e)
    {
        if (!dragging || e.LeftButton != MouseButtonState.Pressed) return;
        var point = e.GetPosition(this);
        offset += point - lastMouse;
        lastMouse = point;
        ClampOffset();
        InvalidateVisual();
        ViewChanged?.Invoke();
    }

    protected override void OnRender(DrawingContext drawing)
    {
        if (!MapOnly) drawing.DrawRectangle(BackgroundBrush, null, new Rect(RenderSize));
        if (image is null) return;
        drawing.DrawImage(image, new Rect(offset.X, offset.Y, image.PixelWidth * scale, image.PixelHeight * scale));
        foreach (var pin in pins)
        {
            var point = ToScreen(new Point(pin.X, pin.Y));
            if (point.X < -250 || point.Y < -40 || point.X > ActualWidth + 20 || point.Y > ActualHeight + 20) continue;
            drawing.DrawEllipse(LabelBrush, new Pen(PinBrush, SelectedPin == pin ? 3 : 1.5), point, 7, 7);
            drawing.DrawEllipse(PinBrush, null, point, 2, 2);
            var text = new FormattedText(pin.Name, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 12, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip)
                { MaxTextWidth = 220, MaxLineCount = 1, Trimming = TextTrimming.CharacterEllipsis };
            var labelX = Math.Clamp(point.X + 12, 2, Math.Max(2, ActualWidth - text.Width - 14));
            var labelY = Math.Clamp(point.Y - 14, 2, Math.Max(2, ActualHeight - text.Height - 8));
            drawing.DrawRoundedRectangle(LabelBrush, null, new Rect(labelX, labelY, text.Width + 12, text.Height + 8), 4, 4);
            drawing.DrawText(text, new Point(labelX + 6, labelY + 4));
        }
    }
}
