using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Car;

/// <summary>
/// Spread 3: the crank's turning force through the two turns of a cycle, each cylinder's push faint and their sum in
/// gold, the average dashed. Below, the flywheel turning, a little faster when the force is above average and slower
/// when below, and a gauge showing how far its speed swings.
/// The reader taps beside the graph for another cylinder (round from eight back to one) and drags the flywheel's rim
/// up for a heavier wheel, which grows smoothly.
/// </summary>
public sealed class CylindersDiagram : GearPicture
{
    /// <summary>Which of <see cref="Cylinders.Choices"/> (0 for one cylinder), as the spread's slider sets it.</summary>
    public static readonly StyledProperty<int> CountStepProperty =
        AvaloniaProperty.Register<CylindersDiagram, int>(nameof(CountStep), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> FlywheelProperty =
        AvaloniaProperty.Register<CylindersDiagram, double>(nameof(Flywheel), 1, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The flywheel's lightest and heaviest (the slider's ends).</summary>
    private const double LightestFlywheel = 0.2;
    private const double HeaviestFlywheel = 3;

    /// <summary>Crank degrees a second on average (slowed right down).</summary>
    private const double Speed = 160;

    /// <summary>The gauge's ends, as a share above or below the average speed.</summary>
    private const double GaugeRange = 0.15;

    private double _crank;
    private double[] _speeds = [];
    private (int, double) _speedsFor = (-1, 0);

    /// <summary>The flywheel's heft as drawn, easing toward <see cref="Flywheel"/>; NaN until the first frame.</summary>
    private double _heft = double.NaN;

    static CylindersDiagram()
    {
        AffectsRender<CylindersDiagram>(CountStepProperty, FlywheelProperty);
    }

    public int CountStep
    {
        get => GetValue(CountStepProperty);
        set => SetValue(CountStepProperty, value);
    }

    /// <summary>How many cylinders.</summary>
    public int Count => Cylinders.Choices[Math.Clamp(CountStep, 0, Cylinders.Choices.Count - 1)];

    /// <summary>The flywheel's heft, 1 being an ordinary one.</summary>
    public double Flywheel
    {
        get => GetValue(FlywheelProperty);
        set => SetValue(FlywheelProperty, value);
    }

    private double[] Speeds
    {
        get
        {
            if (_speedsFor != (Count, Flywheel))
            {
                _speeds = Cylinders.SpeedThroughCycle(Count, Flywheel);
                _speedsFor = (Count, Flywheel);
            }

            return _speeds;
        }
    }

    /// <summary>The crank's speed now, as a share above or below its average.</summary>
    private double Now => Speeds[(int)(_crank / 720 * Speeds.Length) % Speeds.Length];

    protected override void Advance(double seconds)
    {
        _crank = (_crank + (Speed * (1 + Now) * seconds)) % 720;
        var heft = Math.Clamp(Flywheel, LightestFlywheel, HeaviestFlywheel);
        _heft = double.IsNaN(_heft) ? heft : Glide(_heft, heft, seconds, 0.15);
    }

    /// <summary>The flywheel's radius on screen for a heft of <paramref name="heft"/>: its mass goes as the square.</summary>
    private static double FlywheelRadius(Rect bounds, double heft) => Math.Min(bounds.Width * 0.24, bounds.Height * 0.2) * Math.Sqrt(heft / 3);

    /// <summary>The heft (to a tenth) whose rim reaches <paramref name="reach"/> pixels from the flywheel's middle.</summary>
    internal static double HeftForRim(Rect bounds, double reach)
    {
        var most = FlywheelRadius(bounds, 3);
        var ratio = Math.Max(0, reach) / most;
        return Math.Clamp(Math.Round(3 * ratio * ratio, 1), LightestFlywheel, HeaviestFlywheel);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var force = new Rect(bounds.X + 40, bounds.Y + 44, bounds.Width - 64, bounds.Height * 0.36);
        DrawForce(context, force);

        // The reader's handle on the graph: tap for another cylinder, round from the most back to one.
        Grip("count", new Point(force.X - 20, force.Center.Y), GripAxis.Tap, DiagramText.Get("Car_Grip_Count"),
            _ => CountStep = (Math.Clamp(CountStep, 0, Cylinders.Choices.Count - 1) + 1) % Cylinders.Choices.Count);

        // The flywheel, bigger for a heavier one, turning with the crank.
        var centre = new Point(bounds.Width * 0.32, bounds.Height * 0.72);
        var radius = FlywheelRadius(bounds, double.IsNaN(_heft) ? Math.Clamp(Flywheel, LightestFlywheel, HeaviestFlywheel) : _heft);
        var angle = _crank * Math.PI / 180;
        context.DrawEllipse(CarParts.DarkMetalBrush, CarParts.Edge, centre, radius, radius);
        context.DrawEllipse(null, new ImmutablePen(CarParts.MetalBrush, Math.Max(2, radius * 0.12)), centre, radius * 0.9, radius * 0.9);
        for (var k = 0; k < 4; k++)
        {
            var a = angle + (k * Math.PI / 2);
            context.DrawLine(new ImmutablePen(CarParts.MetalBrush, Math.Max(2, radius * 0.08)), centre,
                new Point(centre.X + (radius * 0.8 * Math.Cos(a)), centre.Y + (radius * 0.8 * Math.Sin(a))));
        }

        context.DrawEllipse(DiagramText.Gold, null, new Point(centre.X + (radius * 0.9 * Math.Cos(angle)), centre.Y + (radius * 0.9 * Math.Sin(angle))), 4, 4);
        Mark("flywheel", centre, Math.Max(radius, 14));
        DiagramText.Draw(context, DiagramText.Get("Car_Cylinders_FlywheelLabel"), new Point(centre.X, bounds.Bottom - 24), 12, DiagramText.Faint, 0.5, 1);
        Grip("flywheel", new Point(centre.X, centre.Y - radius), GripAxis.Vertical, DiagramText.Get("Car_Grip_Flywheel"),
            at => Flywheel = HeftForRim(bounds, centre.Y - at.Y));

        // The gauge: the crank's speed now against its average.
        var dial = new Point(bounds.Width * 0.74, bounds.Height * 0.76);
        var dialRadius = Math.Min(bounds.Width * 0.18, bounds.Height * 0.15);
        var arc = new StreamGeometry();
        using (var g = arc.Open())
        {
            g.BeginFigure(new Point(dial.X - dialRadius, dial.Y), false);
            g.ArcTo(new Point(dial.X + dialRadius, dial.Y), new Size(dialRadius, dialRadius), 0, false, SweepDirection.Clockwise);
            g.EndFigure(false);
        }

        context.DrawGeometry(null, new ImmutablePen(CarParts.Track, 8), arc);
        var swing = Cylinders.Wobble(Count, Flywheel);
        foreach (var edge in new[] { -swing, swing })
        {
            var a = Math.PI * (1.5 + (Math.Clamp(edge / GaugeRange, -1, 1) / 2));
            context.DrawLine(new ImmutablePen(CarParts.Hot, 2), dial + Polar(dialRadius * 0.85, a), dial + Polar(dialRadius * 1.08, a));
        }

        var needle = Math.PI * (1.5 + (Math.Clamp(Now / GaugeRange, -1, 1) / 2));
        context.DrawLine(new ImmutablePen(Brushes.White, 2.5, lineCap: PenLineCap.Round), dial, dial + Polar(dialRadius * 0.9, needle));
        context.DrawEllipse(Brushes.White, null, dial, 4, 4);
        DiagramText.Draw(context, DiagramText.Get("Car_Cylinders_Slower"), dial + new Point(-dialRadius, 6), 10.5, DiagramText.Faint, 0.5);
        DiagramText.Draw(context, DiagramText.Get("Car_Cylinders_Faster"), dial + new Point(dialRadius, 6), 10.5, DiagramText.Faint, 0.5);
        DiagramText.Draw(context, DiagramText.Format("Car_Cylinders_Swing", (swing * 100).ToString("0.0")), dial + new Point(0, 24), 12.5, DiagramText.Ink, 0.5, 0, bold: true);
        Mark("gauge", [dial - new Point(dialRadius, dialRadius), dial + new Point(dialRadius, 20)], 4);
    }

    /// <summary>The turning force through a cycle: each cylinder faint, the sum gold, the average dashed, a line for now.</summary>
    private void DrawForce(DrawingContext context, Rect area)
    {
        const double High = 1.6;
        const double Low = -0.4;
        double X(double d) => area.X + (d / 720 * area.Width);
        double Y(double f) => area.Bottom - ((f - Low) / (High - Low) * area.Height);
        DiagramText.Draw(context, DiagramText.Format("Car_Cylinders_Force", Count), new Point(area.X, area.Y - 8), 12, DiagramText.Ink, 0, 1, bold: true);
        CarParts.DrawCycleAxis(context, area, 720, Y(0));
        context.DrawLine(CarParts.Axis, new Point(area.X, area.Y), new Point(area.X, area.Bottom));
        var faint = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x70, 0x9F, 0xB0, 0xC8)), 1);
        if (Count > 1)
        {
            for (var k = 0; k < Count; k++)
            {
                var one = new List<Point>();
                for (var d = 0.0; d <= 720; d += 4) one.Add(new Point(X(d), Y(Cylinders.One(d - (k * 720.0 / Count)))));
                CarParts.DrawLine(context, one, faint);
            }
        }

        var total = new List<Point>();
        for (var d = 0.0; d <= 720; d += 3) total.Add(new Point(X(d), Y(Cylinders.Total(d, Count))));
        CarParts.DrawLine(context, total, new ImmutablePen(DiagramText.Gold, 2.5));
        var mean = Y(Cylinders.Mean(Count));
        context.DrawLine(new ImmutablePen(DiagramText.Cyan, 1.2, new ImmutableDashStyle([5, 4], 0)), new Point(area.X, mean), new Point(area.Right, mean));
        DiagramText.Draw(context, DiagramText.Get("Car_Cylinders_Average"), new Point(area.Right, mean - 3), 10.5, DiagramText.Cyan, 1, 1);
        context.DrawLine(new ImmutablePen(Brushes.White, 1.2), new Point(X(_crank), area.Y), new Point(X(_crank), area.Bottom));
        Mark("graph", total, 4);
    }

    private static Point Polar(double radius, double angle) => new(radius * Math.Cos(angle), radius * Math.Sin(angle));
}
