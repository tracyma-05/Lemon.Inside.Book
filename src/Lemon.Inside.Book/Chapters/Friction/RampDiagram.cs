using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Friction.FrictionParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Friction;

/// <summary>
/// Spread 1: a block on a board hinged at its foot, in metres. Arrows show the block's weight, the part of it pulling
/// downhill, and the friction holding it back, beside a dashed arrow for the most the surface could give. Past the
/// steepest angle the friction cannot keep up, and the block slides.
/// The reader lifts the board's far end to tilt it and taps the board to change its surface. The board swings to its
/// new slope; past the steepest angle the block slides off down it, faster and faster, and is set back on to try again.
/// </summary>
public sealed class RampDiagram : GearPicture
{
    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<RampDiagram, double>(nameof(Angle), 10, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> SurfaceProperty =
        AvaloniaProperty.Register<RampDiagram, int>(nameof(Surface), 1, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The board's length and where the block sits on it, m.</summary>
    private const double Board = 6;
    private const double Along = 3.4;

    /// <summary>The steepest slope the slider gives, degrees.</summary>
    private const double Steepest = 45;

    /// <summary>The board's slope as drawn, easing toward <see cref="Angle"/>; NaN until the first frame.</summary>
    private double _angle = double.NaN;

    /// <summary>Seconds since the block was last set on the board, while it slides.</summary>
    private double _sliding;

    static RampDiagram()
    {
        AffectsRender<RampDiagram>(AngleProperty, SurfaceProperty);
    }

    /// <summary>The board's slope, degrees.</summary>
    public double Angle
    {
        get => GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    /// <summary>0 ice, 1 wood, 2 rubber.</summary>
    public int Surface
    {
        get => GetValue(SurfaceProperty);
        set => SetValue(SurfaceProperty, value);
    }

    /// <summary>
    /// How far a sliding block has gone down the board, m, <paramref name="seconds"/> after it was let go: it speeds up
    /// as the slope's pull beats the (smaller) sliding friction, slowed four times so it can be followed. Sliding grip is
    /// taken as three quarters of the grip at rest.
    /// </summary>
    internal static double SlidDown(double angle, int surface, double seconds)
    {
        var a = angle * Math.PI / 180;
        var pull = Math.Max(0.4, Math.Sin(a) - (0.75 * Ramp.Surface(surface) * Math.Cos(a))) * 9.81;
        var t = seconds / 4;
        return 0.5 * pull * t * t;
    }

    /// <summary>The slope (to half a degree) that puts the board's far end under a point <paramref name="across"/> and <paramref name="up"/> m from the hinge.</summary>
    internal static double AngleFor(double across, double up) =>
        Math.Clamp(Math.Round(Math.Atan2(Math.Max(0, up), Math.Max(1e-6, across)) * 180 / Math.PI * 2) / 2, 0, Steepest);

    protected override void Advance(double seconds)
    {
        var angle = Math.Clamp(Angle, 0, Steepest);
        _angle = double.IsNaN(_angle) ? angle : Glide(_angle, angle, seconds, 0.12);

        // A block that slides goes on down the board, rests a moment at the bottom, and is set back to try again.
        if (Ramp.Slides(_angle, Math.Clamp(Surface, 0, 2)))
        {
            _sliding += seconds;
            if (SlidDown(_angle, Surface, _sliding) > Along - 0.5 + 1.5) _sliding = 0;
        }
        else
        {
            _sliding = 0;
        }
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var angle = double.IsNaN(_angle) ? Math.Clamp(Angle, 0, Steepest) : _angle;
        var surface = Math.Clamp(Surface, 0, 2);
        var slides = Ramp.Slides(angle, surface);
        var a = angle * Math.PI / 180;
        var scale = Math.Min((bounds.Width - 60) / 7.4, (bounds.Height - 150) / 5.6);
        var v = new View(new Point(bounds.Center.X - (3.3 * scale), bounds.Center.Y + (2.2 * scale)), scale);
        var (ux, uy) = (Math.Cos(a), Math.Sin(a));

        // A point on the board: along it from the hinge, then up from its face.
        Point On(double along, double up) => v.At((along * ux) - (up * uy), (along * uy) + (up * ux));

        context.FillRectangle(Ground, new Rect(bounds.X, v.At(0, 0).Y, bounds.Width, bounds.Bottom - v.At(0, 0).Y));

        // The board, its prop, and the angle.
        Point[] board = [On(0, 0), On(Board, 0), On(Board, -0.18), On(0, -0.18)];
        DrawPolygon(context, Surfaces[surface], Edge, board);
        Mark("ramp", [board[0], board[1]], 8);
        context.DrawLine(new ImmutablePen(Wood, 4), On(Board - 0.3, -0.18), v.At((Board - 0.3) * ux, 0));
        var arc = Enumerable.Range(0, 13).Select(i => a * i / 12).Select(t => v.At(1.1 * Math.Cos(t), 1.1 * Math.Sin(t))).ToList();
        if (angle > 1) context.DrawGeometry(null, new ImmutablePen(DiagramText.Gold, 1.5), Plumbing.PlumbingParts.Polyline(arc));
        DiagramText.Draw(context, DiagramText.Format("Friction_Degrees", Number(angle, "0")), v.At(1.3, 0.25), 12.5, DiagramText.Gold, 0, 0.5, bold: true);

        // The block, going on down the board with motion lines once it slides (and stopping at its foot).
        var at = slides ? Math.Max(0.5, Along - SlidDown(angle, surface, _sliding)) : Along;
        Point[] block = [On(at - 0.45, 0), On(at + 0.45, 0), On(at + 0.45, 0.6), On(at - 0.45, 0.6)];
        DrawPolygon(context, Load, Edge, block);
        Mark("block", block, 2);
        var centre = On(at, 0.3);
        DiagramText.Draw(context, Kgf(Ramp.Block), centre, 11, Dark, 0.5, 0.5, bold: true);
        if (slides)
        {
            var pen = new ImmutablePen(DiagramText.Faint, 1.5);
            for (var k = 0; k < 3; k++) context.DrawLine(pen, On(at + 0.6 + (k * 0.15), 0.15 + (k * 0.15)), On(at + 1.1 + (k * 0.15), 0.15 + (k * 0.15)));
            DiagramText.Draw(context, DiagramText.Get("Friction_Ramp_SlidesLabel"), On(at - 0.2, 1.2), 14, GripBrush, 0.5, 0.5, bold: true);
        }

        // The forces, drawn from the block's middle: 1 kg-force to 0.18 m.
        const double perKg = 0.25;
        var down = Ramp.Downhill(angle);
        var most = Ramp.MostGrip(angle, surface);
        var friction = Math.Min(down, most);
        var weightEnd = new Point(centre.X, centre.Y + (Ramp.Block * perKg * scale));
        Arrow(context, centre, weightEnd, WeightBrush, Kgf(Ramp.Block), -0.15, 0.5);
        var downhill = On(at - 0.45 - (down * perKg), 0.3);
        if (down > 0.05)
        {
            Arrow(context, On(at - 0.45, 0.3), downhill, PullBrush, Kgf(down), 0.5, -0.6);
        }

        Mark("pull", [centre, downhill], 8);
        var top = On(at + 0.45, 0.62);
        var mostEnd = On(at + 0.45 + (most * perKg), 0.62);
        context.DrawLine(new ImmutablePen(GripBrush, 1.5, new ImmutableDashStyle([3, 3], 0)), top, mostEnd);
        if (friction > 0.05)
        {
            Arrow(context, On(at + 0.45, 0.85), On(at + 0.45 + (friction * perKg), 0.85), GripBrush, Kgf(friction), 0.5, 1.4);
        }

        Mark("grip", [top, mostEnd, On(at + 0.45 + (friction * perKg), 0.85)], 8);
        DiagramText.Draw(context, DiagramText.Format("Friction_Ramp_MostLabel", Kgf(most)), new Point(mostEnd.X + 6, mostEnd.Y), 11, GripBrush, 0, 0.5);
        DiagramText.Draw(context, DiagramText.Get("Friction_Ramp_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);

        // The reader's handles: the board's far end, lifted for a steeper slope, and the board itself, tapped for
        // another surface (ice, wood, rubber, round again).
        Grip("angle", On(Board, 0.1), GripAxis.Free, DiagramText.Get("Friction_Grip_Angle"),
            p => Angle = AngleFor(v.X(p), v.Y(p)));
        Grip("surface", On(4.8, -0.09), GripAxis.Tap, DiagramText.Get("Friction_Grip_Surface"), _ => Surface = (surface + 1) % 3);
    }
}
