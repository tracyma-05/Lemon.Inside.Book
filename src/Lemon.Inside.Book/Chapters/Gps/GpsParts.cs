using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Gps;

/// <summary>Drawing the chapter's things: satellites, the Earth, a flat map with its satellites and circles.</summary>
internal static class GpsParts
{
    public static readonly IImmutableBrush SignalInk = new ImmutableSolidColorBrush(Color.FromRgb(0x6F, 0xD8, 0xFF));
    public static readonly IImmutableBrush PlaceInk = new ImmutableSolidColorBrush(Color.FromRgb(0xF2, 0xC9, 0x4C));
    public static readonly IImmutableBrush PanelInk = new ImmutableSolidColorBrush(Color.FromRgb(0x4A, 0x7A, 0xC8));
    public static readonly IImmutableBrush BodyInk = new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xD0, 0xDC));
    public static readonly IImmutableBrush SeaInk = new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x5A, 0x8C));
    public static readonly IImmutableBrush LandInk = new ImmutableSolidColorBrush(Color.FromRgb(0x3E, 0x8A, 0x5A));
    public static readonly IImmutableBrush WarnInk = new ImmutableSolidColorBrush(Color.FromRgb(0xF0, 0x6E, 0x8A));
    public static readonly IImmutableBrush GridInk = new ImmutableSolidColorBrush(Color.FromArgb(0x22, 0xE6, 0xEA, 0xF2));
    public static readonly IPen Edge = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E)), 1.2);

    public static IPen Line(IBrush brush, double width = 1.6, bool dashed = false) => new ImmutablePen((IImmutableBrush)brush, width,
        dashed ? new ImmutableDashStyle([4, 4], 0) : null, PenLineCap.Round);

    public static IImmutableBrush Tint(IImmutableBrush brush, double opacity) =>
        new ImmutableSolidColorBrush(((ISolidColorBrush)brush).Color, opacity);

    /// <summary>A satellite: a body with two solar panels, <paramref name="size"/> pixels across the body.</summary>
    public static void DrawSatellite(DrawingContext context, Point at, double size, double opacity = 1)
    {
        using (context.PushOpacity(opacity))
        {
            var panel = new Rect(0, 0, size * 1.3, size * 0.55);
            context.DrawRectangle(PanelInk, Edge, panel.Translate(new Vector(at.X - (size * 0.5) - panel.Width - 2, at.Y - (panel.Height / 2))));
            context.DrawRectangle(PanelInk, Edge, panel.Translate(new Vector(at.X + (size * 0.5) + 2, at.Y - (panel.Height / 2))));
            context.DrawRectangle(BodyInk, Edge, new Rect(at.X - (size / 2), at.Y - (size / 2), size, size), 2, 2);
        }
    }

    /// <summary>The Earth as a disc: sea with a few patches of land, so its turning would show.</summary>
    public static void DrawEarth(DrawingContext context, Point centre, double radius)
    {
        context.DrawEllipse(SeaInk, Edge, centre, radius, radius);
        using (context.PushGeometryClip(new EllipseGeometry(new Rect(centre.X - radius, centre.Y - radius, 2 * radius, 2 * radius))))
        {
            context.DrawEllipse(LandInk, null, centre + new Vector(-radius * 0.35, -radius * 0.3), radius * 0.38, radius * 0.25);
            context.DrawEllipse(LandInk, null, centre + new Vector(radius * 0.4, radius * 0.15), radius * 0.28, radius * 0.4);
            context.DrawEllipse(LandInk, null, centre + new Vector(-radius * 0.15, radius * 0.6), radius * 0.3, radius * 0.18);
        }
    }

    /// <summary>A flat map's frame and grid, <paramref name="half"/> km either side of its middle, drawn in <paramref name="box"/>.</summary>
    public static void DrawMap(DrawingContext context, Rect box, double half)
    {
        context.DrawRectangle(new ImmutableSolidColorBrush(Color.FromArgb(0x30, 0x24, 0x5A, 0x8C)), Colour.ColourParts.Frame, box, 6, 6);
        for (var k = -half; k <= half + 1e-6; k += 50)
        {
            var x = box.X + ((k + half) / (2 * half) * box.Width);
            var y = box.Y + ((k + half) / (2 * half) * box.Height);
            context.DrawLine(new ImmutablePen(GridInk, 1), new Point(x, box.Y), new Point(x, box.Bottom));
            context.DrawLine(new ImmutablePen(GridInk, 1), new Point(box.X, y), new Point(box.Right, y));
        }
    }

    /// <summary>A map point on screen: km east and north to pixels in <paramref name="box"/>.</summary>
    public static Point OnMap(MapPoint p, Rect box, double half) =>
        new(box.Center.X + (p.X / half * box.Width / 2), box.Center.Y - (p.Y / half * box.Height / 2));

    /// <summary>A screen point back to the map.</summary>
    public static MapPoint FromMap(Point at, Rect box, double half) =>
        new((at.X - box.Center.X) / (box.Width / 2) * half, (box.Center.Y - at.Y) / (box.Height / 2) * half);

    /// <summary>A clock face with one hand at <paramref name="turns"/> of a full turn from twelve.</summary>
    public static void DrawClock(DrawingContext context, Point centre, double radius, double turns, IBrush hand)
    {
        context.DrawEllipse(Colour.ColourParts.Track, Colour.ColourParts.Frame, centre, radius, radius);
        for (var k = 0; k < 12; k++)
        {
            var a = k * Math.PI / 6;
            var d = new Vector(Math.Sin(a), -Math.Cos(a));
            context.DrawLine(Line(DiagramText.Faint, 1.4), centre + (d * radius * 0.82), centre + (d * radius * 0.95));
        }

        var t = turns * 2 * Math.PI;
        context.DrawLine(Line(hand, 3), centre, centre + (new Vector(Math.Sin(t), -Math.Cos(t)) * radius * 0.75));
        context.DrawEllipse(hand, null, centre, 3.5, 3.5);
    }

    public static string Number(double value, string format = "0") => value.ToString(format, System.Globalization.CultureInfo.CurrentCulture);

    public static string Km(double km) => DiagramText.Format("Gps_Km", Number(km, km < 100 ? "0.#" : "N0"));
}
