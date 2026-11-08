using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Gps.GpsParts;

namespace Lemon.Inside.Book.Chapters.Gps;

/// <summary>
/// Spread 3: the same map, the receiver really at one spot, but its clock off by the offset the reader sets. Every
/// distance is then wrong by the same amount, so the three circles no longer meet: they leave a little triangle between
/// them. Only the right clock makes them meet at one point. The reader drags the clock offset bar.
/// </summary>
public sealed class ClockDiagram : GearPicture
{
    public static readonly StyledProperty<double> OffsetProperty =
        AvaloniaProperty.Register<ClockDiagram, double>(nameof(Offset), 120, defaultBindingMode: BindingMode.TwoWay);

    private const double Half = 230;

    static ClockDiagram()
    {
        AffectsRender<ClockDiagram>(OffsetProperty);
    }

    /// <summary>How slow the receiver's clock is, µs (negative: fast).</summary>
    public double Offset
    {
        get => GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var offset = Math.Clamp(Offset, -200, 200);
        var size = Math.Min(bounds.Width - 60, bounds.Height - 230);
        var box = new Rect(bounds.Center.X - (size / 2), bounds.Y + 60, size, size);
        DrawMap(context, box, Half);
        Point P(MapPoint p) => OnMap(p, box, Half);
        var pxPerKm = box.Width / (2 * Half);

        var r = Map.Measured(offset);
        using (context.PushClip(box))
        {
            for (var i = 0; i < 3; i++) context.DrawEllipse(null, Line(SignalInk, 2), P(Map.Satellites[i]), r[i] * pxPerKm, r[i] * pxPerKm);
        }

        Mark("circles", P(Map.Satellites[0] + new MapPoint(0, -r[0])), 12);
        for (var i = 0; i < 3; i++) DrawSatellite(context, P(Map.Satellites[i]), 12);

        // Where each pair of circles agrees: three points, one only when the clock is right.
        var corners = new[]
        {
            Circles.Nearest(Map.Satellites[0], r[0], Map.Satellites[1], r[1], Map.Here),
            Circles.Nearest(Map.Satellites[1], r[1], Map.Satellites[2], r[2], Map.Here),
            Circles.Nearest(Map.Satellites[2], r[2], Map.Satellites[0], r[0], Map.Here),
        }.Select(P).ToList();
        Tesseract.TesseractParts.DrawPolygon(context, Tint(WarnInk, 0.35), Line(WarnInk, 1.5), corners);
        foreach (var c in corners) context.DrawEllipse(WarnInk, null, c, 3.5, 3.5);
        Mark("gap", corners, 12);

        // Where the receiver really is.
        var here = P(Map.Here);
        context.DrawLine(Line(PlaceInk, 2), here + new Vector(-7, -7), here + new Vector(7, 7));
        context.DrawLine(Line(PlaceInk, 2), here + new Vector(-7, 7), here + new Vector(7, -7));
        Mark("you", here + new Vector(0, 22), 9);
        DiagramText.Draw(context, DiagramText.Get("Gps_Clock_Here"), here + new Vector(10, 10), 11, PlaceInk);

        // The receiver's clock, and the bar that sets it.
        var clock = new Point(box.Right - 34, box.Y + 34);
        DrawClock(context, clock, 24, offset / 400, Math.Abs(offset) < 10 ? PlaceInk : WarnInk);
        Mark("clock", clock, 26);
        var barLeft = new Point(bounds.X + 30, box.Bottom + 44);
        var barWidth = bounds.Width - 60;
        var knob = Tesseract.TesseractParts.DrawBar(context, barLeft, barWidth, (offset + 200) / 400, DiagramText.Get("Gps_Clock_Offset"));
        context.DrawLine(Line(DiagramText.Faint, 1), new Point(barLeft.X + (barWidth / 2), barLeft.Y - 8), new Point(barLeft.X + (barWidth / 2), barLeft.Y + 8));
        Grip("offset", knob, GripAxis.Horizontal, DiagramText.Get("Gps_Grip_Offset"),
            at => Offset = Math.Round((Tesseract.TesseractParts.Along(at, barLeft, barWidth) * 400) - 200));
        DiagramText.Draw(context, DiagramText.Format("Gps_Clock_Miss", Km(Map.Miss(offset))), new Point(bounds.Center.X, barLeft.Y + 22), 13,
            Math.Abs(offset) < 10 ? PlaceInk : WarnInk, 0.5, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Gps_Clock_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
