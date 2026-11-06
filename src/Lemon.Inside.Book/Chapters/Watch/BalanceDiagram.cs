using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Watch;

/// <summary>
/// Spread 4: the balance and its hairspring swinging, five times slower than life, and below, a trace of its angle
/// over time. A stiffer spring swings faster; a wider swing takes just as long.
/// </summary>
public sealed class BalanceDiagram : GearPicture
{
    public static readonly StyledProperty<double> StiffnessProperty =
        AvaloniaProperty.Register<BalanceDiagram, double>(nameof(Stiffness), 1);

    public static readonly StyledProperty<double> AmplitudeProperty =
        AvaloniaProperty.Register<BalanceDiagram, double>(nameof(Amplitude), 270);

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

    protected override void Advance(double seconds)
    {
        var watchSeconds = seconds / Slower;
        _phase += 2 * Math.PI * Balance.Frequency(Stiffness) * watchSeconds;
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

        // A mark on the frame where the balance rests, and the swing either side.
        var rest = new Point(centre.X, centre.Y - (radius * 1.22));
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 2), rest, rest + new Point(0, -10));

        // The trace: angle against time, newest on the right.
        var area = new Rect(bounds.X + 24, centre.Y + radius + 44, bounds.Width - 48, bounds.Bottom - centre.Y - radius - 72);
        if (area.Height < 30)
        {
            return;
        }

        var mid = area.Center.Y;
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
