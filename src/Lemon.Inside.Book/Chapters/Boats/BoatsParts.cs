using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Boats;

/// <summary>Drawing the chapter's own things: water that darkens with depth, force arrows, a hull in section.</summary>
internal static class BoatsParts
{
    public static readonly Color Shallow = Color.FromRgb(0x2A, 0x6E, 0xA6);
    public static readonly Color Deep = Color.FromRgb(0x0A, 0x22, 0x40);
    public static readonly IImmutableBrush Surface = new ImmutableSolidColorBrush(Color.FromRgb(0x9F, 0xD8, 0xF5));
    public static readonly IImmutableBrush Weight = new ImmutableSolidColorBrush(Color.FromRgb(0xE0, 0x8A, 0x5A));
    public static readonly IImmutableBrush Lift = new ImmutableSolidColorBrush(Color.FromRgb(0x6F, 0xD8, 0xFF));
    public static readonly IImmutableBrush HullFill = new ImmutableSolidColorBrush(Color.FromRgb(0xB0, 0x5A, 0x48));
    public static readonly IImmutableBrush Deck = new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xB0, 0x86));
    public static readonly IImmutableBrush Dark = new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E));
    public static readonly IImmutableBrush Danger = new ImmutableSolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x5A));

    /// <summary>Water from <paramref name="top"/> (its surface) down to the bottom of <paramref name="area"/>, darker the deeper.</summary>
    public static void DrawWater(DrawingContext context, Rect area, double top, double opacity = 0.9)
    {
        if (area.Bottom <= top) return;

        var water = new Rect(area.X, top, area.Width, area.Bottom - top);
        context.FillRectangle(new ImmutableLinearGradientBrush(
            [new ImmutableGradientStop(0, Color.FromArgb((byte)(255 * opacity), Shallow.R, Shallow.G, Shallow.B)),
             new ImmutableGradientStop(1, Color.FromArgb((byte)(255 * opacity), Deep.R, Deep.G, Deep.B))],
            startPoint: new RelativePoint(0, 0, RelativeUnit.Relative), endPoint: new RelativePoint(0, 1, RelativeUnit.Relative)), water);
        context.DrawLine(new ImmutablePen(Surface, 2), new Point(area.X, top), new Point(area.Right, top));
    }

    /// <summary>A thick arrow from <paramref name="from"/> to <paramref name="to"/> (nothing when shorter than a few pixels).</summary>
    public static void DrawArrow(DrawingContext context, Point from, Point to, IImmutableBrush brush, double width = 4)
    {
        var d = to - from;
        var length = Math.Sqrt((d.X * d.X) + (d.Y * d.Y));
        if (length < 3) return;

        var head = Math.Min(length, 8 + (width * 1.5));
        var shaftEnd = from + (d * ((length - (head * 0.6)) / length));
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x80, 0x05, 0x08, 0x10)), width + 3), from, shaftEnd);
        context.DrawLine(new ImmutablePen(brush, width), from, shaftEnd);
        DiagramText.DrawArrowAlong(context, from, to, brush, head);
    }

    /// <summary>A closed outline of screen points.</summary>
    public static StreamGeometry Outline(IEnumerable<Point> points)
    {
        var geometry = new StreamGeometry();
        using var g = geometry.Open();
        var first = true;
        foreach (var p in points)
        {
            if (first) g.BeginFigure(p, true);
            else g.LineTo(p);
            first = false;
        }

        if (!first) g.EndFigure(true);
        return geometry;
    }

    /// <summary>The centre-of-weight sign: a circle in four quarters.</summary>
    public static void DrawCentre(DrawingContext context, Point at, double radius) =>
        Bicycle.BicycleParts.DrawCentreOfMass(context, at, radius);
}
