using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Lifting;

/// <summary>
/// Drawing the chapter's own things: rope, pulley wheels, weights, a hand, and arrows for the forces, in metres with
/// y up (<see cref="View"/>) or straight in pixels.
/// </summary>
internal static class LiftingParts
{
    public static readonly IImmutableBrush Wood = new ImmutableSolidColorBrush(Color.FromRgb(0xA8, 0x7A, 0x4A));
    public static readonly IImmutableBrush SteelBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x8F, 0xA3, 0xBF));
    public static readonly IImmutableBrush DarkSteelBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x4A, 0x58, 0x6E));
    public static readonly IImmutableBrush Stone = new ImmutableSolidColorBrush(Color.FromRgb(0x7D, 0x80, 0x88));
    public static readonly IImmutableBrush Load = new ImmutableSolidColorBrush(Color.FromRgb(0x5F, 0xC2, 0xB0));
    public static readonly IImmutableBrush Skin = new ImmutableSolidColorBrush(Color.FromRgb(0xE8, 0xB4, 0x8A));
    public static readonly IImmutableBrush Oil = new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0x8A, 0x2A));
    public static readonly IImmutableBrush Ground = new ImmutableSolidColorBrush(Color.FromRgb(0x3A, 0x34, 0x2C));
    public static readonly IImmutableBrush Wall = new ImmutableSolidColorBrush(Color.FromArgb(0x40, 0xE6, 0xEA, 0xF2));
    public static readonly IImmutableBrush Dark = new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E));
    public static readonly IImmutableBrush Push = new ImmutableSolidColorBrush(Color.FromRgb(0x7A, 0xD9, 0x8C));
    public static readonly IImmutableBrush Weight = new ImmutableSolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x5A));
    public static readonly IPen Edge = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E)), 1.2);
    public static readonly IPen Rope = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xE9, 0xD8, 0xA6)), 2.4);
    public static readonly IPen Dashed = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0xE6, 0xEA, 0xF2)), 1, new ImmutableDashStyle([4, 4], 0));

    /// <summary>Metres on screen: <see cref="Scale"/> pixels each from <see cref="Origin"/> (the ground under the picture's left edge).</summary>
    public readonly record struct View(Point Origin, double Scale)
    {
        public Point At(double x, double y) => new(Origin.X + (x * Scale), Origin.Y - (y * Scale));

        public Rect Box(double x0, double y0, double x1, double y1) => new(At(x0, y1), At(x1, y0));

        /// <summary>The metres across and up of a point on screen (the way back from <see cref="At"/>, for a dragged grip).</summary>
        public double X(Point screen) => (screen.X - Origin.X) / Scale;

        public double Y(Point screen) => (Origin.Y - screen.Y) / Scale;
    }

    /// <summary>A force said as the weight it would hold up: "50 kg".</summary>
    public static string Kg(double value) =>
        DiagramText.Format("Lifting_Kg", value.ToString(value < 10 ? "0.0" : "N0", CultureInfo.CurrentCulture));

    /// <summary>A pulley wheel: a rim, a groove, an axle. Returns its box.</summary>
    public static Rect DrawWheel(DrawingContext context, Point centre, double radius, IBrush? fill = null)
    {
        context.DrawEllipse(fill ?? SteelBrush, Edge, centre, radius, radius);
        context.DrawEllipse(null, new ImmutablePen(DarkSteelBrush, Math.Max(1, radius * 0.12)), centre, radius * 0.78, radius * 0.78);
        context.DrawEllipse(Dark, null, centre, Math.Max(2, radius * 0.18), Math.Max(2, radius * 0.18));
        return new Rect(centre.X - radius, centre.Y - radius, 2 * radius, 2 * radius);
    }

    /// <summary>A weight hanging or standing: a box with its mass written on it. Returns the box.</summary>
    public static Rect DrawWeight(DrawingContext context, Rect box, string label, IBrush? fill = null)
    {
        context.DrawRectangle(fill ?? Load, Edge, box, 4, 4);
        DiagramText.Draw(context, label, box.Center, Math.Clamp(box.Height * 0.32, 10, 15), Dark, 0.5, 0.5, bold: true);
        return box;
    }

    /// <summary>A hook at <paramref name="at"/>, hanging down. </summary>
    public static void DrawHook(DrawingContext context, Point at, double size)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(at, false);
            g.LineTo(new Point(at.X, at.Y + size));
            g.ArcTo(new Point(at.X - size, at.Y + size), new Size(size / 2, size / 2), 0, false, SweepDirection.Clockwise);
            g.EndFigure(false);
        }

        context.DrawGeometry(null, new ImmutablePen(SteelBrush, 2.2), geometry);
    }

    /// <summary>A hand gripping at <paramref name="at"/>. Returns its box.</summary>
    public static Rect DrawHand(DrawingContext context, Point at, double size)
    {
        var box = new Rect(at.X - (size * 0.55), at.Y - (size * 0.5), size * 1.1, size);
        context.DrawRectangle(Skin, Edge, box, size * 0.4, size * 0.4);
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x9A, 0x6A, 0x4A)), 1), new Point(box.X + (size * 0.25), at.Y), new Point(box.Right - (size * 0.25), at.Y));
        return box;
    }

    /// <summary>A force: an arrow from <paramref name="from"/> to <paramref name="to"/> with its size written beside it.</summary>
    public static void DrawForce(DrawingContext context, Point from, Point to, IBrush brush, string? label, double side = 1)
    {
        context.DrawLine(new ImmutablePen((IImmutableBrush)brush, 3), from, to);
        DiagramText.DrawArrowAlong(context, from, to, brush, 11);
        if (label is null) return;
        var mid = new Point((from.X + to.X) / 2, (from.Y + to.Y) / 2);
        DiagramText.Draw(context, label, new Point(mid.X + (side * 10), mid.Y), 12.5, brush, side > 0 ? 0 : 1, 0.5, bold: true);
    }

    /// <summary>A straight run of rope.</summary>
    public static void DrawRope(DrawingContext context, Point a, Point b) => context.DrawLine(Rope, a, b);

    /// <summary>Rope lying on a wheel: half its rim, over the top (<paramref name="over"/>) or under the bottom.</summary>
    public static void DrawRopeArc(DrawingContext context, Point centre, double radius, bool over)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(new Point(centre.X - radius, centre.Y), false);
            g.ArcTo(new Point(centre.X + radius, centre.Y), new Size(radius, radius), 0, false,
                over ? SweepDirection.Clockwise : SweepDirection.CounterClockwise);
            g.EndFigure(false);
        }

        context.DrawGeometry(null, Rope, geometry);
    }

    /// <summary>A closed shape through <paramref name="points"/>.</summary>
    public static void DrawPolygon(DrawingContext context, IBrush fill, IPen? pen, IReadOnlyList<Point> points)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(points[0], true);
            foreach (var p in points.Skip(1)) g.LineTo(p);
            g.EndFigure(true);
        }

        context.DrawGeometry(fill, pen, geometry);
    }

    /// <summary>Moves <paramref name="value"/> toward <paramref name="target"/> at <paramref name="rate"/> a second.</summary>
    public static double Ease(double value, double target, double rate, double seconds)
    {
        var step = rate * seconds;
        return Math.Abs(target - value) <= step ? target : value + (Math.Sign(target - value) * step);
    }
}
