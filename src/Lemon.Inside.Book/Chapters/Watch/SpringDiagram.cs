using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Watch;

/// <summary>
/// Spread 1: the mainspring in its barrel, seen from above. Wound, its coils wrap tight round the arbor in the middle;
/// run down, they lie against the barrel's wall. Beside it, how hard it pulls and how long it will run.
/// </summary>
public sealed class SpringDiagram : GearPicture
{
    public static readonly StyledProperty<double> WoundProperty =
        AvaloniaProperty.Register<SpringDiagram, double>(nameof(Wound), 0.6);

    private const int Coils = 11;
    private static readonly double Pressure = 20 * Math.PI / 180;
    private static readonly IImmutableBrush Floor = new ImmutableSolidColorBrush(Color.FromRgb(0x2A, 0x2F, 0x3A));
    private static readonly IImmutableBrush Track = new ImmutableSolidColorBrush(Color.FromArgb(0x30, 0xE6, 0xEA, 0xF2));

    static SpringDiagram()
    {
        AffectsRender<SpringDiagram>(WoundProperty);
    }

    /// <summary>How far the spring is wound, 0 (run down) to 1 (full).</summary>
    public double Wound
    {
        get => GetValue(WoundProperty);
        set => SetValue(WoundProperty, value);
    }

    protected override bool Animates => false;

    /// <summary>How far from the centre the spring is at <paramref name="along"/> (0 inner end to 1 outer end), as a share of the barrel's radius.</summary>
    public static double CoilRadius(double along, double wound)
    {
        const double Arbor = 0.2, Wall = 0.94, Packed = 0.028;
        var tight = Arbor + (Packed * Coils * along);
        var loose = Wall - (Packed * Coils * (1 - along));
        return loose + ((tight - loose) * Math.Clamp(wound, 0, 1));
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var wound = Math.Clamp(Wound, 0, 1);
        var radius = Math.Min(bounds.Width * 0.3, bounds.Height * 0.32);
        var centre = new Point(bounds.Width * 0.38, bounds.Height * 0.46);

        // The barrel: a toothed drum (it drives the train) with the spring inside.
        DrawGear(context, 80, Pressure, centre, radius * 1.08 / 40, -wound * 0.4, WatchParts.Brass, mark: false);
        context.DrawEllipse(Floor, new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x6A, 0x5A, 0x3A)), 2), centre, radius, radius);

        // The spring: its inner end hooks on the arbor, which winding turns; the coils outside it turn less.
        var turned = wound * GoingTrain.SpringTurns * 2 * Math.PI;
        var spring = new StreamGeometry();
        using (var g = spring.Open())
        {
            const int Steps = 900;
            for (var i = 0; i <= Steps; i++)
            {
                var s = (double)i / Steps;
                var p = WatchParts.At(centre, radius * CoilRadius(s, wound), (s * Coils * 2 * Math.PI) + (turned * (1 - s)));
                if (i == 0) g.BeginFigure(p, false);
                else g.LineTo(p);
            }

            g.EndFigure(false);
        }

        context.DrawGeometry(null, new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xA8, 0xC4, 0xE8)), Math.Max(1.4, radius * 0.012)), spring);
        context.DrawEllipse(new ImmutableSolidColorBrush(WatchParts.Steel), null, centre, radius * 0.18, radius * 0.18);
        var square = radius * 0.08;
        using (context.PushTransform(Matrix.CreateTranslation(-centre.X, -centre.Y) * Matrix.CreateRotation(turned) * Matrix.CreateTranslation(centre.X, centre.Y)))
        {
            context.FillRectangle(WatchParts.Dark, new Rect(centre.X - square, centre.Y - square, 2 * square, 2 * square));
        }

        DiagramText.Draw(context, DiagramText.Get("Watch_Spring_Arbor"), new Point(centre.X, centre.Y + (radius * 0.2) + 4), 12, DiagramText.Ink, 0.5);
        DiagramText.Draw(context, DiagramText.Get("Watch_Spring_Barrel"), new Point(centre.X, centre.Y - (radius * 1.12) - 6), 12, DiagramText.Faint, 0.5, 1);

        // Two gauges: the pull, and the hours left.
        var x = bounds.Width * 0.76;
        var top = bounds.Height * 0.2;
        var height = bounds.Height * 0.52;
        var width = Math.Min(28, bounds.Width * 0.05);
        Gauge(context, new Rect(x - width - 14, top, width, height), wound, DiagramText.Gold, DiagramText.Get("Watch_Spring_Pull"));
        Gauge(context, new Rect(x + 14, top, width, height), wound, DiagramText.Cyan,
            DiagramText.Format("Watch_Spring_Hours", (wound * GoingTrain.ReserveHours).ToString("0")));
    }

    private static void Gauge(DrawingContext context, Rect area, double share, IBrush brush, string label)
    {
        context.FillRectangle(Track, area, 4);
        context.FillRectangle(brush, new Rect(area.X, area.Bottom - (area.Height * share), area.Width, area.Height * share), 4);
        DiagramText.Draw(context, label, new Point(area.Center.X, area.Bottom + 8), 12, DiagramText.Ink, 0.5);
    }
}
