using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Gps.GpsParts;

namespace Lemon.Inside.Book.Chapters.Gps;

/// <summary>
/// Spread 5: the Earth and one satellite's orbit at the height the reader sets, to scale. The satellite goes round
/// (a picture second is half an hour), and two lines from it graze the Earth, marking the cap of ground it can see. The
/// reader drags the height bar, which has the space station, GPS and the geostationary height marked on it.
/// </summary>
public sealed class OrbitDiagram : GearPicture
{
    public static readonly StyledProperty<double> AltitudeProperty =
        AvaloniaProperty.Register<OrbitDiagram, double>(nameof(Altitude), 20_200, defaultBindingMode: BindingMode.TwoWay);

    private const double Lowest = 400;
    private const double Highest = 36_000;

    /// <summary>How far round the satellite has gone, in turns.</summary>
    private double _turns;

    static OrbitDiagram()
    {
        AffectsRender<OrbitDiagram>(AltitudeProperty);
    }

    /// <summary>The orbit's height above the ground, km.</summary>
    public double Altitude
    {
        get => GetValue(AltitudeProperty);
        set => SetValue(AltitudeProperty, value);
    }

    protected override void Advance(double seconds) => _turns = (_turns + (seconds / (2 * Orbit.Hours(Math.Clamp(Altitude, Lowest, Highest))))) % 1;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var height = Math.Clamp(Altitude, Lowest, Highest);
        var room = Math.Min(bounds.Width - 60, bounds.Height - 250) / 2;
        var scale = room / (Orbit.EarthRadius + Math.Max(height, 6000));
        var centre = new Point(bounds.Center.X, bounds.Y + 60 + room);
        var r = Orbit.EarthRadius * scale;
        var a = (Orbit.EarthRadius + height) * scale;
        DrawEarth(context, centre, r);
        Mark("earth", centre, r * 0.5);

        context.DrawEllipse(null, Line(DiagramText.Faint, 1.2, dashed: true), centre, a, a);
        var angle = _turns * 2 * Math.PI;
        var sat = centre + (new Vector(Math.Sin(angle), -Math.Cos(angle)) * a);
        var opposite = centre - (new Vector(Math.Sin(angle), -Math.Cos(angle)) * a);
        Mark("orbit", opposite, 12);

        // The cap of ground inside its horizon, and the lines that graze the Earth.
        var half = Math.Acos(Orbit.EarthRadius / (Orbit.EarthRadius + height));
        var arc = new StreamGeometry();
        using (var g = arc.Open())
        {
            for (var k = 0; k <= 32; k++)
            {
                var t = angle - half + (2 * half * k / 32);
                var p = centre + (new Vector(Math.Sin(t), -Math.Cos(t)) * r);
                if (k == 0) g.BeginFigure(p, false);
                else g.LineTo(p);
            }

            g.EndFigure(false);
        }

        context.DrawGeometry(null, Line(PlaceInk, 4), arc);
        foreach (var t in new[] { angle - half, angle + half })
        {
            context.DrawLine(Line(Tint(PlaceInk, 0.6), 1.2, dashed: true), sat, centre + (new Vector(Math.Sin(t), -Math.Cos(t)) * r));
        }

        Mark("seen", centre + (new Vector(Math.Sin(angle), -Math.Cos(angle)) * r), 12);
        DrawSatellite(context, sat, 10);
        Mark("satellite", sat, 18);

        // How long a round takes.
        DiagramText.Draw(context, DiagramText.Format("Gps_Orbit_Round", OrbitSpread.Period(height)), new Point(bounds.Center.X, centre.Y + room + 12), 14,
            PlaceInk, 0.5, 0, bold: true);

        // The height bar, with three heights worth knowing.
        var barLeft = new Point(bounds.X + 30, bounds.Bottom - 120);
        var barWidth = bounds.Width - 60;
        double Share(double h) => (h - Lowest) / (Highest - Lowest);
        var knob = Tesseract.TesseractParts.DrawBar(context, barLeft, barWidth, Share(height), DiagramText.Get("Gps_Orbit_Height"));
        (double H, string Key)[] marks = [(400, "Gps_Orbit_Station"), (Orbit.Gps, "Gps_Orbit_Gps"), (35_786, "Gps_Orbit_Geo")];
        foreach (var (h, key) in marks)
        {
            var x = barLeft.X + (barWidth * Share(h));
            context.DrawLine(Line(DiagramText.Faint, 1), new Point(x, barLeft.Y + 6), new Point(x, barLeft.Y + 14));
            DiagramText.Draw(context, DiagramText.Get(key), new Point(x, barLeft.Y + 16), 10.5, DiagramText.Faint, h < 1000 ? 0 : h > 30_000 ? 1 : 0.5);
        }

        Grip("height", knob, GripAxis.Horizontal, DiagramText.Get("Gps_Grip_Height"),
            at => Altitude = Math.Round((Lowest + ((Highest - Lowest) * Tesseract.TesseractParts.Along(at, barLeft, barWidth))) / 100) * 100);
        DiagramText.Draw(context, DiagramText.Get("Gps_Orbit_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
