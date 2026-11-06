using Avalonia.Media.Immutable;
using Avalonia.Media;
using Avalonia;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Gears;

/// <summary>
/// Spread 4: two gears turning slowly, with their base circles, the line of action between them, and a gold dot
/// wherever a pair of teeth is touching. The dots slide along the line.
/// </summary>
public sealed class MeshDiagram : GearPicture
{
    public static readonly StyledProperty<double> PressureAngleProperty =
        AvaloniaProperty.Register<MeshDiagram, double>(nameof(PressureAngle), 20);

    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<MeshDiagram, double>(nameof(Speed), 0.5);

    public const int DriverTeeth = 16;
    public const int DrivenTeeth = 26;

    /// <summary>Turns per second of the gold gear at full speed.</summary>
    private const double FullSpeed = 0.12;

    private static readonly IImmutableBrush BaseCircle = new ImmutableSolidColorBrush(Color.FromArgb(0x80, 0x6F, 0xD8, 0xFF));
    private static readonly IPen LinePen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x70, 0x6F, 0xD8, 0xFF)), 1.5);
    private static readonly IPen PathPen = new ImmutablePen(DiagramText.Cyan, 3.5, lineCap: PenLineCap.Round);
    private static readonly IPen CentresPen = new ImmutablePen(DiagramText.Faint, 1, new ImmutableDashStyle([2, 4], 0));

    private double _angle;

    static MeshDiagram()
    {
        AffectsRender<MeshDiagram>(PressureAngleProperty);
    }

    /// <summary>The slant of the line of action from square to the line of centres, in degrees.</summary>
    public double PressureAngle
    {
        get => GetValue(PressureAngleProperty);
        set => SetValue(PressureAngleProperty, value);
    }

    /// <summary>How fast the gears turn, 0 (still) to 1.</summary>
    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    protected override void Advance(double seconds) => _angle += 2 * Math.PI * FullSpeed * Speed * seconds;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var pressure = Math.Round(Math.Clamp(PressureAngle, 10, 35) * 2) / 2 * Math.PI / 180; // half degrees: a few shapes to keep
        double r1 = GearGeometry.PitchRadius(DriverTeeth), r2 = GearGeometry.PitchRadius(DrivenTeeth);
        var scale = Math.Min(bounds.Width * 0.92 / ((2 * r1) + (2 * r2) + 4), bounds.Height * 0.78 / ((2 * r2) + 2));
        var y = bounds.Height * 0.47;
        var c1 = new Point(bounds.Center.X - ((r1 + r2 + r1 + r2) * scale / 2) + (r1 * scale), y);
        var c2 = new Point(c1.X + ((r1 + r2) * scale), y);

        // The gold gear turns counter-clockwise and drives.
        var a2 = GearGeometry.MeshAngle(DriverTeeth, _angle, DrivenTeeth, 0);
        DrawGear(context, DrivenTeeth, pressure, c2, scale, a2, Steel, 0.9);
        DrawGear(context, DriverTeeth, pressure, c1, scale, _angle, Gold, 0.9);
        DrawCircle(context, c1, GearGeometry.BaseRadius(DriverTeeth, pressure) * scale, BaseCircle);
        DrawCircle(context, c2, GearGeometry.BaseRadius(DrivenTeeth, pressure) * scale, BaseCircle);
        context.DrawLine(CentresPen, c1, c2);

        // The line of action from base circle to base circle, the stretch where teeth touch, and the touching pairs.
        var between = (r1 + r2) * Math.Sin(pressure);
        Point At(double along) => ToScreen(GearGeometry.OnLineOfAction(DriverTeeth, pressure, along), c1, scale);
        context.DrawLine(LinePen, At(0), At(between));
        var (contacts, from, to) = GearGeometry.Contacts(DriverTeeth, DrivenTeeth, pressure, _angle);
        context.DrawLine(PathPen, At(from), At(to));
        foreach (var s in contacts)
        {
            context.DrawEllipse(DiagramText.Gold, new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E)), 1.5), At(s), 6, 6);
        }

        var pitch = new Point(c1.X + (r1 * scale), y);
        context.DrawEllipse(Brushes.White, null, pitch, 3, 3);
        DiagramText.Draw(context, DiagramText.Get("Gears_Mesh_Pitch"), pitch + new Point(-8, 8), 12, DiagramText.Faint, 1, 0);
        var labelAt = At(between);
        DiagramText.Draw(context, DiagramText.Get("Gears_Mesh_Line"), labelAt + new Point(6, -4), 12, DiagramText.Cyan, 0, 1);
        DiagramText.Draw(context, DiagramText.Format("Gears_Degrees", PressureAngle.ToString("0.#")), new Point(bounds.X + 16, bounds.Y + 14), 13, DiagramText.Ink);
    }
}
