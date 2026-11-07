using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Boats;

/// <summary>
/// Spread 6: a submarine in a 60-metre sea, its ballast tanks as full as the slider says. It rises, sinks or hangs
/// as its weight compares with the water it pushes aside; a gauge on the right shows its depth.
/// </summary>
public sealed class SubmarineDiagram : GearPicture
{
    public static readonly StyledProperty<double> BallastProperty =
        AvaloniaProperty.Register<SubmarineDiagram, double>(nameof(Ballast));

    /// <summary>The picture runs this many times faster than real time, so a dive takes seconds.</summary>
    private const double TimeScale = 4;

    private static readonly IImmutableBrush HullBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x4A, 0x52, 0x62));
    private static readonly IImmutableBrush TankAir = new ImmutableSolidColorBrush(Color.FromRgb(0x2A, 0x30, 0x3C));
    private static readonly IImmutableBrush TankWater = new ImmutableSolidColorBrush(Color.FromRgb(0x4F, 0xA8, 0xE0));
    private static readonly IImmutableBrush Seabed = new ImmutableSolidColorBrush(Color.FromRgb(0x3A, 0x34, 0x2A));
    private static readonly IPen Tick = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0xA0, 0xE6, 0xEA, 0xF2)), 1);

    private readonly Submarine _sub = new();
    private double _seconds;

    static SubmarineDiagram()
    {
        AffectsRender<SubmarineDiagram>(BallastProperty);
    }

    /// <summary>How full the ballast tanks are, 0 to 1.</summary>
    public double Ballast
    {
        get => GetValue(BallastProperty);
        set => SetValue(BallastProperty, value);
    }

    internal Submarine Boat => _sub;

    protected override void Advance(double seconds)
    {
        _seconds += seconds;
        _sub.Step(seconds * TimeScale, Ballast);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var surface = bounds.Height * 0.14;
        var seabed = bounds.Height * 0.86;
        var perMetre = (seabed - surface) / Submarine.SeaDepth;
        var gaugeX = bounds.Right - 40;

        // The submarine: a long rounded hull with a tower, its keel Depth metres down.
        var height = Submarine.Height * perMetre;
        var length = Math.Min(bounds.Width * 0.55, height * 6);
        var keel = Math.Min(seabed, surface + (_sub.Depth * perMetre));
        var body = new Rect((bounds.Width * 0.42) - (length / 2), keel - height, length, height);
        var tower = new Rect(body.X + (length * 0.42), body.Y - (height * 0.55), length * 0.16, height * 0.6);
        context.DrawRectangle(HullBrush, new ImmutablePen(BoatsParts.Dark, 1.5), tower, 4, 4);
        context.DrawRectangle(HullBrush, new ImmutablePen(BoatsParts.Dark, 1.5), body, height / 2, height / 2);

        // The ballast tanks, low in the hull at each end, filled from below.
        var tankBoxes = new List<Point>();
        foreach (var x in new[] { body.X + (length * 0.08), body.X + (length * 0.66) })
        {
            var tank = new Rect(x, body.Y + (height * 0.45), length * 0.26, height * 0.42);
            context.DrawRectangle(TankAir, null, tank, 4, 4);
            var water = tank.Height * Math.Clamp(Ballast, 0, 1);
            context.DrawRectangle(TankWater, null, new Rect(tank.X, tank.Bottom - water, tank.Width, water), 4, 4);
            context.DrawRectangle(null, new ImmutablePen(BoatsParts.Dark, 1.2), tank, 4, 4);
            tankBoxes.Add(tank.TopLeft);
            tankBoxes.Add(tank.BottomRight);
        }

        // The propeller turning at the back.
        var prop = new Point(body.X - 4, body.Center.Y);
        var blade = height * 0.4 * Math.Abs(Math.Sin(_seconds * 9));
        context.DrawLine(new ImmutablePen(BoatsParts.Deck, 3, lineCap: PenLineCap.Round), prop - new Point(0, blade), prop + new Point(0, blade));

        BoatsParts.DrawWater(context, new Rect(bounds.X, bounds.Y, bounds.Width, seabed), surface, 0.66);
        context.FillRectangle(Seabed, new Rect(bounds.X, seabed, bounds.Width, bounds.Bottom - seabed));
        Mark("sub", [body.TopLeft, body.BottomRight, tower.TopLeft], 4);
        Mark("tanks", tankBoxes, 2);

        // Weight down, the water's push up, as long as each is.
        var weight = Submarine.Weight(Ballast);
        var arrow = height * 1.4;
        var middle = body.Center;
        BoatsParts.DrawArrow(context, middle + new Point(-length * 0.05, 0), middle + new Point(-length * 0.05, weight * arrow), BoatsParts.Weight, 4);
        BoatsParts.DrawArrow(context, middle + new Point(length * 0.05, 0), middle + new Point(length * 0.05, -_sub.Submerged * arrow), BoatsParts.Lift, 4);

        // The depth gauge.
        context.DrawLine(Tick, new Point(gaugeX, surface), new Point(gaugeX, seabed));
        for (var m = 0; m <= Submarine.SeaDepth; m += 10)
        {
            var y = surface + (m * perMetre);
            context.DrawLine(Tick, new Point(gaugeX - 5, y), new Point(gaugeX + 5, y));
            DiagramText.Draw(context, DiagramText.Format("Boats_Metres", m), new Point(gaugeX - 8, y), 10.5, DiagramText.Faint, 1, 0.5);
        }

        var depth = Math.Max(0, _sub.Depth - Submarine.Height);
        var gaugeY = surface + (Math.Min(Submarine.SeaDepth, _sub.Depth) * perMetre);
        context.DrawEllipse(DiagramText.Gold, new ImmutablePen(BoatsParts.Dark, 1.2), new Point(gaugeX, gaugeY), 6, 6);
        Mark("gauge", [new Point(gaugeX - 30, surface), new Point(gaugeX + 8, seabed)], 4);

        var state = _sub.OnSeabed ? "Boats_Submarine_OnSeabed"
            : _sub.Submerged < 0.999 ? "Boats_Submarine_Surfaced"
            : Math.Abs(_sub.Speed) < 0.02 ? "Boats_Submarine_Hanging"
            : _sub.Speed > 0 ? "Boats_Submarine_Sinking" : "Boats_Submarine_Rising";
        var first = DiagramText.Draw(context, DiagramText.Get(state), new Point(bounds.X + 18, bounds.Y + 16), 15, DiagramText.Ink, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Boats_Submarine_Depth", depth.ToString("0")), new Point(bounds.X + 18, first.Bottom + 4), 12.5, DiagramText.Faint);
    }
}
