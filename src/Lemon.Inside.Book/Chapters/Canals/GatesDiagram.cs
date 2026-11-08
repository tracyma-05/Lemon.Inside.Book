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
/// Spread 2: a pair of mitre gates seen from above, in metres, the higher water on the left. Pointed upstream, the
/// leaves meet in a shallow V that the water's push squeezes shut, each leaf pressing on the other at the mitre; pointed
/// downstream, the push swings them open. Long balance beams reach back over the towpath from each leaf's hinge.
/// The reader drags the level on the gauge at the left and taps the switch below to turn the gates round; the leaves
/// swing rather than jump, and the push on them throbs with the water.
/// </summary>
public sealed class GatesDiagram : GearPicture
{
    public static readonly StyledProperty<double> HeadProperty =
        AvaloniaProperty.Register<GatesDiagram, double>(nameof(Head), 3, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> DownstreamProperty =
        AvaloniaProperty.Register<GatesDiagram, bool>(nameof(Downstream), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Half the canal's width, m; the highest difference the gauge shows, m.</summary>
    private const double Half = 3;
    private const double MostHead = 4;

    /// <summary>How far the leaves have swung open, 0 (meeting at the mitre) to 1 (pushed back against the walls).</summary>
    private double _swing;

    static GatesDiagram()
    {
        AffectsRender<GatesDiagram>(HeadProperty, DownstreamProperty);
    }

    /// <summary>How much higher the water on the left stands, m.</summary>
    public double Head
    {
        get => GetValue(HeadProperty);
        set => SetValue(HeadProperty, value);
    }

    /// <summary>Gates wrongly pointed downstream.</summary>
    public bool Downstream
    {
        get => GetValue(DownstreamProperty);
        set => SetValue(DownstreamProperty, value);
    }

    protected override void Advance(double seconds) =>
        _swing = Glide(_swing, Gates.Hold(Math.Clamp(Head, 0, MostHead), Downstream) ? 0 : 1, seconds, 0.3);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var head = Math.Clamp(Head, 0, MostHead);
        var downstream = Downstream;
        var holds = Gates.Hold(head, downstream);
        var scale = Math.Min((bounds.Width - 40) / 18, (bounds.Height - 150) / 12);
        var v = new View(new Point(bounds.Center.X, bounds.Center.Y), scale);

        // Land, and the water each side of the gates.
        context.FillRectangle(Grass, bounds);
        context.FillRectangle(DeepWater, new Rect(new Point(bounds.X, v.At(0, Half).Y), v.At(0, -Half)));
        context.FillRectangle(head > 0.01 ? Water : DeepWater, new Rect(v.At(0, Half), new Point(bounds.Right, v.At(0, -Half).Y)));
        context.FillRectangle(Masonry, new Rect(new Point(bounds.X, v.At(0, Half + 0.6).Y), new Point(bounds.Right, v.At(0, Half).Y)));
        context.FillRectangle(Masonry, new Rect(new Point(bounds.X, v.At(0, -Half).Y), new Point(bounds.Right, v.At(0, -Half - 0.6).Y)));
        DiagramText.Draw(context, DiagramText.Get("Canals_Gates_UpperSide"), v.At(-7, 0), 12.5, DiagramText.Ink, 0.5, 0.5, bold: true);
        DiagramText.Draw(context, head > 0.01 ? DiagramText.Format("Canals_Gates_LowerSide", Number(head)) : DiagramText.Get("Canals_Gates_SameSide"),
            v.At(7, 0), 12.5, DiagramText.Ink, 0.5, 0.5, bold: true);
        var water = v.Box(-9, -Half, -2.5, Half);
        Mark("water", water);

        // The leaves: hinged at the walls, meeting at the mitre, or swung open when the push is the wrong way.
        var lean = Math.Sqrt((Gates.Leaf * Gates.Leaf) - (Half * Half));
        var mitre = v.At(downstream ? lean : -lean, 0);
        var swing = _swing;
        static Point Between(Point a, Point b, double t) => new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));
        var leafPen = new ImmutablePen(Timber, Math.Max(6, 0.35 * scale), null, PenLineCap.Round);
        var leaves = new List<Point>();
        foreach (var side in new[] { -1, 1 })
        {
            var hinge = v.At(0, side * Half);
            var end = Between(mitre, v.At(Gates.Leaf * 0.97, side * (Half - 0.4)), swing);
            context.DrawLine(leafPen, hinge, end);
            leaves.AddRange([hinge, end]);

            // The balance beam reaching back over the towpath.
            var beamEnd = Between(v.At(-4.2, side * (Half + 1.6)), v.At(0.5, side * (Half + 4.2)), swing);
            context.DrawLine(new ImmutablePen(Timber, Math.Max(4, 0.22 * scale), null, PenLineCap.Round), hinge, beamEnd);
            if (side < 0) Mark("beam", [hinge, beamEnd], 6);

            // The water's push on the leaf's upstream face.
            if (holds && head > 0.01 && swing < 0.05)
            {
                var push = Gates.Push(head);
                var length = Math.Clamp(push / 20, 0.15, 1) * 1.4 * scale * (1 + (0.08 * Math.Sin((Seconds * 4) + side)));
                for (var k = 1; k <= 3; k++)
                {
                    var t = k / 4.0;
                    var at = new Point(hinge.X + ((mitre.X - hinge.X) * t), hinge.Y + ((mitre.Y - hinge.Y) * t));
                    var (dx, dy) = (mitre.X - hinge.X, mitre.Y - hinge.Y);
                    var n = Math.Sqrt((dx * dx) + (dy * dy));
                    var (nx, ny) = (dy / n * side, -dx / n * side);
                    if (nx > 0) (nx, ny) = (-nx, -ny);
                    var from = new Point(at.X + (nx * length), at.Y + (ny * length));
                    context.DrawLine(new ImmutablePen(Flow, 2), from, at);
                    DiagramText.DrawArrowAlong(context, from, at, Flow, 8);
                }
            }
        }

        Mark("leaves", leaves, 6);
        Mark("mitre", mitre, Math.Max(12, 0.6 * scale));
        DrawRipples(context, bounds.X, v.At(-0.3, 0).X, v.At(0, Half - 0.8).Y, 0.8, Seconds * 0.6, 34);
        DrawRipples(context, bounds.X, v.At(-0.3, 0).X, v.At(0, -Half + 0.8).Y, 0.8, Seconds * 0.6 + 1, 34);
        if (swing < 0.05)
        {
            context.DrawEllipse(DiagramText.Gold, Edge, mitre, 5, 5);
            if (head > 0.01)
            {
                DiagramText.Draw(context, DiagramText.Format("Canals_Gates_PushLabel", Number(Gates.Push(head), "0")), v.At(-lean - 3.4, Half - 0.9), 12.5, Flow, 0.5, 0.5, bold: true);
            }
        }
        else if (swing > 0.3)
        {
            var travelled = Seconds * 120;
            for (var k = -2; k <= 2; k++)
            {
                Plumbing.PlumbingParts.DrawFlow(context, [v.At(-4, k * 0.9), v.At(8, k * 0.9)], 0.4 * scale, travelled);
            }

            DiagramText.Draw(context, DiagramText.Get("Canals_Gates_BurstLabel"), v.At(4, Half + 1.6), 14, Warn, 0.5, 0.5, bold: true);
        }

        DiagramText.Draw(context, DiagramText.Get("Canals_Gates_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);

        // The gauge on the bank at the top left: how much higher the upper water stands, with a grip to drag it.
        var gaugeX = bounds.X + 34;
        var gaugeBottom = v.At(0, Half + 0.6).Y - 18;
        var gaugeTop = Math.Max(bounds.Y + 56, gaugeBottom - 160);
        double GaugeY(double metres) => gaugeBottom + ((gaugeTop - gaugeBottom) * metres / MostHead);
        context.DrawLine(new ImmutablePen(Dark, 6, lineCap: PenLineCap.Round), new Point(gaugeX, gaugeBottom), new Point(gaugeX, gaugeTop));
        context.DrawLine(new ImmutablePen(Flow, 4, lineCap: PenLineCap.Round), new Point(gaugeX, gaugeBottom), new Point(gaugeX, GaugeY(head)));
        for (var m = 0; m <= MostHead; m++)
        {
            context.DrawLine(new ImmutablePen(DiagramText.Faint, 1), new Point(gaugeX + 5, GaugeY(m)), new Point(gaugeX + 9, GaugeY(m)));
            DiagramText.Draw(context, DiagramText.Format("Canals_Metres", Number(m, "0")), new Point(gaugeX + 12, GaugeY(m)), 10, DiagramText.Faint, 0, 0.5);
        }

        Grip("head", new Point(gaugeX, GaugeY(head)), GripAxis.Vertical, DiagramText.Get("Canals_Grip_Head"),
            at => Head = Math.Round(Math.Clamp((at.Y - gaugeBottom) / (gaugeTop - gaugeBottom) * MostHead, 0, MostHead), 1));
        Grip("flip", v.At(2.5, -Half - 1.3), GripAxis.Tap, DiagramText.Get("Canals_Grip_Flip"), _ => Downstream = !Downstream);
    }

    private static readonly IImmutableBrush Warn = new ImmutableSolidColorBrush(Color.FromRgb(0xF0, 0x6A, 0x5A));
}
