using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Tesseract.TesseractParts;

namespace Lemon.Inside.Book.Chapters.Tesseract;

/// <summary>
/// Spread 6: top left, a cross of six squares folding up into a cube. On the right, the tesseract's cross of eight
/// cubes, lettered A to H: the chosen cube gold, the six it touches once folded up in four directions cyan, and the one
/// opposite it, which it never touches, rose. The reader drags the fold bar and taps the chosen cube to pick the next.
/// </summary>
public sealed class NetDiagram : GearPicture
{
    public static readonly StyledProperty<double> FoldProperty =
        AvaloniaProperty.Register<NetDiagram, double>(nameof(Fold), 0.6, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> ChosenProperty =
        AvaloniaProperty.Register<NetDiagram, double>(nameof(Chosen), 1, defaultBindingMode: BindingMode.TwoWay);

    static NetDiagram()
    {
        AffectsRender<NetDiagram>(FoldProperty, ChosenProperty);
    }

    public double Fold
    {
        get => GetValue(FoldProperty);
        set => SetValue(FoldProperty, value);
    }

    public double Chosen
    {
        get => GetValue(ChosenProperty);
        set => SetValue(ChosenProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var fold = Math.Clamp(Fold, 0, 1);
        var chosen = (int)Math.Round(Math.Clamp(Chosen, 0, Net.Count - 1));
        var w = bounds.Width;

        // The cube's cross, folding: squares far to near.
        var s1 = Math.Min(w * 0.095, bounds.Height * 0.08);
        var c1 = new Point(bounds.X + 40 + (s1 * 2), bounds.Y + 80 + (s1 * 2.4));
        Point P1(Vec3 p) => Project(p - new Vec3(0.5, 0.5, 0), c1, s1, -25, 35);
        var squares = Net.Squares.Select(sq =>
        {
            var pts = new[] { (0.0, 0.0), (1.0, 0.0), (1.0, 1.0), (0.0, 1.0) }
                .Select(o => Net.FoldCube(sq.X + Inset(o.Item1), sq.Y + Inset(o.Item2), fold)).ToList();
            return (Points: pts, Depth: pts.Average(p => View(p - new Vec3(0.5, 0.5, 0), -25, 35).Y));
        }).OrderByDescending(q => q.Depth);
        var crossPts = new List<Point>();
        foreach (var q in squares)
        {
            var pts = q.Points.Select(P1).ToList();
            DrawPolygon(context, Tint(SteelInk, 0.45), Wire(SteelInk, 1.4), pts);
            crossPts.AddRange(pts);
        }

        Mark("cross", crossPts, 2);
        var barLeft = new Point(bounds.X + 30, crossPts.Max(p => p.Y) + 40);
        var barWidth = Math.Min(200, w * 0.36);
        var knob = DrawBar(context, barLeft, barWidth, fold, DiagramText.Get("Tesseract_Net_Fold"));
        Grip("fold", knob, GripAxis.Horizontal, DiagramText.Get("Tesseract_Grip_Fold"), at => Fold = Math.Round(Along(at, barLeft, barWidth) * 100) / 100);

        // The tesseract's cross of eight cubes: every face of every cube, far to near.
        var s2 = Math.Min(w * 0.11, bounds.Height * 0.1);
        var c2 = new Point(bounds.X + (w * 0.7), bounds.Y + (bounds.Height * 0.42));
        const double Yaw = 32;
        const double Pitch = 20;
        Point P2(Vec3 p) => Project(p, c2, s2, Yaw, Pitch);
        var touching = Net.Touching(chosen);
        var opposite = Net.Opposite(chosen);
        IImmutableBrush BrushOf(int cube) => cube == chosen ? GoldInk : cube == opposite ? RoseInk : touching.Contains(cube) ? CyanInk : SteelInk;
        var faces = new List<(List<Vec3> Points, double Depth, int Cube)>();
        for (var cube = 0; cube < Net.Count; cube++)
        {
            var (x, y, z) = Net.Places[cube];
            var corners = HyperCube.CornerList(3).Select(c => new Vec3(c.X + x, c.Y + y, c.Z + z)).ToList();
            foreach (var sq in HyperCube.SquareList(3))
            {
                var pts = sq.Select(i => corners[i]).ToList();
                faces.Add((pts, pts.Average(p => View(p, Yaw, Pitch).Y), cube));
            }
        }

        foreach (var face in faces.OrderByDescending(f => f.Depth))
        {
            var brush = BrushOf(face.Cube);
            DrawPolygon(context, Tint(brush, 0.22), Wire(brush, 1.2), face.Points.Select(P2).ToList());
        }

        var centres = Net.Places.Select(p => P2(new Vec3(p.X, p.Y, p.Z))).ToList();
        for (var cube = 0; cube < Net.Count; cube++)
        {
            DiagramText.Draw(context, Net.Letter(cube), centres[cube], 14, cube == chosen ? DiagramText.Gold : DiagramText.Ink, 0.5, 0.5, bold: true);
        }

        Mark("net", faces.SelectMany(f => f.Points).Select(P2), 2);
        Mark("chosen", centres[chosen], s2 * 0.35);
        Mark("opposite", centres[opposite], s2 * 0.35);
        Grip("choose", centres[chosen] + new Vector(s2 * 0.35, s2 * 0.35), GripAxis.Tap, DiagramText.Get("Tesseract_Grip_Choose"),
            _ => Chosen = (chosen + 1) % Net.Count);

        // A key to the colours.
        var keyY = Math.Min(bounds.Bottom - 130, barLeft.Y + 40);
        string[] keys = ["Tesseract_Net_KeyChosen", "Tesseract_Net_KeyTouching", "Tesseract_Net_KeyOpposite"];
        IImmutableBrush[] brushes = [GoldInk, CyanInk, RoseInk];
        for (var k = 0; k < 3; k++)
        {
            var at = new Point(bounds.X + 30, keyY + (k * 20));
            context.FillRectangle(brushes[k], new Rect(at.X, at.Y - 5, 12, 10));
            DiagramText.Draw(context, DiagramText.Get(keys[k]), at + new Vector(18, 0), 11.5, DiagramText.Faint, 0, 0.5);
        }

        DiagramText.Draw(context, DiagramText.Get("Tesseract_Net_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }

    /// <summary>A hair in from each square's edge, so the folded squares do not fight over the same line.</summary>
    private static double Inset(double v) => v == 0 ? 0.01 : 0.99;
}
