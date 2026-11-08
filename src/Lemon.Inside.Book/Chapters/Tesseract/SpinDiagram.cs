using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Tesseract.TesseractParts;

namespace Lemon.Inside.Book.Chapters.Tesseract;

/// <summary>
/// Spread 4: the tesseract seen as a cube inside a cube, turned in the x–w plane (and, with "both", in the y–z plane
/// too). The corners that started as the inner cube stay cyan, so a half turn shows the inner cube grow out through the
/// outer one and the outer shrink in. A dial shows the angle, and four arrows which directions the turn moves. The
/// reader drags the dial's handle round.
/// </summary>
public sealed class SpinDiagram : GearPicture
{
    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<SpinDiagram, double>(nameof(Angle), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> BothProperty =
        AvaloniaProperty.Register<SpinDiagram, bool>(nameof(Both), defaultBindingMode: BindingMode.TwoWay);

    private const double Eye = 2.5;

    static SpinDiagram()
    {
        AffectsRender<SpinDiagram>(AngleProperty, BothProperty);
    }

    public double Angle
    {
        get => GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    public bool Both
    {
        get => GetValue(BothProperty);
        set => SetValue(BothProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var angle = Math.Clamp(Angle, 0, 360);
        var scale = Math.Min(bounds.Width, bounds.Height) * 0.28;
        var centre = new Point(bounds.Center.X, bounds.Y + (bounds.Height * 0.4));
        var corners = HyperCube.CornerList(4);
        var near = Look.Scale(0.5, Eye);
        var screen = corners.Select(p => Project(Look.Shadow(Spin.Turn(p, angle, Both), Eye) * (1 / near), centre, scale, 30, 18)).ToList();
        foreach (var (a, b) in HyperCube.EdgeList(4))
        {
            var (wa, wb) = (corners[a].W, corners[b].W);
            var brush = wa != wb ? SteelInk : wa < 0 ? CyanInk : GoldInk;
            context.DrawLine(Wire(brush, wa != wb ? 1.3 : 2.2), screen[a], screen[b]);
        }

        var innerPts = Enumerable.Range(0, 16).Where(i => corners[i].W < 0).Select(i => screen[i]).ToList();
        var outerPts = Enumerable.Range(0, 16).Where(i => corners[i].W > 0).Select(i => screen[i]).ToList();
        foreach (var p in innerPts) DrawDot(context, p, CyanInk);
        foreach (var p in outerPts) DrawDot(context, p, GoldInk);
        Mark("inner", innerPts, 6);
        Mark("outer", outerPts, 8);

        // The dial: drag its handle round.
        var r = Math.Min(54, bounds.Width * 0.09);
        var dial = new Point(bounds.X + 40 + r, Math.Min(bounds.Bottom - 120 - r, centre.Y + (scale * 1.3) + r));
        context.DrawEllipse(Colour.ColourParts.Track, Colour.ColourParts.Frame, dial, r, r);
        var a0 = angle * Math.PI / 180;
        var handle = dial + (new Vector(Math.Sin(a0), -Math.Cos(a0)) * r);
        context.DrawLine(Wire(GoldInk, 2.5), dial, handle);
        DiagramText.Draw(context, DiagramText.Format("Tesseract_Degrees", Math.Round(angle)), dial, 13, DiagramText.Ink, 0.5, 0.5, bold: true);
        DiagramText.Draw(context, DiagramText.Get(Both ? "Tesseract_Spin_Planes" : "Tesseract_Spin_Plane"), new Point(dial.X, dial.Y + r + 10), 11.5,
            DiagramText.Faint, 0.5);
        Mark("dial", dial, r);
        Grip("dial", handle, GripAxis.Free, DiagramText.Get("Tesseract_Grip_Dial"),
            at => Angle = Math.Round(((Math.Atan2(at.X - dial.X, dial.Y - at.Y) * 180 / Math.PI) + 360) % 360));

        // The four directions: those the turn moves in gold, those it leaves alone faint.
        var origin = new Point(bounds.Right - 150, dial.Y - 10);
        string[] names = ["x", "y", "z", "w"];
        Vector[] dirs = [new(1, 0), new(0, -1), new(0.6, -0.4), new(-0.45, -0.55)];
        var moving = Both ? new[] { true, true, true, true } : new[] { true, false, false, true };
        for (var k = 0; k < 4; k++)
        {
            var tip = origin + (dirs[k] * 48);
            var brush = moving[k] ? GoldInk : DiagramText.Faint;
            context.DrawLine(Wire(brush, moving[k] ? 2.4 : 1.4), origin, tip);
            DiagramText.DrawArrowAlong(context, origin, tip, brush, 8);
            DiagramText.Draw(context, names[k], tip + (dirs[k] * 10), 13, brush, 0.5, 0.5, bold: true);
        }

        DiagramText.Draw(context, DiagramText.Format("Tesseract_Spin_Still", Spin.StillDirections(Both)), new Point(origin.X, origin.Y + 30), 11.5,
            DiagramText.Faint, 0.5);
        Mark("axes", [origin + new Vector(-50, -60), origin + new Vector(60, 30)], 4);
        DiagramText.Draw(context, DiagramText.Get("Tesseract_Spin_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
