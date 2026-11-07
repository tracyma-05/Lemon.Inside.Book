using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Bridges.BridgesParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Bridges;

/// <summary>
/// Spread 4: a stone arch over a river, its wedge-shaped stones following a parabola, the keystone at the top, the
/// road on fill above. At each foot an arrow shows the push down and the push outward, which the abutments take; take
/// them away and the feet slide apart and the crown drops.
/// </summary>
public sealed class ArchDiagram : GearPicture
{
    public static readonly StyledProperty<double> RiseProperty =
        AvaloniaProperty.Register<ArchDiagram, double>(nameof(Rise), 3);

    public static readonly StyledProperty<bool> AbutmentsProperty =
        AvaloniaProperty.Register<ArchDiagram, bool>(nameof(Abutments), true);

    /// <summary>The ring's thickness, m; the number of stones; the road above the crown, m.</summary>
    private const double Ring = 0.7;
    private const int Stones = 13;
    private const double Cover = 0.8;

    static ArchDiagram()
    {
        AffectsRender<ArchDiagram>(RiseProperty, AbutmentsProperty);
    }

    /// <summary>How high the arch's middle stands above its feet, m.</summary>
    public double Rise
    {
        get => GetValue(RiseProperty);
        set => SetValue(RiseProperty, value);
    }

    public bool Abutments
    {
        get => GetValue(AbutmentsProperty);
        set => SetValue(AbutmentsProperty, value);
    }

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var rise = Math.Clamp(Rise, 1, 5);
        var held = Abutments;
        var scale = Math.Min((bounds.Width - 60) / 17, (bounds.Height - 150) / 9);
        var v = new View(new Point(bounds.Center.X, bounds.Center.Y + (2.6 * scale)), scale);
        const double half = Arch.Span / 2;

        // Without abutments the feet slide out and the crown sinks.
        var spread = held ? 0 : 0.9;
        var shape = held ? rise : rise * 0.45;
        var span = half + spread;
        var drop = held ? 0 : 1.6;
        (double X, double Y) Centre(double t) => (t * span, (shape * (1 - (t * t))) - drop);
        (double X, double Y) Normal(double t)
        {
            var (nx, ny) = (2 * shape * t / span, 1.0);
            var length = Math.Sqrt((nx * nx) + (ny * ny));
            return (nx / length, ny / length);
        }

        Point Face(double t, double side)
        {
            var (x, y) = Centre(t);
            var (nx, ny) = Normal(t);
            return v.At(x + (nx * side * Ring / 2), y + (ny * side * Ring / 2));
        }

        context.FillRectangle(River, v.Box(-half, -2.2, half, -1.4));
        context.FillRectangle(Ground, new Rect(bounds.X, v.At(0, -2.2).Y, bounds.Width, bounds.Bottom - v.At(0, -2.2).Y));

        // The abutments: solid blocks the feet push against.
        var road = rise + (Ring / 2) + Cover;
        var blocks = new[] { v.Box(-half - 3, -2.2, -half, road), v.Box(half, -2.2, half + 3, road) };
        if (held)
        {
            foreach (var block in blocks) context.DrawRectangle(StoneDark, Edge, block);
            Mark("abutments", [blocks[0].TopLeft, blocks[0].BottomRight, blocks[1].TopLeft, blocks[1].BottomRight], 4);
        }
        else
        {
            foreach (var block in blocks)
            {
                context.DrawRectangle(null, new ImmutablePen(DiagramText.Faint, 1.2, new ImmutableDashStyle([4, 4], 0)), block);
            }

            Mark("abutments", [blocks[0].TopLeft, blocks[0].BottomRight, blocks[1].TopLeft, blocks[1].BottomRight], 4);
        }

        // The fill and the road, then the ring of stones.
        var extrados = Enumerable.Range(0, 41).Select(i => Face(-1 + (i / 20.0), 1)).ToList();
        var roadLeft = v.At(-span, held ? road : shape + (Ring / 2) + Cover - drop);
        var roadRight = v.At(span, held ? road : shape + (Ring / 2) + Cover - drop);
        DrawPolygon(context, StoneLight, null, [.. extrados, roadRight, roadLeft]);
        var roadBox = new Rect(roadLeft.X - (held ? 3 * scale : 0), roadLeft.Y - (0.25 * scale), roadRight.X - roadLeft.X + (held ? 6 * scale : 0), 0.25 * scale);
        context.DrawRectangle(Concrete, Edge, roadBox);
        Mark("road", roadBox);

        var stonePoints = new List<Point>();
        Rect keystone = default;
        for (var i = 0; i < Stones; i++)
        {
            var t0 = -1 + (2.0 * i / Stones);
            var t1 = -1 + (2.0 * (i + 1) / Stones);
            Point[] quad = [Face(t0, -1), Face(t1, -1), Face(t1, 1), Face(t0, 1)];
            var key = i == Stones / 2;
            DrawPolygon(context, key ? Keystone : StoneDark, new ImmutablePen(Dark, 1.5), quad);
            stonePoints.AddRange(quad);
            if (key)
            {
                keystone = new Rect(quad[0], quad[2]).Normalize();
            }
        }

        Mark("stones", stonePoints, 2);
        Mark("keystone", keystone.Inflate(4));

        if (held)
        {
            // At each foot: the weight's half pushing down, and the thrust pushing out into the abutment.
            var thrust = Arch.Thrust(rise);
            var length = Math.Clamp(thrust / Arch.Weight, 0.15, 1.4) * 2.2 * scale;
            foreach (var side in new[] { -1, 1 })
            {
                var foot = v.At(side * half, 0);
                DrawPush(context, foot, new Point(foot.X + (side * length), foot.Y), SqueezeBrush, Tonnes(thrust), 0.5, 1.4);
                DrawPush(context, new Point(foot.X - (side * 0.3 * scale), foot.Y), new Point(foot.X - (side * 0.3 * scale), foot.Y + (1.1 * scale)),
                    DiagramText.Ink, Tonnes(Arch.Weight / 2), side > 0 ? 1.15 : -0.15, 0.5);
            }
        }
        else
        {
            DiagramText.Draw(context, DiagramText.Get("Bridges_Arch_FallsLabel"), v.At(0, shape + Ring + Cover + 0.9 - drop), 14, SqueezeBrush, 0.5, 1, bold: true);
            var crack = new ImmutablePen(Dark, 2);
            context.DrawLine(crack, Face(0, -1), Face(0.02, 1));
        }

        if (held) DiagramText.Draw(context, DiagramText.Format("Bridges_Arch_RiseLabel", Number(rise)), v.At(0, -0.5), 12, DiagramText.Ink, 0.5, 0.5);
        DiagramText.Draw(context, DiagramText.Get("Bridges_Arch_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
        DrawLegend(context, bounds);
    }
}
