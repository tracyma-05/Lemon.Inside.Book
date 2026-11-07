using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Friction;

/// <summary>
/// Drawing the chapter's own things: surfaces of ice, wood and rubber, force arrows, and a small chart frame. The
/// arrows are <see cref="Bridges.BridgesParts.DrawPush"/>; positions in metres go through
/// <see cref="Lifting.LiftingParts.View"/>.
/// </summary>
internal static class FrictionParts
{
    public static readonly IImmutableBrush[] Surfaces =
    [
        new ImmutableSolidColorBrush(Color.FromRgb(0xB8, 0xE4, 0xF4)),
        new ImmutableSolidColorBrush(Color.FromRgb(0xA8, 0x7A, 0x4A)),
        new ImmutableSolidColorBrush(Color.FromRgb(0x36, 0x36, 0x3C)),
    ];

    public static readonly IImmutableBrush WeightBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xE6, 0xEA, 0xF2));
    public static readonly IImmutableBrush PullBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x7A, 0xD9, 0x8C));
    public static readonly IImmutableBrush GripBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xF0, 0x6A, 0x5A));
    public static readonly IImmutableBrush Asphalt = new ImmutableSolidColorBrush(Color.FromRgb(0x3C, 0x40, 0x48));
    public static readonly IImmutableBrush Crate = new ImmutableSolidColorBrush(Color.FromRgb(0xB0, 0x86, 0x52));
    public static readonly IImmutableBrush OilBrush = new ImmutableSolidColorBrush(Color.FromArgb(0xC0, 0xD8, 0xA0, 0x30));
    public static readonly IImmutableBrush Metal = new ImmutableSolidColorBrush(Color.FromRgb(0x8F, 0xA3, 0xBF));
    public static readonly IPen Axis = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0xB0, 0xE6, 0xEA, 0xF2)), 1);

    /// <summary>A force arrow with its size written beside it.</summary>
    public static void Arrow(DrawingContext context, Point from, Point to, IBrush brush, string label, double ax, double ay) =>
        Bridges.BridgesParts.DrawPush(context, from, to, brush, label, ax, ay);

    /// <summary>A chart's frame with its two axis titles. Returns the plotting area.</summary>
    public static Rect DrawChartFrame(DrawingContext context, Rect box, string xTitle, string yTitle)
    {
        context.DrawRectangle(Lifting.LiftingParts.Dark, new ImmutablePen(DiagramText.Faint, 1), box, 6, 6);
        var plot = new Rect(box.X + 44, box.Y + 26, box.Width - 60, box.Height - 56);
        context.DrawLine(Axis, plot.BottomLeft, plot.BottomRight);
        context.DrawLine(Axis, plot.BottomLeft, plot.TopLeft);
        DiagramText.Draw(context, xTitle, new Point(plot.Center.X, plot.Bottom + 8), 11, DiagramText.Faint, 0.5, 0);
        DiagramText.Draw(context, yTitle, new Point(box.X + 10, box.Y + 8), 11, DiagramText.Faint, 0, 0);
        return plot;
    }

    public static string Number(double value, string format = "0.#") => value.ToString(format, CultureInfo.CurrentCulture);

    public static string Kgf(double value) => DiagramText.Format("Friction_Kg", Number(value, value < 10 ? "0.#" : "N0"));
}
