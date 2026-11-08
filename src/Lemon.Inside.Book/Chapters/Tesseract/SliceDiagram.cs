using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Tesseract.TesseractParts;

namespace Lemon.Inside.Book.Chapters.Tesseract;

/// <summary>
/// Spread 5: on the left the flat slice a plane cuts from a cube, on the right the solid slice our space cuts from a
/// tesseract, slowly turning so its shape shows. Face first they are a square and a cube; corner first a triangle or
/// hexagon, and a tetrahedron, a tetrahedron with its corners cut off, or an octahedron. The reader drags the depth gauge
/// and taps the switch between face first and corner first.
/// </summary>
public sealed class SliceDiagram : GearPicture
{
    public static readonly StyledProperty<double> DepthProperty =
        AvaloniaProperty.Register<SliceDiagram, double>(nameof(Depth), 0.5, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> CornerFirstProperty =
        AvaloniaProperty.Register<SliceDiagram, bool>(nameof(CornerFirst), defaultBindingMode: BindingMode.TwoWay);

    static SliceDiagram()
    {
        AffectsRender<SliceDiagram>(DepthProperty, CornerFirstProperty);
    }

    public double Depth
    {
        get => GetValue(DepthProperty);
        set => SetValue(DepthProperty, value);
    }

    public bool CornerFirst
    {
        get => GetValue(CornerFirstProperty);
        set => SetValue(CornerFirstProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var depth = Math.Clamp(Depth, 0, 1);
        var w = bounds.Width;

        // The cube's flat slice, on the left.
        var flatScale = Math.Min(w * 0.13, 80);
        var flatCentre = new Point(bounds.X + 30 + (flatScale * 1.1), bounds.Y + (bounds.Height * 0.36));
        var flat = Shadow.Hull(Slice.Points(3, depth, CornerFirst).Select(p => (p[0], p[1])).ToList())
            .Select(p => new Point(flatCentre.X + (p.Item1 * flatScale), flatCentre.Y - (p.Item2 * flatScale))).ToList();
        var flatFrame = new Rect(flatCentre.X - (flatScale * 1.05), flatCentre.Y - (flatScale * 1.05), flatScale * 2.1, flatScale * 2.1);
        context.DrawRectangle(null, Colour.ColourParts.Frame, flatFrame, 6, 6);
        if (flat.Count >= 3) DrawPolygon(context, Tint(CyanInk, 0.35), Wire(CyanInk, 2), flat);
        Mark("flat", flatFrame.Deflate(flatScale * 0.3));
        DiagramText.Draw(context, DiagramText.Get("Tesseract_Slice_FlatLabel"), new Point(flatCentre.X, flatFrame.Bottom + 8), 11.5, DiagramText.Faint, 0.5);

        // The tesseract's solid slice, turning slowly.
        var scale = Math.Min(w * 0.2, bounds.Height * 0.2);
        var centre = new Point(bounds.X + (w * 0.66), flatCentre.Y);
        var yaw = (Seconds * 18) % 360;
        Point P(double[] p) => Project(new Vec3(p[0], p[1], p[2]), centre, scale, yaw, 20);
        var segments = Slice.Segments(4, depth, CornerFirst);
        foreach (var (a, b) in segments) context.DrawLine(Wire(GoldInk, 2), P(a), P(b));
        var points = Slice.Points(4, depth, CornerFirst);
        foreach (var p in points) DrawDot(context, P(p), GoldInk, 3.5);
        var solidBox = new Rect(centre.X - (scale * 1.1), centre.Y - (scale * 1.1), scale * 2.2, scale * 2.2);
        Mark("solid", solidBox.Deflate(scale * 0.3));
        DiagramText.Draw(context, DiagramText.Get("Tesseract_Slice_SolidLabel"), new Point(centre.X, solidBox.Bottom + 8), 11.5, DiagramText.Faint, 0.5);
        var count = DiagramText.Draw(context, DiagramText.Format("Tesseract_Slice_Count", points.Count, DiagramText.Get(Slice.SolidKey(points.Count))),
            new Point(centre.X, solidBox.Bottom + 30), 13, DiagramText.Gold, 0.5, 0, bold: true);
        Mark("count", count.Inflate(4));

        // The depth gauge and the switch.
        var gaugeLeft = new Point(bounds.X + 30, Math.Min(bounds.Bottom - 110, solidBox.Bottom + 100));
        var gaugeWidth = w - 60;
        var knob = DrawBar(context, gaugeLeft, gaugeWidth, depth, DiagramText.Get("Tesseract_Slice_Depth"));
        Grip("depth", knob, GripAxis.Horizontal, DiagramText.Get("Tesseract_Grip_Depth"),
            at => Depth = Math.Round(Along(at, gaugeLeft, gaugeWidth) * 100) / 100);
        Mark("gauge", [gaugeLeft + new Vector(0, -8), gaugeLeft + new Vector(gaugeWidth, 8)], 4);
        var box = Colour.ColourParts.DrawSwitch(context, new Point(bounds.Center.X, gaugeLeft.Y + 40),
            DiagramText.Get(CornerFirst ? "Tesseract_Slice_CornerLabel" : "Tesseract_Slice_FaceLabel"), CornerFirst);
        Grip("first", new Point(box.Left - 2, box.Center.Y), GripAxis.Tap, DiagramText.Get("Tesseract_Grip_First"), _ => CornerFirst = !CornerFirst);
        DiagramText.Draw(context, DiagramText.Get("Tesseract_Slice_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
