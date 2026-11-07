using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Lemon.Inside.Book.Controls;

/// <summary>
/// The base of every chapter picture. While it paints, the picture marks where its named parts are ("lamp", "spring",
/// …); a walkthrough sets <see cref="Highlight"/> to one of those names and the picture rings that part on top. Since
/// the marks come from the drawing itself, a ring always sits on the part as it is drawn now, wherever the reader has
/// turned or moved it.
/// </summary>
public abstract class Picture : Control
{
    public static readonly StyledProperty<string?> HighlightProperty =
        AvaloniaProperty.Register<Picture, string?>(nameof(Highlight));

    // A dark halo under a light stroke: the ring shows on the dark plates and on a brightly lit floor alike.
    private static readonly IPen Halo = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x99, 0x05, 0x08, 0x10)), 6);
    private static readonly IPen Ring = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0xE6, 0x8A)), 2.5);

    private readonly Dictionary<string, Rect> _parts = new(StringComparer.Ordinal);

    static Picture()
    {
        AffectsRender<Picture>(HighlightProperty);
    }

    /// <summary>The part to ring, by the name the picture marks it with; null for none.</summary>
    public string? Highlight
    {
        get => GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    /// <summary>The parts marked in the last frame (for the tests).</summary>
    internal IReadOnlyCollection<string> MarkedParts => _parts.Keys;

    public sealed override void Render(DrawingContext context)
    {
        _parts.Clear();
        Paint(context);
        if (Highlight is { } id && _parts.TryGetValue(id, out var box))
        {
            DrawRing(context, box);
        }
    }

    /// <summary>Draws the picture, marking its parts as it goes.</summary>
    protected abstract void Paint(DrawingContext context);

    /// <summary>Marks a round part: <paramref name="radius"/> pixels round <paramref name="centre"/>.</summary>
    protected void Mark(string id, Point centre, double radius) =>
        Mark(id, new Rect(centre.X - radius, centre.Y - radius, 2 * radius, 2 * radius));

    /// <summary>Marks a part by the box round it. Marking the same part again grows the box to hold both.</summary>
    protected void Mark(string id, Rect box) =>
        _parts[id] = _parts.TryGetValue(id, out var before) ? before.Union(box) : box;

    /// <summary>Marks a part by the points it spans (a line, a patch), with <paramref name="pad"/> pixels to spare.</summary>
    protected void Mark(string id, IEnumerable<Point> points, double pad = 6)
    {
        // Plain min and max: Rect.Union skips empty rects, so it cannot grow a box from points.
        double left = double.MaxValue, top = double.MaxValue, right = double.MinValue, bottom = double.MinValue;
        foreach (var p in points)
        {
            left = Math.Min(left, p.X);
            top = Math.Min(top, p.Y);
            right = Math.Max(right, p.X);
            bottom = Math.Max(bottom, p.Y);
        }

        if (left > right) return;

        var box = new Rect(left, top, right - left, bottom - top);
        Mark(id, pad >= 0 ? box.Inflate(pad) : box.Deflate(Math.Min(-pad, Math.Min(box.Width, box.Height) / 2)));
    }

    private void DrawRing(DrawingContext context, Rect box)
    {
        // An ellipse just round the part, never smaller than a finger tip, kept inside the picture.
        var rx = Math.Max(16, (box.Width / 2) + 8);
        var ry = Math.Max(16, (box.Height / 2) + 8);
        var centre = box.Center;
        var bounds = new Rect(Bounds.Size).Deflate(3);
        rx = Math.Min(rx, bounds.Width / 2);
        ry = Math.Min(ry, bounds.Height / 2);
        centre = new Point(
            Math.Clamp(centre.X, bounds.Left + rx, bounds.Right - rx),
            Math.Clamp(centre.Y, bounds.Top + ry, bounds.Bottom - ry));
        context.DrawEllipse(null, Halo, centre, rx, ry);
        context.DrawEllipse(null, Ring, centre, rx, ry);
    }
}
