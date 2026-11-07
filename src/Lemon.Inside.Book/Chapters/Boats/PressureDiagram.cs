using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Boats;

/// <summary>
/// Spread 2: a cube held under water. Arrows on each face show how hard the water presses there: harder the deeper.
/// The sides' pushes cancel; the bottom is pushed up harder than the top is pushed down, by the same amount at any depth.
/// </summary>
public sealed class PressureDiagram : GearPicture
{
    public static readonly StyledProperty<double> DepthProperty =
        AvaloniaProperty.Register<PressureDiagram, double>(nameof(Depth), 2);

    /// <summary>The water shown, metres.</summary>
    private const double Shown = 16;

    private static readonly IPen Tick = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0xA0, 0xE6, 0xEA, 0xF2)), 1);
    private static readonly IImmutableBrush Cube = new ImmutableSolidColorBrush(Color.FromRgb(0x8A, 0x94, 0xA8));

    static PressureDiagram()
    {
        AffectsRender<PressureDiagram>(DepthProperty);
    }

    /// <summary>How deep the cube's top is, metres.</summary>
    public double Depth
    {
        get => GetValue(DepthProperty);
        set => SetValue(DepthProperty, value);
    }

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var surface = bounds.Height * 0.1;
        var bottom = bounds.Height * 0.9;
        var perMetre = (bottom - surface) / Shown;
        BoatsParts.DrawWater(context, new Rect(bounds.X, bounds.Y, bounds.Width, bottom), surface);

        // A depth scale down the left, with the pressure at each depth.
        var scaleX = bounds.X + 18;
        for (var m = 0; m <= Shown; m += 2)
        {
            var y = surface + (m * perMetre);
            context.DrawLine(Tick, new Point(scaleX, y), new Point(scaleX + 8, y));
            DiagramText.Draw(context, DiagramText.Format("Boats_Pressure_Tick", m, (Water.Pressure(m) / 1000).ToString("0")), new Point(scaleX + 12, y), 10.5, DiagramText.Faint, 0, 0.5);
        }

        // The cube, PressureSpread.Side metres a side, its top Depth metres down.
        var side = PressureSpread.Side * perMetre;
        var top = surface + (Depth * perMetre);
        var cube = new Rect((bounds.Width * 0.58) - (side / 2), top, side, side);
        context.DrawRectangle(Cube, new ImmutablePen(BoatsParts.Dark, 1.5), cube, 2, 2);
        Mark("cube", cube);

        // Arrows as long as the pressure where they push: 100 kPa is this many pixels.
        var perKilopascal = Math.Min(bounds.Width * 0.18, 110) / 100;
        double Length(double y) => Water.Pressure((y - surface) / perMetre) / 1000 * perKilopascal;
        var gap = 6.0;
        var topPoints = new List<Point>();
        var bottomPoints = new List<Point>();
        for (var k = 0; k < 3; k++)
        {
            var x = cube.X + (side * (k + 0.5) / 3);
            var down = Length(cube.Top);
            BoatsParts.DrawArrow(context, new Point(x, cube.Top - gap - down), new Point(x, cube.Top - 2), BoatsParts.Weight, 3);
            topPoints.Add(new Point(x, cube.Top - gap - down));
            topPoints.Add(new Point(x, cube.Top));
            var up = Length(cube.Bottom);
            BoatsParts.DrawArrow(context, new Point(x, cube.Bottom + gap + up), new Point(x, cube.Bottom + 2), BoatsParts.Lift, 3);
            bottomPoints.Add(new Point(x, cube.Bottom + gap + up));
            bottomPoints.Add(new Point(x, cube.Bottom));
        }

        var sidePoints = new List<Point>();
        foreach (var dir in new[] { -1, 1 })
        {
            var face = dir < 0 ? cube.Left : cube.Right;
            for (var k = 0; k < 3; k++)
            {
                var y = cube.Top + (side * (k + 0.5) / 3);
                var push = Length(y);
                var from = new Point(face + (dir * (gap + push)), y);
                BoatsParts.DrawArrow(context, from, new Point(face + (dir * 2), y), DiagramText.Faint, 2.5);
                sidePoints.Add(from);
            }
        }

        Mark("top", topPoints, 4);
        Mark("bottom", bottomPoints, 4);
        Mark("sides", sidePoints, 4);

        // The difference: the lift, the same at every depth.
        var lift = (Water.Pressure(Depth + PressureSpread.Side) - Water.Pressure(Depth)) * PressureSpread.Side * PressureSpread.Side / 1000;
        DiagramText.Draw(context, DiagramText.Format("Boats_Pressure_Kpa", (Water.Pressure(Depth) / 1000).ToString("0")),
            new Point(cube.Right + 10, cube.Top - 18), 12, BoatsParts.Weight, 0, 1);
        DiagramText.Draw(context, DiagramText.Format("Boats_Pressure_Kpa", (Water.Pressure(Depth + PressureSpread.Side) / 1000).ToString("0")),
            new Point(cube.Right + 10, cube.Bottom + 18), 12, BoatsParts.Lift, 0, 0);
        DiagramText.Draw(context, DiagramText.Format("Boats_Pressure_Lift", lift.ToString("0.0")),
            new Point(bounds.Right - 18, bottom + 14), 13, DiagramText.Ink, 1, 0, bold: true);
    }
}
