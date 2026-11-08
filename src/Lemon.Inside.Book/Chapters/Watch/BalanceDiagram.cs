using Avalonia;
using Avalonia.Data;
using Lemon.Inside.Book.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Watch;

/// <summary>
/// Spread 4: the balance and its hairspring swinging, five times slower than life, and below, a trace of its angle
/// over time. A stiffer spring swings faster; a wider swing takes just as long. The reader can pull the balance round
/// by its rim and let go (how far is the swing), and move the regulator's index on the right, which shortens or
/// lengthens the working hairspring as a watchmaker does (how stiff it is).
/// </summary>
public sealed class BalanceDiagram : GearPicture
{
    public static readonly StyledProperty<double> StiffnessProperty =
        AvaloniaProperty.Register<BalanceDiagram, double>(nameof(Stiffness), 1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> AmplitudeProperty =
        AvaloniaProperty.Register<BalanceDiagram, double>(nameof(Amplitude), 270, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The picture runs this much slower than life.</summary>
    public const double Slower = 5;

    /// <summary>Seconds of the watch's time the trace shows.</summary>
    private const double TraceSeconds = 2;

    private static readonly IPen TracePen = new ImmutablePen(DiagramText.Cyan, 2, lineJoin: PenLineJoin.Round);
    private static readonly IPen AxisPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x50, 0xE6, 0xEA, 0xF2)), 1);

    /// <summary>How far round its swing the balance is, in radians (kept, so changing the spring does not make it jump).</summary>
    private double _phase;

    /// <summary>The watch's time in the picture, in seconds.</summary>
    private double _time;

    private readonly Queue<(double Time, double Angle)> _trace = new();

    public double Stiffness
    {
        get => GetValue(StiffnessProperty);
        set => SetValue(StiffnessProperty, value);
    }

    public double Amplitude
    {
        get => GetValue(AmplitudeProperty);
        set => SetValue(AmplitudeProperty, value);
    }

    /// <summary>The sliders' ranges.</summary>
    private const double Softest = 0.8, Stiffest = 1.25, Narrowest = 120, Widest = 300;

    /// <summary>The regulator's index sweeps this far either side of 3 o'clock, radians.</summary>
    private static readonly double IndexSweep = 35 * Math.PI / 180;

    private static readonly IPen IndexPen = new ImmutablePen(new ImmutableSolidColorBrush(WatchParts.Steel), 3, lineCap: PenLineCap.Round);
    private static readonly IPen ScalePen = new ImmutablePen(DiagramText.Faint, 1.5);

    /// <summary>The stiffness the regulator's index sets at <paramref name="angle"/> (radians clockwise from 3 o'clock).</summary>
    internal static double StiffnessAt(double angle) =>
        Math.Round((Softest + ((Stiffest - Softest) * (Math.Clamp(angle, -IndexSweep, IndexSweep) + IndexSweep) / (2 * IndexSweep))) * 200) / 200;

    /// <summary>Where the regulator's index points for <paramref name="stiffness"/>, radians clockwise from 3 o'clock.</summary>
    private static double IndexAngle(double stiffness) =>
        -IndexSweep + (2 * IndexSweep * (Math.Clamp(stiffness, Softest, Stiffest) - Softest) / (Stiffest - Softest));

    /// <summary>The balance swings on; held by the reader, it waits where it is put.</summary>
    protected override void Advance(double seconds)
    {
        var watchSeconds = seconds / Slower;
        if (!Gripping)
        {
            _phase += 2 * Math.PI * Balance.Frequency(Stiffness) * watchSeconds;
        }

        _trace.Enqueue((_time += watchSeconds, Amplitude * Math.Sin(_phase)));
        while (_trace.Count > 0 && _trace.Peek().Time < _time - TraceSeconds)
        {
            _trace.Dequeue();
        }
    }


    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var angle = Amplitude * Math.Sin(_phase);
        var radius = Math.Min(bounds.Width * 0.27, bounds.Height * 0.25);
        var centre = new Point(bounds.Center.X, (bounds.Height * 0.08) + radius + 14);
        WatchParts.DrawBalance(context, centre, radius, angle, pin: false);
        WatchParts.DrawHairspring(context, centre, radius * 0.16, radius * 0.62, angle);
        Mark("balance", centre, radius * 1.1);
        Mark("hairspring", centre, radius * 0.62);

        // A mark on the frame where the balance rests, and the swing either side.
        var rest = new Point(centre.X, centre.Y - (radius * 1.22));
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 2), rest, rest + new Point(0, -10));

        // The regulator: an index over the hairspring, its tip on a little scale to the right. Clockwise, it shortens
        // the working spring: stiffer, faster.
        var reach = radius * 1.32;
        for (var i = 0; i <= 6; i++)
        {
            var a = -IndexSweep + (2 * IndexSweep * i / 6);
            context.DrawLine(ScalePen, WatchParts.At(centre, reach + 4, a), WatchParts.At(centre, reach + (i % 3 == 0 ? 12 : 8), a));
        }

        var index = WatchParts.At(centre, reach, IndexAngle(Stiffness));
        context.DrawLine(IndexPen, WatchParts.At(centre, radius * 0.5, IndexAngle(Stiffness)), index);
        Grip("regulator", index, GripAxis.Free, DiagramText.Get("Watch_Grip_Regulator"),
            at => Stiffness = StiffnessAt(Math.Atan2(at.Y - centre.Y, at.X - centre.X)));

        // The rim where the roller's pin points: pulled round and let go, the balance swings that far.
        var held = angle * Math.PI / 180;
        Grip("swing", WatchParts.At(centre, radius, held + (Math.PI / 2)), GripAxis.Free, DiagramText.Get("Watch_Grip_PullBalance"), at =>
        {
            var towards = Math.Atan2(at.Y - centre.Y, at.X - centre.X) - (Math.PI / 2);
            var degrees = (held + Math.IEEERemainder(towards - held, 2 * Math.PI)) * 180 / Math.PI;
            held = degrees * Math.PI / 180;
            Amplitude = Math.Round(Math.Clamp(Math.Abs(degrees), Narrowest, Widest));
            _phase = degrees >= 0 ? Math.PI / 2 : -Math.PI / 2; // at the end of its swing, about to come back
        });

        // The trace: angle against time, newest on the right.
        var area = new Rect(bounds.X + 24, centre.Y + radius + 44, bounds.Width - 48, bounds.Bottom - centre.Y - radius - 72);
        if (area.Height < 30)
        {
            return;
        }

        var mid = area.Center.Y;
        Mark("trace", area.Deflate(4));
        context.DrawLine(AxisPen, new Point(area.X, mid), new Point(area.Right, mid));
        DiagramText.Draw(context, DiagramText.Get("Watch_Balance_Trace"), new Point(area.X, area.Y - 6), 12, DiagramText.Faint, 0, 1);
        var points = _trace.ToArray();
        if (points.Length > 1)
        {
            var line = new StreamGeometry();
            using (var g = line.Open())
            {
                for (var i = 0; i < points.Length; i++)
                {
                    var p = new Point(
                        area.Right - ((_time - points[i].Time) / TraceSeconds * area.Width),
                        mid - (points[i].Angle / 330 * area.Height / 2));
                    if (i == 0) g.BeginFigure(p, false);
                    else g.LineTo(p);
                }

                g.EndFigure(false);
            }

            context.DrawGeometry(null, TracePen, line);
        }

        // One second of the watch's time, as a scale.
        var second = area.Width / TraceSeconds;
        var y = area.Bottom + 10;
        context.DrawLine(new ImmutablePen(DiagramText.Ink, 1.5), new Point(area.Right - second, y), new Point(area.Right, y));
        DiagramText.Draw(context, DiagramText.Get("Watch_Balance_OneSecond"), new Point(area.Right - (second / 2), y + 4), 11.5, DiagramText.Ink, 0.5);
        DiagramText.Draw(context, DiagramText.Format("Watch_Balance_Slower", Slower.ToString("0")), new Point(bounds.X + 16, bounds.Y + 14), 12.5, DiagramText.Ink);
    }
}
