using Avalonia;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// What the chapter's grips share: finding the value whose drawing lies under the pointer (which also works for a 3D
/// scene, where a lamp's path is a projected line seen at a slant), snapping it like the slider beside it, and easing a
/// shown value toward the real one so a picture glides between states instead of jumping.
/// </summary>
internal static class LightGrips
{
    /// <summary>
    /// The value between <paramref name="min"/> and <paramref name="max"/> whose drawn point (<paramref name="drawnAt"/>,
    /// null when it is off the picture or behind the camera) is nearest <paramref name="pointer"/>.
    /// </summary>
    public static double Nearest(Func<double, Point?> drawnAt, double min, double max, Point pointer, int steps = 160)
    {
        var best = min;
        var bestDistance = double.MaxValue;
        for (var i = 0; i <= steps; i++)
        {
            var value = min + ((max - min) * i / steps);
            if (drawnAt(value) is not { } at) continue;

            var distance = ((Vector)(at - pointer)).SquaredLength;
            if (distance < bestDistance)
            {
                best = value;
                bestDistance = distance;
            }
        }

        return best;
    }

    /// <summary><paramref name="value"/> kept in range and rounded to whole <paramref name="step"/>s (without float dust).</summary>
    public static double Snap(double value, double min, double max, double step) =>
        Math.Round(Math.Clamp(Math.Round(value / step) * step, min, max), 6);

    /// <summary>
    /// Keeps a grip inside the picture, <paramref name="margin"/> pixels from its edges, and out of the top right
    /// corner, where the page puts its button that starts the walkthrough.
    /// </summary>
    public static Point Inside(Point point, Rect bounds, double margin = 16)
    {
        var x = Math.Clamp(point.X, bounds.Left + margin, Math.Max(bounds.Left + margin, bounds.Right - margin));
        var y = Math.Clamp(point.Y, bounds.Top + margin, Math.Max(bounds.Top + margin, bounds.Bottom - margin));
        if (x > bounds.Right - ButtonCorner.Width && y < bounds.Top + ButtonCorner.Height) y = bounds.Top + ButtonCorner.Height;
        return new Point(x, y);
    }

    /// <summary>
    /// Moves grips that would sit on top of each other (several parts out of the picture, waiting at its edge) side by
    /// side, keeping them inside it.
    /// </summary>
    public static Point[] Apart(Rect bounds, params Point[] grips)
    {
        const double Gap = 32;
        var placed = new Point[grips.Length];
        for (var i = 0; i < grips.Length; i++)
        {
            var p = grips[i];
            for (var tries = 0; tries < grips.Length && placed.Take(i).Any(q => ((Vector)(q - p)).Length < Gap); tries++)
            {
                p = Inside(new Point(p.X + (p.X > bounds.Center.X ? -Gap : Gap), p.Y), bounds);
            }

            placed[i] = p;
        }

        return placed;
    }

    /// <summary>The room the walkthrough's button takes in the picture's top right corner.</summary>
    private static readonly Size ButtonCorner = new(150, 58);

    /// <summary>Eases <paramref name="value"/> toward <paramref name="target"/>, about a third of the way in <paramref name="time"/> seconds.</summary>
    public static double Ease(double value, double target, double seconds, double time = 0.15) =>
        double.IsNaN(value) ? target : value + ((target - value) * (1 - Math.Exp(-seconds / time * 0.4)));

    /// <summary>The angle of <paramref name="to"/> seen from <paramref name="from"/>, in degrees from straight up, positive to the right.</summary>
    public static double FromUp(Point from, Point to) => Math.Atan2(to.X - from.X, from.Y - to.Y) * 180 / Math.PI;
}
