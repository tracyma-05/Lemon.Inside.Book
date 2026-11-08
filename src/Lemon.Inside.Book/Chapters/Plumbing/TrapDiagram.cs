using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;
using static Lemon.Inside.Book.Chapters.Plumbing.PlumbingParts;

namespace Lemon.Inside.Book.Chapters.Plumbing;

/// <summary>
/// Spread 5: the U-bend under a sink, in centimetres. Water fills the bend up to the drain's lip on the far side; the
/// part above the bend's dip is the seal. Suction in the drain pulls the sink side down (the drain side spills over
/// its lip); once it reaches the dip, drain air bubbles through into the room. A vent pipe on the drain lets air in
/// instead.
/// </summary>
public sealed class TrapDiagram : GearPicture
{
    public static readonly StyledProperty<double> SuctionProperty =
        AvaloniaProperty.Register<TrapDiagram, double>(nameof(Suction), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> VentProperty =
        AvaloniaProperty.Register<TrapDiagram, bool>(nameof(Vent), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The drain's lip (where water spills on), the bend's dip and the bore, cm.</summary>
    private const double Lip = 3;
    private const double Dip = Lip - Trap.Seal;
    private const double Bore = 4;
    private const double VentX = 30;

    static TrapDiagram()
    {
        AffectsRender<TrapDiagram>(SuctionProperty, VentProperty);
    }

    /// <summary>The suction in the drain, cm of water.</summary>
    public double Suction
    {
        get => GetValue(SuctionProperty);
        set => SetValue(SuctionProperty, value);
    }

    public bool Vent
    {
        get => GetValue(VentProperty);
        set => SetValue(VentProperty, value);
    }

    /// <summary>How far the sink side's water has fallen, cm: it is pulled down, or settles back, over a moment.</summary>
    private double _drop = double.NaN;

    private double Drop => double.IsNaN(_drop) ? _drop = Trap.Drop(Math.Clamp(Suction, 0, 12), Vent) : _drop;

    protected override void Advance(double seconds) => _drop = Glide(Drop, Trap.Drop(Math.Clamp(Suction, 0, 12), Vent), seconds, 0.25);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var suction = Math.Clamp(Suction, 0, 12);
        var broken = Trap.Broken(suction, Vent);
        var sinkLevel = Lip - Drop;
        var scale = Math.Min((bounds.Width - 40) / 76, (bounds.Height - 70) / 48);
        var v = new View(new Point(bounds.Center.X - (20 * scale), bounds.Bottom - 30 - (10 * scale)), scale);
        var bore = Bore * scale;

        // The pipe: the sink's tail down, round the bend, up to the drain, which runs off to the right.
        List<Point> bend = [v.At(0, 22), v.At(0, 0)];
        for (var i = 1; i < 24; i++)
        {
            var a = Math.PI + (Math.PI * i / 24);
            bend.Add(v.At(4 + (4 * Math.Cos(a)), 4 * Math.Sin(a)));
        }

        bend.AddRange([v.At(8, 0), v.At(8, Lip + (Bore / 2))]);
        List<Point> drain = [v.At(8, Lip + (Bore / 2)), v.At(44, Lip + (Bore / 2) - 0.8)];
        List<Point> vent = [v.At(VentX, Lip + (Bore / 2) - 0.6), v.At(VentX, 36)];
        DrawPipe(context, vent, bore * 0.75, false);
        DrawPipe(context, [.. bend, drain[1]], bore, false);

        // The water in the bend: up to the drain's lip on the far side, lower on the sink side when the drain sucks.
        var middle = v.At(4, 0).X;
        var waterPen = new ImmutablePen(WaterBrush, bore, null, PenLineCap.Flat, PenLineJoin.Round);
        var sinkY = v.At(0, sinkLevel).Y;
        var lipY = v.At(0, Lip).Y;
        using (context.PushClip(new Rect(bounds.X, sinkY, middle - bounds.X, Math.Max(0, bounds.Bottom - sinkY))))
        {
            context.DrawGeometry(null, waterPen, Polyline(bend));
        }

        using (context.PushClip(new Rect(middle, lipY, bounds.Right - middle, Math.Max(0, bounds.Bottom - lipY))))
        {
            context.DrawGeometry(null, waterPen, Polyline(bend));
        }

        Mark("seal", v.Box(-2.5, -6.5, 10.5, Lip + 0.5));

        // The sink above.
        Point[] basin = [v.At(-15, 32), v.At(-12, 24.5), v.At(-2.5, 22), v.At(2.5, 22), v.At(12, 24.5), v.At(15, 32)];
        context.DrawGeometry(null, new ImmutablePen(Porcelain, Math.Max(4, 1.1 * scale), null, PenLineCap.Round, PenLineJoin.Round), Polyline(basin));
        Mark("sink", v.Box(-15, 21, 15, 32));

        // Suction in the drain: a toilet flushing further along pulls the air behind its water.
        var felt = Trap.Felt(suction, Vent);
        if (suction > 0.05)
        {
            var arrowPen = new ImmutablePen(AirArrow, 2);
            // The arrows drift toward the sewer, faster the harder it pulls.
            for (var x = 13.0 + (Seconds * (1 + suction) % 9); x < 42; x += 9)
            {
                if (Math.Abs(x - VentX) < 2) continue;
                var from = v.At(x, Lip + (Bore / 2) - 0.3);
                var to = v.At(x + 4, Lip + (Bore / 2) - 0.4);
                context.DrawLine(arrowPen, from, to);
                DiagramText.DrawArrowAlong(context, from, to, AirArrow, 7);
            }

            DiagramText.Draw(context, DiagramText.Format("Plumbing_Trap_SuctionLabel", Number(felt, "0.#")), v.At(26, Lip - 1.5), 12,
                AirArrow, 0.5, 0, bold: true);
        }

        DiagramText.Draw(context, DiagramText.Get("Plumbing_Trap_Sewer"), v.At(44.5, Lip + 1.5), 11.5, DiagramText.Faint, 0, 0.5);
        Mark("drain", drain, 8);

        // The vent: open to the air above the roof, or capped.
        var top = vent[1];
        if (Vent)
        {
            if (suction > 0.05)
            {
                var from = new Point(top.X, top.Y + 8);
                var to = new Point(top.X, top.Y + (5 * scale));
                context.DrawLine(new ImmutablePen(AirArrow, 2), from, to);
                DiagramText.DrawArrowAlong(context, from, to, AirArrow, 7);
            }

            DiagramText.Draw(context, DiagramText.Get("Plumbing_Trap_VentOpen"), new Point(top.X + 10, top.Y + 4), 11.5, DiagramText.Ink, 0, 0);
        }
        else
        {
            context.DrawRectangle(DarkSteelBrush, Edge, new Rect(top.X - (bore * 0.6), top.Y - 4, bore * 1.2, 7), 2, 2);
            DiagramText.Draw(context, DiagramText.Get("Plumbing_Trap_VentShut"), new Point(top.X + 10, top.Y + 4), 11.5, DiagramText.Faint, 0, 0);
        }

        Mark("vent", vent, 7);

        // The seal that is left, from the dip up to the sink side's water.
        var dipY = v.At(0, Dip).Y;
        var x0 = v.At(-4.5, 0).X;
        var brush = broken ? Smell : DiagramText.Gold;
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 1, new ImmutableDashStyle([3, 3], 0)), new Point(x0 - 4, dipY), v.At(10, Dip));
        if (!broken)
        {
            context.DrawLine(new ImmutablePen(brush, 1.5), new Point(x0, dipY), new Point(x0, sinkY));
            DiagramText.DrawArrowAlong(context, new Point(x0, sinkY), new Point(x0, dipY), brush, 7);
            DiagramText.DrawArrowAlong(context, new Point(x0, dipY), new Point(x0, sinkY), brush, 7);
        }

        DiagramText.Draw(context,
            broken ? DiagramText.Get("Plumbing_Trap_BrokenLabel") : DiagramText.Format("Plumbing_Trap_SealLabel", Number(Trap.Left(suction, Vent), "0.#")),
            new Point(x0 - 6, (dipY + sinkY) / 2), 12.5, brush, 1, 0.5, bold: true);

        if (broken)
        {
            // Drain air bubbling back round the bend and up into the room.
            for (var i = 0; i < 4; i++)
            {
                var t = ((Seconds * 0.6) + (i / 4.0)) % 1;
                var p = v.At(Math.Sin((t * 9) + i) * 0.8, Dip + (t * 30));
                context.DrawEllipse(null, new ImmutablePen(Smell, 1.5), p, 0.8 * scale, 0.8 * scale);
            }

            var wavy = new ImmutablePen(Smell, 1.6);
            for (var k = -1; k <= 1; k++)
            {
                var points = new List<Point>();
                for (var j = 0; j <= 10; j++) points.Add(v.At((k * 6) + (Math.Sin((j * 0.9) + (Seconds * 3)) * 1.2), 26 + (j * 1.1)));
                context.DrawGeometry(null, wavy, Polyline(points));
            }
        }

        DiagramText.Draw(context, DiagramText.Get("Plumbing_Trap_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);

        // The reader's handles: a grip slid along the drain for more suction (toward the sewer, harder), and the vent tapped.
        Grip("suction", v.At(13 + (28 * suction / 12), Lip + (Bore / 2) - 0.4), GripAxis.Horizontal, DiagramText.Get("Plumbing_Grip_Suction"),
            at => Suction = Snap((v.X(at) - 13) / 28 * 12, 0.25, 0, 12));
        Grip("vent", new Point(top.X, top.Y - 16), GripAxis.Tap, DiagramText.Get("Plumbing_Grip_Vent"), _ => Vent = !Vent);
    }
}
