using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Tesseract.TesseractParts;

namespace Lemon.Inside.Book.Chapters.Tesseract;

/// <summary>
/// Spread 1: a shape slid along a new direction leaves a copy of itself, every corner joined to its copy by a new edge.
/// Each direction is drawn at its own slant on the page, so the fourth fits beside the other three. Below, a row of the
/// five shapes with their corners and edges. The reader drags the copy along and taps to add a direction.
/// </summary>
public sealed class BuildDiagram : GearPicture
{
    public static readonly StyledProperty<double> DimensionProperty =
        AvaloniaProperty.Register<BuildDiagram, double>(nameof(Dimension), 4, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> SlideProperty =
        AvaloniaProperty.Register<BuildDiagram, double>(nameof(Slide), 1, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The page slant each direction is drawn at: x across, y up, z and w at angles of their own.</summary>
    private static readonly Vector[] Slants = [new(1, 0), new(0, -1), new(0.6, -0.36), new(-0.34, -0.6)];

    static BuildDiagram()
    {
        AffectsRender<BuildDiagram>(DimensionProperty, SlideProperty);
    }

    public double Dimension
    {
        get => GetValue(DimensionProperty);
        set => SetValue(DimensionProperty, value);
    }

    public double Slide
    {
        get => GetValue(SlideProperty);
        set => SetValue(SlideProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var n = (int)Math.Round(Math.Clamp(Dimension, 1, 4));
        var slide = Math.Clamp(Slide, 0, 1);
        var size = Math.Min(bounds.Width, bounds.Height) * 0.27;
        var centre = new Point(bounds.Center.X, bounds.Y + (bounds.Height * 0.4));

        // The shape one direction down sits at −½ along the new direction; its copy has slid `slide` of the way to +½.
        Point At(int corner, double along)
        {
            var p = HyperCube.Corner(corner, n - 1);
            var v = new Vector(0, 0);
            for (var k = 0; k < n - 1; k++) v += Slants[k] * p[k];
            v += Slants[n - 1] * (along - 0.5);
            return centre + (v * size);
        }

        var count = HyperCube.Corners(n - 1);
        var original = Enumerable.Range(0, count).Select(i => At(i, 0)).ToList();
        var copy = Enumerable.Range(0, count).Select(i => At(i, slide)).ToList();
        var edges = HyperCube.EdgeList(n - 1);

        // The new edges, swept by each corner as it slides.
        for (var i = 0; i < count; i++) context.DrawLine(Wire(CyanInk, 1.8), original[i], copy[i]);
        foreach (var (a, b) in edges) context.DrawLine(Wire(SteelInk, 2.4), original[a], original[b]);
        foreach (var (a, b) in edges) context.DrawLine(Wire(GoldInk, 2.4), copy[a], copy[b]);
        foreach (var p in original) DrawDot(context, p, SteelInk, 4.5);
        foreach (var p in copy) DrawDot(context, p, GoldInk, 4.5);
        Mark("original", original, 10);
        Mark("copy", copy, 10);
        Mark("joins", [Mid(original[0], copy[0]), Mid(original[^1], copy[^1])], 12);

        // Which way the new direction goes.
        var from = Mid(original[0], original[^1]);
        var dir = Slants[n - 1];
        var tip = from + (dir * size * 1.25);
        context.DrawLine(Wire(CyanInk, 1.2), from, tip);
        DiagramText.DrawArrowAlong(context, from, tip, CyanInk, 9);
        DiagramText.Draw(context, DiagramText.Get("Tesseract_Build_New"), tip + (dir * 10), 11.5, CyanInk, dir.X < 0 ? 1 : 0, 0.5);

        // Drag the copy along the new direction.
        var copyMid = Mid(copy[0], copy[^1]);
        var start = Mid(original[0], original[^1]);
        Grip("slide", copyMid, GripAxis.Free, DiagramText.Get("Tesseract_Grip_Slide"), at =>
        {
            var d = at - start;
            Slide = Math.Round(Math.Clamp(((d.X * dir.X) + (d.Y * dir.Y)) / (size * ((dir.X * dir.X) + (dir.Y * dir.Y))), 0, 1) * 100) / 100;
        });

        // The five shapes, with their corners and edges; the one being built picked out.
        var rowY = Math.Min(bounds.Bottom - 120, centre.Y + (size * 1.55));
        var cell = (bounds.Width - 40) / 5;
        var countBox = new Rect(bounds.X + 20, rowY - 8, cell * 5, 52);
        for (var k = 0; k <= 4; k++)
        {
            var x = bounds.X + 20 + (cell * (k + 0.5));
            var brush = k == n ? DiagramText.Gold : DiagramText.Faint;
            DiagramText.Draw(context, DiagramText.Get(Build.NameKey(k)), new Point(x, rowY), 12, brush, 0.5, 0, bold: k == n);
            DiagramText.Draw(context, DiagramText.Format("Tesseract_Build_Count", HyperCube.Corners(k), HyperCube.Edges(k)), new Point(x, rowY + 20), 11,
                brush, 0.5, 0);
        }

        Mark("count", countBox);

        // Tap to add a direction (after the tesseract, start again from a line).
        var box = Colour.ColourParts.DrawSwitch(context, new Point(bounds.X + 90, bounds.Y + 52), DiagramText.Get("Tesseract_Build_Add"), false);
        Grip("next", new Point(box.Left - 2, box.Center.Y), GripAxis.Tap, DiagramText.Get("Tesseract_Grip_Next"),
            _ => (Dimension, Slide) = n >= 4 ? (1, 0) : (n + 1, 0));
        DiagramText.Draw(context, DiagramText.Get("Tesseract_Build_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }

    private static Point Mid(Point a, Point b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2);
}
