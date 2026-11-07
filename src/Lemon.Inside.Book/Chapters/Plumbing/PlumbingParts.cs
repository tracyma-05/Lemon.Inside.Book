using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Chapters.Lifting;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Plumbing;

/// <summary>
/// Drawing the chapter's own things: pipes full of water or of air, water moving along them, and arrows for the air
/// pressing down. Positions in metres or centimetres go through <see cref="LiftingParts.View"/>.
/// </summary>
internal static class PlumbingParts
{
    public static readonly IImmutableBrush WaterBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x3A, 0x84, 0xD4));
    public static readonly IImmutableBrush WaterLight = new ImmutableSolidColorBrush(Color.FromArgb(0xD0, 0xB8, 0xE0, 0xFF));
    public static readonly IImmutableBrush AirBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x1C, 0x22, 0x30));
    public static readonly IImmutableBrush Brass = new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xA0, 0x4A));
    public static readonly IImmutableBrush Rubber = new ImmutableSolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2E));
    public static readonly IImmutableBrush Porcelain = new ImmutableSolidColorBrush(Color.FromRgb(0xDC, 0xE2, 0xEA));
    public static readonly IImmutableBrush Brick = new ImmutableSolidColorBrush(Color.FromRgb(0x7A, 0x56, 0x48));
    public static readonly IImmutableBrush Earth = new ImmutableSolidColorBrush(Color.FromRgb(0x4A, 0x3E, 0x30));
    public static readonly IImmutableBrush Smell = new ImmutableSolidColorBrush(Color.FromRgb(0xA8, 0xC8, 0x50));
    public static readonly IImmutableBrush AirArrow = new ImmutableSolidColorBrush(Color.FromRgb(0xB8, 0xC4, 0xD8));

    /// <summary>A line through <paramref name="points"/>, as one shape (so a thick pen joins its corners).</summary>
    public static StreamGeometry Polyline(IReadOnlyList<Point> points)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(points[0], false);
            foreach (var p in points.Skip(1)) g.LineTo(p);
            g.EndFigure(false);
        }

        return geometry;
    }

    /// <summary>A pipe along <paramref name="points"/>: its wall, and a bore <paramref name="bore"/> pixels wide.</summary>
    public static void DrawPipe(DrawingContext context, IReadOnlyList<Point> points, double bore, bool full, IImmutableBrush? wall = null)
    {
        var line = Polyline(points);
        context.DrawGeometry(null, new ImmutablePen(wall ?? LiftingParts.DarkSteelBrush, bore + 5, null, PenLineCap.Flat, PenLineJoin.Round), line);
        context.DrawGeometry(null, new ImmutablePen(full ? WaterBrush : AirBrush, bore, null, PenLineCap.Flat, PenLineJoin.Round), line);
    }

    /// <summary>Water moving along <paramref name="points"/>: light dashes that slide <paramref name="travelled"/> pixels.</summary>
    public static void DrawFlow(DrawingContext context, IReadOnlyList<Point> points, double bore, double travelled)
    {
        var thickness = Math.Max(1.5, bore * 0.28);
        var dash = new ImmutableDashStyle([1.5, 4], -travelled / thickness);
        context.DrawGeometry(null, new ImmutablePen(WaterLight, thickness, dash, PenLineCap.Round, PenLineJoin.Round), Polyline(points));
    }

    /// <summary>A row of short arrows pressing down on a water surface from <paramref name="left"/> to <paramref name="right"/>.</summary>
    public static void DrawAirPushing(DrawingContext context, Point left, Point right, double length, int count, string? label)
    {
        var pen = new ImmutablePen(AirArrow, 2);
        for (var i = 0; i < count; i++)
        {
            var x = left.X + ((right.X - left.X) * (i + 0.5) / count);
            var from = new Point(x, left.Y - length - 3);
            var to = new Point(x, left.Y - 3);
            context.DrawLine(pen, from, to);
            DiagramText.DrawArrowAlong(context, from, to, AirArrow, 7);
        }

        if (label is not null)
        {
            DiagramText.Draw(context, label, new Point((left.X + right.X) / 2, left.Y - length - 6), 11.5, AirArrow, 0.5, 1);
        }
    }

    /// <summary>"12.5" in the reader's culture.</summary>
    public static string Number(double value, string format = "0.#") => value.ToString(format, CultureInfo.CurrentCulture);
}
