using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Lemon.Inside.Book.Controls;

/// <summary>
/// The base of every chapter picture. While it paints, the picture marks where its named parts are ("lamp", "spring",
/// …); a walkthrough sets <see cref="Highlight"/> to one of those names and the picture rings that part on top. Since
/// the marks come from the drawing itself, a ring always sits on the part as it is drawn now, wherever the reader has
/// turned or moved it. The parts named in <see cref="PartNames"/> can also be pointed at (their name shows) and clicked,
/// which sets <see cref="SelectedPart"/>; the spread's card then explains that part.
/// </summary>
public abstract class Picture : Control
{
    public static readonly StyledProperty<string?> HighlightProperty =
        AvaloniaProperty.Register<Picture, string?>(nameof(Highlight));

    public static readonly StyledProperty<string?> SelectedPartProperty =
        AvaloniaProperty.Register<Picture, string?>(nameof(SelectedPart), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IReadOnlyDictionary<string, string>?> PartNamesProperty =
        AvaloniaProperty.Register<Picture, IReadOnlyDictionary<string, string>?>(nameof(PartNames));

    /// <summary>A press that moves less than this (pixels) before it lets go is a click, not a drag.</summary>
    private const double ClickSlop = 5;

    // A dark halo under each light stroke: rings show on the dark plates and on a brightly lit floor alike.
    private static readonly IPen Halo = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x99, 0x05, 0x08, 0x10)), 6);
    private static readonly IPen Ring = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0xE6, 0x8A)), 2.5);
    private static readonly IPen SelectedRing = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x8E, 0xC5, 0xFF)), 2);
    private static readonly IPen HoverRing = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0xC8, 0xFF, 0xFF, 0xFF)), 1.4);
    private static readonly IBrush SelectedFill = new ImmutableSolidColorBrush(Color.FromArgb(0x1C, 0x8E, 0xC5, 0xFF));
    private static readonly IBrush PillBrush = new ImmutableSolidColorBrush(Color.FromArgb(0xE0, 0x0B, 0x10, 0x19));

    private readonly Dictionary<string, Rect> _parts = new(StringComparer.Ordinal);
    private string? _hover;
    private Point? _pressedAt;

    static Picture()
    {
        AffectsRender<Picture>(HighlightProperty, SelectedPartProperty, PartNamesProperty);
    }

    /// <summary>The part to ring, by the name the picture marks it with; null for none.</summary>
    public string? Highlight
    {
        get => GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    /// <summary>The part whose card is open: set by a click, cleared by clicking it again or by the card's close button.</summary>
    public string? SelectedPart
    {
        get => GetValue(SelectedPartProperty);
        set => SetValue(SelectedPartProperty, value);
    }

    /// <summary>The parts that can be clicked, with the name shown while the pointer is over one.</summary>
    public IReadOnlyDictionary<string, string>? PartNames
    {
        get => GetValue(PartNamesProperty);
        set => SetValue(PartNamesProperty, value);
    }

    /// <summary>The parts marked in the last frame (for the tests).</summary>
    internal IReadOnlyCollection<string> MarkedParts => _parts.Keys;

    public sealed override void Render(DrawingContext context)
    {
        _parts.Clear();
        Paint(context);
        if (SelectedPart is { } selected && _parts.TryGetValue(selected, out var chosen))
        {
            DrawRing(context, chosen, SelectedRing, SelectedFill);
        }

        if (_hover is { } hover && hover != SelectedPart && _parts.TryGetValue(hover, out var under))
        {
            DrawRing(context, under, HoverRing, null);
            if (PartNames?.TryGetValue(hover, out var name) == true) DrawName(context, name, under);
        }

        if (Highlight is { } id && _parts.TryGetValue(id, out var box))
        {
            DrawRing(context, box, Ring, null);
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

    /// <summary>
    /// The clickable part under <paramref name="point"/>. Where rings overlap, the smallest wins, so a lamp stays
    /// clickable over the floor it lights.
    /// </summary>
    internal string? PartAt(Point point)
    {
        if (PartNames is not { Count: > 0 } names) return null;

        string? best = null;
        var bestSize = double.MaxValue;
        foreach (var (id, box) in _parts)
        {
            var ring = Ellipse(box);
            if (!names.ContainsKey(id) || !Inside(ring.Inflate(4), point)) continue;

            var size = ring.Width * ring.Height;
            if (size < bestSize)
            {
                best = id;
                bestSize = size;
            }
        }

        return best;
    }

    /// <summary>Clicks at <paramref name="point"/> as a reader would: opens that part's card, or closes it if open.</summary>
    internal void ClickAt(Point point)
    {
        var id = PartAt(point);
        SelectedPart = id is not null && id != SelectedPart ? id : null;
    }

    /// <summary>The middle of a part's ring (for the tests).</summary>
    internal Point? PartCentre(string id) => _parts.TryGetValue(id, out var box) ? Ellipse(box).Center : null;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _pressedAt = e.GetPosition(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var at = e.GetPosition(this);
        if (_pressedAt is { } from && ((Vector)(at - from)).Length > ClickSlop) _pressedAt = null;

        // No name pills while the reader drags the picture round.
        var dragging = _pressedAt is null && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
        SetHover(dragging ? null : PartAt(at));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pressedAt is { } from && ((Vector)(e.GetPosition(this) - from)).Length <= ClickSlop)
        {
            ClickAt(from);
        }

        _pressedAt = null;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        SetHover(null);
    }

    private void SetHover(string? id)
    {
        if (id == _hover) return;

        _hover = id;
        Cursor = id is null ? null : new Cursor(StandardCursorType.Hand);
        InvalidateVisual();
    }

    private static bool Inside(Rect ellipse, Point p)
    {
        var dx = (p.X - ellipse.Center.X) / (ellipse.Width / 2);
        var dy = (p.Y - ellipse.Center.Y) / (ellipse.Height / 2);
        return (dx * dx) + (dy * dy) <= 1;
    }

    private void DrawRing(DrawingContext context, Rect box, IPen pen, IBrush? fill)
    {
        var ring = Ellipse(box);
        context.DrawEllipse(fill, Halo, ring.Center, ring.Width / 2, ring.Height / 2);
        context.DrawEllipse(null, pen, ring.Center, ring.Width / 2, ring.Height / 2);
    }

    /// <summary>The part's name in a dark pill just above its ring (below, when there is no room above).</summary>
    private void DrawName(DrawingContext context, string name, Rect box)
    {
        var ring = Ellipse(box);
        var text = new FormattedText(name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.White);
        var width = text.Width + 16;
        var height = text.Height + 8;
        var x = Math.Clamp(ring.Center.X - (width / 2), 4, Math.Max(4, Bounds.Width - width - 4));
        var y = ring.Top - height - 6 >= 4 ? ring.Top - height - 6 : Math.Min(ring.Bottom + 6, Bounds.Height - height - 4);
        var pill = new Rect(x, y, width, height);
        context.DrawRectangle(PillBrush, null, pill, height / 2, height / 2);
        context.DrawText(text, new Point(pill.X + 8, pill.Y + 4));
    }

    /// <summary>The ring round a part's box: just outside it, never smaller than a finger tip, kept inside the picture.</summary>
    private Rect Ellipse(Rect box)
    {
        var rx = Math.Max(16, (box.Width / 2) + 8);
        var ry = Math.Max(16, (box.Height / 2) + 8);
        var bounds = new Rect(Bounds.Size).Deflate(3);
        rx = Math.Min(rx, bounds.Width / 2);
        ry = Math.Min(ry, bounds.Height / 2);
        var centre = new Point(
            Math.Clamp(box.Center.X, bounds.Left + rx, bounds.Right - rx),
            Math.Clamp(box.Center.Y, bounds.Top + ry, bounds.Bottom - ry));
        return new Rect(centre.X - rx, centre.Y - ry, 2 * rx, 2 * ry);
    }
}
