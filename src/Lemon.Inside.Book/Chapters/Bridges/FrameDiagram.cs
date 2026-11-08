using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Bridges.BridgesParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Bridges;

/// <summary>
/// Spread 2: four bars pinned into a square, its feet pinned to the ground, a hand pushing its top sideways. Without a
/// brace the square leans into a parallelogram, every bar still its own length; with a brace from a foot to the far top
/// corner it is two triangles and stays square, the brace stretched. The hand pushes through a little spring that
/// squeezes up as it pushes harder (so its length shows the force); the frame swings over and settles, and fitting
/// the brace swings it back upright. The reader drags the hand and taps the brace to fit it or take it out.
/// </summary>
public sealed class FrameDiagram : GearPicture
{
    public static readonly StyledProperty<double> PushProperty =
        AvaloniaProperty.Register<FrameDiagram, double>(nameof(Push), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> BraceProperty =
        AvaloniaProperty.Register<FrameDiagram, bool>(nameof(Brace), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The square's side, m.</summary>
    private const double Side = 3;

    /// <summary>From the hand's middle to the corner it pushes, m, with no push; how much the spring between squeezes a tonne, m.</summary>
    private const double Reach = 1.3;
    private const double Squeeze = 0.3;

    /// <summary>The drawn lean, degrees, swinging toward the frame's lean; null until the first frame.</summary>
    private Bounce? _lean;

    static FrameDiagram()
    {
        AffectsRender<FrameDiagram>(PushProperty, BraceProperty);
    }

    /// <summary>The sideways push, tonnes.</summary>
    public double Push
    {
        get => GetValue(PushProperty);
        set => SetValue(PushProperty, value);
    }

    public bool Brace
    {
        get => GetValue(BraceProperty);
        set => SetValue(BraceProperty, value);
    }

    private double Settled => Frame.Lean(Math.Clamp(Push, 0, Frame.MostPush), Brace);

    /// <summary>The frame swings over to its new lean (or back upright when braced) and settles.</summary>
    protected override void Advance(double seconds) =>
        _lean = (_lean ?? Bounce.At(Settled)).Step(Settled, seconds, 7, 0.3);

    /// <summary>Across from the left foot to the hand's middle, m, pushing with <paramref name="push"/> tonnes once settled.</summary>
    internal static double HandAt(double push, bool brace) =>
        (Side * Math.Sin(Frame.Lean(push, brace) * Math.PI / 180)) - Reach + (Squeeze * push);

    /// <summary>The push that puts the hand <paramref name="across"/> m from the left foot (the way back from <see cref="HandAt"/>).</summary>
    internal static double PushFor(double across, bool brace)
    {
        double low = 0, high = Frame.MostPush;
        if (across <= HandAt(low, brace)) return low;
        if (across >= HandAt(high, brace)) return high;
        for (var i = 0; i < 40; i++)
        {
            var mid = (low + high) / 2;
            if (HandAt(mid, brace) < across) low = mid;
            else high = mid;
        }

        return (low + high) / 2;
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var push = Math.Clamp(Push, 0, Frame.MostPush);
        var lean = (_lean ??= Bounce.At(Settled)).Value * Math.PI / 180;
        var scale = Math.Min((bounds.Width - 80) / 7, (bounds.Height - 140) / 4.6);
        var v = new View(new Point(bounds.Center.X - (1.2 * scale), bounds.Center.Y + (1.9 * scale)), scale);

        context.FillRectangle(Ground, new Rect(bounds.X, v.At(0, 0).Y, bounds.Width, bounds.Bottom - v.At(0, 0).Y));

        // The corners: the feet stay put, the top swings over with every bar its own length.
        var feet = (Left: v.At(0, 0), Right: v.At(Side, 0));
        var dx = Side * Math.Sin(lean);
        var dy = Side * Math.Cos(lean);
        var tops = (Left: v.At(dx, dy), Right: v.At(Side + dx, dy));
        var thick = Math.Max(6, 0.16 * scale);

        // The brace: drawn faint where it could go, solid and stretched once it is fitted.
        var brace = new[] { feet.Left, tops.Right };
        if (Brace)
        {
            context.DrawLine(new ImmutablePen(push > 0.001 ? StretchBrush : Idle, thick, null, PenLineCap.Round), feet.Left, tops.Right);
            if (push > 0.001)
            {
                DiagramText.Draw(context, DiagramText.Format("Bridges_Frame_BraceLabel", Tonnes(Frame.BraceForce(push))),
                    new Point(((feet.Left.X + tops.Right.X) / 2) + 10, ((feet.Left.Y + tops.Right.Y) / 2) + 6), 12.5, StretchBrush, 0, 0, bold: true);
            }
        }
        else
        {
            context.DrawLine(new ImmutablePen(DiagramText.Faint, 1.5, new ImmutableDashStyle([4, 4], 0)), feet.Left, tops.Right);
        }

        Mark("brace", brace, 8);

        var barPen = new ImmutablePen(Wood, thick, null, PenLineCap.Round);
        Point[][] bars = [[feet.Left, tops.Left], [tops.Left, tops.Right], [tops.Right, feet.Right], [feet.Left, feet.Right]];
        foreach (var bar in bars) context.DrawLine(barPen, bar[0], bar[1]);
        Mark("bars", bars.SelectMany(b => b).ToList(), 6);

        Point[] pins = [feet.Left, feet.Right, tops.Left, tops.Right];
        foreach (var pin in pins) DrawPin(context, pin, Math.Max(5, thick * 0.6));
        Mark("pins", feet.Right, Math.Max(10, thick * 1.4));

        // The hand pushing the top-left corner to the right through a spring: the harder, the shorter the spring.
        var handAt = new Point(tops.Left.X - ((Reach - (Squeeze * push)) * scale), tops.Left.Y);
        var hand = DrawHand(context, handAt, 0.45 * scale);
        Mark("hand", hand);
        DrawSpring(context, new Point(hand.Right, tops.Left.Y), new Point(tops.Left.X - (thick * 0.6), tops.Left.Y), 0.14 * scale);
        if (push > 0.001)
        {
            var mid = new Point((hand.Right + tops.Left.X) / 2, tops.Left.Y - (0.3 * scale));
            DiagramText.Draw(context, Tonnes(push), mid, 12.5, Lifting.LiftingParts.Push, 0.5, 1, bold: true);
        }

        if (!Brace && push > 0.001)
        {
            DiagramText.Draw(context, DiagramText.Format("Bridges_Frame_LeanLabel", Number(Frame.Lean(push, false), "0")),
                v.At(Side / 2 + dx / 2, dy / 2), 13, DiagramText.Gold, 0.5, 0.5, bold: true);
        }

        DiagramText.Draw(context, DiagramText.Get("Bridges_Frame_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
        DrawLegend(context, bounds);

        // The reader's handles: the hand, dragged to push harder, and the brace, tapped to fit it or take it out.
        var braced = Brace;
        var foot = feet.Left.X;
        Grip("push", handAt, GripAxis.Horizontal, DiagramText.Get("Bridges_Grip_Push"),
            at => Push = Math.Round(PushFor((at.X - foot) / scale, braced) * 20) / 20);
        var tap = new Point(feet.Left.X + ((tops.Right.X - feet.Left.X) * 0.3), feet.Left.Y + ((tops.Right.Y - feet.Left.Y) * 0.3));
        Grip("brace", tap, GripAxis.Tap, DiagramText.Get(braced ? "Bridges_Grip_BraceOut" : "Bridges_Grip_BraceIn"), _ => Brace = !Brace);
    }

    /// <summary>A coil spring drawn as a zigzag from <paramref name="a"/> to <paramref name="b"/> (level), <paramref name="half"/> high each side.</summary>
    private static void DrawSpring(DrawingContext context, Point a, Point b, double half)
    {
        if (b.X - a.X < 4) return;
        const int Coils = 7;
        var points = new List<Point> { a };
        for (var k = 0; k < 2 * Coils; k++)
        {
            var x = a.X + ((b.X - a.X) * (k + 0.5) / (2 * Coils));
            points.Add(new Point(x, a.Y + (k % 2 == 0 ? -half : half)));
        }

        points.Add(b);
        context.DrawGeometry(null, new ImmutablePen(Lifting.LiftingParts.Push, 2, lineJoin: PenLineJoin.Round), Plumbing.PlumbingParts.Polyline(points));
    }
}
