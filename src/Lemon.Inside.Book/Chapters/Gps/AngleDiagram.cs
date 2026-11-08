using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Gps.GpsParts;

namespace Lemon.Inside.Book.Chapters.Gps;

/// <summary>
/// Spread 6: close up round the receiver, two satellites' distance circles look like straight bands, as wide as each
/// distance's error. The bands cross in a diamond where the receiver could be: small when the satellites are at right
/// angles as seen from it, long and thin when they are close together in the sky. The reader drags the second satellite
/// round and the error bar.
/// </summary>
public sealed class AngleDiagram : GearPicture
{
    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<AngleDiagram, double>(nameof(Angle), 25, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> ErrorProperty =
        AvaloniaProperty.Register<AngleDiagram, double>(nameof(Error), 5, defaultBindingMode: BindingMode.TwoWay);

    static AngleDiagram()
    {
        AffectsRender<AngleDiagram>(AngleProperty, ErrorProperty);
    }

    /// <summary>The angle between the two satellites, as seen from the receiver, degrees.</summary>
    public double Angle
    {
        get => GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    /// <summary>How far out each distance may be, metres.</summary>
    public double Error
    {
        get => GetValue(ErrorProperty);
        set => SetValue(ErrorProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var angle = Math.Clamp(Angle, 5, 175);
        var error = Math.Clamp(Error, 1, 10);
        var size = Math.Min(bounds.Width - 60, bounds.Height - 240);
        var box = new Rect(bounds.Center.X - (size / 2), bounds.Y + 60, size, size);
        var c = box.Center;
        var px = size / 90; // pixels a metre
        context.DrawRectangle(new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.FromArgb(0x30, 0x24, 0x5A, 0x8C)), Colour.ColourParts.Frame, box, 6, 6);

        // Which way each satellite is (straight up is 90°), and its band: across that way, ±error wide.
        Vector Dir(double degrees) => new(Math.Cos(degrees * Math.PI / 180), -Math.Sin(degrees * Math.PI / 180));
        var d1 = Dir(90 + (angle / 2));
        var d2 = Dir(90 - (angle / 2));
        using (context.PushClip(box))
        {
            foreach (var d in new[] { d1, d2 })
            {
                var t = new Vector(-d.Y, d.X);
                var e = d * error * px;
                var long_ = t * size * 1.5;
                Tesseract.TesseractParts.DrawPolygon(context, Tint(SignalInk, 0.22), Line(SignalInk, 1.2),
                    [c + long_ + e, c - long_ + e, c - long_ - e, c + long_ - e]);
            }

            // The diamond both bands share.
            var corners = new List<Point>();
            foreach (var (s1, s2) in new[] { (1, 1), (1, -1), (-1, -1), (-1, 1) })
            {
                // Solve d1·p = s1·e and d2·p = s2·e for p (screen vectors, metres scaled to pixels).
                var (a1, b1, a2, b2) = (d1.X, d1.Y, d2.X, d2.Y);
                var det = (a1 * b2) - (a2 * b1);
                var (r1, r2) = (s1 * error * px, s2 * error * px);
                corners.Add(c + new Vector(((r1 * b2) - (r2 * b1)) / det, ((a1 * r2) - (a2 * r1)) / det));
            }

            Tesseract.TesseractParts.DrawPolygon(context, Tint(PlaceInk, 0.6), Line(PlaceInk, 1.5), corners);
            Mark("patch", corners.Select(p => new Point(Math.Clamp(p.X, box.X, box.Right), Math.Clamp(p.Y, box.Y, box.Bottom))), 4);
        }

        var t1 = new Vector(-d1.Y, d1.X);
        Mark("band", c + (t1 * size * 0.38), 12);

        // The two satellites, out along their directions, and the angle between them.
        var far = size * 0.42;
        var s1Pos = c + (d1 * far);
        var s2Pos = c + (d2 * far);
        context.DrawLine(Line(DiagramText.Faint, 1, dashed: true), c, s1Pos);
        context.DrawLine(Line(DiagramText.Faint, 1, dashed: true), c, s2Pos);
        DrawSatellite(context, s1Pos, 12);
        DrawSatellite(context, s2Pos, 12);
        Mark("satellite", [s1Pos, s2Pos], 18);
        var arc = new StreamGeometry();
        using (var g = arc.Open())
        {
            for (var k = 0; k <= 24; k++)
            {
                var p = c + (Dir(90 - (angle / 2) + (angle * k / 24)) * 40);
                if (k == 0) g.BeginFigure(p, false);
                else g.LineTo(p);
            }

            g.EndFigure(false);
        }

        context.DrawGeometry(null, Line(DiagramText.Ink, 1.5), arc);
        var label = c + (Dir(90) * 56);
        DiagramText.Draw(context, DiagramText.Format("Gps_Degrees", Number(angle)), label, 13, DiagramText.Ink, 0.5, 0.5, bold: true);
        Mark("angle", label, 14);
        Grip("satellite", s2Pos + new Vector(0, 16), GripAxis.Free, DiagramText.Get("Gps_Grip_Satellite"), at =>
        {
            var v = at - new Vector(0, 16) - c;
            var up = Math.Atan2(-v.Y, v.X) * 180 / Math.PI;
            Angle = Math.Round(Math.Clamp(2 * (90 - up), 5, 175));
        });

        DiagramText.Draw(context, DiagramText.Format("Gps_Angle_Patch", Number(Patch.Longest(error, angle))), new Point(bounds.Center.X, box.Bottom + 12), 14,
            PlaceInk, 0.5, 0, bold: true);
        var barLeft = new Point(bounds.X + 30, box.Bottom + 70);
        var barWidth = bounds.Width - 60;
        var knob = Tesseract.TesseractParts.DrawBar(context, barLeft, barWidth, (error - 1) / 9, DiagramText.Get("Gps_Angle_Error"));
        Grip("error", knob, GripAxis.Horizontal, DiagramText.Get("Gps_Grip_Error"),
            at => Error = Math.Round((1 + (9 * Tesseract.TesseractParts.Along(at, barLeft, barWidth))) * 2) / 2);
        DiagramText.Draw(context, DiagramText.Get("Gps_Angle_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
