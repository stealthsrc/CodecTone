using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace AudioConverter.Desktop.Controls;

public sealed class WaveformControl : FrameworkElement
{
    private enum DragHandle { None, Start, End }
    private DragHandle dragging;

    public static readonly DependencyProperty PeaksProperty = DependencyProperty.Register(
        nameof(Peaks), typeof(double[]), typeof(WaveformControl), new FrameworkPropertyMetadata(Array.Empty<double>(), FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
        nameof(Duration), typeof(double), typeof(WaveformControl), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty StartProperty = DependencyProperty.Register(
        nameof(Start), typeof(double), typeof(WaveformControl), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty EndProperty = DependencyProperty.Register(
        nameof(End), typeof(double), typeof(WaveformControl), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public double[] Peaks { get => (double[])GetValue(PeaksProperty); set => SetValue(PeaksProperty, value); }
    public double Duration { get => (double)GetValue(DurationProperty); set => SetValue(DurationProperty, value); }
    public double Start { get => (double)GetValue(StartProperty); set => SetValue(StartProperty, value); }
    public double End { get => (double)GetValue(EndProperty); set => SetValue(EndProperty, value); }

    public WaveformControl() { MinHeight = 250; Cursor = Cursors.SizeWE; Focusable = true; }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        var background = FindBrush("PanelRaisedBrush", Brushes.Black);
        var muted = FindBrush("MutedTextBrush", Brushes.Gray);
        var accent = FindBrush("AccentBrush", Brushes.MediumSpringGreen);
        var border = FindBrush("BorderBrush", Brushes.DimGray);
        drawingContext.DrawRectangle(background, new Pen(border, 1), new Rect(0, 0, ActualWidth, ActualHeight));
        if (Peaks.Length == 0 || Duration <= 0)
        {
            var text = new FormattedText("Choose an audio file to generate its waveform", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 13, muted, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            drawingContext.DrawText(text, new Point((ActualWidth - text.Width) / 2, (ActualHeight - text.Height) / 2)); return;
        }
        var middle = ActualHeight / 2;
        var startX = Start / Duration * ActualWidth;
        var endX = End / Duration * ActualWidth;
        for (var x = 0; x < (int)ActualWidth; x++)
        {
            var index = Math.Min((int)(x / ActualWidth * Peaks.Length), Peaks.Length - 1);
            var half = Math.Max(1, Peaks[index] * (ActualHeight - 44) / 2);
            drawingContext.DrawLine(new Pen(x >= startX && x <= endX ? accent : muted, 1), new Point(x, middle - half), new Point(x, middle + half));
        }
        drawingContext.DrawLine(new Pen(accent, 3), new Point(startX, 0), new Point(startX, ActualHeight));
        drawingContext.DrawLine(new Pen(accent, 3), new Point(endX, 0), new Point(endX, ActualHeight));
        drawingContext.DrawRectangle(accent, null, new Rect(startX - 5, 0, 10, 12));
        drawingContext.DrawRectangle(accent, null, new Rect(endX - 5, 0, 10, 12));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (Duration <= 0) return;
        Focus(); CaptureMouse(); var x = e.GetPosition(this).X;
        var startX = Start / Duration * ActualWidth; var endX = End / Duration * ActualWidth;
        dragging = Math.Abs(x - startX) <= Math.Abs(x - endX) ? DragHandle.Start : DragHandle.End;
        MoveHandle(x); e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e) { if (dragging != DragHandle.None) MoveHandle(e.GetPosition(this).X); }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) { dragging = DragHandle.None; ReleaseMouseCapture(); e.Handled = true; }
    private void MoveHandle(double x)
    {
        var time = Math.Clamp(x / Math.Max(1, ActualWidth) * Duration, 0, Duration);
        if (dragging == DragHandle.Start) Start = Math.Min(time, End - 0.001); else if (dragging == DragHandle.End) End = Math.Max(time, Start + 0.001);
    }
    private Brush FindBrush(string key, Brush fallback) => TryFindResource(key) as Brush ?? fallback;
}
