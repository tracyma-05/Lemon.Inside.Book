using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Lemon.Inside.Book.Chapters.Sewing;

/// <summary>
/// Drawing the chapter's own things: cloth cut through, the two threads, the needle, a handwheel and seams sewn in
/// either stitch. The upper thread is always red and the bobbin thread blue, in every picture.
/// </summary>
internal static class SewingParts
{
    public static readonly Color UpperColor = Color.FromRgb(0xE8, 0x5A, 0x5A);
    public static readonly Color LowerColor = Color.FromRgb(0x5A, 0xB4, 0xF0);
    public static readonly IImmutableBrush Cloth = new ImmutableSolidColorBrush(Color.FromRgb(0xC9, 0xB8, 0x8E));
    public static readonly IImmutableBrush ClothDark = new ImmutableSolidColorBrush(Color.FromRgb(0xA8, 0x96, 0x6C));
    public static readonly IImmutableBrush Metal = new ImmutableSolidColorBrush(Color.FromRgb(0xB8, 0xC4, 0xD4));
    public static readonly IImmutableBrush DarkMetal = new ImmutableSolidColorBrush(Color.FromRgb(0x5A, 0x66, 0x7A));
    public static readonly IImmutableBrush Body = new ImmutableSolidColorBrush(Color.FromRgb(0x2E, 0x4A, 0x5E));
    public static readonly IImmutableBrush UpperBrush = new ImmutableSolidColorBrush(UpperColor);
    public static readonly IImmutableBrush LowerBrush = new ImmutableSolidColorBrush(LowerColor);

    public static IPen UpperPen(double width) => new ImmutablePen(UpperBrush, width, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

    public static IPen LowerPen(double width) => new ImmutablePen(LowerBrush, width, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

    /// <summary>A polyline (or a smooth curve through the points when <paramref name="smooth"/>).</summary>
    public static void DrawLine(DrawingContext context, IPen pen, IReadOnlyList<Point> points, bool smooth = false)
    {
        if (points.Count < 2) return;

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(points[0], false);
            if (!smooth || points.Count < 3)
            {
                for (var i = 1; i < points.Count; i++) g.LineTo(points[i]);
            }
            else
            {
                // Through every point, bending at the midpoints between them.
                for (var i = 1; i < points.Count - 1; i++)
                {
                    var mid = (points[i] + points[i + 1]) / 2;
                    g.QuadraticBezierTo(points[i], i == points.Count - 2 ? points[i + 1] : mid);
                }
            }

            g.EndFigure(false);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    /// <summary>
    /// A needle from the side, its tip at <paramref name="tip"/>, pointing down, <paramref name="width"/> pixels thick.
    /// Its eye is just above the point, or at the top for a hand-sewing needle. Returns the eye's centre.
    /// </summary>
    public static Point DrawNeedle(DrawingContext context, Point tip, double length, double width, bool eyeAtPoint = true)
    {
        var top = tip.Y - length;
        var taper = width * 2.2;
        var outline = new[]
        {
            new Point(tip.X - (width / 2), top), new Point(tip.X + (width / 2), top),
            new Point(tip.X + (width / 2), tip.Y - taper), tip, new Point(tip.X - (width / 2), tip.Y - taper),
        };
        Lifting.LiftingParts.DrawPolygon(context, Metal, Lifting.LiftingParts.Edge, outline);

        // The long groove the thread lies in on the way down.
        context.DrawLine(new ImmutablePen(DarkMetal, Math.Max(1, width * 0.18)), new Point(tip.X - (width * 0.2), top),
            new Point(tip.X - (width * 0.2), tip.Y - (taper * 1.6)));
        var eye = eyeAtPoint ? new Point(tip.X, tip.Y - (taper * 1.3)) : new Point(tip.X, top + (width * 1.4));
        context.DrawEllipse(Lifting.LiftingParts.Dark, null, eye, width * 0.22, width * 0.7);
        return eye;
    }

    /// <summary>
    /// A handwheel seen face on, turned to <paramref name="angle"/> degrees (0 with its handle at the top, clockwise).
    /// Returns the handle's centre.
    /// </summary>
    public static Point DrawHandwheel(DrawingContext context, Point centre, double radius, double angle)
    {
        context.DrawEllipse(DarkMetal, Lifting.LiftingParts.Edge, centre, radius, radius);
        context.DrawEllipse(Metal, Lifting.LiftingParts.Edge, centre, radius * 0.78, radius * 0.78);
        var spokePen = new ImmutablePen(DarkMetal, Math.Max(2, radius * 0.12));
        for (var k = 0; k < 3; k++)
        {
            var a = (angle + (k * 120)) * Math.PI / 180;
            context.DrawLine(spokePen, centre, centre + new Vector(Math.Sin(a), -Math.Cos(a)) * radius * 0.78);
        }

        context.DrawEllipse(DarkMetal, Lifting.LiftingParts.Edge, centre, radius * 0.18, radius * 0.18);
        var h = angle * Math.PI / 180;
        var handle = centre + new Vector(Math.Sin(h), -Math.Cos(h)) * radius * 0.62;
        context.DrawEllipse(Lifting.LiftingParts.Wood, Lifting.LiftingParts.Edge, handle, radius * 0.13, radius * 0.13);
        return handle;
    }

    /// <summary>The handwheel angle, degrees, of a point dragged round <paramref name="centre"/>.</summary>
    public static double AngleAround(Point centre, Point at) =>
        ((Math.Atan2(at.X - centre.X, centre.Y - at.Y) * 180 / Math.PI) + 360) % 360;

    /// <summary>
    /// A lockstitch seam cut along its length: the cloth from <paramref name="top"/> to <paramref name="bottom"/>, a hole
    /// every <paramref name="pitch"/> pixels from <paramref name="x0"/>, and the threads locked at
    /// <paramref name="knot"/> (0 on the top surface, 1 on the underside). Returns the knots' centres.
    /// </summary>
    public static List<Point> DrawLockSeam(DrawingContext context, double x0, double pitch, int count, double top, double bottom,
        double knot, double thread)
    {
        var knots = new List<Point>();
        var upper = new List<Point> { new(x0 - (pitch / 2), top - (thread / 2)) };
        var lower = new List<Point> { new(x0 - (pitch / 2), bottom + (thread / 2)) };
        var gap = thread * 0.9;

        // Pulled right through, the knot sits just outside the cloth, where it shows.
        var y = knot <= 0.001 ? top - (thread * 1.2) : knot >= 0.999 ? bottom + (thread * 1.2) : top + (knot * (bottom - top));
        for (var i = 0; i < count; i++)
        {
            var x = x0 + (i * pitch);
            upper.AddRange([new Point(x - gap, top - (thread / 2)), new Point(x - (gap * 0.3), y), new Point(x + (gap * 0.3), y),
                new Point(x + gap, top - (thread / 2))]);
            lower.AddRange([new Point(x - gap, bottom + (thread / 2)), new Point(x - (gap * 0.3), y), new Point(x + (gap * 0.3), y),
                new Point(x + gap, bottom + (thread / 2))]);
            knots.Add(new Point(x, y));
        }

        upper.Add(new Point(x0 + ((count - 0.5) * pitch), top - (thread / 2)));
        lower.Add(new Point(x0 + ((count - 0.5) * pitch), bottom + (thread / 2)));
        DrawLine(context, LowerPen(thread), lower);
        DrawLine(context, UpperPen(thread), upper);

        // Where the two threads hook round each other.
        foreach (var k in knots)
        {
            context.DrawEllipse(null, LowerPen(thread * 0.7), k, thread * 0.9, thread * 0.6);
        }

        return knots;
    }

    /// <summary>
    /// A chain stitch cut along its length: one thread along the top, down each hole and looped along the underside
    /// through the next stitch's loop. The last <paramref name="undone"/> stitches have been pulled out and hang loose.
    /// Returns the loose end.
    /// </summary>
    public static Point DrawChainSeam(DrawingContext context, double x0, double pitch, int count, double top, double bottom,
        double undone, double thread, double seconds)
    {
        var pen = UpperPen(thread);
        var held = Math.Clamp(count - undone, 0, count);
        var whole = (int)Math.Floor(held);
        var depth = Math.Min(pitch * 0.35, (bottom - top) * 0.9);
        var path = new List<Point> { new(x0 - (pitch / 2), top - (thread / 2)) };
        for (var i = 0; i < whole; i++)
        {
            var x = x0 + (i * pitch);

            // Along the top to the hole, down through it and a loop forward under the cloth, round the next one.
            path.Add(new Point(x, top - (thread / 2)));
            context.DrawLine(pen, new Point(x, top), new Point(x, bottom + (depth * 0.4)));
            var loop = new Rect(x - (thread * 0.6), bottom + (thread * 0.4), pitch * 1.15, depth);
            context.DrawGeometry(null, pen, new EllipseGeometry(loop));
        }

        var end = whole > 0 ? new Point(x0 + ((whole - 1) * pitch), top - (thread / 2)) : path[0];
        path.Add(end);
        DrawLine(context, pen, path);

        // The pulled-out thread: a loose wavy tail off the last stitch still holding.
        var loose = Math.Max(0, count - held) * pitch * 1.6;
        if (loose > 1)
        {
            var tail = new List<Point>();
            for (var s = 0.0; s <= loose; s += 6)
            {
                tail.Add(new Point(end.X + (s * 0.55), end.Y - (s * 0.5) + (4 * Math.Sin((s / 14) + (seconds * 2)))));
            }

            DrawLine(context, pen, tail, smooth: true);
            return tail[^1];
        }

        return end;
    }

    /// <summary>Two layers of cloth cut through, from <paramref name="left"/> to <paramref name="right"/>.</summary>
    public static Rect DrawCloth(DrawingContext context, double left, double right, double top, double bottom)
    {
        var box = new Rect(left, top, right - left, bottom - top);
        context.FillRectangle(Cloth, box);
        context.FillRectangle(ClothDark, new Rect(left, top + ((bottom - top) / 2) - 0.6, right - left, 1.2));
        return box;
    }

    public static string Number(double value, string format = "0.#") => value.ToString(format, CultureInfo.CurrentCulture);
}
