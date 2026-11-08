using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// Spread 8, from the side: light falls on a surface and leaves it again. The shape over the surface shows how much
/// goes off in each direction: one ray for a mirror, a narrow beam for a glossy surface, a round puff for a matte
/// one. An eye looking from somewhere sees as much as the shape reaches towards it. Bits of light stream in from the
/// lamp and out again, brighter the way more of it goes. The reader drags the lamp and the eye round the hit point and
/// slides the grip under the surface from mirror to matte (the surface's top grows bumpier as it goes).
/// </summary>
public sealed class ReflectionDiagram : GearPicture
{
    public static readonly StyledProperty<double> LightAngleProperty =
        AvaloniaProperty.Register<ReflectionDiagram, double>(nameof(LightAngle), 40, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> RoughnessProperty =
        AvaloniaProperty.Register<ReflectionDiagram, double>(nameof(Roughness), 0.3, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> EyeAngleProperty =
        AvaloniaProperty.Register<ReflectionDiagram, double>(nameof(EyeAngle), 40, defaultBindingMode: BindingMode.TwoWay);

    private static readonly IImmutableBrush Flat = new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x16, 0x22));
    private static readonly IImmutableBrush BitBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0xE8, 0x9A));
    private static readonly IPen TrackPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x2A, 0x33, 0x45)), 6, lineCap: PenLineCap.Round);
    private static readonly IPen TrackFill = new ImmutablePen(DiagramText.Gold, 4, lineCap: PenLineCap.Round);
    private static readonly IPen BumpPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x8A, 0x93, 0xA5)), 1.5);

    /// <summary>The sliders' ranges, in degrees.</summary>
    private const double MostLight = 80;
    private const double MostEye = 80;

    /// <summary>The eye sits a little nearer the hit point than the lamp, so the two never cover each other.</summary>
    private const double EyeReach = 0.85;

    /// <summary>The angles and roughness drawn, easing toward the real ones (NaN until the first frame).</summary>
    private double _shownLight = double.NaN;
    private double _shownRough = double.NaN;
    private double _shownEye = double.NaN;
    private static readonly IImmutableBrush SurfaceBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x5A, 0x63, 0x75));
    private static readonly IPen InPen = new ImmutablePen(DiagramText.Gold, 2.5);
    private static readonly IPen MirrorPen = new ImmutablePen(DiagramText.Gold, 2.5, new ImmutableDashStyle([6, 4], 0));
    private static readonly IImmutableBrush LobeBrush = new ImmutableSolidColorBrush(Color.FromArgb(0x50, 0xF2, 0xC9, 0x4C));
    private static readonly IPen LobePen = new ImmutablePen(DiagramText.Gold, 1.5);
    private static readonly IPen EyePen = new ImmutablePen(DiagramText.Cyan, 1.5);
    private static readonly IPen NormalPen = new ImmutablePen(DiagramText.Faint, 1, new ImmutableDashStyle([3, 4], 0));
    private static readonly IImmutableBrush PanelBrush = new ImmutableSolidColorBrush(Color.FromArgb(0xC0, 0x05, 0x08, 0x0E));

    static ReflectionDiagram()
    {
        AffectsRender<ReflectionDiagram>(LightAngleProperty, RoughnessProperty, EyeAngleProperty);
    }

    /// <summary>Where the light comes from, in degrees from straight up (on the left).</summary>
    public double LightAngle
    {
        get => GetValue(LightAngleProperty);
        set => SetValue(LightAngleProperty, value);
    }

    /// <summary>0 is a perfect mirror, 1 a matte surface.</summary>
    public double Roughness
    {
        get => GetValue(RoughnessProperty);
        set => SetValue(RoughnessProperty, value);
    }

    /// <summary>Where the eye looks from, in degrees from straight up (positive on the right, with the mirror ray).</summary>
    public double EyeAngle
    {
        get => GetValue(EyeAngleProperty);
        set => SetValue(EyeAngleProperty, value);
    }

    /// <summary>
    /// How much light leaves towards <paramref name="angle"/> (degrees from straight up, positive on the right): a
    /// beam round the mirror direction, narrower the smoother the surface, mixed with a matte cos puff. 1 at most.
    /// </summary>
    public static double Leaving(double angle, double lightAngle, double roughness)
    {
        var m = Math.Clamp(roughness, 0, 1);
        var off = (angle - lightAngle) * Math.PI / 180;
        var sharpness = 2 + (600 * Math.Pow(1 - m, 3));
        var beam = Math.Abs(off) < Math.PI / 2 ? Math.Pow(Math.Cos(off), sharpness) : 0;
        var matte = Math.Max(0, Math.Cos(angle * Math.PI / 180));
        return ((1 - m) * beam) + (m * matte);
    }

    protected override void Advance(double seconds)
    {
        _shownLight = Gripping ? LightAngle : LightGrips.Ease(_shownLight, LightAngle, seconds, 0.12);
        _shownRough = Gripping ? Roughness : LightGrips.Ease(_shownRough, Roughness, seconds, 0.12);
        _shownEye = Gripping ? EyeAngle : LightGrips.Ease(_shownEye, EyeAngle, seconds, 0.12);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        context.FillRectangle(Flat, bounds);
        if (bounds.Width < 80 || bounds.Height < 80)
        {
            return;
        }

        if (double.IsNaN(_shownLight)) (_shownLight, _shownRough, _shownEye) = (LightAngle, Roughness, EyeAngle);
        var (lightAngle, roughness, eyeAngle) = (_shownLight, Math.Clamp(_shownRough, 0, 1), _shownEye);
        var hit = new Point(bounds.Center.X, bounds.Height * 0.72);
        var reach = Math.Min(bounds.Width * 0.42, bounds.Height * 0.6);
        context.FillRectangle(SurfaceBrush, new Rect(bounds.X + 20, hit.Y, bounds.Width - 40, 14), 3);
        DrawBumps(context, bounds.X + 22, bounds.Right - 22, hit.Y, roughness);
        context.DrawLine(NormalPen, hit, new Point(hit.X, hit.Y - reach));

        Point Towards(double degrees, double length) =>
            new(hit.X + (Math.Sin(degrees * Math.PI / 180) * length), hit.Y - (Math.Cos(degrees * Math.PI / 180) * length));

        // The light coming in from the left, and the way a mirror would send it on.
        var from = Towards(-lightAngle, reach);
        context.DrawLine(InPen, from, hit);
        DiagramText.DrawArrowAlong(context, from, hit, DiagramText.Gold);
        context.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xFF, 0xF4, 0xD0)), null, from, 8, 8);
        context.DrawLine(MirrorPen, hit, Towards(lightAngle, reach * 0.95));

        // The shape of the light leaving: its distance from the hit point in each direction is how much goes that way.
        var lobe = new StreamGeometry();
        using (var g = lobe.Open())
        {
            g.BeginFigure(hit, true);
            for (var a = -90.0; a <= 90; a += 0.5)
            {
                g.LineTo(Towards(a, reach * 0.8 * Leaving(a, lightAngle, roughness)));
            }

            g.EndFigure(true);
        }

        context.DrawGeometry(LobeBrush, LobePen, lobe);
        DrawStream(context, from, hit, Towards, reach, lightAngle, roughness);
        Mark("lamp", from, 12);
        Mark("mirror", [hit, Towards(lightAngle, reach * 0.95)], pad: 4);
        Mark("lobe", Enumerable.Range(0, 37).Select(i => -90.0 + (5 * i))
            .Select(a => Towards(a, reach * 0.8 * Leaving(a, lightAngle, roughness))).Append(hit), pad: 4);

        // The eye and what it sees.
        var eye = Towards(eyeAngle, reach * EyeReach);
        context.DrawLine(EyePen, hit, eye);
        context.DrawEllipse(DiagramText.Ink, null, eye, 6, 6);
        Mark("eye", eye, 10);
        DiagramText.Draw(context, DiagramText.Get("Light_Angle_Eye"), new Point(eye.X, eye.Y - 16), 12, DiagramText.Faint, 0.5, 1);

        var seen = Leaving(eyeAngle, lightAngle, roughness);
        var panel = new Rect(bounds.X + 12, bounds.Y + 12, Math.Min(230, bounds.Width - 24), 64);
        context.FillRectangle(PanelBrush, panel, 8);
        Mark("view", panel.Deflate(8));
        var shade = (byte)(30 + (225 * Math.Sqrt(Math.Clamp(seen, 0, 1))));
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(shade, shade, (byte)(shade * 0.85))), new Rect(panel.X + 12, panel.Y + 12, 40, 40), 4);
        DiagramText.Draw(context, DiagramText.Get("Light_Angle_View"), new Point(panel.X + 64, panel.Y + 12), 11.5, DiagramText.Faint);
        DiagramText.Draw(context, DiagramText.Format("Light_Percent", (seen * 100).ToString("0")), new Point(panel.X + 64, panel.Y + 30), 15, DiagramText.Ink, bold: true);

        // Under the surface, a track from mirror to matte.
        var trackWidth = Math.Min(240, bounds.Width * 0.5);
        var trackY = Math.Min(hit.Y + 46, bounds.Bottom - 40);
        var trackLeft = hit.X - (trackWidth / 2);
        var knob = new Point(trackLeft + (trackWidth * roughness), trackY);
        context.DrawLine(TrackPen, new Point(trackLeft, trackY), new Point(trackLeft + trackWidth, trackY));
        context.DrawLine(TrackFill, new Point(trackLeft, trackY), knob);
        DiagramText.Draw(context, DiagramText.Get("Light_Reflection_Roughness"), new Point(hit.X, trackY + 16), 11.5, DiagramText.Faint, 0.5, 0);

        // The grips: the lamp and the eye swing round the hit point; the knob slides from mirror to matte.
        Grip("lamp", from, GripAxis.Free, DiagramText.Get("Light_Grip_Lamp"), at =>
            LightAngle = LightGrips.Snap(-LightGrips.FromUp(hit, at), 0, MostLight, 1));
        Grip("eye", eye, GripAxis.Free, DiagramText.Get("Light_Grip_Look"), at =>
            EyeAngle = LightGrips.Snap(LightGrips.FromUp(hit, at), -MostEye, MostEye, 1));
        Grip("rough", knob, GripAxis.Horizontal, DiagramText.Get("Light_Grip_Roughness"), at =>
            Roughness = LightGrips.Snap((at.X - trackLeft) / trackWidth, 0, 1, 0.02));
    }

    /// <summary>
    /// Bits of light: coming in from the lamp, and leaving the hit point in every direction, each as bright as the
    /// share of the light that goes that way (so a mirror sends one bright stream, matte a dim spray).
    /// </summary>
    private void DrawStream(DrawingContext context, Point from, Point hit, Func<double, double, Point> towards, double reach,
        double lightAngle, double roughness)
    {
        const double Speed = 0.5;
        for (var k = 0; k < 3; k++)
        {
            var t = ((Seconds * Speed) + (k / 3.0)) % 1;
            context.DrawEllipse(BitBrush, null, new Point(from.X + ((hit.X - from.X) * t), from.Y + ((hit.Y - from.Y) * t)), 2.4, 2.4);
        }

        // Outgoing: the mirror direction and every 10 degrees either side of it, over the surface.
        for (var a = lightAngle - 170; a <= 88; a += 10)
        {
            if (a < -88) continue;

            var share = Leaving(a, lightAngle, roughness);
            if (share < 0.03) continue;

            var brush = new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0xE8, 0x9A), Math.Clamp(share, 0.15, 1));
            for (var k = 0; k < 3; k++)
            {
                var t = ((Seconds * Speed) + (k / 3.0) + 0.5) % 1;
                context.DrawEllipse(brush, null, towards(a, reach * 0.95 * t), 2.2, 2.2);
            }
        }
    }

    /// <summary>The surface's top, bumpier the rougher it is: light landing on it is scattered by those little slopes.</summary>
    private static void DrawBumps(DrawingContext context, double left, double right, double top, double roughness)
    {
        if (roughness < 0.02) return;

        var bumps = new StreamGeometry();
        using (var g = bumps.Open())
        {
            g.BeginFigure(new Point(left, top), false);
            var up = true;
            for (var x = left + 4; x <= right; x += 4)
            {
                g.LineTo(new Point(x, top - (up ? 4 * roughness : 0)));
                up = !up;
            }

            g.EndFigure(false);
        }

        context.DrawGeometry(null, BumpPen, bumps);
    }
}
