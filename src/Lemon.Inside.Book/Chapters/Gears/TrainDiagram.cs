using Avalonia.Media;
using Avalonia;
using Avalonia.Data;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Gears;

/// <summary>
/// Spread 5: a gear train. A small gold gear drives a big one; a small gear on the big one's axle drives the next big
/// one, and so on. Each step turns the speed down <see cref="StepRatio"/> times. The reader can turn the gold input by
/// hand (the motor waits while it is held) and feel how little the last gear moves, and tap the last gear to add a step.
/// </summary>
public sealed class TrainDiagram : GearPicture
{
    public static readonly StyledProperty<int> StepsProperty =
        AvaloniaProperty.Register<TrainDiagram, int>(nameof(Steps), 2, defaultBindingMode: BindingMode.TwoWay);

    public const int SmallTeeth = 12;
    public const int BigTeeth = 36;
    public const int StepRatio = BigTeeth / SmallTeeth;

    /// <summary>Turns per second of the gold input gear.</summary>
    private const double InputSpeed = 0.5;

    private static readonly double Pressure = 20 * Math.PI / 180;

    private double _turns;

    static TrainDiagram()
    {
        AffectsRender<TrainDiagram>(StepsProperty);
    }

    public int Steps
    {
        get => GetValue(StepsProperty);
        set => SetValue(StepsProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StepsProperty)
        {
            _turns = 0;
        }
    }

    /// <summary>The slider's range of steps.</summary>
    private const int MostSteps = 4;

    /// <summary>Turns of the input so far (for the tests).</summary>
    internal double Turns => _turns;

    protected override void Advance(double seconds)
    {
        if (!Gripping)
        {
            _turns += InputSpeed * seconds;
        }
    }

    /// <summary>The steps after a tap on the last gear: one more, and back to one after the most.</summary>
    internal static int NextSteps(int steps) => (Math.Clamp(steps, 1, MostSteps) % MostSteps) + 1;

    /// <summary>The axles' centres (in modules) and the direction from each to the next: right, zig-zagging up and down.</summary>
    public static (Point[] Axles, double[] Towards) Layout(int steps)
    {
        var axles = new Point[steps + 1];
        var towards = new double[steps];
        var step = GearGeometry.PitchRadius(SmallTeeth) + GearGeometry.PitchRadius(BigTeeth);
        for (var i = 0; i < steps; i++)
        {
            towards[i] = (i % 2 == 0 ? -1 : 1) * 32 * Math.PI / 180;
            axles[i + 1] = axles[i] + new Point(step * Math.Cos(towards[i]), step * Math.Sin(towards[i]));
        }

        return (axles, towards);
    }

    /// <summary>Each axle's angle after <paramref name="turns"/> turns of the input; the small and big gear on an axle turn together.</summary>
    public static double[] Angles(int steps, double turns)
    {
        var (_, towards) = Layout(steps);
        var angles = new double[steps + 1];
        angles[0] = -2 * Math.PI * turns;
        for (var i = 0; i < steps; i++)
        {
            angles[i + 1] = GearGeometry.MeshAngle(SmallTeeth, angles[i], BigTeeth, towards[i]);
        }

        return angles;
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var steps = Math.Clamp(Steps, 1, MostSteps);
        var (axles, _) = Layout(steps);
        var angles = Angles(steps, _turns);
        var big = GearGeometry.PitchRadius(BigTeeth) + 1;
        double minX = axles.Min(a => a.X) - big, maxX = axles.Max(a => a.X) + big;
        double minY = axles.Min(a => a.Y) - big, maxY = axles.Max(a => a.Y) + big;
        var scale = Math.Min(bounds.Width * 0.92 / (maxX - minX), bounds.Height * 0.72 / (maxY - minY));
        var origin = new Point(
            bounds.Center.X - ((minX + maxX) / 2 * scale),
            (bounds.Height * 0.46) + ((minY + maxY) / 2 * scale));

        // Big gears first (each later one over the one before), then the small gears on top.
        for (var i = 1; i <= steps; i++)
        {
            DrawGear(context, BigTeeth, Pressure, ToScreen(axles[i], origin, scale), scale, angles[i], i == steps ? Copper : Steel, 0.82, mark: i == steps);
        }

        for (var i = 0; i < steps; i++)
        {
            DrawGear(context, SmallTeeth, Pressure, ToScreen(axles[i], origin, scale), scale, angles[i], i == 0 ? Gold : Teal, mark: i == 0);
        }

        var output = ToScreen(axles[steps], origin, scale);
        Mark("input", ToScreen(axles[0], origin, scale), (GearGeometry.PitchRadius(SmallTeeth) + 1) * scale);
        Mark("output", output, big * scale);
        DrawHand(context, output, big * scale * 0.8, angles[steps]);

        var text = Math.Max(12, Math.Min(14, bounds.Width / 42));
        var input = ToScreen(axles[0], origin, scale);
        var low = bounds.Height - 30;
        DiagramText.Draw(context, DiagramText.Format("Gears_Train_In", _turns.ToString("0.0")), new Point(bounds.X + 16, low), text, DiagramText.Gold);
        DiagramText.Draw(context, DiagramText.Format("Gears_Train_Out", (_turns / Math.Pow(StepRatio, steps)).ToString("0.000")),
            new Point(bounds.Right - 16, low), text, DiagramText.Ink, 1);
        DiagramText.Draw(context, DiagramText.Get("Gears_Train_InLabel"), input + new Point(0, -(GearGeometry.PitchRadius(SmallTeeth) + 2) * scale), 12, DiagramText.Gold, 0.5, 1);

        // A handle on the gold gear, along its marked tooth, to turn the input by hand; a tap on the last gear's middle
        // adds a step.
        var reach = GearGeometry.PitchRadius(SmallTeeth) * 0.55;
        var handle = ToScreen(new Point(Math.Cos(angles[0]) * reach, Math.Sin(angles[0]) * reach), input, scale);
        Grip("turn", handle, GripAxis.Free, DiagramText.Get("Gears_Grip_TurnInput"),
            at => _turns = -GearsGrips.Follow(-2 * Math.PI * _turns, GearsGrips.AngleOf(input, at)) / (2 * Math.PI));
        Grip("step", output, GripAxis.Tap, DiagramText.Get("Gears_Grip_AddStep"), _ => Steps = NextSteps(Steps));
    }

    /// <summary>A pointer on the output gear, like a clock hand.</summary>
    private static void DrawHand(DrawingContext context, Point centre, double length, double angle)
    {
        var tip = ToScreen(new Point(Math.Cos(angle) * length, Math.Sin(angle) * length), centre, 1);
        context.DrawLine(new Avalonia.Media.Immutable.ImmutablePen(Brushes.White, 3.5, lineCap: PenLineCap.Round), centre, tip);
        context.DrawEllipse(Brushes.White, null, centre, 5, 5);
    }
}
