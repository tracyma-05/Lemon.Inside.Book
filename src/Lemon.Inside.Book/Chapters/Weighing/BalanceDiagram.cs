using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;
using static Lemon.Inside.Book.Chapters.Weighing.WeighingParts;

namespace Lemon.Inside.Book.Chapters.Weighing;

/// <summary>
/// Spread 1: an equal-arm balance, in centimetres. The beam turns on a knife edge at the top of its post; its centre
/// of mass (the dark dot) hangs a little below the edge and swings back under it, which is what makes the beam settle
/// at a tilt instead of tipping right over. The pans hang straight down on their threads; a long pointer reads the tilt
/// on a scale at the foot of the post.
/// </summary>
public sealed class BalanceDiagram : GearPicture
{
    public static readonly StyledProperty<double> DifferenceProperty =
        AvaloniaProperty.Register<BalanceDiagram, double>(nameof(Difference));

    public static readonly StyledProperty<double> DropProperty =
        AvaloniaProperty.Register<BalanceDiagram, double>(nameof(Drop), 2);

    /// <summary>The knife edge's height, cm; the pans' threads, cm; the pointer's length, cm.</summary>
    private const double Edge0 = 34;
    private const double Thread0 = 16;
    private const double Needle = 24;

    static BalanceDiagram()
    {
        AffectsRender<BalanceDiagram>(DifferenceProperty, DropProperty);
    }

    /// <summary>Grams more on the right pan than the left.</summary>
    public double Difference
    {
        get => GetValue(DifferenceProperty);
        set => SetValue(DifferenceProperty, value);
    }

    /// <summary>How far the beam's centre of mass hangs below the knife edge, cm (negative: above it).</summary>
    public double Drop
    {
        get => GetValue(DropProperty);
        set => SetValue(DropProperty, value);
    }

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var difference = Math.Clamp(Difference, 0, 20);
        var drop = Math.Clamp(Drop, -1, 4);
        var tilt = Balance.Tilt(difference, drop) * Math.PI / 180;
        var scale = Math.Min((bounds.Width - 60) / 46, (bounds.Height - 140) / 46);
        var v = new View(new Point(bounds.Center.X, bounds.Center.Y + (24 * scale)), scale);
        const double arm = Balance.Arm * 100;

        // A point fixed to the beam, (x along it, y up from the edge), after the beam has turned.
        Point OnBeam(double x, double y) => v.At((x * Math.Cos(tilt)) + (y * Math.Sin(tilt)), Edge0 - (x * Math.Sin(tilt)) + (y * Math.Cos(tilt)));

        // The base and the post, with the pointer's scale at its foot.
        context.DrawRectangle(Wood, Edge, v.Box(-16, 0, 16, 2.5), 3, 3);
        context.DrawRectangle(Brass, Edge, v.Box(-1, 2.5, 1, Edge0 - 1));
        var scalePen = new ImmutablePen(DiagramText.Faint, 1);
        var arc = Enumerable.Range(0, 33).Select(i => (-40 + (2.5 * i)) * Math.PI / 180).Select(a => v.At(Math.Sin(a) * Needle, Edge0 - (Math.Cos(a) * Needle))).ToList();
        context.DrawGeometry(null, scalePen, Plumbing.PlumbingParts.Polyline(arc));
        for (var d = -40; d <= 40; d += 10)
        {
            var a = d * Math.PI / 180;
            var inner = v.At(Math.Sin(a) * (Needle - 1.5), Edge0 - (Math.Cos(a) * (Needle - 1.5)));
            var outer = v.At(Math.Sin(a) * Needle, Edge0 - (Math.Cos(a) * Needle));
            context.DrawLine(d == 0 ? new ImmutablePen(DiagramText.Ink, 2) : scalePen, inner, outer);
        }

        // The pans on their threads, straight down from the beam's ends.
        var pans = new List<Point>();
        foreach (var side in new[] { -1, 1 })
        {
            var hang = OnBeam(side * arm, 0);
            var rim = new Point(hang.X, hang.Y + (Thread0 * scale));
            context.DrawLine(ThreadPen, hang, new Point(rim.X - (5 * scale), rim.Y));
            context.DrawLine(ThreadPen, hang, new Point(rim.X + (5 * scale), rim.Y));
            var pan = DrawPan(context, rim, 12 * scale);
            pans.AddRange([pan.TopLeft, pan.BottomRight]);

            // A 100 g weight on each pan, and the extra on the right.
            var weight = new Rect(rim.X - (2 * scale), rim.Y - (4 * scale), 4 * scale, 4 * scale);
            context.DrawRectangle(Iron, Edge, weight, 2, 2);
            DiagramText.Draw(context, Grams(100), new Point(weight.Center.X, weight.Top - 2), 10.5, DiagramText.Ink, 0.5, 1);
            if (side > 0 && difference > 0.01)
            {
                var extra = new Rect(weight.Right + 2, rim.Y - (2 * scale), 2.4 * scale, 2 * scale);
                context.DrawRectangle(DiagramText.Gold, Edge, extra, 1, 1);
                DiagramText.Draw(context, "+" + Grams(difference), new Point(extra.Right + 3, extra.Center.Y), 11.5, DiagramText.Gold, 0, 0.5, bold: true);
            }
        }

        Mark("pans", pans, 4);

        // The knife edge: a little triangle the beam rocks on.
        var edge = v.At(0, Edge0);
        DrawPolygon(context, DiagramText.Gold, Edge, [edge, new Point(edge.X - (1.2 * scale), edge.Y + (1.6 * scale)), new Point(edge.X + (1.2 * scale), edge.Y + (1.6 * scale))]);
        Mark("edge", edge, Math.Max(10, 1.6 * scale));

        // The beam, its centre of mass, and the pointer fixed to it.
        Point[] beam = [OnBeam(-arm - 0.6, 0.5), OnBeam(arm + 0.6, 0.5), OnBeam(arm + 0.6, -0.5), OnBeam(0, -2.2), OnBeam(-arm - 0.6, -0.5)];
        DrawPolygon(context, SteelBrush, Edge, beam);
        Mark("beam", [beam[0], beam[1]], 8);
        var needleTip = OnBeam(0, -Needle);
        context.DrawLine(new ImmutablePen(Warn, 2, null, PenLineCap.Round), OnBeam(0, -1), needleTip);
        Mark("pointer", [OnBeam(0, -Needle * 0.55), needleTip], 6);
        var mass = OnBeam(0, -drop);
        context.DrawEllipse(Dark, new ImmutablePen(DiagramText.Ink, 1.2), mass, 0.7 * scale, 0.7 * scale);
        DiagramText.Draw(context, DiagramText.Get("Weighing_Balance_MassLabel"), new Point(mass.X + (1.4 * scale), mass.Y + (1.2 * scale)), 11, DiagramText.Faint, 0, 0);

        DiagramText.Draw(context,
            Balance.Topples(drop) ? DiagramText.Get("Weighing_Balance_ToppleLabel") : DiagramText.Format("Weighing_Balance_TiltLabel", Number(tilt * 180 / Math.PI)),
            new Point(needleTip.X + 10, needleTip.Y + 6), 13, Balance.Topples(drop) ? Warn : DiagramText.Gold, 0, 0.5, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Weighing_Balance_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
