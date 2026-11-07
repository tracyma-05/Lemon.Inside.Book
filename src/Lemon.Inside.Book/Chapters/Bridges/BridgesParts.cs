using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Bridges;

/// <summary>
/// Drawing the chapter's own things: members coloured by whether they are squeezed or stretched, stone, rivers, and
/// the legend that says which colour is which. Positions in metres go through <see cref="Lifting.LiftingParts.View"/>.
/// </summary>
internal static class BridgesParts
{
    public static readonly Color SqueezeColor = Color.FromRgb(0xF0, 0x6A, 0x5A);
    public static readonly Color StretchColor = Color.FromRgb(0x5A, 0xB8, 0xF0);
    public static readonly IImmutableBrush SqueezeBrush = new ImmutableSolidColorBrush(SqueezeColor);
    public static readonly IImmutableBrush StretchBrush = new ImmutableSolidColorBrush(StretchColor);
    public static readonly IImmutableBrush Idle = new ImmutableSolidColorBrush(Color.FromRgb(0x8F, 0xA3, 0xBF));
    public static readonly IImmutableBrush StoneLight = new ImmutableSolidColorBrush(Color.FromRgb(0xB8, 0xAE, 0x9C));
    public static readonly IImmutableBrush StoneDark = new ImmutableSolidColorBrush(Color.FromRgb(0x8E, 0x84, 0x74));
    public static readonly IImmutableBrush Keystone = new ImmutableSolidColorBrush(Color.FromRgb(0xD8, 0xB8, 0x6A));
    public static readonly IImmutableBrush River = new ImmutableSolidColorBrush(Color.FromRgb(0x2C, 0x5A, 0x8A));
    public static readonly IImmutableBrush Concrete = new ImmutableSolidColorBrush(Color.FromRgb(0x9A, 0x9C, 0xA2));

    /// <summary>The brush for a force: squeezed (negative), stretched (positive), or hardly anything.</summary>
    public static IImmutableBrush ForBrush(double force, double small) =>
        Math.Abs(force) <= small ? Idle : force < 0 ? SqueezeBrush : StretchBrush;

    /// <summary>A member from <paramref name="a"/> to <paramref name="b"/>, as thick as its share of <paramref name="most"/>.</summary>
    public static void DrawMember(DrawingContext context, Point a, Point b, double force, double most, double thin, double thick)
    {
        var share = most <= 0 ? 0 : Math.Clamp(Math.Abs(force) / most, 0, 1);
        var pen = new ImmutablePen(ForBrush(force, most * 0.01), thin + ((thick - thin) * share), null, PenLineCap.Round);
        context.DrawLine(pen, a, b);
    }

    /// <summary>A pin joint.</summary>
    public static void DrawPin(DrawingContext context, Point at, double radius) =>
        context.DrawEllipse(Lifting.LiftingParts.Dark, new ImmutablePen(Idle, 1.5), at, radius, radius);

    /// <summary>"■ squeezed  ■ stretched" in the picture's top-right corner, under the walkthrough button.</summary>
    public static void DrawLegend(DrawingContext context, Rect bounds)
    {
        var x = bounds.Right - 16;
        var y = bounds.Top + 52;
        foreach (var (key, brush) in new[] { ("Bridges_Stretched", StretchBrush), ("Bridges_Squeezed", SqueezeBrush) })
        {
            var box = DiagramText.Draw(context, DiagramText.Get(key), new Point(x, y), 12, DiagramText.Ink, 1, 0.5);
            context.FillRectangle(brush, new Rect(box.Left - 18, y - 5, 12, 10), 2);
            x = box.Left - 30;
        }
    }

    /// <summary>A sideways or downward push drawn as an arrow with its size written beside it.</summary>
    public static void DrawPush(DrawingContext context, Point from, Point to, IBrush brush, string label, double ax, double ay)
    {
        context.DrawLine(new ImmutablePen((IImmutableBrush)brush, 3), from, to);
        DiagramText.DrawArrowAlong(context, from, to, brush, 10);
        var mid = new Point((from.X + to.X) / 2, (from.Y + to.Y) / 2);
        DiagramText.Draw(context, label, mid, 12.5, brush, ax, ay, bold: true);
    }

    public static string Tonnes(double value) =>
        DiagramText.Format("Bridges_Tonnes", value.ToString(value < 10 ? "0.#" : "N0", CultureInfo.CurrentCulture));

    public static string Number(double value, string format = "0.#") => value.ToString(format, CultureInfo.CurrentCulture);
}
