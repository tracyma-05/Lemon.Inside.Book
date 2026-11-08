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
/// Spread 4: a stone arch over a river, its wedge-shaped stones following a parabola, the keystone at the top, the
/// road on fill above. At each foot an arrow shows the push down and the push outward, which the abutments take; take
/// them away and the feet slide apart and the crown drops, falling faster as it goes; put them back and the arch is
/// built up again. The reader drags the keystone up or down and taps an abutment to take it away or put it back.
/// </summary>
public sealed class ArchDiagram : GearPicture
{
    public static readonly StyledProperty<double> RiseProperty =
        AvaloniaProperty.Register<ArchDiagram, double>(nameof(Rise), 3, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> AbutmentsProperty =
        AvaloniaProperty.Register<ArchDiagram, bool>(nameof(Abutments), true, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The ring's thickness, m; the number of stones; the road above the crown, m.</summary>
    private const double Ring = 0.7;
    private const int Stones = 13;
    private const double Cover = 0.8;

    /// <summary>The drawn rise, m; how far the arch has fallen, 0 (standing) to 1 (down); and how fast it is falling.</summary>
    private double _rise = double.NaN;
    private double _fall = double.NaN;
    private double _falling;

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

    /// <summary>
    /// Without abutments the arch falls, gathering speed as under gravity until it lands; with them it is built back
    /// up, easing into place. The rise eases to its new height.
    /// </summary>
    protected override void Advance(double seconds)
    {
        Start();
        _rise = Glide(_rise, Math.Clamp(Rise, 1, 5), seconds, 0.15);
        if (Abutments)
        {
            _falling = 0;
            _fall = Glide(_fall, 0, seconds, 0.2);
        }
        else
        {
            _falling += 3 * seconds;
            _fall = Math.Min(1, _fall + (_falling * seconds));
        }
    }

    private void Start()
    {
        if (double.IsNaN(_rise)) _rise = Math.Clamp(Rise, 1, 5);
        if (double.IsNaN(_fall)) _fall = Abutments ? 0 : 1;
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        Start();
        var rise = _rise;
        var held = Abutments;
        var fall = _fall;
        var standing = fall < 0.02;
        var scale = Math.Min((bounds.Width - 60) / 17, (bounds.Height - 150) / 9);
        var v = new View(new Point(bounds.Center.X, bounds.Center.Y + (2.6 * scale)), scale);
        const double half = Arch.Span / 2;

        // Without abutments the feet slide out and the crown sinks.
        var spread = 0.9 * fall;
        var shape = rise * (1 - (0.55 * fall));
        var span = half + spread;
        var drop = 1.6 * fall;
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
        var roadLeft = v.At(-span, shape + (Ring / 2) + Cover - drop);
        var roadRight = v.At(span, shape + (Ring / 2) + Cover - drop);
        DrawPolygon(context, StoneLight, null, [.. extrados, roadRight, roadLeft]);
        var reach = (1 - fall) * 3 * scale;
        var roadBox = new Rect(roadLeft.X - reach, roadLeft.Y - (0.25 * scale), roadRight.X - roadLeft.X + (2 * reach), 0.25 * scale);
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

        if (held && standing)
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
        else if (fall > 0.5)
        {
            DiagramText.Draw(context, DiagramText.Get("Bridges_Arch_FallsLabel"), v.At(0, shape + Ring + Cover + 0.9 - drop), 14, SqueezeBrush, 0.5, 1, bold: true);
            var crack = new ImmutablePen(Dark, 2);
            context.DrawLine(crack, Face(0, -1), Face(0.02, 1));
        }

        if (held && standing) DiagramText.Draw(context, DiagramText.Format("Bridges_Arch_RiseLabel", Number(rise)), v.At(0, -0.5), 12, DiagramText.Ink, 0.5, 0.5);
        DiagramText.Draw(context, DiagramText.Get("Bridges_Arch_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
        DrawLegend(context, bounds);

        // The reader's handles: the keystone, dragged up for a taller arch, and the right abutment, tapped away or back.
        var crown = v.At(0, shape - drop);
        Grip("rise", crown, GripAxis.Vertical, DiagramText.Get("Bridges_Grip_Rise"),
            at => Rise = Math.Clamp(Math.Round((v.Y(at) + drop) / (1 - (0.55 * fall)), 1), 1, 5));
        Grip("abutments", v.At(half + 1.5, Math.Max(road - 1, 1.5)), GripAxis.Tap,
            DiagramText.Get(held ? "Bridges_Grip_AbutmentsOut" : "Bridges_Grip_AbutmentsIn"), _ => Abutments = !Abutments);
    }
}
