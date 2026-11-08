using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Tesseract.TesseractParts;

namespace Lemon.Inside.Book.Chapters.Tesseract;

/// <summary>
/// Spread 2: a cube hangs above the floor in light from straight overhead, and its flat shadow lies below it. Turned
/// and tipped, the shadow changes shape: a square face on, a hexagon standing on a corner. The reader drags two bars,
/// one to turn the cube and one to tip it.
/// </summary>
public sealed class ShadowDiagram : GearPicture
{
    public static readonly StyledProperty<double> TurnProperty =
        AvaloniaProperty.Register<ShadowDiagram, double>(nameof(Turn), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> TiltProperty =
        AvaloniaProperty.Register<ShadowDiagram, double>(nameof(Tilt), defaultBindingMode: BindingMode.TwoWay);

    private const double Yaw = 28;
    private const double Pitch = 24;
    private const double Lift = 1.7;

    static ShadowDiagram()
    {
        AffectsRender<ShadowDiagram>(TurnProperty, TiltProperty);
    }

    public double Turn
    {
        get => GetValue(TurnProperty);
        set => SetValue(TurnProperty, value);
    }

    public double Tilt
    {
        get => GetValue(TiltProperty);
        set => SetValue(TiltProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var turn = Math.Clamp(Turn, 0, 90);
        var tilt = Math.Clamp(Tilt, 0, 90);
        var scale = Math.Min(bounds.Width, bounds.Height) * 0.17;
        var centre = new Point(bounds.Center.X, bounds.Y + (bounds.Height * 0.5));
        Point P(Vec3 p) => Project(p, centre, scale, Yaw, Pitch);

        // The floor.
        var f = 1.8;
        var floor = new[] { new Vec3(-f, -f, 0), new Vec3(f, -f, 0), new Vec3(f, f, 0), new Vec3(-f, f, 0) }.Select(P).ToList();
        DrawPolygon(context, Tint(SteelInk, 0.16), Wire(FaintInk, 1), floor);
        Mark("floor", [floor[0], Lerp(floor[0], floor[1], 0.25), Lerp(floor[0], floor[3], 0.25)], 4);

        // The shadow: the cube's corners dropped straight down, and the outline round them.
        var outline = Shadow.Outline(turn, tilt);
        var shadow = outline.Select(q => P(new Vec3(q.X, q.Y, 0))).ToList();
        DrawPolygon(context, new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Color.FromArgb(0xE0, 0x05, 0x07, 0x0C)), Wire(CyanInk, 1.5), shadow);
        Mark("shadow", shadow, 2);

        // The cube above it, faces far to near.
        var corners = Shadow.Corners(turn, tilt).Select(c => c + new Vec3(0, 0, Lift)).ToList();
        var faces = HyperCube.SquareList(3)
            .Select(sq => (Points: sq.Select(i => corners[i]).ToList(), Depth: sq.Average(i => View(corners[i], Yaw, Pitch).Y)))
            .OrderByDescending(face => face.Depth);
        foreach (var face in faces) DrawPolygon(context, Tint(GoldInk, 0.28), Wire(GoldInk, 1.6), face.Points.Select(P).ToList());
        Mark("cube", corners.Select(P), 6);

        // Light straight down: a few rays from the cube's lowest corners to their shadows.
        foreach (var c in corners.OrderBy(c => c.Z).Take(3))
        {
            context.DrawLine(new Avalonia.Media.Immutable.ImmutablePen(FaintInk, 1, new Avalonia.Media.Immutable.ImmutableDashStyle([3, 4], 0)), P(c),
                P(c with { Z = 0 }));
        }

        var sun = P(new Vec3(0, 0, Lift + 1.25));
        context.DrawEllipse(GoldInk, null, sun, 14, 14);
        for (var k = 0; k < 8; k++)
        {
            var a = k * Math.PI / 4;
            var d = new Vector(Math.Cos(a), Math.Sin(a));
            context.DrawLine(Wire(GoldInk, 2), sun + (d * 19), sun + (d * 26));
        }

        Mark("sun", sun, 22);

        // The shadow's shape, and the two bars.
        DiagramText.Draw(context, DiagramText.Format("Tesseract_Shadow_Shape", outline.Count, Shadow.Area(turn, tilt).ToString("0.00", System.Globalization.CultureInfo.CurrentCulture)),
            new Point(bounds.Center.X, floor.Max(p => p.Y) + 14), 13, DiagramText.Gold, 0.5, 0, bold: true);
        var barWidth = Math.Min(220, (bounds.Width - 80) / 2);
        var barY = Math.Min(bounds.Bottom - 110, floor.Max(p => p.Y) + 70);
        var left1 = new Point(bounds.X + 30, barY);
        var left2 = new Point(bounds.Right - 30 - barWidth, barY);
        var k1 = DrawBar(context, left1, barWidth, turn / 90, DiagramText.Get("Tesseract_Shadow_Turn"));
        var k2 = DrawBar(context, left2, barWidth, tilt / 90, DiagramText.Get("Tesseract_Shadow_Tilt"));
        Grip("turn", k1, GripAxis.Horizontal, DiagramText.Get("Tesseract_Grip_Turn"), at => Turn = Math.Round(Along(at, left1, barWidth) * 90));
        Grip("tilt", k2, GripAxis.Horizontal, DiagramText.Get("Tesseract_Grip_Tilt"), at => Tilt = Math.Round(Along(at, left2, barWidth) * 90));
        DiagramText.Draw(context, DiagramText.Get("Tesseract_Shadow_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }

    private static Point Lerp(Point a, Point b, double t) => new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));
}
