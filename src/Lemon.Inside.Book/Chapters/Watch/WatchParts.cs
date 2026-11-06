using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Lemon.Inside.Book.Chapters.Watch;

/// <summary>Drawing the watch's own parts (not plain gears): the balance, its hairspring, the escape wheel, the mainspring.</summary>
internal static class WatchParts
{
    public static readonly Color Brass = Color.FromRgb(0xD9, 0xB3, 0x5B);
    public static readonly Color Steel = Color.FromRgb(0x9F, 0xB0, 0xC8);
    public static readonly Color Ruby = Color.FromRgb(0xE0, 0x4A, 0x5A);
    public static readonly IImmutableBrush Dark = new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E));

    /// <summary>
    /// A balance wheel: a rim on three spokes with screws round it, turned <paramref name="degrees"/> clockwise, and its
    /// roller's pin (pointing down at 0°) when <paramref name="pin"/>.
    /// </summary>
    public static void DrawBalance(DrawingContext context, Point centre, double radius, double degrees, bool pin = true, double opacity = 1)
    {
        var angle = degrees * Math.PI / 180;
        var brass = new ImmutableSolidColorBrush(Brass, opacity);
        var rim = new ImmutablePen(brass, Math.Max(2.5, radius * 0.11));
        context.DrawEllipse(null, rim, centre, radius, radius);
        var spoke = new ImmutablePen(brass, Math.Max(1.5, radius * 0.06), lineCap: PenLineCap.Round);
        for (var i = 0; i < 3; i++)
        {
            context.DrawLine(spoke, centre, At(centre, radius, angle + (i * 2 * Math.PI / 3)));
        }

        var screw = new ImmutableSolidColorBrush(Color.FromRgb(0xF2, 0xDC, 0x9A), opacity);
        for (var i = 0; i < 12; i++)
        {
            context.DrawEllipse(screw, null, At(centre, radius * 1.08, angle + (i * Math.PI / 6) + 0.26), radius * 0.045 + 1, radius * 0.045 + 1);
        }

        context.DrawEllipse(new ImmutableSolidColorBrush(Brass, opacity), null, centre, radius * 0.14, radius * 0.14);
        if (pin)
        {
            context.DrawEllipse(new ImmutableSolidColorBrush(Ruby, opacity), null, At(centre, radius * 0.26, angle + (Math.PI / 2)), Math.Max(2.5, radius * 0.05), Math.Max(2.5, radius * 0.05));
        }
    }

    /// <summary>
    /// The hairspring: a flat spiral whose inner end turns with the balance (by <paramref name="degrees"/>) and whose outer
    /// end is pinned, so the coils between twist part way.
    /// </summary>
    public static void DrawHairspring(DrawingContext context, Point centre, double inner, double outer, double degrees, double opacity = 1, int coils = 8)
    {
        var twist = degrees * Math.PI / 180;
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            const int Steps = 360;
            for (var i = 0; i <= Steps; i++)
            {
                var s = (double)i / Steps;
                var p = At(centre, inner + ((outer - inner) * s), (s * coils * 2 * Math.PI) + (twist * (1 - s)));
                if (i == 0) g.BeginFigure(p, false);
                else g.LineTo(p);
            }

            g.EndFigure(false);
        }

        context.DrawGeometry(null, new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x8F, 0xD8, 0xF0), opacity), 1.2), geometry);
        context.DrawEllipse(Dark, null, At(centre, outer, coils * 2 * Math.PI), 3, 3); // the stud that holds the outer end
    }

    /// <summary>An escape wheel's outline, radius 1, tooth 0's tip at angle 0 and its gaps halfway between tips.</summary>
    public static IReadOnlyList<Point> EscapeOutline(int teeth)
    {
        var pitch = 2 * Math.PI / teeth;
        var points = new List<Point>();
        for (var k = 0; k < teeth; k++)
        {
            var a = k * pitch;
            points.Add(Polar(0.72, a - (0.42 * pitch)));
            points.Add(Polar(0.80, a - (0.22 * pitch)));
            points.Add(Polar(1.0, a - (0.05 * pitch)));
            points.Add(Polar(1.0, a + (0.06 * pitch)));
            points.Add(Polar(0.74, a + (0.2 * pitch)));
            points.Add(Polar(0.72, a + (0.4 * pitch)));
        }

        return points;
    }

    /// <summary>A point on screen <paramref name="radius"/> from <paramref name="centre"/>, <paramref name="angle"/> radians clockwise from 3 o'clock.</summary>
    public static Point At(Point centre, double radius, double angle) =>
        new(centre.X + (radius * Math.Cos(angle)), centre.Y + (radius * Math.Sin(angle)));

    private static Point Polar(double radius, double angle) => new(radius * Math.Cos(angle), radius * Math.Sin(angle));
}
