using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Boats;

/// <summary>
/// Spread 5: a boat rolling as waves knock it, end-on, with its cargo stacked as high as the weight's centre asks.
/// Below, its righting lever against the angle of tip: above the line it rights itself, below it tips further.
/// </summary>
public sealed class StabilityDiagram : GearPicture
{
    public static readonly StyledProperty<double> CentreHeightProperty =
        AvaloniaProperty.Register<StabilityDiagram, double>(nameof(CentreHeight), 1.6);

    public static readonly StyledProperty<double> BeamProperty =
        AvaloniaProperty.Register<StabilityDiagram, double>(nameof(Beam), 6);

    /// <summary>Seconds between waves, and how long a capsized boat stays over before it is set upright again.</summary>
    private const double WaveEvery = 4;
    private const double Upturned = 2.5;

    private static readonly IPen Axis = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0xE6, 0xEA, 0xF2)), 1);
    private static readonly IImmutableBrush Good = new ImmutableSolidColorBrush(Color.FromArgb(0x28, 0x5F, 0xC2, 0xB0));
    private static readonly IImmutableBrush Bad = new ImmutableSolidColorBrush(Color.FromArgb(0x28, 0xE8, 0x5A, 0x5A));

    private readonly Roll _roll = new();
    private double _sinceWave = WaveEvery - 0.5;
    private double _over;
    private int _waves;

    static StabilityDiagram()
    {
        AffectsRender<StabilityDiagram>(CentreHeightProperty, BeamProperty);
        CentreHeightProperty.Changed.AddClassHandler<StabilityDiagram>((d, _) => d.Restart());
        BeamProperty.Changed.AddClassHandler<StabilityDiagram>((d, _) => d.Restart());
    }

    /// <summary>The weight's centre above the keel, metres.</summary>
    public double CentreHeight
    {
        get => GetValue(CentreHeightProperty);
        set => SetValue(CentreHeightProperty, value);
    }

    public double Beam
    {
        get => GetValue(BeamProperty);
        set => SetValue(BeamProperty, value);
    }

    private Section Boat => new(Beam, StabilitySpread.HullDepth, StabilitySpread.Draft, CentreHeight);

    private void Restart()
    {
        _roll.Reset();
        _over = 0;
        _sinceWave = WaveEvery - 0.5;
    }

    protected override void Advance(double seconds)
    {
        if (_roll.Capsized)
        {
            _over += seconds;
            if (_over >= Upturned) Restart();
            return;
        }

        _sinceWave += seconds;
        if (_sinceWave >= WaveEvery)
        {
            _sinceWave = 0;
            _waves++;

            // A top-heavy boat needs only the smallest push; a steady one gets a good shove.
            _roll.Nudge(_waves % 2 == 0 ? 0.35 : -0.35);
        }

        _roll.Step(seconds, Boat);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var boat = Boat;
        var top = new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height * 0.6);
        var scale = Math.Min(top.Width * 0.55 / 8, top.Height * 0.62 / 7);
        var drawn = BoatSection.Draw(context, boat, _roll.Angle, new Point(top.Center.X, top.Height * 0.6), scale, top,
            cargoHeight: Math.Max(boat.HullDepth, (2 * CentreHeight) - 1.2));
        Mark("boat", drawn.Hull);
        BoatsParts.DrawCentre(context, drawn.Weight, 7);
        Mark("weight", drawn.Weight, 10);

        var words = _roll.Capsized ? DiagramText.Get("Boats_Stability_Capsized")
            : DiagramText.Format("Boats_Stability_Tipped", Math.Abs(_roll.Angle * 180 / Math.PI).ToString("0"));
        DiagramText.Draw(context, words, new Point(bounds.X + 18, bounds.Y + 16), 15, _roll.Capsized ? BoatsParts.Danger : DiagramText.Ink, bold: true);

        DrawCurve(context, boat, new Rect(bounds.X + 50, top.Bottom + 34, bounds.Width - 74, bounds.Height - top.Bottom - 70));
    }

    /// <summary>The righting lever from upright to 80°, with a dot at the boat's angle now.</summary>
    private void DrawCurve(DrawingContext context, Section boat, Rect area)
    {
        if (area.Height < 30) return;

        const double Most = 80;
        const double Range = 1.2; // metres of lever from the middle line to the top
        var zero = area.Center.Y;
        double X(double degrees) => area.X + (degrees / Most * area.Width);
        double Y(double lever) => zero - (Math.Clamp(lever, -Range, Range) / Range * area.Height / 2);
        context.FillRectangle(Good, new Rect(area.X, area.Y, area.Width, area.Height / 2));
        context.FillRectangle(Bad, new Rect(area.X, zero, area.Width, area.Height / 2));
        context.DrawLine(Axis, new Point(area.X, zero), new Point(area.Right, zero));
        context.DrawLine(Axis, new Point(area.X, area.Y), new Point(area.X, area.Bottom));
        DiagramText.Draw(context, DiagramText.Get("Boats_Stability_RightsItself"), new Point(area.Right - 4, area.Y + 4), 11, DiagramText.Faint, 1);
        DiagramText.Draw(context, DiagramText.Get("Boats_Stability_TipsFurther"), new Point(area.Right - 4, area.Bottom - 4), 11, DiagramText.Faint, 1, 1);
        DiagramText.Draw(context, DiagramText.Get("Boats_Stability_Lever"), new Point(area.X - 8, area.Y), 11, DiagramText.Faint, 1, 0);
        for (var d = 0; d <= Most; d += 20)
        {
            DiagramText.Draw(context, DiagramText.Format("Boats_Degrees", d), new Point(X(d), area.Bottom + 4), 10.5, DiagramText.Faint, 0.5);
        }

        var points = new List<Point>();
        var line = new StreamGeometry();
        using (var g = line.Open())
        {
            for (var d = 0.0; d <= Most + 1e-9; d += 2)
            {
                var p = new Point(X(d), Y(boat.RightingLever(Math.Max(1e-4, d) * Math.PI / 180)));
                if (d == 0) g.BeginFigure(p, false);
                else g.LineTo(p);
                points.Add(p);
            }

            g.EndFigure(false);
        }

        context.DrawGeometry(null, new ImmutablePen(DiagramText.Gold, 2.5), line);
        Mark("curve", points, 4);

        var now = Math.Abs(_roll.Angle) * 180 / Math.PI;
        if (now > 0.5)
        {
            context.DrawEllipse(Brushes.White, new ImmutablePen(BoatsParts.Dark, 1.5),
                new Point(X(Math.Min(now, Most)), Y(boat.RightingLever(Math.Max(1e-4, now) * Math.PI / 180))), 5, 5);
        }
    }
}
