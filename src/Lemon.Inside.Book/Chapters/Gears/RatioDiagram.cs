using Avalonia.Media.Immutable;
using Avalonia.Media;
using Avalonia;
using Avalonia.Data;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Gears;

/// <summary>
/// Spread 1: a gold gear turning a grey one, their rolling circles dashed, and how many turns each has made since the
/// teeth were last changed. A grip on top of each gear's rolling circle drags it bigger or smaller (more or fewer teeth).
/// </summary>
public sealed class RatioDiagram : GearPicture
{
    public static readonly StyledProperty<int> DriverTeethProperty =
        AvaloniaProperty.Register<RatioDiagram, int>(nameof(DriverTeeth), 12, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> DrivenTeethProperty =
        AvaloniaProperty.Register<RatioDiagram, int>(nameof(DrivenTeeth), 30, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Turns of the gold gear per second.</summary>
    public const double DriverSpeed = 0.2;

    private static readonly double Pressure = 20 * Math.PI / 180;
    private static readonly IImmutableBrush Circle = new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0xE6, 0xEA, 0xF2));

    private double _turns;

    static RatioDiagram()
    {
        AffectsRender<RatioDiagram>(DriverTeethProperty, DrivenTeethProperty);
    }

    public int DriverTeeth
    {
        get => GetValue(DriverTeethProperty);
        set => SetValue(DriverTeethProperty, value);
    }

    public int DrivenTeeth
    {
        get => GetValue(DrivenTeethProperty);
        set => SetValue(DrivenTeethProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DriverTeethProperty || change.Property == DrivenTeethProperty)
        {
            _turns = 0; // count again from here
        }
    }

    protected override void Advance(double seconds) => _turns += DriverSpeed * seconds;

    /// <summary>The slider ranges of the two gears' teeth.</summary>
    private const int FewestTeeth = 8, MostDriver = 30, MostDriven = 60;

    /// <summary>Pixels per module and the two axles for these teeth.</summary>
    private static (double Scale, Point C1, Point C2) Layout(Rect bounds, int z1, int z2)
    {
        double r1 = GearGeometry.PitchRadius(z1), r2 = GearGeometry.PitchRadius(z2);
        var width = r1 + r2 + r1 + r2 + 4;
        var scale = Math.Min(bounds.Width * 0.9 / width, bounds.Height * 0.66 / ((2 * Math.Max(r1, r2)) + 2));
        var y = bounds.Height * 0.44;
        var c1 = new Point(bounds.Center.X - ((r1 + r2 + r1 + r2) * scale / 2) + (r1 * scale), y);
        return (scale, c1, new Point(c1.X + ((r1 + r2) * scale), y));
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        int z1 = DriverTeeth, z2 = DrivenTeeth;
        double r1 = GearGeometry.PitchRadius(z1), r2 = GearGeometry.PitchRadius(z2);
        var (scale, c1, c2) = Layout(bounds, z1, z2);
        var y = c1.Y;

        // The gold gear turns clockwise on screen; the grey one follows the other way.
        var a1 = -2 * Math.PI * _turns;
        var a2 = GearGeometry.MeshAngle(z1, a1, z2, 0);
        DrawGear(context, z2, Pressure, c2, scale, a2, Steel);
        DrawGear(context, z1, Pressure, c1, scale, a1, Gold);
        DrawCircle(context, c1, r1 * scale, Circle);
        DrawCircle(context, c2, r2 * scale, Circle);
        Mark("driver", c1, (r1 + 1) * scale);
        Mark("driven", c2, (r2 + 1) * scale);

        // A grip on top of each rolling circle: drag it up for a bigger gear, down for a smaller one.
        Grip("driverTeeth", new Point(c1.X, y - (r1 * scale)), GripAxis.Vertical, DiagramText.Get("Gears_Grip_DriverTeeth"),
            at => DriverTeeth = GearsGrips.Nearest(FewestTeeth, MostDriver, y - at.Y,
                z => GearGeometry.PitchRadius(z) * Layout(bounds, z, DrivenTeeth).Scale));
        Grip("drivenTeeth", new Point(c2.X, y - (r2 * scale)), GripAxis.Vertical, DiagramText.Get("Gears_Grip_DrivenTeeth"),
            at => DrivenTeeth = GearsGrips.Nearest(FewestTeeth, MostDriven, y - at.Y,
                z => GearGeometry.PitchRadius(z) * Layout(bounds, DriverTeeth, z).Scale));

        var label = y + ((Math.Max(r1, r2) + 2.5) * scale);
        var text = Math.Max(12, Math.Min(15, bounds.Width / 40));
        DiagramText.Draw(context, DiagramText.Format("Gears_TeethCount", z1), new Point(c1.X, label), text, DiagramText.Gold, 0.5);
        DiagramText.Draw(context, DiagramText.Format("Gears_TeethCount", z2), new Point(c2.X, label), text, DiagramText.Ink, 0.5);
        DiagramText.Draw(context, DiagramText.Format("Gears_Turns", _turns.ToString("0.00")), new Point(c1.X, label + text + 6), text, DiagramText.Gold, 0.5);
        DiagramText.Draw(context, DiagramText.Format("Gears_Turns", (_turns * z1 / z2).ToString("0.00")), new Point(c2.X, label + text + 6), text, DiagramText.Ink, 0.5);
        DiagramText.Draw(context, DiagramText.Get("Gears_Ratio_Circles"), new Point(bounds.X + 16, bounds.Y + 14), 12, DiagramText.Faint);
    }
}
