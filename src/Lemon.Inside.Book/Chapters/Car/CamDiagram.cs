using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Car;

/// <summary>
/// Spread 2: a cam on the camshaft pressing a valve open against its spring, the valve's head lifting off its seat in
/// the cylinder head. Below, the valve's lift through the crank's two turns, open for the cam's duration.
/// </summary>
public sealed class CamDiagram : GearPicture
{
    public static readonly StyledProperty<double> LiftProperty =
        AvaloniaProperty.Register<CamDiagram, double>(nameof(Lift), 9);

    public static readonly StyledProperty<double> DurationProperty =
        AvaloniaProperty.Register<CamDiagram, double>(nameof(Duration), 240);

    /// <summary>Crank degrees a second (slowed right down).</summary>
    private const double Speed = 150;

    private static readonly IImmutableBrush Port = new ImmutableSolidColorBrush(Color.FromRgb(0x2A, 0x30, 0x3C));
    private static readonly IImmutableBrush Head = new ImmutableSolidColorBrush(Color.FromRgb(0x46, 0x4E, 0x60));
    private static readonly IImmutableBrush Open = new ImmutableSolidColorBrush(Color.FromArgb(0x28, 0x6F, 0xD8, 0xFF));
    private static readonly IPen SpringPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x8F, 0xD8, 0xF0)), 2);

    private double _crank;

    static CamDiagram()
    {
        AffectsRender<CamDiagram>(LiftProperty, DurationProperty);
    }

    /// <summary>How far the valve opens, millimetres.</summary>
    public double Lift
    {
        get => GetValue(LiftProperty);
        set => SetValue(LiftProperty, value);
    }

    /// <summary>How long it stays open, crank degrees.</summary>
    public double Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    protected override void Advance(double seconds) => _crank = (_crank + (Speed * seconds)) % 720;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var mm = Math.Min(bounds.Width * 0.14 / (Cam.BaseRadius + 12), bounds.Height * 0.5 / 140);
        var camCentre = new Point(bounds.Width * 0.5, (bounds.Height * 0.06) + ((Cam.BaseRadius + 12) * mm));
        var lift = Cam.Lift(_crank, Lift, Duration);

        // The cam: its outline turned so the nose points straight down at crank 360°.
        var nose = (Math.PI / 2) + ((_crank - 360) / 2 * Math.PI / 180);
        var outline = new List<Point>();
        for (var d = 0; d < 360; d += 3)
        {
            var radius = Cam.Radius(d, Lift, Duration) * mm;
            var a = nose + (d * Math.PI / 180);
            outline.Add(new Point(camCentre.X + (radius * Math.Cos(a)), camCentre.Y + (radius * Math.Sin(a))));
        }

        // The valve below: bucket on the cam, stem down through the spring, head in its seat.
        var bucketTop = camCentre.Y + ((Cam.BaseRadius + lift) * mm);
        var bucket = new Rect(camCentre.X - (14 * mm), bucketTop, 28 * mm, 12 * mm);
        var stemLength = 62 * mm;
        var rest = camCentre.Y + ((Cam.BaseRadius + 12) * mm); // the bucket's bottom with the valve shut
        var seat = rest + stemLength;
        var headPlate = new Rect(bounds.X + 30, rest + (30 * mm), bounds.Width - 60, seat - rest - (30 * mm) + (6 * mm));
        context.FillRectangle(Head, headPlate);
        var portWidth = 30 * mm;
        context.FillRectangle(Port, new Rect(camCentre.X - (portWidth / 2), headPlate.Y + (18 * mm), portWidth, headPlate.Height - (18 * mm)));
        context.FillRectangle(Open, new Rect(bounds.X + 30, headPlate.Bottom, bounds.Width - 60, 40 * mm));

        var stemBottom = bucket.Bottom + stemLength;
        context.DrawLine(new ImmutablePen(CarParts.MetalBrush, Math.Max(3, 5 * mm)), new Point(camCentre.X, bucket.Bottom), new Point(camCentre.X, stemBottom));
        var valveHead = Outline([
            new Point(camCentre.X - (2.5 * mm), stemBottom - (5 * mm)), new Point(camCentre.X + (2.5 * mm), stemBottom - (5 * mm)),
            new Point(camCentre.X + (16 * mm), stemBottom), new Point(camCentre.X + (16 * mm), stemBottom + (3 * mm)),
            new Point(camCentre.X - (16 * mm), stemBottom + (3 * mm)), new Point(camCentre.X - (16 * mm), stemBottom),
        ]);
        context.DrawGeometry(CarParts.MetalBrush, CarParts.Edge, valveHead);
        if (lift > 0.3)
        {
            DiagramText.Draw(context, DiagramText.Get("Car_Cam_Open"), new Point(camCentre.X + (20 * mm), stemBottom + (2 * mm)), 12, DiagramText.Cyan, 0, 0.5, bold: true);
        }

        // The spring, from the head down on the bucket's underside.
        var springTop = bucket.Bottom;
        var springBottom = headPlate.Y + (2 * mm);
        DrawSpring(context, camCentre.X, springTop, springBottom, 11 * mm);

        context.DrawRectangle(new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xCE, 0xD8)), CarParts.Edge, bucket, 3, 3);
        context.DrawGeometry(DiagramText.Gold, CarParts.Edge, Outline(outline));
        context.DrawEllipse(CarParts.Dark, null, camCentre, 4 * mm, 4 * mm);

        Mark("cam", camCentre, (Cam.BaseRadius + Lift) * mm);
        Mark("valve", [new Point(camCentre.X - (16 * mm), stemBottom - (6 * mm)), new Point(camCentre.X + (16 * mm), stemBottom + (4 * mm))], 4);
        Mark("spring", [new Point(camCentre.X - (11 * mm), springTop), new Point(camCentre.X + (11 * mm), springBottom)], 2);

        var graphTop = headPlate.Bottom + (40 * mm) + 30;
        DrawGraph(context, new Rect(bounds.X + 30, graphTop, bounds.Width - 54, bounds.Bottom - graphTop - 30));
    }

    /// <summary>The valve's lift through the crank's two turns, the open stretch shaded, a dot for now.</summary>
    private void DrawGraph(DrawingContext context, Rect area)
    {
        if (area.Height < 30) return;

        double X(double crank) => area.X + (crank / 720 * area.Width);
        double Y(double lift) => area.Bottom - (lift / 12 * area.Height);
        context.FillRectangle(new ImmutableSolidColorBrush(Color.FromArgb(0x24, 0x6F, 0xD8, 0xFF)),
            new Rect(X(360 - (Duration / 2)), area.Y, Duration / 720 * area.Width, area.Height));
        CarParts.DrawCycleAxis(context, area, 720, area.Bottom);
        var curve = new List<Point>();
        for (var d = 0.0; d <= 720; d += 4) curve.Add(new Point(X(d), Y(Cam.Lift(d, Lift, Duration))));
        CarParts.DrawLine(context, curve, new ImmutablePen(DiagramText.Gold, 2.5));
        Mark("graph", curve, 4);
        context.DrawEllipse(Brushes.White, CarParts.Edge, new Point(X(_crank), Y(Cam.Lift(_crank, Lift, Duration))), 5, 5);
        DiagramText.Draw(context, DiagramText.Format("Car_Cam_Graph", Lift.ToString("0.0")), new Point(area.X, area.Y - 4), 11, DiagramText.Faint, 0, 1);
        DiagramText.Draw(context, DiagramText.Get("Car_Cam_Crank"), new Point(area.Right, area.Y - 4), 11, DiagramText.Faint, 1, 1);
    }

    private static void DrawSpring(DrawingContext context, double x, double top, double bottom, double half)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            const int Coils = 7;
            g.BeginFigure(new Point(x - half, top), false);
            for (var i = 1; i <= Coils * 2; i++)
            {
                g.LineTo(new Point(x + (i % 2 == 0 ? -half : half), top + ((bottom - top) * i / (Coils * 2))));
            }

            g.EndFigure(false);
        }

        context.DrawGeometry(null, SpringPen, geometry);
    }

    private static StreamGeometry Outline(IReadOnlyList<Point> points)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(points[0], true);
            for (var i = 1; i < points.Count; i++) g.LineTo(points[i]);
            g.EndFigure(true);
        }

        return geometry;
    }
}
