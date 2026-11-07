using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Car;

/// <summary>Drawing the chapter's own things: metal, graphs along a cycle, spinning stripes on parts seen edge-on.</summary>
internal static class CarParts
{
    public static readonly Color Metal = Color.FromRgb(0x9F, 0xB0, 0xC8);
    public static readonly Color DarkMetal = Color.FromRgb(0x5A, 0x64, 0x78);
    public static readonly IImmutableBrush MetalBrush = new ImmutableSolidColorBrush(Metal);
    public static readonly IImmutableBrush DarkMetalBrush = new ImmutableSolidColorBrush(DarkMetal);
    public static readonly IImmutableBrush Dark = new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E));
    public static readonly IImmutableBrush Copper = new ImmutableSolidColorBrush(Color.FromRgb(0xD0, 0x8A, 0x5A));
    public static readonly IImmutableBrush Hot = new ImmutableSolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x5A));
    public static readonly IImmutableBrush Cool = new ImmutableSolidColorBrush(Color.FromRgb(0x6F, 0xD8, 0xFF));
    public static readonly IImmutableBrush Track = new ImmutableSolidColorBrush(Color.FromArgb(0x30, 0xE6, 0xEA, 0xF2));
    public static readonly IPen Axis = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0xE6, 0xEA, 0xF2)), 1);
    public static readonly IPen Edge = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E)), 1.4);

    /// <summary>A polyline through screen points.</summary>
    public static void DrawLine(DrawingContext context, IReadOnlyList<Point> points, IPen pen)
    {
        if (points.Count < 2) return;

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(points[0], false);
            for (var i = 1; i < points.Count; i++) g.LineTo(points[i]);
            g.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    /// <summary>
    /// A part seen edge-on (a wheel, a disc): a rounded box with stripes sliding up as it turns
    /// <paramref name="turns"/> (so its turning shows), <paramref name="stripes"/> stripes across its height.
    /// </summary>
    public static void DrawEdgeOn(DrawingContext context, Rect box, double turns, IBrush fill, IBrush stripe, int stripes = 6, double corner = 4)
    {
        context.DrawRectangle(fill, Edge, box, corner, corner);
        using (context.PushClip(box.Deflate(1.5)))
        {
            var gap = box.Height / stripes;
            var shift = ((turns % 1) + 1) % 1 * box.Height;
            var pen = new ImmutablePen((IImmutableBrush)stripe, Math.Max(1.5, gap * 0.25));
            for (var k = -1; k <= stripes; k++)
            {
                var y = box.Bottom - (((k * gap) + shift) % box.Height);
                context.DrawLine(pen, new Point(box.X, y), new Point(box.Right, y));
            }
        }
    }

    /// <summary>The graph's frame along a cycle: an axis, ticks every 90° (crank degrees) and their labels.</summary>
    public static void DrawCycleAxis(DrawingContext context, Rect area, double most, double zeroY)
    {
        context.DrawLine(Axis, new Point(area.X, zeroY), new Point(area.Right, zeroY));
        for (var d = 0.0; d <= most + 1e-9; d += 180)
        {
            var x = area.X + (d / most * area.Width);
            context.DrawLine(Axis, new Point(x, area.Bottom), new Point(x, area.Bottom + 4));
            DiagramText.Draw(context, DiagramText.Format("Car_Degrees", d.ToString("0")), new Point(x, area.Bottom + 5), 10.5, DiagramText.Faint, 0.5);
        }
    }
}
