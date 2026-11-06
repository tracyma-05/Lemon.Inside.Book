using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Gears;

/// <summary>
/// Spread 2: a hand crank on a small gold gear turns a big gear with a drum on its axle; the drum winds up a rope and
/// lifts a weight. Below, bars compare the two axles' speed and turning force.
/// </summary>
public sealed class TorqueDiagram : GearPicture
{
    public static readonly StyledProperty<int> DrivenTeethProperty =
        AvaloniaProperty.Register<TorqueDiagram, int>(nameof(DrivenTeeth), 36);

    /// <summary>Teeth on the crank's gear.</summary>
    public const int CrankTeeth = 12;

    /// <summary>Turns of the crank per second.</summary>
    private const double CrankSpeed = 0.35;

    private static readonly double Pressure = 20 * Math.PI / 180;
    private static readonly IPen RopePen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xB8, 0x96)), 2);
    private static readonly IPen HandlePen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xB0, 0x95, 0x3A)), 5, lineCap: PenLineCap.Round);
    private static readonly IImmutableBrush Drum = new ImmutableSolidColorBrush(Color.FromRgb(0x6A, 0x55, 0x40));
    private static readonly IImmutableBrush Weight = new ImmutableSolidColorBrush(Color.FromRgb(0x4A, 0x52, 0x62));
    private static readonly IImmutableBrush Track = new ImmutableSolidColorBrush(Color.FromArgb(0x30, 0xE6, 0xEA, 0xF2));

    private double _turns;

    static TorqueDiagram()
    {
        AffectsRender<TorqueDiagram>(DrivenTeethProperty);
    }

    public int DrivenTeeth
    {
        get => GetValue(DrivenTeethProperty);
        set => SetValue(DrivenTeethProperty, value);
    }

    protected override void Advance(double seconds) => _turns += CrankSpeed * seconds;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var z2 = DrivenTeeth;
        double r1 = GearGeometry.PitchRadius(CrankTeeth), r2 = GearGeometry.PitchRadius(z2);
        var ratio = (double)z2 / CrankTeeth;
        var scale = Math.Min(bounds.Width * 0.8 / ((2 * r1) + (2 * r2) + 10), bounds.Height * 0.5 / ((2 * r2) + 2));
        var y = bounds.Height * 0.36;
        var c2 = new Point(bounds.Width * 0.6, y);
        var c1 = new Point(c2.X - ((r1 + r2) * scale), y);

        var a1 = -2 * Math.PI * _turns;
        var a2 = GearGeometry.MeshAngle(CrankTeeth, a1, z2, 0);
        DrawGear(context, z2, Pressure, c2, scale, a2, Steel);
        DrawGear(context, CrankTeeth, Pressure, c1, scale, a1, Gold, mark: false);

        // The drum on the big gear's axle, and the rope down its right side to the weight. The weight climbs while the
        // drum winds; it starts again from the floor when it reaches the top.
        var drum = r2 * scale * 0.42;
        context.DrawEllipse(Drum, new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x3A, 0x2E, 0x22)), 1.5), c2, drum, drum);
        var spoke = ToScreen(new Point(Math.Cos(a2) * drum / scale, Math.Sin(a2) * drum / scale), c2, scale);
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x3A, 0x2E, 0x22)), 2), c2, spoke);
        var floor = bounds.Height * 0.8;
        var box = Math.Max(16, Math.Min(40, scale * 4));
        var top = c2.Y + drum + 8;
        var travel = Math.Max(1, floor - box - top);
        var climbed = 2 * Math.PI * drum * (_turns / ratio) % travel;
        var boxTop = floor - box - climbed;
        var ropeX = c2.X + drum;
        context.DrawLine(RopePen, new Point(ropeX, c2.Y), new Point(ropeX, boxTop));
        context.FillRectangle(Weight, new Rect(ropeX - (box / 2), boxTop, box, box), 3);
        DiagramText.Draw(context, DiagramText.Get("Gears_Torque_Load"), new Point(ropeX + (box / 2) + 6, boxTop + (box / 2)), 12, DiagramText.Faint, 0, 0.5);

        // The crank's handle and the hand's push.
        var handle = ToScreen(new Point(Math.Cos(a1) * (r1 + 3), Math.Sin(a1) * (r1 + 3)), c1, scale);
        context.DrawLine(HandlePen, c1, handle);
        context.DrawEllipse(DiagramText.Gold, null, handle, 6, 6);
        DiagramText.Draw(context, DiagramText.Get("Gears_Torque_Hand"), new Point(c1.X, c1.Y - ((r1 + 5) * scale)), 12, DiagramText.Gold, 0.5, 1);

        DrawBars(context, new Rect(bounds.X + 18, floor + 10, bounds.Width - 36, bounds.Height - floor - 18), ratio);
    }

    /// <summary>Speed and turning force of the crank (gold) and the drum (grey), each as a share of the larger.</summary>
    private static void DrawBars(DrawingContext context, Rect area, double ratio)
    {
        if (area.Height < 24)
        {
            return;
        }

        var rows = new[]
        {
            (DiagramText.Get("Gears_Torque_Speed"), 1.0, 1 / ratio),
            (DiagramText.Get("Gears_Torque_Force"), 1 / ratio, 1.0),
        };
        var label = area.Width * 0.3;
        var bar = Math.Min(8, area.Height / 6);
        var key = DiagramText.Draw(context, DiagramText.Get("Gears_Torque_Drum"), new Point(area.Right, area.Y - 4), 11.5, DiagramText.Ink, 1, 1);
        DiagramText.Draw(context, DiagramText.Get("Gears_Torque_Crank"), new Point(key.X - 12, area.Y - 4), 11.5, DiagramText.Gold, 1, 1);
        for (var i = 0; i < rows.Length; i++)
        {
            var (name, crank, drum) = rows[i];
            var y = area.Y + (i * area.Height / 2);
            DiagramText.Draw(context, name, new Point(area.X, y + bar), 12, DiagramText.Ink, 0, 0.5);
            var x = area.X + label;
            var w = area.Width - label;
            context.FillRectangle(Track, new Rect(x, y, w, bar), 2);
            context.FillRectangle(DiagramText.Gold, new Rect(x, y, w * crank, bar), 2);
            context.FillRectangle(Track, new Rect(x, y + bar + 3, w, bar), 2);
            context.FillRectangle(new ImmutableSolidColorBrush(Steel), new Rect(x, y + bar + 3, w * drum, bar), 2);
        }
    }
}
