using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Chapters.Lifting;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Weighing;

/// <summary>
/// Drawing the chapter's own things: pans on strings, sacks, coil springs, dials and digital displays. Positions in
/// centimetres go through <see cref="LiftingParts.View"/>.
/// </summary>
internal static class WeighingParts
{
    public static readonly IImmutableBrush Brass = new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xA0, 0x4A));
    public static readonly IImmutableBrush Sacking = new ImmutableSolidColorBrush(Color.FromRgb(0xC2, 0xA8, 0x78));
    public static readonly IImmutableBrush Iron = new ImmutableSolidColorBrush(Color.FromRgb(0x5A, 0x60, 0x6C));
    public static readonly IImmutableBrush Face = new ImmutableSolidColorBrush(Color.FromRgb(0xEE, 0xEA, 0xDC));
    public static readonly IImmutableBrush Screen = new ImmutableSolidColorBrush(Color.FromRgb(0x1E, 0x32, 0x28));
    public static readonly IImmutableBrush Digits = new ImmutableSolidColorBrush(Color.FromRgb(0x8C, 0xF0, 0xA8));
    public static readonly IImmutableBrush Warn = new ImmutableSolidColorBrush(Color.FromRgb(0xF0, 0x6A, 0x5A));
    public static readonly IImmutableBrush Stretched = new ImmutableSolidColorBrush(Color.FromRgb(0x5A, 0xB8, 0xF0));
    public static readonly IImmutableBrush Squeezed = new ImmutableSolidColorBrush(Color.FromRgb(0xF0, 0x6A, 0x5A));
    public static readonly IPen ThreadPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xCC, 0xD4)), 1.2);

    /// <summary>A coil spring from <paramref name="top"/> straight down to <paramref name="bottom"/>.</summary>
    public static IReadOnlyList<Point> DrawCoil(DrawingContext context, Point top, Point bottom, double width, int turns, IBrush brush)
    {
        var points = new List<Point> { top };
        for (var i = 1; i < 2 * turns; i++)
        {
            var y = top.Y + ((bottom.Y - top.Y) * i / (2 * turns));
            points.Add(new Point(top.X + ((i % 2 == 0 ? -1 : 1) * width / 2), y));
        }

        points.Add(bottom);
        context.DrawGeometry(null, new ImmutablePen((IImmutableBrush)brush, 2.2, null, PenLineCap.Round, PenLineJoin.Round), Plumbing.PlumbingParts.Polyline(points));
        return points;
    }

    /// <summary>A shallow pan, its rim's middle at <paramref name="centre"/>. Returns its box.</summary>
    public static Rect DrawPan(DrawingContext context, Point centre, double width)
    {
        var depth = width * 0.18;
        LiftingParts.DrawPolygon(context, Brass, LiftingParts.Edge,
        [
            new Point(centre.X - (width / 2), centre.Y), new Point(centre.X + (width / 2), centre.Y),
            new Point(centre.X + (width * 0.36), centre.Y + depth), new Point(centre.X - (width * 0.36), centre.Y + depth),
        ]);
        return new Rect(centre.X - (width / 2), centre.Y - 2, width, depth + 2);
    }

    /// <summary>A sack with its mass written on it. Returns its box.</summary>
    public static Rect DrawSack(DrawingContext context, Rect box, string label)
    {
        context.DrawRectangle(Sacking, LiftingParts.Edge, box, box.Width * 0.25, box.Width * 0.2);
        DiagramText.Draw(context, label, box.Center, Math.Clamp(box.Height * 0.28, 10, 14), LiftingParts.Dark, 0.5, 0.5, bold: true);
        return box;
    }

    /// <summary>A round dial reading <paramref name="share"/> (0..1) of its sweep, with its value written under the needle.</summary>
    public static Rect DrawDial(DrawingContext context, Point centre, double radius, double share, string label)
    {
        context.DrawEllipse(Face, LiftingParts.Edge, centre, radius, radius);
        var tick = new ImmutablePen(LiftingParts.Dark, 1);
        for (var i = 0; i <= 10; i++)
        {
            var a = Angle(i / 10.0);
            context.DrawLine(tick, new Point(centre.X + (Math.Cos(a) * radius * 0.82), centre.Y + (Math.Sin(a) * radius * 0.82)),
                new Point(centre.X + (Math.Cos(a) * radius * 0.95), centre.Y + (Math.Sin(a) * radius * 0.95)));
        }

        var needle = Angle(Math.Clamp(share, 0, 1.05));
        context.DrawLine(new ImmutablePen(Warn, 2, null, PenLineCap.Round), centre,
            new Point(centre.X + (Math.Cos(needle) * radius * 0.85), centre.Y + (Math.Sin(needle) * radius * 0.85)));
        context.DrawEllipse(LiftingParts.Dark, null, centre, 3, 3);
        DiagramText.Draw(context, label, new Point(centre.X, centre.Y + (radius * 0.45)), Math.Clamp(radius * 0.24, 10, 14), LiftingParts.Dark, 0.5, 0.5, bold: true);
        return new Rect(centre.X - radius, centre.Y - radius, 2 * radius, 2 * radius);

        // The sweep runs from lower left, over the top, to lower right.
        static double Angle(double s) => (Math.PI * 0.75) + (s * Math.PI * 1.5);
    }

    /// <summary>A digital display. Returns its box.</summary>
    public static Rect DrawDisplay(DrawingContext context, Rect box, string text, bool wrong)
    {
        context.DrawRectangle(Iron, LiftingParts.Edge, box.Inflate(4), 6, 6);
        context.DrawRectangle(Screen, null, box, 3, 3);
        DiagramText.Draw(context, text, box.Center, Math.Clamp(box.Height * 0.5, 12, 22), wrong ? Warn : Digits, 0.5, 0.5, bold: true);
        return box.Inflate(4);
    }

    /// <summary>
    /// The value between <paramref name="low"/> and <paramref name="high"/> where <paramref name="rising"/> (which must
    /// not fall as the value grows) reaches <paramref name="target"/>: the way back from a drawing to the value that
    /// drew it, for a grip whose place is not a straight line of its value.
    /// </summary>
    public static double Solve(Func<double, double> rising, double target, double low, double high)
    {
        if (target <= rising(low)) return low;
        if (target >= rising(high)) return high;
        for (var i = 0; i < 40; i++)
        {
            var mid = (low + high) / 2;
            if (rising(mid) < target) low = mid;
            else high = mid;
        }

        return (low + high) / 2;
    }

    public static string Number(double value, string format = "0.#") => value.ToString(format, CultureInfo.CurrentCulture);

    public static string Kilos(double value, string format = "0.##") => DiagramText.Format("Weighing_Kg", Number(value, format));

    public static string Grams(double value) => DiagramText.Format("Weighing_Grams", Number(value, "0"));
}
