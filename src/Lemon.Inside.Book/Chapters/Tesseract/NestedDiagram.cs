using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Tesseract.TesseractParts;

namespace Lemon.Inside.Book.Chapters.Tesseract;

/// <summary>
/// Spread 3: a cube seen straight on in perspective is a square inside a square; a tesseract seen the same way along its
/// fourth direction is a cube inside a cube, the six cubes between them squashed into wedges. The far (inner) cube is
/// cyan, the near (outer) one gold. The reader drags the eye nearer or farther and turns the view.
/// </summary>
public sealed class NestedDiagram : GearPicture
{
    public static readonly StyledProperty<double> DistanceProperty =
        AvaloniaProperty.Register<NestedDiagram, double>(nameof(Distance), 2.5, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> TurnProperty =
        AvaloniaProperty.Register<NestedDiagram, double>(nameof(Turn), 30, defaultBindingMode: BindingMode.TwoWay);

    static NestedDiagram()
    {
        AffectsRender<NestedDiagram>(DistanceProperty, TurnProperty);
    }

    public double Distance
    {
        get => GetValue(DistanceProperty);
        set => SetValue(DistanceProperty, value);
    }

    public double Turn
    {
        get => GetValue(TurnProperty);
        set => SetValue(TurnProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var d = Math.Clamp(Distance, 1.2, 5);
        var near = Look.Scale(0.5, d);

        // A cube seen straight on, the near face full size: a square inside a square.
        var small = Math.Min(bounds.Width * 0.11, 60);
        var c0 = new Point(bounds.X + 30 + small, bounds.Y + 70 + small);
        var inner2 = small * Look.InnerRatio(d);
        var outerSq = Square(c0, small);
        var innerSq = Square(c0, inner2);
        for (var i = 0; i < 4; i++) context.DrawLine(Wire(SteelInk, 1.4), outerSq[i], innerSq[i]);
        DrawPolygon(context, null, Wire(GoldInk, 2), outerSq);
        DrawPolygon(context, null, Wire(CyanInk, 2), innerSq);
        Mark("square", outerSq, 6);
        DiagramText.Draw(context, DiagramText.Get("Tesseract_Nested_Cube"), new Point(c0.X, c0.Y + small + 10), 11.5, DiagramText.Faint, 0.5);

        // The tesseract: along w in perspective, then the 3D result turned for a view of it.
        var scale = Math.Min(bounds.Width, bounds.Height) * 0.3;
        var centre = new Point(bounds.Center.X + (bounds.Width * 0.08), bounds.Y + (bounds.Height * 0.47));
        var corners = HyperCube.CornerList(4);
        var screen = corners.Select(p => Project(Look.Shadow(p, d) * (1 / near), centre, scale, Turn, 18)).ToList();
        foreach (var (a, b) in HyperCube.EdgeList(4))
        {
            var (wa, wb) = (corners[a].W, corners[b].W);
            var brush = wa != wb ? SteelInk : wa < 0 ? CyanInk : GoldInk;
            context.DrawLine(Wire(brush, wa != wb ? 1.4 : 2.2), screen[a], screen[b]);
        }

        var innerPts = Enumerable.Range(0, 16).Where(i => corners[i].W < 0).Select(i => screen[i]).ToList();
        var outerPts = Enumerable.Range(0, 16).Where(i => corners[i].W > 0).Select(i => screen[i]).ToList();
        foreach (var p in innerPts) DrawDot(context, p, CyanInk);
        foreach (var p in outerPts) DrawDot(context, p, GoldInk);
        Mark("outer", outerPts, 8);
        Mark("inner", innerPts, 6);

        // One of the six cubes between: the one on the +x side, shown by its middle.
        var side = Enumerable.Range(0, 16).Where(i => corners[i].X > 0).Select(i => screen[i]).ToList();
        var mid = new Point(side.Average(p => p.X), side.Average(p => p.Y));
        Mark("between", mid, 14);

        // The two bars: the eye's distance along w, and the turn of the view.
        var width = Math.Min(220, (bounds.Width - 80) / 2);
        var y = Math.Min(bounds.Bottom - 110, centre.Y + (scale * 1.25));
        var left1 = new Point(bounds.X + 30, y);
        var left2 = new Point(bounds.Right - 30 - width, y);
        var k1 = DrawBar(context, left1, width, (d - 1.2) / 3.8, DiagramText.Get("Tesseract_Nested_Distance"));
        var k2 = DrawBar(context, left2, width, Math.Clamp(Turn, 0, 360) / 360, DiagramText.Get("Tesseract_Nested_Turn"));
        Grip("eye", k1, GripAxis.Horizontal, DiagramText.Get("Tesseract_Grip_Eye"), at => Distance = Math.Round((1.2 + (3.8 * Along(at, left1, width))) * 10) / 10);
        Grip("turn", k2, GripAxis.Horizontal, DiagramText.Get("Tesseract_Grip_Turn"), at => Turn = Math.Round(Along(at, left2, width) * 360));
        DiagramText.Draw(context, DiagramText.Format("Tesseract_Nested_Ratio", (100 * Look.InnerRatio(d)).ToString("0", System.Globalization.CultureInfo.CurrentCulture)),
            new Point(bounds.Center.X, y - 44), 13, DiagramText.Gold, 0.5, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Tesseract_Nested_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }

    private static List<Point> Square(Point c, double half) =>
        [new(c.X - half, c.Y - half), new(c.X + half, c.Y - half), new(c.X + half, c.Y + half), new(c.X - half, c.Y + half)];
}
