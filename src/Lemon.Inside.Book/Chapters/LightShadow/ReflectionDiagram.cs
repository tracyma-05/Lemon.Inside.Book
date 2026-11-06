using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// Spread 8, from the side: light falls on a surface and leaves it again. The shape over the surface shows how much
/// goes off in each direction: one ray for a mirror, a narrow beam for a glossy surface, a round puff for a matte
/// one. An eye looking from somewhere sees as much as the shape reaches towards it.
/// </summary>
public sealed class ReflectionDiagram : Control
{
    public static readonly StyledProperty<double> LightAngleProperty =
        AvaloniaProperty.Register<ReflectionDiagram, double>(nameof(LightAngle), 40);

    public static readonly StyledProperty<double> RoughnessProperty =
        AvaloniaProperty.Register<ReflectionDiagram, double>(nameof(Roughness), 0.3);

    public static readonly StyledProperty<double> EyeAngleProperty =
        AvaloniaProperty.Register<ReflectionDiagram, double>(nameof(EyeAngle), 40);

    private static readonly IImmutableBrush Plate = new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x16, 0x22));
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

    public ReflectionDiagram()
    {
        ClipToBounds = true;
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

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(Plate, bounds);
        if (bounds.Width < 80 || bounds.Height < 80)
        {
            return;
        }

        var hit = new Point(bounds.Center.X, bounds.Height * 0.72);
        var reach = Math.Min(bounds.Width * 0.42, bounds.Height * 0.6);
        context.FillRectangle(SurfaceBrush, new Rect(bounds.X + 20, hit.Y, bounds.Width - 40, 14), 3);
        context.DrawLine(NormalPen, hit, new Point(hit.X, hit.Y - reach));

        Point Towards(double degrees, double length) =>
            new(hit.X + (Math.Sin(degrees * Math.PI / 180) * length), hit.Y - (Math.Cos(degrees * Math.PI / 180) * length));

        // The light coming in from the left, and the way a mirror would send it on.
        var from = Towards(-LightAngle, reach);
        context.DrawLine(InPen, from, hit);
        DiagramText.DrawArrowAlong(context, from, hit, DiagramText.Gold);
        context.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xFF, 0xF4, 0xD0)), null, from, 8, 8);
        context.DrawLine(MirrorPen, hit, Towards(LightAngle, reach * 0.95));

        // The shape of the light leaving: its distance from the hit point in each direction is how much goes that way.
        var lobe = new StreamGeometry();
        using (var g = lobe.Open())
        {
            g.BeginFigure(hit, true);
            for (var a = -90.0; a <= 90; a += 0.5)
            {
                g.LineTo(Towards(a, reach * 0.8 * Leaving(a, LightAngle, Roughness)));
            }

            g.EndFigure(true);
        }

        context.DrawGeometry(LobeBrush, LobePen, lobe);

        // The eye and what it sees.
        var eye = Towards(EyeAngle, reach);
        context.DrawLine(EyePen, hit, eye);
        context.DrawEllipse(DiagramText.Ink, null, eye, 6, 6);
        DiagramText.Draw(context, DiagramText.Get("Light_Angle_Eye"), new Point(eye.X, eye.Y - 10), 12, DiagramText.Faint, 0.5, 1);

        var seen = Leaving(EyeAngle, LightAngle, Roughness);
        var panel = new Rect(bounds.X + 12, bounds.Y + 12, Math.Min(230, bounds.Width - 24), 64);
        context.FillRectangle(PanelBrush, panel, 8);
        var shade = (byte)(30 + (225 * Math.Sqrt(Math.Clamp(seen, 0, 1))));
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(shade, shade, (byte)(shade * 0.85))), new Rect(panel.X + 12, panel.Y + 12, 40, 40), 4);
        DiagramText.Draw(context, DiagramText.Get("Light_Angle_View"), new Point(panel.X + 64, panel.Y + 12), 11.5, DiagramText.Faint);
        DiagramText.Draw(context, DiagramText.Format("Light_Percent", (seen * 100).ToString("0")), new Point(panel.X + 64, panel.Y + 30), 15, DiagramText.Ink, bold: true);
    }
}
