using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Colour;

/// <summary>Drawing the chapter's own things: three overlapping discs, level bars with their grips, swatches and switches.</summary>
internal static class ColourParts
{
    public static readonly IPen Edge = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E)), 1.2);
    public static readonly IPen Frame = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0xE6, 0xEA, 0xF2)), 1);
    public static readonly IImmutableBrush Track = new ImmutableSolidColorBrush(Color.FromArgb(0x30, 0xE6, 0xEA, 0xF2));

    public static IImmutableBrush Brush(Rgb colour, double opacity = 1) => new ImmutableSolidColorBrush(colour.ToColor(opacity));

    public static IImmutableBrush Brush(Color colour) => new ImmutableSolidColorBrush(colour);

    /// <summary>
    /// Three discs of radius <paramref name="radius"/> at <paramref name="centres"/>, each of the seven regions they
    /// make filled with <paramref name="colour"/> of which discs cover it (bit 0 the first disc, bit 1 the second, bit 2
    /// the third).
    /// </summary>
    public static void DrawThreeDiscs(DrawingContext context, IReadOnlyList<Point> centres, double radius, Func<int, Rgb> colour)
    {
        var discs = centres.Select(c => (Geometry)new EllipseGeometry(new Rect(c.X - radius, c.Y - radius, 2 * radius, 2 * radius))).ToArray();
        for (var mask = 1; mask < 8; mask++)
        {
            Geometry? region = null;
            for (var i = 0; i < 3; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                region = region is null ? discs[i] : new CombinedGeometry(GeometryCombineMode.Intersect, region, discs[i]);
            }

            for (var i = 0; i < 3; i++)
            {
                if ((mask & (1 << i)) != 0) continue;
                region = new CombinedGeometry(GeometryCombineMode.Exclude, region!, discs[i]);
            }

            context.DrawGeometry(Brush(colour(mask)), null, region!);
        }
    }

    /// <summary>Where only disc <paramref name="index"/> covers: pushed out from the three centres' middle.</summary>
    public static Point OnlyIn(IReadOnlyList<Point> centres, int index, double radius)
    {
        var middle = Middle(centres);
        var away = centres[index] - middle;
        var length = Math.Max(1, Math.Sqrt((away.X * away.X) + (away.Y * away.Y)));
        return centres[index] + (away / length * radius * 0.45);
    }

    public static Point Middle(IReadOnlyList<Point> centres) =>
        new(centres.Average(c => c.X), centres.Average(c => c.Y));

    /// <summary>
    /// A level bar standing from <paramref name="bottom"/> up <paramref name="height"/> pixels, filled to
    /// <paramref name="share"/> with <paramref name="fill"/>. Returns where its grip goes (the top of the fill).
    /// </summary>
    public static Point DrawLevel(DrawingContext context, Point bottom, double height, double width, double share, IBrush fill)
    {
        var s = Math.Clamp(share, 0, 1);
        var track = new Rect(bottom.X - (width / 2), bottom.Y - height, width, height);
        context.DrawRectangle(Track, Frame, track, 3, 3);
        context.FillRectangle(fill, new Rect(track.X, bottom.Y - (height * s), width, height * s));
        return new Point(bottom.X, bottom.Y - (height * s));
    }

    /// <summary>The share a vertical level of <paramref name="height"/> standing on <paramref name="bottomY"/> is dragged to.</summary>
    public static double LevelAt(Point at, double bottomY, double height) => Math.Clamp((bottomY - at.Y) / height, 0, 1);

    /// <summary>
    /// A horizontal bar from <paramref name="left"/>, <paramref name="width"/> long, filled to <paramref name="share"/>.
    /// Returns the end of the fill.
    /// </summary>
    public static Point DrawMeter(DrawingContext context, Point left, double width, double height, double share, IBrush fill)
    {
        var s = Math.Clamp(share, 0, 1);
        var track = new Rect(left.X, left.Y - (height / 2), width, height);
        context.DrawRectangle(Track, Frame, track, 3, 3);
        context.FillRectangle(fill, new Rect(track.X, track.Y, width * s, height));
        return new Point(left.X + (width * s), left.Y);
    }

    /// <summary>A switch: a pill with its label, lit when <paramref name="on"/>. Returns its box.</summary>
    public static Rect DrawSwitch(DrawingContext context, Point centre, string label, bool on)
    {
        var typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold);
        var text = new FormattedText(label, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, 12, DiagramText.Ink);
        var box = new Rect(centre.X - (text.Width / 2) - 14, centre.Y - 13, text.Width + 28, 26);
        context.DrawRectangle(on ? Brush(Color.FromArgb(0x60, 0xF2, 0xC9, 0x4C)) : Track, Frame, box, 13, 13);
        context.DrawText(text, new Point(centre.X - (text.Width / 2), centre.Y - (text.Height / 2)));
        return box;
    }

    /// <summary>A grey checkerboard, the usual sign for "see-through here".</summary>
    public static void DrawCheckers(DrawingContext context, Rect box, double cell)
    {
        var light = Brush(Color.FromRgb(0x9A, 0xA0, 0xA8));
        var dark = Brush(Color.FromRgb(0x6A, 0x70, 0x78));
        context.FillRectangle(dark, box);
        using (context.PushClip(box))
        {
            for (var y = 0; box.Y + (y * cell) < box.Bottom; y++)
            {
                for (var x = (y % 2); box.X + (x * cell) < box.Right; x += 2)
                {
                    context.FillRectangle(light, new Rect(box.X + (x * cell), box.Y + (y * cell), cell, cell));
                }
            }
        }
    }

    public static string Number(double value, string format = "0") => value.ToString(format, CultureInfo.CurrentCulture);

    public static string Percent(double share) => DiagramText.Format("Colour_Percent", Number(100 * share));
}
