using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Friction.FrictionParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Friction;

/// <summary>
/// Spread 1: a block on a board hinged at its foot, in metres. Arrows show the block's weight, the part of it pulling
/// downhill, and the friction holding it back, beside a dashed arrow for the most the surface could give. Past the
/// steepest angle the friction cannot keep up, and the block slides.
/// </summary>
public sealed class RampDiagram : GearPicture
{
    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<RampDiagram, double>(nameof(Angle), 10);

    public static readonly StyledProperty<int> SurfaceProperty =
        AvaloniaProperty.Register<RampDiagram, int>(nameof(Surface), 1);

    /// <summary>The board's length and where the block sits on it, m.</summary>
    private const double Board = 6;
    private const double Along = 3.4;

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

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var angle = Math.Clamp(Angle, 0, 45);
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

        // The block, a little downhill and with motion lines once it slides.
        var at = slides ? Along - 0.9 : Along;
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
    }
}
