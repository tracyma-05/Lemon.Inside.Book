using Avalonia;

namespace Lemon.Inside.Book.Chapters.Gears;

/// <summary>What the chapter's grips share: turning a gear by dragging round it, and choosing a size from a drag.</summary>
internal static class GearsGrips
{
    /// <summary>The direction from <paramref name="centre"/> to <paramref name="at"/> (screen points), counter-clockwise from the right, y up.</summary>
    public static double AngleOf(Point centre, Point at) => Math.Atan2(centre.Y - at.Y, at.X - centre.X);

    /// <summary>
    /// The angle nearest <paramref name="current"/> that points the same way as <paramref name="towards"/>: a gear
    /// dragged round follows the finger without jumping a whole turn as the finger crosses the left.
    /// </summary>
    public static double Follow(double current, double towards)
    {
        var step = Math.IEEERemainder(towards - current, 2 * Math.PI);
        return current + step;
    }

    /// <summary>
    /// The choice in <paramref name="from"/>..<paramref name="to"/> whose drawn size (<paramref name="size"/>, pixels) is
    /// nearest <paramref name="wanted"/>: the drawing may rescale with the choice, so the choice is found, not divided out.
    /// </summary>
    public static int Nearest(int from, int to, double wanted, Func<int, double> size)
    {
        var best = from;
        var gap = double.MaxValue;
        for (var z = from; z <= to; z++)
        {
            var d = Math.Abs(size(z) - wanted);
            if (d < gap)
            {
                (best, gap) = (z, d);
            }
        }

        return best;
    }
}
