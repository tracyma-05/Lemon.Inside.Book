using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// Spread 7, from the side: an eye looks at a glowing panel through a narrow cone. Step back and the cone takes in
/// more of the panel, but each bit of it is further away; tip the panel and the cone takes in more of it, but each
/// bit sends less light that way. The two always cancel, so the panel looks just as bright. Bits of light stream from
/// the seen part to the eye. The reader drags the eye back and forth and turns the panel by its top end; both glide
/// to a new place rather than jump.
/// </summary>
public sealed class RadianceDiagram : GearPicture
{
    public static readonly StyledProperty<double> DistanceProperty =
        AvaloniaProperty.Register<RadianceDiagram, double>(nameof(Distance), 2, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> TiltProperty =
        AvaloniaProperty.Register<RadianceDiagram, double>(nameof(Tilt), 0, defaultBindingMode: BindingMode.TwoWay);

    public const double MaxDistance = 6;

    /// <summary>The sliders' other ends: the nearest the eye comes, metres; the furthest the panel turns, degrees.</summary>
    private const double MinDistance = 1;
    private const double MostTilt = 80;

    /// <summary>Half the eye's cone, in degrees (much wider than a real eye's, to be seen).</summary>
    private const double ConeHalfAngle = 5;

    private static readonly IImmutableBrush Flat = new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x16, 0x22));
    private static readonly IImmutableBrush BitBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0xE8, 0x9A));

    /// <summary>The eye's distance and the panel's tilt drawn, easing toward the real ones (NaN until the first frame).</summary>
    private double _shownDistance = double.NaN;
    private double _shownTilt = double.NaN;
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

    protected override void Advance(double seconds)
    {
        _shownDistance = Gripping ? Distance : LightGrips.Ease(_shownDistance, Distance, seconds, 0.12);
        _shownTilt = Gripping ? Tilt : LightGrips.Ease(_shownTilt, Tilt, seconds, 0.12);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        context.FillRectangle(Flat, bounds);
        if (bounds.Width < 80 || bounds.Height < 80)
        {
            return;
        }

        if (double.IsNaN(_shownDistance)) (_shownDistance, _shownTilt) = (Distance, Tilt);
        var perMetre = (bounds.Width - 110) / MaxDistance;
        var centre = new Point(bounds.Right - 50, bounds.Height * 0.58);
        var eye = new Point(centre.X - (_shownDistance * perMetre), centre.Y);
        var tilt = Math.Clamp(_shownTilt, 0, MostTilt) * Math.PI / 180;

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

        // Light leaving a few bits of the seen part towards the eye, and bits of it on their way there.
        for (var i = 0; i < 5; i++)
        {
            var t = (i + 0.5) / 5;
            var from = new Point(top.X + ((bottom.X - top.X) * t), top.Y + ((bottom.Y - top.Y) * t));
            context.DrawLine(RayPen, from, eye);
            for (var k = 0; k < 3; k++)
            {
                var travelled = ((Seconds * 0.45) + (k / 3.0) + (i * 0.21)) % 1;
                context.DrawEllipse(BitBrush, null, new Point(from.X + ((eye.X - from.X) * travelled), from.Y + ((eye.Y - from.Y) * travelled)), 2, 2);
            }
        }

        context.DrawEllipse(DiagramText.Ink, null, eye, 6, 6);
        Mark("eye", eye, 10);
        Mark("panel", [centre - (along * half), centre + (along * half)], pad: 4);
        Mark("seen", [top, bottom], pad: 4);
        DiagramText.Draw(context, DiagramText.Get("Light_Angle_Eye"), new Point(eye.X, eye.Y + 18), 12, DiagramText.Faint, 0.5, 0);
        DiagramText.Draw(context, DiagramText.Get("Light_Radiance_Panel"), new Point(centre.X, centre.Y + half + 10), 12, DiagramText.Faint, 0.5, 0);

        DrawReadout(context, bounds);

        // Walk the eye nearer or further; turn the panel about its middle by its top end.
        Grip("eye", eye, GripAxis.Horizontal, DiagramText.Get("Light_Grip_Eye"), at =>
            Distance = LightGrips.Snap((centre.X - at.X) / perMetre, MinDistance, MaxDistance, 0.1));
        Grip("panel", centre - (along * half), GripAxis.Free, DiagramText.Get("Light_Grip_Panel"), at =>
            Tilt = LightGrips.Snap(Math.Atan2(centre.X - at.X, centre.Y - at.Y) * 180 / Math.PI, 0, MostTilt, 1));
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
