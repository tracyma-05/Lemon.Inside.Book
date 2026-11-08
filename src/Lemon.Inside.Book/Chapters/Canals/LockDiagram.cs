using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Canals.CanalsParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Canals;

/// <summary>
/// Spread 1: a lock cut lengthways, in metres. The lower canal on the left, the chamber with a boat in it, the upper
/// canal on the right three metres higher. Open the paddles in the upper gate and water pours in, fast while the
/// difference is big and slower as the levels close; shut them and the lower paddles let it out again. Time runs
/// thirty times fast. The reader taps either gate to open its paddles and drags the upper gate's grip for more of them;
/// the boat rocks and the surface churns while water runs.
/// </summary>
public sealed class LockDiagram : GearPicture
{
    public static readonly StyledProperty<int> PaddlesProperty =
        AvaloniaProperty.Register<LockDiagram, int>(nameof(Paddles), 1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> FillingProperty =
        AvaloniaProperty.Register<LockDiagram, bool>(nameof(Filling), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>How much faster than life the picture runs.</summary>
    private const double Speedup = 30;

    /// <summary>How much taller than life the heights are drawn.</summary>
    private const double Tall = 3;

    private double _level;

    static LockDiagram()
    {
        AffectsRender<LockDiagram>(PaddlesProperty, FillingProperty);
    }

    public int Paddles
    {
        get => GetValue(PaddlesProperty);
        set => SetValue(PaddlesProperty, value);
    }

    public bool Filling
    {
        get => GetValue(FillingProperty);
        set => SetValue(FillingProperty, value);
    }

    protected override void Advance(double seconds)
    {
        var paddles = Math.Clamp(Paddles, 1, 4);
        var dt = seconds * Speedup;
        if (Filling)
        {
            _level = Math.Min(Chamber.Rise, _level + (Chamber.RiseRate(_level, paddles) * dt) + 1e-4);
        }
        else
        {
            // Emptying through the lower gate's paddles: the same law, the difference now the chamber's height above the lower canal.
            _level = Math.Max(0, _level - (Chamber.RiseRate(Chamber.Rise - _level, paddles) * dt) - 1e-4);
        }
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var paddles = Math.Clamp(Paddles, 1, 4);
        var scale = Math.Min((bounds.Width - 40) / 52, (bounds.Height - 170) / (8 * Tall));
        var v = new View(new Point(bounds.Center.X - (15 * scale), bounds.Center.Y + (Tall * scale)), scale);
        Point P(double x, double y) => v.At(x, y * Tall);
        Rect B(double x0, double y0, double x1, double y1) => v.Box(x0, y0 * Tall, x1, y1 * Tall);
        const double floor = -2.5;
        const double top = 4.2;
        var rise = Chamber.Rise;

        // Ground and masonry around the cut.
        context.FillRectangle(Ground, new Rect(bounds.X, P(0, top).Y, bounds.Width, bounds.Bottom - P(0, top).Y));
        context.FillRectangle(AirFill, new Rect(P(-11, 0).X, P(0, top).Y, P(41, 0).X - P(-11, 0).X, P(0, floor).Y - P(0, top).Y));
        context.FillRectangle(AirFill, B(30, 0.5, 41, top));
        context.FillRectangle(Ground, B(30, floor, 41, 0.5));

        // The water: lower canal, chamber, upper canal.
        context.FillRectangle(Water, B(-11, floor, 0, 0));
        var chamber = B(0, floor, 30, _level);
        context.FillRectangle(Water, chamber);
        context.FillRectangle(DeepWater, B(30, 0.5, 41, rise));
        Mark("chamber", B(0, floor, 30, top));

        // Water pouring through the open paddles, or out through the lower ones.
        var flowing = Filling ? _level < rise - 0.01 : _level > 0.01;
        if (flowing)
        {
            var travelled = Seconds * 90;
            for (var k = 0; k < paddles; k++)
            {
                var y = Filling ? 0.8 + (k * 0.5) : floor + 0.4 + (k * 0.5);
                var from = Filling ? P(30.5, y) : P(1.5, y);
                var to = Filling ? P(26, y) : P(-3, y);
                Plumbing.PlumbingParts.DrawFlow(context, [from, to], 0.35 * scale, travelled);
            }
        }

        // The gates, with their paddles: small doors near the bottom of each gate.
        var gatePen = new ImmutablePen(Timber, Math.Max(5, 0.6 * scale));
        context.DrawLine(gatePen, P(0, floor), P(0, top - 0.2));
        context.DrawLine(gatePen, P(30, 0.5), P(30, top - 0.2));
        var paddleBoxes = new List<Point>();
        for (var k = 0; k < paddles; k++)
        {
            var upper = B(29.6, 0.6 + (k * 0.5), 30.4, 1.0 + (k * 0.5));
            var lower = B(-0.4, floor + 0.3 + (k * 0.5), 0.4, floor + 0.7 + (k * 0.5));
            context.DrawRectangle(Filling ? Flow : Dark, Edge, upper);
            context.DrawRectangle(!Filling ? Flow : Dark, Edge, lower);
            paddleBoxes.AddRange([upper.TopLeft, upper.BottomRight]);
        }

        Mark("paddles", paddleBoxes, 6);
        Mark("gates", [P(-0.4, floor), P(0.4, top), P(29.6, 0.5), P(30.4, top)], 4);

        // The surface, churned by the water coming in or going out, and the boat rocking on it.
        var churn = flowing ? 0.6 + (0.5 * paddles) : 0.5;
        DrawRipples(context, chamber.Left + 2, chamber.Right - 2, chamber.Top, churn, Seconds);
        DrawRipples(context, B(-11, 0, 0, 0).Left, B(-11, 0, 0, 0).Right, P(0, 0).Y, 0.5, Seconds * 0.7);
        DrawRipples(context, B(30, 0, 41, 0).Left + 2, B(30, 0, 41, 0).Right, P(0, rise).Y, 0.5, Seconds * 0.7);
        var rock = (flowing ? 0.05 * paddles : 0.02) * Math.Sin(Seconds * 2.2);
        var boat = DrawBoat(context, P(15 + (rock * 6), _level - 0.3 + rock), 14 * scale, scale * 1.6);
        Mark("boat", boat);

        // The levels.
        var dash = new ImmutablePen(DiagramText.Faint, 1, new ImmutableDashStyle([4, 4], 0));
        context.DrawLine(dash, P(-11, rise), P(30, rise));
        DiagramText.Draw(context, DiagramText.Get("Canals_Lock_Upper"), P(41, rise + 0.2), 11.5, DiagramText.Ink, 1, 1);
        DiagramText.Draw(context, DiagramText.Get("Canals_Lock_Lower"), P(-11, 0.2), 11.5, DiagramText.Ink, 0, 1);
        DiagramText.Draw(context, DiagramText.Format("Canals_Lock_LevelLabel", Number(_level, "0.0"), Number(rise, "0")), P(15, floor - 0.4), 13,
            DiagramText.Gold, 0.5, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Canals_Lock_Taller"), new Point(bounds.X + 16, bounds.Y + 34), 11, DiagramText.Faint);
        DiagramText.Draw(context, DiagramText.Get("Canals_Lock_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);

        // The reader's handles: a switch on top of each gate, and the upper gate's paddles dragged up for more.
        Grip("fill", P(30, top + 0.35), GripAxis.Tap, DiagramText.Get("Canals_Grip_FillUpper"), _ => Filling = true);
        Grip("empty", P(0, top + 0.35), GripAxis.Tap, DiagramText.Get("Canals_Grip_EmptyLower"), _ => Filling = false);
        Grip("paddles", P(30, 0.8 + (paddles * 0.5)), GripAxis.Vertical, DiagramText.Get("Canals_Grip_Paddles"),
            at => Paddles = Math.Clamp((int)Math.Round(((v.Y(at) / Tall) - 0.8) / 0.5), 1, 4));
    }

    private static readonly IImmutableBrush AirFill = new ImmutableSolidColorBrush(Color.FromRgb(0x14, 0x1A, 0x26));
}
