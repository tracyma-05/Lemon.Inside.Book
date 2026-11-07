using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// Spread 7, from the side: an eye looks at a glowing panel through a narrow cone. Step back and the cone takes in
/// more of the panel, but each bit of it is further away; tip the panel and the cone takes in more of it, but each
/// bit sends less light that way. The two always cancel, so the panel looks just as bright.
/// </summary>
public sealed class RadianceDiagram : Picture
{
    public static readonly StyledProperty<double> DistanceProperty =
        AvaloniaProperty.Register<RadianceDiagram, double>(nameof(Distance), 2);

    public static readonly StyledProperty<double> TiltProperty =
        AvaloniaProperty.Register<RadianceDiagram, double>(nameof(Tilt), 0);

    public const double MaxDistance = 6;

    /// <summary>Half the eye's cone, in degrees (much wider than a real eye's, to be seen).</summary>
    private const double ConeHalfAngle = 5;

    private static readonly IImmutableBrush Plate = new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x16, 0x22));
    private static readonly IPen PanelPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0xD8, 0x7A)), 6, lineCap: PenLineCap.Round);
    private static readonly IPen SeenPen = new ImmutablePen(DiagramText.Cyan, 6, lineCap: PenLineCap.Flat);
    private static readonly IPen ConePen = new ImmutablePen(DiagramText.Cyan, 1.2);
    private static readonly IImmutableBrush ConeBrush = new ImmutableSolidColorBrush(Color.FromArgb(0x22, 0x6F, 0xD8, 0xFF));
    private static readonly IPen RayPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0xA0, 0xF2, 0xC9, 0x4C)), 1);
    private static readonly IImmutableBrush PanelBrush = new ImmutableSolidColorBrush(Color.FromArgb(0xC0, 0x05, 0x08, 0x0E));
    private static readonly IImmutableBrush GlowBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0xD8, 0x7A));

    static RadianceDiagram()
    {
        AffectsRender<RadianceDiagram>(DistanceProperty, TiltProperty);
    }

    public RadianceDiagram()
    {
        ClipToBounds = true;
    }

    /// <summary>How far the eye is from the panel, in metres.</summary>
    public double Distance
    {
        get => GetValue(DistanceProperty);
        set => SetValue(DistanceProperty, value);
    }

    /// <summary>How far the panel is turned away from facing the eye, in degrees.</summary>
    public double Tilt
    {
        get => GetValue(TiltProperty);
        set => SetValue(TiltProperty, value);
    }

    /// <summary>How much of the panel the cone takes in, against facing it from 1 m (an area: grows with d² / cos).</summary>
    public static double AreaSeen(double distance, double tiltDegrees) => distance * distance / Math.Cos(tiltDegrees * Math.PI / 180);

    /// <summary>How much of each bit's light reaches the eye, against facing it from 1 m (cos / d²).</summary>
    public static double LightPerBit(double distance, double tiltDegrees) => Math.Cos(tiltDegrees * Math.PI / 180) / (distance * distance);

    protected override void Paint(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(Plate, bounds);
        if (bounds.Width < 80 || bounds.Height < 80)
        {
            return;
        }

        var perMetre = (bounds.Width - 110) / MaxDistance;
        var centre = new Point(bounds.Right - 50, bounds.Height * 0.58);
        var eye = new Point(centre.X - (Distance * perMetre), centre.Y);
        var tilt = Math.Clamp(Tilt, 0, 80) * Math.PI / 180;

        // The panel: a line through its centre, turned by the tilt (screen y points down).
        var along = new Vector(Math.Sin(tilt), Math.Cos(tilt));
        var half = bounds.Height * 0.3;
        context.DrawLine(PanelPen, centre - (along * half), centre + (along * half));

        // The eye's cone, out to where it meets the panel; the part of the panel it takes in.
        var cone = ConeHalfAngle * Math.PI / 180;
        var top = Hit(eye, -cone, centre, along);
        var bottom = Hit(eye, cone, centre, along);
        var wedge = new StreamGeometry();
        using (var g = wedge.Open())
        {
            g.BeginFigure(eye, true);
            g.LineTo(top);
            g.LineTo(bottom);
            g.EndFigure(true);
        }

        context.DrawGeometry(ConeBrush, null, wedge);
        context.DrawLine(ConePen, eye, top);
        context.DrawLine(ConePen, eye, bottom);
        context.DrawLine(SeenPen, top, bottom);

        // Light leaving a few bits of the seen part towards the eye.
        for (var i = 0; i < 5; i++)
        {
            var t = (i + 0.5) / 5;
            var from = new Point(top.X + ((bottom.X - top.X) * t), top.Y + ((bottom.Y - top.Y) * t));
            context.DrawLine(RayPen, from, eye);
        }

        context.DrawEllipse(DiagramText.Ink, null, eye, 6, 6);
        Mark("eye", eye, 10);
        Mark("panel", [centre - (along * half), centre + (along * half)], pad: 4);
        Mark("seen", [top, bottom], pad: 4);
        DiagramText.Draw(context, DiagramText.Get("Light_Angle_Eye"), new Point(eye.X, eye.Y + 12), 12, DiagramText.Faint, 0.5, 0);
        DiagramText.Draw(context, DiagramText.Get("Light_Radiance_Panel"), new Point(centre.X, centre.Y + half + 10), 12, DiagramText.Faint, 0.5, 0);

        DrawReadout(context, bounds);
    }

    private void DrawReadout(DrawingContext context, Rect bounds)
    {
        var panel = new Rect(bounds.X + 12, bounds.Y + 12, Math.Min(330, bounds.Width - 24), 96);
        context.FillRectangle(PanelBrush, panel, 8);
        Mark("readout", panel.Deflate(8));
        var area = AreaSeen(Distance, Tilt);
        var perBit = LightPerBit(Distance, Tilt);
        DiagramText.Draw(context, DiagramText.Format("Light_Radiance_Area", area.ToString("0.00")), new Point(panel.X + 12, panel.Y + 10), 12.5, DiagramText.Cyan);
        DiagramText.Draw(context, DiagramText.Format("Light_Radiance_PerBit", perBit.ToString("0.000")), new Point(panel.X + 12, panel.Y + 32), 12.5, DiagramText.Gold);
        DiagramText.Draw(context, DiagramText.Format("Light_Radiance_Total", (area * perBit).ToString("0.00")), new Point(panel.X + 12, panel.Y + 58), 13, DiagramText.Ink, bold: true);
        context.DrawEllipse(GlowBrush, null, new Point(panel.Right - 30, panel.Y + 66), 14, 14);
    }

    /// <summary>Where the line from <paramref name="from"/> at <paramref name="angle"/> (radians, 0 = right) meets the panel.</summary>
    private static Point Hit(Point from, double angle, Point centre, Vector along)
    {
        var dir = new Vector(Math.Cos(angle), Math.Sin(angle));
        // from + s·dir = centre + t·along, solved for s.
        var det = (dir.X * -along.Y) - (dir.Y * -along.X);
        var dx = centre.X - from.X;
        var dy = centre.Y - from.Y;
        var s = ((dx * -along.Y) - (dy * -along.X)) / det;
        return from + (dir * s);
    }
}
