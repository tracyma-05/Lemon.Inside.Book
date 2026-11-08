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
/// Spread 3: where a lock's water goes. The chamber, drawn full, is cut into equal slabs: with side ponds, the upper
/// slabs are let into the ponds beside it on the way down and taken back on the way up; the last two slabs always go
/// downhill and must be made up from the summit. A bar below compares what a boat uses with a whole chamberful.
/// A boat works the lock over and over: the chamber empties slab by slab into the ponds and then downhill, and fills
/// again from the ponds and then the summit. The reader taps a pond's slot to add or take away a pond and drags the
/// grip under the bar for the boats a day.
/// </summary>
public sealed class WaterDiagram : GearPicture
{
    public static readonly StyledProperty<int> SidePondsProperty =
        AvaloniaProperty.Register<WaterDiagram, int>(nameof(SidePonds), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> BoatsProperty =
        AvaloniaProperty.Register<WaterDiagram, double>(nameof(Boats), 40, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Seconds each slab takes to move, and the rest at the bottom and the top of the cycle.</summary>
    private const double SlabTime = 1.2;
    private const double Rest = 0.9;

    private static readonly IImmutableBrush Empty = new ImmutableSolidColorBrush(Color.FromRgb(0x14, 0x1A, 0x26));

    static WaterDiagram()
    {
        AffectsRender<WaterDiagram>(SidePondsProperty, BoatsProperty);
    }

    public int SidePonds
    {
        get => GetValue(SidePondsProperty);
        set => SetValue(SidePondsProperty, value);
    }

    /// <summary>Boats a day.</summary>
    public double Boats
    {
        get => GetValue(BoatsProperty);
        set => SetValue(BoatsProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var ponds = Math.Clamp(SidePonds, 0, 3);
        var slabs = ponds + 2;
        var top = bounds.Y + 70;
        var height = bounds.Height * 0.42;
        var chamber = new Rect(bounds.X + (bounds.Width * 0.3), top + 30, bounds.Width * 0.22, height);
        var slab = chamber.Height / slabs;

        // The summit pound above and to the left, feeding the top of the lock.
        var summit = new Rect(chamber.Right + 30, top - 14, bounds.Right - chamber.Right - 60, 36);
        context.FillRectangle(DeepWater, summit);
        context.DrawRectangle(null, new ImmutablePen(Masonry, 3), summit);
        DiagramText.Draw(context, DiagramText.Get("Canals_Water_SummitLabel"), new Point(summit.Center.X, summit.Bottom + 6), 11.5, DiagramText.Ink, 0.5, 0);
        Mark("summit", summit);

        // The lower pound to the right of the chamber, at its bottom.
        context.FillRectangle(Water, new Rect(chamber.Right + 6, chamber.Bottom - 30, bounds.Right - chamber.Right - 26, 30));
        DiagramText.Draw(context, DiagramText.Get("Canals_Water_LowerLabel"), new Point(bounds.Right - 30, chamber.Bottom + 6), 11.5, DiagramText.Ink, 1, 0);

        // Where the boat's trip has got to: the chamber's water in slabs from the bottom, how full each pond is, the slab on the move.
        var cycle = Cycle(ponds, Seconds);

        // The chamber in slabs: the pond slabs at the top, the two lost slabs at the bottom; the water as high as it now stands.
        var surface = chamber.Bottom - (cycle.Level * slab);
        context.FillRectangle(Empty, chamber);
        for (var k = 0; k < slabs; k++)
        {
            var band = new Rect(chamber.X, chamber.Y + (k * slab), chamber.Width, slab);
            var wet = band.Intersect(new Rect(chamber.X, surface, chamber.Width, chamber.Bottom - surface));
            if (wet.Height > 0) context.FillRectangle(k >= ponds ? DeepWater : Water, wet);
            context.DrawLine(new ImmutablePen(Dark, 1, new ImmutableDashStyle([3, 3], 0)), band.TopLeft, band.TopRight);
        }

        DrawRipples(context, chamber.Left + 2, chamber.Right - 2, surface, 0.8, Seconds, 18);
        DrawBoat(context, new Point(chamber.Center.X, surface + 6), chamber.Width * 0.7, 9);

        context.DrawRectangle(null, new ImmutablePen(Masonry, 4), chamber);
        Mark("chamber", chamber);
        var lostBand = new Rect(chamber.X, chamber.Y + (ponds * slab), chamber.Width, 2 * slab);
        DiagramText.Draw(context, DiagramText.Get("Canals_Water_LostLabel"), lostBand.Center, 12, DiagramText.Ink, 0.5, 0.5, bold: true);
        var down = new Point(chamber.Right + 30, lostBand.Center.Y);
        context.DrawLine(new ImmutablePen(Flow, 2), new Point(chamber.Right, lostBand.Center.Y), down);
        DiagramText.DrawArrowAlong(context, new Point(chamber.Right, lostBand.Center.Y), down, Flow, 8);
        var travelled = Seconds * 80;
        if (cycle.ToLower)
        {
            Plumbing.PlumbingParts.DrawFlow(context, [new Point(chamber.Right, Math.Min(surface + 4, chamber.Bottom - 4)), new Point(chamber.Right + 40, chamber.Bottom - 15)],
                12, travelled);
        }

        if (cycle.FromSummit)
        {
            Plumbing.PlumbingParts.DrawFlow(context, [new Point(summit.Left + 10, summit.Bottom), new Point(summit.Left + 10, chamber.Y + 8), new Point(chamber.Right - 8, chamber.Y + 8)],
                12, travelled);
        }

        // The side ponds, beside the chamber at the heights of their slabs.
        var pondPoints = new List<Point>();
        for (var k = 0; k < 3; k++)
        {
            var band = new Rect(chamber.X, chamber.Y + (k * slab), chamber.Width, slab);
            var pond = new Rect(chamber.X - (bounds.Width * 0.22) - 14 + (k * 6), band.Y + (slab * 0.15), bounds.Width * 0.2, slab * 0.7);
            if (k < ponds)
            {
                var full = cycle.Ponds[k];
                context.FillRectangle(Empty, pond);
                context.FillRectangle(Water, new Rect(pond.X, pond.Bottom - (pond.Height * full), pond.Width, pond.Height * full));
                if (cycle.Pond == k)
                {
                    var (from, to) = cycle.Emptying
                        ? (new Point(chamber.Left, Math.Min(surface + 4, chamber.Bottom - 4)), new Point(pond.Right, pond.Bottom - 4))
                        : (new Point(pond.Right, pond.Bottom - 4), new Point(chamber.Left, Math.Min(surface + 4, chamber.Bottom - 4)));
                    Plumbing.PlumbingParts.DrawFlow(context, [from, to], 12, travelled);
                }

                context.DrawRectangle(null, new ImmutablePen(Masonry, 2.5), pond);
                context.DrawLine(new ImmutablePen(Flow, 2), new Point(pond.Right, pond.Center.Y), new Point(chamber.Left, pond.Center.Y));
                DiagramText.DrawArrowAlong(context, new Point(chamber.Left, pond.Center.Y), new Point(pond.Right, pond.Center.Y), Flow, 7);
                DiagramText.DrawArrowAlong(context, new Point(pond.Right, pond.Center.Y), new Point(chamber.Left, pond.Center.Y), Flow, 7);
                DiagramText.Draw(context, DiagramText.Format("Canals_Water_PondLabel", Number(k + 1, "0")), pond.Center, 11, DiagramText.Ink, 0.5, 0.5, bold: true);
                pondPoints.AddRange([pond.TopLeft, pond.BottomRight]);
            }
            else if (k == ponds)
            {
                context.DrawRectangle(null, new ImmutablePen(DiagramText.Faint, 1.2, new ImmutableDashStyle([4, 4], 0)), pond);
                if (ponds == 0)
                {
                    DiagramText.Draw(context, DiagramText.Get("Canals_Water_NoPonds"), pond.Center, 11, DiagramText.Faint, 0.5, 0.5);
                    pondPoints.AddRange([pond.TopLeft, pond.BottomRight]);
                }
            }

            // A tap on a pond takes it (and those below it) away; a tap on the next empty slot digs one more.
            if (k <= ponds)
            {
                var slot = k;
                Grip($"pond{k + 1}", new Point(pond.Left, pond.Center.Y), GripAxis.Tap,
                    DiagramText.Get(k < ponds ? "Canals_Grip_RemovePond" : "Canals_Grip_AddPond"), _ => SidePonds = slot < ponds ? slot : slot + 1, 9);
            }
        }

        Mark("ponds", pondPoints, 4);

        // The bar: a boat's water against a whole chamberful.
        var bar = new Rect(bounds.X + 30, chamber.Bottom + 60, bounds.Width - 60, 26);
        context.DrawRectangle(null, new ImmutablePen(DiagramText.Faint, 1), bar, 3, 3);
        var share = Ponds.Share(ponds);
        context.FillRectangle(DeepWater, new Rect(bar.X, bar.Y, bar.Width * share, bar.Height), 3);
        DiagramText.Draw(context, DiagramText.Format("Canals_Water_BarLabel", Number(Ponds.PerBoat(ponds), "N0"), Number(share * 100, "0")),
            new Point(bar.X + 8, bar.Center.Y), 12, DiagramText.Ink, 0, 0.5, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Canals_Water_DayLabel", Number(Math.Clamp(Boats, 10, 100), "0"), Number(Ponds.PerBoat(ponds) * Math.Clamp(Boats, 10, 100), "N0")),
            new Point(bar.X, bar.Bottom + 10), 12.5, DiagramText.Gold, 0, 0, bold: true);
        Mark("bar", bar);

        // The boats a day, dragged along a track under the bar.
        var boats = Math.Clamp(Boats, 10, 100);
        var trackY = bar.Bottom + 44;
        var knob = new Point(bar.X + (bar.Width * (boats - 10) / 90), trackY);
        context.DrawLine(new ImmutablePen(Dark, 6, lineCap: PenLineCap.Round), new Point(bar.X, trackY), new Point(bar.Right, trackY));
        context.DrawLine(new ImmutablePen(DiagramText.Gold, 3, lineCap: PenLineCap.Round), new Point(bar.X, trackY), knob);
        Grip("boats", knob, GripAxis.Horizontal, DiagramText.Get("Canals_Grip_Boats"),
            at => Boats = Math.Round(Math.Clamp(10 + ((at.X - bar.X) / bar.Width * 90), 10, 100)));
        DiagramText.Draw(context, DiagramText.Get("Canals_Water_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }

    /// <summary>
    /// Where a boat's trip through the lock has got to after <paramref name="seconds"/>: the chamber's water, in slabs
    /// from the bottom; how full each pond is; which pond is taking or giving water (-1 for none); and whether a slab is
    /// running downhill or coming from the summit. Emptying, the top slabs go into the ponds, highest first, and the last
    /// two downhill; filling, the lowest pond gives back first and the summit makes up the top two.
    /// </summary>
    internal static (double Level, double[] Ponds, int Pond, bool Emptying, bool ToLower, bool FromSummit) Cycle(int ponds, double seconds)
    {
        ponds = Math.Clamp(ponds, 0, 3);
        var slabs = ponds + 2;
        var half = (slabs * SlabTime) + Rest;
        var t = seconds % (2 * half);
        var fill = new double[3];
        if (t < half)
        {
            // Going down.
            var e = Math.Min(slabs, t / SlabTime);
            var k = (int)Math.Floor(e);
            for (var p = 0; p < ponds; p++) fill[p] = Math.Clamp(e - p, 0, 1);
            var moving = e < slabs;
            return (slabs - e, fill, moving && k < ponds ? k : -1, true, moving && k >= ponds, false);
        }

        // Coming up.
        var f = Math.Min(slabs, (t - half) / SlabTime);
        var j = (int)Math.Floor(f);
        for (var p = 0; p < ponds; p++) fill[p] = 1 - Math.Clamp(f - (ponds - 1 - p), 0, 1);
        var rising = f < slabs;
        return (f, fill, rising && j < ponds ? ponds - 1 - j : -1, false, false, rising && j >= ponds);
    }
}
