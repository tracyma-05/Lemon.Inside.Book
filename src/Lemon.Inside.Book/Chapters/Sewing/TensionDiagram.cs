using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Sewing.SewingParts;

namespace Lemon.Inside.Book.Chapters.Sewing;

/// <summary>
/// Spread 6: a lockstitch seam cut along its length, with the two pulls on it: the upper thread's (red, up), set on
/// the tension dial, and the bobbin thread's (blue, down), set by the little screw on the bobbin case. The knots slide
/// towards whichever pulls harder; pulled right through they show on the far side as dots of the other colour. The
/// reader drags the lever beside the dial up or down, and the screw along its scale.
/// </summary>
public sealed class TensionDiagram : GearPicture
{
    public static readonly StyledProperty<double> DialProperty =
        AvaloniaProperty.Register<TensionDiagram, double>(nameof(Dial), 4, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> BobbinProperty =
        AvaloniaProperty.Register<TensionDiagram, double>(nameof(Bobbin), 30, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Where the knots are drawn, gliding to where the pulls put them.</summary>
    private double _knot = 0.5;

    static TensionDiagram()
    {
        AffectsRender<TensionDiagram>(DialProperty, BobbinProperty);
    }

    /// <summary>The tension dial's number, 0 to 9.</summary>
    public double Dial
    {
        get => GetValue(DialProperty);
        set => SetValue(DialProperty, value);
    }

    /// <summary>The bobbin thread's pull, gf.</summary>
    public double Bobbin
    {
        get => GetValue(BobbinProperty);
        set => SetValue(BobbinProperty, value);
    }

    protected override void Advance(double seconds) => _knot = Glide(_knot, Tension.Knot(Dial, Bobbin), seconds, 0.25);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var dial = Math.Clamp(Dial, 0, 9);
        var bobbin = Math.Clamp(Bobbin, 15, 45);
        var w = bounds.Width;

        // The seam, big, in the middle.
        var top = bounds.Center.Y - 30;
        var bottom = bounds.Center.Y + 30;
        var left = bounds.X + 24;
        var right = bounds.Right - 24;
        var pitch = Math.Min(110, (right - left) / 4.6);
        var x0 = left + (pitch * 0.8);
        var count = (int)Math.Floor((right - x0) / pitch);
        DrawCloth(context, left, right, top, bottom);
        Mark("cloth", new Rect(left, top, x0 - left - 10, bottom - top));
        var knots = DrawLockSeam(context, x0, pitch, count, top, bottom, _knot, Math.Clamp(w * 0.012, 4, 7));
        Mark("knots", knots, 9);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Stitch_Top"), new Point(left, top - 12), 11.5, DiagramText.Faint, 0, 1);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Stitch_Under"), new Point(left, bottom + 12), 11.5, DiagramText.Faint, 0, 0);

        // The two pulls on the last knot, as long as they are strong.
        var last = knots[^1] + new Vector(pitch * 0.5, 0);
        var per = 1.6;
        var upper = Tension.Upper(dial);
        DrawPull(context, new Point(last.X, top - 8), new Point(last.X, top - 8 - (upper * per)), UpperBrush,
            DiagramText.Format("Sewing_Grams", Number(upper, "0")));
        DrawPull(context, new Point(last.X, bottom + 8), new Point(last.X, bottom + 8 + (bobbin * per)), LowerBrush,
            DiagramText.Format("Sewing_Grams", Number(bobbin, "0")));

        // The tension dial, top left, with its lever beside it.
        var radius = Math.Min(60, w * 0.11);
        var centre = new Point(bounds.X + radius + 30, top - radius - 70);
        context.DrawEllipse(Body, Lifting.LiftingParts.Edge, centre, radius, radius);
        for (var k = 0; k <= 9; k++)
        {
            var a = (-135 + (k * 30)) * Math.PI / 180;
            DiagramText.Draw(context, Number(k, "0"), centre + (new Vector(Math.Sin(a), -Math.Cos(a)) * radius * 0.75), 11, DiagramText.Ink, 0.5, 0.5);
        }

        var pointer = (-135 + (dial * 30)) * Math.PI / 180;
        context.DrawLine(new ImmutablePen(UpperBrush, 3, lineCap: PenLineCap.Round), centre,
            centre + (new Vector(Math.Sin(pointer), -Math.Cos(pointer)) * radius * 0.5));
        Mark("dial", centre, radius);
        var high = centre.Y - radius;
        var low = centre.Y + radius;
        var leverX = centre.X + radius + 22;
        context.DrawLine(new ImmutablePen(Lifting.LiftingParts.Dark, 6, lineCap: PenLineCap.Round), new Point(leverX, high), new Point(leverX, low));
        Grip("dial", new Point(leverX, low - ((low - high) * dial / 9)), GripAxis.Vertical, DiagramText.Get("Sewing_Grip_Dial"),
            at => Dial = Math.Round(Math.Clamp((low - at.Y) / (low - high) * 9, 0, 9) * 2) / 2);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Tension_DialLabel"), new Point(centre.X, centre.Y + radius + 6), 11.5, DiagramText.Faint, 0.5);

        // The bobbin case, bottom left, with its screw and a scale for it.
        var caseAt = new Point(bounds.X + 70, bottom + 110);
        context.DrawEllipse(DarkMetal, Lifting.LiftingParts.Edge, caseAt, 34, 34);
        context.DrawEllipse(LowerBrush, null, caseAt, 20, 20);
        var screw = caseAt + new Vector(26, -22);
        context.DrawEllipse(Metal, Lifting.LiftingParts.Edge, screw, 7, 7);
        context.DrawLine(new ImmutablePen(Lifting.LiftingParts.Dark, 1.5), screw + new Vector(-5, 0), screw + new Vector(5, 0));
        Mark("screw", [caseAt + new Vector(-34, -34), caseAt + new Vector(34, 34)], 4);
        var s0 = caseAt.X + 60;
        var s1 = Math.Min(bounds.Right - 40, s0 + 240);
        var sy = caseAt.Y;
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 1), new Point(s0, sy), new Point(s1, sy));
        DiagramText.Draw(context, DiagramText.Get("Sewing_Tension_Looser"), new Point(s0, sy + 12), 11, DiagramText.Faint, 0);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Tension_Tighter"), new Point(s1, sy + 12), 11, DiagramText.Faint, 1);
        Grip("screw", new Point(s0 + ((s1 - s0) * (bobbin - 15) / 30), sy), GripAxis.Horizontal, DiagramText.Get("Sewing_Grip_Screw"),
            at => Bobbin = Math.Round(15 + (30 * Math.Clamp((at.X - s0) / (s1 - s0), 0, 1))));

        var verdict = Tension.Verdict(dial, bobbin);
        DiagramText.Draw(context, DiagramText.Get(verdict switch { -1 => "Sewing_Tension_ShowTight", 1 => "Sewing_Tension_ShowLoose", _ => "Sewing_Tension_ShowEven" }),
            new Point(bounds.Center.X + 40, bounds.Bottom - 34), 13, verdict == 0 ? DiagramText.Gold : UpperBrush, 0.5, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Tension_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }

    private static void DrawPull(DrawingContext context, Point from, Point to, IBrush brush, string label)
    {
        context.DrawLine(new ImmutablePen((IImmutableBrush)brush, 3), from, to);
        DiagramText.DrawArrowAlong(context, from, to, brush, 10);
        DiagramText.Draw(context, label, new Point(to.X - 10, (from.Y + to.Y) / 2), 12, brush, 1, 0.5);
    }
}
