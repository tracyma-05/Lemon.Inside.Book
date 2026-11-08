using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Scene;
using Lemon.Inside.Book.Chapters.Colour;

namespace Lemon.Inside.Book.Chapters.Tesseract;

/// <summary>
/// Drawing the chapter's things: points in three directions seen from a turned viewpoint (x right, y away, z up), wire
/// edges, and the slider bars the pictures are worked with.
/// </summary>
internal static class TesseractParts
{
    public static readonly IImmutableBrush GoldInk = new ImmutableSolidColorBrush(Color.FromRgb(0xF2, 0xC9, 0x4C));
    public static readonly IImmutableBrush CyanInk = new ImmutableSolidColorBrush(Color.FromRgb(0x6F, 0xD8, 0xFF));
    public static readonly IImmutableBrush SteelInk = new ImmutableSolidColorBrush(Color.FromRgb(0x8F, 0xA3, 0xBF));
    public static readonly IImmutableBrush RoseInk = new ImmutableSolidColorBrush(Color.FromRgb(0xF0, 0x6E, 0x8A));
    public static readonly IImmutableBrush FaintInk = new ImmutableSolidColorBrush(Color.FromArgb(0x60, 0xE6, 0xEA, 0xF2));

    public static IPen Wire(IBrush brush, double width = 2) =>
        new ImmutablePen((IImmutableBrush)brush, width, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

    public static IImmutableBrush Tint(IImmutableBrush brush, double opacity) =>
        new ImmutableSolidColorBrush(((ISolidColorBrush)brush).Color, opacity);

    /// <summary>A point turned by <paramref name="yaw"/> about the upright and tipped toward the eye by <paramref name="pitch"/>.</summary>
    public static Vec3 View(Vec3 p, double yaw, double pitch)
    {
        var y = yaw * Math.PI / 180;
        var (x1, y1) = ((p.X * Math.Cos(y)) - (p.Y * Math.Sin(y)), (p.X * Math.Sin(y)) + (p.Y * Math.Cos(y)));
        var t = pitch * Math.PI / 180;
        return new Vec3(x1, (y1 * Math.Cos(t)) - (p.Z * Math.Sin(t)), (y1 * Math.Sin(t)) + (p.Z * Math.Cos(t)));
    }

    /// <summary>A viewed point on screen (its y is depth, away from the eye, and is dropped).</summary>
    public static Point Screen(Vec3 viewed, Point centre, double scale) => new(centre.X + (viewed.X * scale), centre.Y - (viewed.Z * scale));

    public static Point Project(Vec3 p, Point centre, double scale, double yaw, double pitch) => Screen(View(p, yaw, pitch), centre, scale);

    public static void DrawPolygon(DrawingContext context, IBrush? fill, IPen? pen, IReadOnlyList<Point> points)
    {
        if (points.Count < 2) return;

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(points[0], fill is not null);
            for (var i = 1; i < points.Count; i++) g.LineTo(points[i]);
            g.EndFigure(true);
        }

        context.DrawGeometry(fill, pen, geometry);
    }

    /// <summary>
    /// A slider bar drawn in the picture: its <paramref name="label"/> above, a track <paramref name="width"/> long from
    /// <paramref name="left"/>, filled to <paramref name="share"/>. Returns where its grip goes.
    /// </summary>
    public static Point DrawBar(DrawingContext context, Point left, double width, double share, string label)
    {
        DiagramText.Draw(context, label, left + new Vector(0, -12), 11.5, DiagramText.Faint, 0, 1);
        return ColourParts.DrawMeter(context, left, width, 6, share, FaintInk);
    }

    /// <summary>The share along a bar a pointer is dragged to.</summary>
    public static double Along(Point at, Point left, double width) => Math.Clamp((at.X - left.X) / width, 0, 1);

    public static void DrawDot(DrawingContext context, Point at, IBrush brush, double radius = 3.5) =>
        context.DrawEllipse(brush, null, at, radius, radius);
}
