using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Canals;

/// <summary>
/// Drawing the chapter's own things: canal water, stone walls, timber gates and a narrowboat's hull. Positions in
/// metres go through <see cref="Lifting.LiftingParts.View"/>.
/// </summary>
internal static class CanalsParts
{
    public static readonly IImmutableBrush Water = new ImmutableSolidColorBrush(Color.FromRgb(0x3A, 0x84, 0xD4));
    public static readonly IImmutableBrush DeepWater = new ImmutableSolidColorBrush(Color.FromRgb(0x2A, 0x64, 0xB0));
    public static readonly IImmutableBrush Masonry = new ImmutableSolidColorBrush(Color.FromRgb(0x8E, 0x84, 0x74));
    public static readonly IImmutableBrush Timber = new ImmutableSolidColorBrush(Color.FromRgb(0x6A, 0x4A, 0x30));
    public static readonly IImmutableBrush Grass = new ImmutableSolidColorBrush(Color.FromRgb(0x46, 0x62, 0x3A));
    public static readonly IImmutableBrush Hull = new ImmutableSolidColorBrush(Color.FromRgb(0xC0, 0x4A, 0x3A));
    public static readonly IImmutableBrush Cabin = new ImmutableSolidColorBrush(Color.FromRgb(0x2E, 0x6A, 0x4A));
    public static readonly IImmutableBrush Flow = new ImmutableSolidColorBrush(Color.FromRgb(0xB8, 0xE0, 0xFF));

    /// <summary>A narrowboat from the side, its hull's bottom at <paramref name="keel"/>. Returns its box.</summary>
    public static Rect DrawBoat(DrawingContext context, Point keel, double length, double scale)
    {
        var hull = new Rect(keel.X - (length / 2), keel.Y - (0.9 * scale), length, 0.9 * scale);
        context.DrawRectangle(Hull, Lifting.LiftingParts.Edge, hull, 3, 3);
        var cabin = new Rect(hull.X + (length * 0.25), hull.Y - (0.7 * scale), length * 0.6, 0.7 * scale);
        context.DrawRectangle(Cabin, Lifting.LiftingParts.Edge, cabin, 2, 2);
        return hull.Union(cabin);
    }

    private static readonly IPen Ripple = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0xA0, 0xD8, 0xEE, 0xFF)), 1.4,
        lineCap: PenLineCap.Round);

    /// <summary>
    /// Little waves along a water surface from <paramref name="left"/> to <paramref name="right"/> at height
    /// <paramref name="y"/>: <paramref name="height"/> pixels high, rolling along with <paramref name="seconds"/>.
    /// </summary>
    public static void DrawRipples(DrawingContext context, double left, double right, double y, double height, double seconds, double wavelength = 26)
    {
        if (right - left < 4 || height < 0.2) return;

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(new Point(left, y), false);
            for (var x = left; x <= right; x += 3)
            {
                g.LineTo(new Point(x, y + (height * Math.Sin(((x - left) / wavelength * 2 * Math.PI) - (seconds * 3)))));
            }

            g.EndFigure(false);
        }

        context.DrawGeometry(null, Ripple, geometry);
    }

    public static string Number(double value, string format = "0.#") => value.ToString(format, CultureInfo.CurrentCulture);
}
