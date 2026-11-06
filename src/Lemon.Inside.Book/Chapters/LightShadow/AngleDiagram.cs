using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// Spread 4, from above: an eye, a car side on down the road, and the angle between the car's two ends. An arc
/// round the eye turns the angle into a length (radians); the inset shows how big the car looks.
/// </summary>
public sealed class AngleDiagram : Control
{
    public static readonly StyledProperty<double> DistanceProperty =
        AvaloniaProperty.Register<AngleDiagram, double>(nameof(Distance), 10);

    /// <summary>The furthest the slider goes, so the scale stays the same while the car moves.</summary>
    public const double MaxDistance = 40;

    private static readonly IBrush Plate = new SolidColorBrush(Color.FromRgb(0x10, 0x16, 0x22));
    private static readonly IBrush RoadBrush = new SolidColorBrush(Color.FromRgb(0x24, 0x2B, 0x38));
    private static readonly IBrush CarBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0x5D, 0x4B));
    private static readonly IBrush WedgeBrush = new SolidColorBrush(Color.FromArgb(0x22, 0x6F, 0xD8, 0xFF));
    private static readonly IPen SightPen = new Pen(DiagramText.Cyan, 1.5);
    private static readonly IPen ArcPen = new Pen(DiagramText.Gold, 3);
    private static readonly IPen CirclePen = new Pen(DiagramText.Faint, 1, new DashStyle([3, 4], 0));
    private static readonly IBrush PanelBrush = new SolidColorBrush(Color.FromArgb(0xC0, 0x05, 0x08, 0x0E));

    static AngleDiagram()
    {
        AffectsRender<AngleDiagram>(DistanceProperty);
    }

    public AngleDiagram()
    {
        ClipToBounds = true;
    }

    /// <summary>How far the car is from the eye, in metres.</summary>
    public double Distance
    {
        get => GetValue(DistanceProperty);
        set => SetValue(DistanceProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(Plate, bounds);
        if (bounds.Width < 80 || bounds.Height < 80)
        {
            return;
        }

        var eye = new Point(bounds.X + 36, bounds.Height * 0.62);
        var perMetre = (bounds.Width - 70) / (MaxDistance + 2);
        var half = AngleSpread.CarLength / 2 * perMetre;
        var carX = eye.X + (Distance * perMetre);
        var radians = 2 * Math.Atan(AngleSpread.CarLength / 2 / Distance);

        // The road, the car (side on, seen from above) and the two lines of sight to its ends.
        context.FillRectangle(RoadBrush, new Rect(bounds.X, eye.Y - (half * 1.6), bounds.Width, half * 3.2));
        var front = new Point(carX, eye.Y - half);
        var back = new Point(carX, eye.Y + half);
        var wedge = new StreamGeometry();
        using (var g = wedge.Open())
        {
            g.BeginFigure(eye, true);
            g.LineTo(front);
            g.LineTo(back);
            g.EndFigure(true);
        }

        context.DrawGeometry(WedgeBrush, null, wedge);
        context.FillRectangle(CarBrush, new Rect(carX - Math.Max(3, perMetre * 0.9), front.Y, Math.Max(6, perMetre * 1.8), half * 2), 3);
        context.DrawLine(SightPen, eye, front);
        context.DrawLine(SightPen, eye, back);
        context.DrawEllipse(DiagramText.Ink, null, eye, 6, 6);
        DiagramText.Draw(context, DiagramText.Get("Light_Angle_Eye"), new Point(eye.X, eye.Y + 12), 12, DiagramText.Faint, 0.5, 0);

        // A circle round the eye; the arc between the lines of sight is the angle, its length over the radius is radians.
        var radius = Math.Min(70, Math.Max(40, (carX - eye.X) * 0.6));
        context.DrawEllipse(null, CirclePen, eye, radius, radius);
        var arc = new StreamGeometry();
        using (var g = arc.Open())
        {
            g.BeginFigure(new Point(eye.X + (radius * Math.Cos(radians / 2)), eye.Y - (radius * Math.Sin(radians / 2))), false);
            g.ArcTo(new Point(eye.X + (radius * Math.Cos(radians / 2)), eye.Y + (radius * Math.Sin(radians / 2))),
                new Size(radius, radius), 0, false, SweepDirection.Clockwise);
            g.EndFigure(false);
        }

        context.DrawGeometry(null, ArcPen, arc);
        DiagramText.Draw(context, DiagramText.Get("Light_Angle_Arc"), new Point(eye.X + radius + 6, eye.Y - radius - 4), 11.5, DiagramText.Gold, 0, 1);
        DiagramText.Draw(context, DiagramText.Format("Light_Metres", Distance.ToString("0.0")),
            new Point((eye.X + carX) / 2, eye.Y + (half * 1.6) + 6), 12, DiagramText.Faint, 0.5, 0);

        DrawView(context, bounds, radians);
    }

    /// <summary>What the eye sees: a 60° view with the car as wide as its angle.</summary>
    private static void DrawView(DrawingContext context, Rect bounds, double radians)
    {
        const double fieldOfView = Math.PI / 3;
        var panel = new Rect(bounds.Right - Math.Min(260, bounds.Width * 0.55) - 12, bounds.Y + 12, Math.Min(260, bounds.Width * 0.55), 120);
        context.FillRectangle(PanelBrush, panel, 8);
        DiagramText.Draw(context, DiagramText.Get("Light_Angle_View"), new Point(panel.X + 10, panel.Y + 8), 11.5, DiagramText.Faint);
        var horizon = panel.Y + 78;
        context.DrawLine(new Pen(DiagramText.Faint, 1), new Point(panel.X + 10, horizon), new Point(panel.Right - 10, horizon));
        var width = Math.Min(panel.Width - 20, (panel.Width - 20) * radians / fieldOfView);
        var height = width * 0.38;
        context.FillRectangle(CarBrush, new Rect(panel.Center.X - (width / 2), horizon - height, width, height), (float)Math.Min(6, height / 3));
        DiagramText.Draw(context,
            DiagramText.Format("Light_Angle_Value", (radians * 180 / Math.PI).ToString("0.0"), radians.ToString("0.000")),
            new Point(panel.Center.X, horizon + 10), 12, DiagramText.Ink, 0.5, 0);
    }
}
