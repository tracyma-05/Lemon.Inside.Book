using Avalonia.Media.Immutable;
using Avalonia.Media;
using Avalonia;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Gears;

/// <summary>
/// Spread 1: a gold gear turning a grey one, their rolling circles dashed, and how many turns each has made since the
/// teeth were last changed.
/// </summary>
public sealed class RatioDiagram : GearPicture
{
    public static readonly StyledProperty<int> DriverTeethProperty =
        AvaloniaProperty.Register<RatioDiagram, int>(nameof(DriverTeeth), 12);

    public static readonly StyledProperty<int> DrivenTeethProperty =
        AvaloniaProperty.Register<RatioDiagram, int>(nameof(DrivenTeeth), 30);

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

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        int z1 = DriverTeeth, z2 = DrivenTeeth;
        double r1 = GearGeometry.PitchRadius(z1), r2 = GearGeometry.PitchRadius(z2);
        var width = r1 + r2 + r1 + r2 + 4;
        var scale = Math.Min(bounds.Width * 0.9 / width, bounds.Height * 0.66 / ((2 * Math.Max(r1, r2)) + 2));
        var y = bounds.Height * 0.44;
        var c1 = new Point(bounds.Center.X - ((r1 + r2 + r1 + r2) * scale / 2) + (r1 * scale), y);
        var c2 = new Point(c1.X + ((r1 + r2) * scale), y);

        // The gold gear turns clockwise on screen; the grey one follows the other way.
        var a1 = -2 * Math.PI * _turns;
        var a2 = GearGeometry.MeshAngle(z1, a1, z2, 0);
        DrawGear(context, z2, Pressure, c2, scale, a2, Steel);
        DrawGear(context, z1, Pressure, c1, scale, a1, Gold);
        DrawCircle(context, c1, r1 * scale, Circle);
        DrawCircle(context, c2, r2 * scale, Circle);

        var label = y + ((Math.Max(r1, r2) + 2.5) * scale);
        var text = Math.Max(12, Math.Min(15, bounds.Width / 40));
        DiagramText.Draw(context, DiagramText.Format("Gears_TeethCount", z1), new Point(c1.X, label), text, DiagramText.Gold, 0.5);
        DiagramText.Draw(context, DiagramText.Format("Gears_TeethCount", z2), new Point(c2.X, label), text, DiagramText.Ink, 0.5);
        DiagramText.Draw(context, DiagramText.Format("Gears_Turns", _turns.ToString("0.00")), new Point(c1.X, label + text + 6), text, DiagramText.Gold, 0.5);
        DiagramText.Draw(context, DiagramText.Format("Gears_Turns", (_turns * z1 / z2).ToString("0.00")), new Point(c2.X, label + text + 6), text, DiagramText.Ink, 0.5);
        DiagramText.Draw(context, DiagramText.Get("Gears_Ratio_Circles"), new Point(bounds.X + 16, bounds.Y + 14), 12, DiagramText.Faint);
    }
}
