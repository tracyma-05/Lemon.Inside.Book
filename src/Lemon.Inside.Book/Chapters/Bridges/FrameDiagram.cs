using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Bridges.BridgesParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Bridges;

/// <summary>
/// Spread 2: four bars pinned into a square, its feet pinned to the ground, a hand pushing its top sideways. Without a
/// brace the square leans into a parallelogram, every bar still its own length; with a brace from a foot to the far top
/// corner it is two triangles and stays square, the brace stretched.
/// </summary>
public sealed class FrameDiagram : GearPicture
{
    public static readonly StyledProperty<double> PushProperty =
        AvaloniaProperty.Register<FrameDiagram, double>(nameof(Push));

    public static readonly StyledProperty<bool> BraceProperty =
        AvaloniaProperty.Register<FrameDiagram, bool>(nameof(Brace));

    /// <summary>The square's side, m.</summary>
    private const double Side = 3;

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

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var push = Math.Clamp(Push, 0, Frame.MostPush);
        var lean = Frame.Lean(push, Brace) * Math.PI / 180;
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

        // The hand pushing the top-left corner to the right.
        var handAt = new Point(tops.Left.X - (0.9 * scale), tops.Left.Y);
        var hand = DrawHand(context, handAt, 0.45 * scale);
        Mark("hand", hand);
        if (push > 0.001)
        {
            DrawPush(context, new Point(hand.Right + 2, tops.Left.Y), new Point(tops.Left.X - thick, tops.Left.Y), Lifting.LiftingParts.Push, Tonnes(push), 0.5, 1.6);
        }

        if (!Brace && push > 0.001)
        {
            DiagramText.Draw(context, DiagramText.Format("Bridges_Frame_LeanLabel", Number(Frame.Lean(push, false), "0")),
                v.At(Side / 2 + dx / 2, dy / 2), 13, DiagramText.Gold, 0.5, 0.5, bold: true);
        }

        DiagramText.Draw(context, DiagramText.Get("Bridges_Frame_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
        DrawLegend(context, bounds);
    }
}
