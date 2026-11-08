using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Gps.GpsParts;

namespace Lemon.Inside.Book.Chapters.Gps;

/// <summary>
/// Spread 1: a GPS satellite high above the Earth (to scale), sending out its signal as a growing ring. The delay the
/// reader sets says how far the signal went: a circle of that size round the satellite, crossing the ground at the two
/// places the receiver could be. The reader drags the delay bar.
/// </summary>
public sealed class EchoDiagram : GearPicture
{
    public static readonly StyledProperty<double> DelayProperty =
        AvaloniaProperty.Register<EchoDiagram, double>(nameof(Delay), 75, defaultBindingMode: BindingMode.TwoWay);

    static EchoDiagram()
    {
        AffectsRender<EchoDiagram>(DelayProperty);
    }

    /// <summary>The signal's travel time, ms.</summary>
    public double Delay
    {
        get => GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var delay = Math.Clamp(Delay, 60, 90);
        var km = Radio.Km(delay);
        var top = bounds.Y + 150;
        var scale = (bounds.Height - 300) / Orbit.Gps;
        var sat = new Point(bounds.Center.X, top);
        var earth = new Point(sat.X, top + ((Orbit.Gps + Orbit.EarthRadius) * scale));
        var r = Orbit.EarthRadius * scale;
        DrawEarth(context, earth, r);

        // The circle: every point the signal could have reached in that time.
        var radius = km * scale;
        context.DrawEllipse(null, Line(SignalInk, 1.8, dashed: true), sat, radius, radius);
        Mark("circle", sat + new Vector(-radius * 0.7071, -radius * 0.7071), 12);

        // The signal going out, again and again.
        var pulse = (Seconds % 1.6) / 1.6 * radius;
        context.DrawEllipse(null, Line(Tint(SignalInk, 1 - (pulse / radius * 0.8)), 3), sat, pulse, pulse);
        Mark("pulse", sat + new Vector(radius * 0.35, -radius * 0.35), 14);

        DrawSatellite(context, sat, 18);
        Mark("satellite", sat, 26);

        // Where the circle meets the ground.
        var angle = Orbit.GroundAngle(Orbit.Gps, km);
        var foot = new Point(sat.X, earth.Y - r);
        if (angle is { } a)
        {
            var rad = a * Math.PI / 180;
            var left = earth + (new Vector(-Math.Sin(rad), -Math.Cos(rad)) * r);
            var right = earth + (new Vector(Math.Sin(rad), -Math.Cos(rad)) * r);
            foreach (var p in new[] { left, right })
            {
                context.DrawEllipse(PlaceInk, Edge, p, 6, 6);
                context.DrawLine(Line(Tint(PlaceInk, 0.5), 1), sat, p);
            }

            Mark("places", [left, right], 10);
        }
        else
        {
            Mark("places", foot, 12);
        }

        // The clocks: when it was sent, when it arrived.
        DiagramText.Draw(context, DiagramText.Get("Gps_Echo_Sent"), new Point(bounds.X + 16, bounds.Y + 40), 12, DiagramText.Faint);
        DiagramText.Draw(context, DiagramText.Format("Gps_Echo_Arrived", Number(delay, "0.0")), new Point(bounds.X + 16, bounds.Y + 58), 12, DiagramText.Ink);
        DiagramText.Draw(context, DiagramText.Format("Gps_Echo_Distance", Km(km)), new Point(sat.X + 22, sat.Y + 18), 13, PlaceInk, 0, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Gps_Echo_Height", Km(Orbit.Gps)), new Point(sat.X + 8, (sat.Y + foot.Y) / 2), 11.5, DiagramText.Faint);
        context.DrawLine(Line(DiagramText.Faint, 1, dashed: true), sat + new Vector(0, 22), foot);

        var barLeft = new Point(bounds.X + 30, bounds.Y + 104);
        var barWidth = bounds.Width - 60;
        var knob = Tesseract.TesseractParts.DrawBar(context, barLeft, barWidth, (delay - 60) / 30, DiagramText.Get("Gps_Echo_Delay"));
        Grip("delay", knob, GripAxis.Horizontal, DiagramText.Get("Gps_Grip_Delay"),
            at => Delay = Math.Round((60 + (30 * Tesseract.TesseractParts.Along(at, barLeft, barWidth))) * 10) / 10);
        DiagramText.Draw(context, DiagramText.Get("Gps_Echo_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
