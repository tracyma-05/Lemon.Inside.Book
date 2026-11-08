using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;
using static Lemon.Inside.Book.Chapters.Weighing.WeighingParts;
using Bounce = Lemon.Inside.Book.Chapters.Bridges.Bounce;

namespace Lemon.Inside.Book.Chapters.Weighing;

/// <summary>
/// Spread 5: a load cell. Above, a metal bar clamped at one end with the load hanging from the other, its bending
/// drawn far bigger than it is; a gauge on its top is stretched (and with four gauges, two more underneath are
/// squeezed). Below, the gauges wired as a bridge between the 5 V supply and the meter, and the display it drives.
/// One gauge warmed reads too much; four warmed alike cancel. Change the load and the bar springs to its new bend and
/// rings down, the display's digits following it until they settle, as a real scale's do before they hold still. The
/// reader drags the load's rod down for more, taps beside the gauge to warm it, and taps the bridge to wire in one
/// gauge or four.
/// </summary>
public sealed class StrainDiagram : GearPicture
{
    public static readonly StyledProperty<double> MassProperty =
        AvaloniaProperty.Register<StrainDiagram, double>(nameof(Mass), 2, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> WarmProperty =
        AvaloniaProperty.Register<StrainDiagram, bool>(nameof(Warm), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> BridgeProperty =
        AvaloniaProperty.Register<StrainDiagram, bool>(nameof(Bridge), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The load the bar is drawn bent by, kg, springing toward the load on it; null until the first frame.</summary>
    private Bounce? _bent;

    static StrainDiagram()
    {
        AffectsRender<StrainDiagram>(MassProperty, WarmProperty, BridgeProperty);
    }

    public double Mass
    {
        get => GetValue(MassProperty);
        set => SetValue(MassProperty, value);
    }

    public bool Warm
    {
        get => GetValue(WarmProperty);
        set => SetValue(WarmProperty, value);
    }

    public bool Bridge
    {
        get => GetValue(BridgeProperty);
        set => SetValue(BridgeProperty, value);
    }

    private double Load => Math.Clamp(Mass, 0, Gauge.FullLoad);

    /// <summary>A stiff steel bar rings quickly; the load cell's own damping settles it in a second or so.</summary>
    protected override void Advance(double seconds) =>
        _bent = (_bent ?? Bounce.At(Load)).Step(Load, seconds, 11, 0.12);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var mass = Load;
        var bent = (_bent ??= Bounce.At(Load)).Value;
        var bridge = Bridge;
        var warm = Warm;
        DrawBar(context, bounds, mass, bent, warm, bridge);
        DrawCircuit(context, bounds, Math.Max(0, bent), warm, bridge);
        DiagramText.Draw(context, DiagramText.Get("Weighing_Strain_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }

    /// <summary>The bar, its gauges and its load.</summary>
    private void DrawBar(DrawingContext context, Rect bounds, double mass, double bent, bool warm, bool bridge)
    {
        var clamp = new Rect(bounds.X + 40, bounds.Y + 70, 46, 150);
        context.DrawRectangle(Iron, Edge, clamp, 4, 4);
        var x0 = clamp.Right;
        var length = bounds.Width * 0.55;
        var y0 = clamp.Center.Y;
        const double thick = 28;
        var deflection = bent / Gauge.FullLoad * 46;
        double Down(double x)
        {
            var u = x / length;
            return deflection * u * u * (3 - u) / 2;
        }

        var top = new List<Point>();
        var bottom = new List<Point>();
        for (var i = 0; i <= 30; i++)
        {
            var x = length * i / 30;
            top.Add(new Point(x0 + x, y0 - (thick / 2) + Down(x)));
            bottom.Add(new Point(x0 + x, y0 + (thick / 2) + Down(x)));
        }

        DrawPolygon(context, SteelBrush, Edge, [.. top, .. Enumerable.Reverse(bottom)]);
        Mark("bar", [top[10], top[30], bottom[30]], 4);

        // The gauges, near the clamp where the bar bends most: stretched on top, squeezed below.
        var gauge = DrawGauge(context, x0, x0 + (length * 0.08), length * 0.2, y0 - (thick / 2), Down, -1, Stretched);
        if (bridge)
        {
            DrawGauge(context, x0, x0 + (length * 0.08), length * 0.2, y0 + (thick / 2), Down, 1, Squeezed);
            DiagramText.Draw(context, DiagramText.Get("Weighing_Strain_FourLabel"), new Point(gauge.Center.X, y0 + (thick / 2) + 26 + Down(length * 0.2)), 11,
                DiagramText.Faint, 0.5, 0);
        }

        Mark("gauge", gauge.Inflate(4));
        if (warm)
        {
            DiagramText.Draw(context, DiagramText.Get("Weighing_Strain_WarmLabel"), new Point(gauge.Center.X, gauge.Top - 6), 12, Warn, 0.5, 1, bold: true);
        }

        // The load hanging from the free end.
        var end = new Point(x0 + length - 10, y0 + (thick / 2) + Down(length - 10));
        var weightTop = new Point(end.X, end.Y + 30);
        context.DrawLine(new ImmutablePen(SteelBrush, 2), end, weightTop);
        if (mass > 0.01)
        {
            var size = 22 + (mass * 5);
            var box = new Rect(weightTop.X - (size / 2), weightTop.Y, size, size * 0.8);
            context.DrawRectangle(Iron, Edge, box, 3, 3);
            DiagramText.Draw(context, Kilos(mass, "0.#"), box.Center, 12, DiagramText.Ink, 0.5, 0.5, bold: true);
        }

        DiagramText.Draw(context, DiagramText.Get("Weighing_Strain_Drawn"), new Point(x0 + (length / 2), y0 - (thick / 2) - 30), 11, DiagramText.Faint, 0.5, 1);

        // The reader's handles: the load's rod, dragged down to hang more on (the rod settles where the pointer is), and
        // beside the gauge, tapped to warm it or let it cool.
        var u = (length - 10) / length;
        var perKg = 46 / Gauge.FullLoad * u * u * (3 - u) / 2;
        var rest = y0 + (thick / 2) + 13;
        Grip("mass", new Point(end.X, end.Y + 13), GripAxis.Vertical, DiagramText.Get("Weighing_Grip_StrainMass"),
            at => Mass = Math.Clamp(Math.Round((at.Y - rest) / perKg, 1), 0, Gauge.FullLoad));
        Grip("warm", new Point(gauge.Right + 16, y0 - (thick / 2) + 8 + Down(gauge.Right + 16 - x0)), GripAxis.Tap,
            DiagramText.Get(warm ? "Weighing_Grip_Cool" : "Weighing_Grip_Warm"), _ => Warm = !Warm);
    }

    /// <summary>A zigzag gauge lying on the bar's face at <paramref name="face"/>, bent with it. Returns its box.</summary>
    private static Rect DrawGauge(DrawingContext context, double barStart, double from, double width, double face, Func<double, double> down, int side, IBrush brush)
    {
        var points = new List<Point>();
        for (var i = 0; i <= 14; i++)
        {
            var x = from + (width * i / 14);
            points.Add(new Point(x, face + down(x - barStart) + ((i % 2 == 0 ? 0 : 7) * side)));
        }

        context.DrawGeometry(null, new ImmutablePen((IImmutableBrush)brush, 2.2, null, PenLineCap.Round, PenLineJoin.Round), Plumbing.PlumbingParts.Polyline(points));
        var ys = points.Select(p => p.Y).ToList();
        return new Rect(from, ys.Min(), width, ys.Max() - ys.Min());
    }

    /// <summary>The bridge, the meter and the display.</summary>
    private void DrawCircuit(DrawingContext context, Rect bounds, double mass, bool warm, bool bridge)
    {
        var centre = new Point(bounds.X + (bounds.Width * 0.32), bounds.Y + (bounds.Height * 0.66));
        var r = Math.Min(bounds.Width * 0.14, bounds.Height * 0.13);
        Point[] nodes = [new(centre.X, centre.Y - r), new(centre.X + r, centre.Y), new(centre.X, centre.Y + r), new(centre.X - r, centre.Y)];
        var wire = new ImmutablePen(DiagramText.Faint, 1.5);

        // Four arms: with one gauge, one is the gauge and three are fixed; with four, two stretched and two squeezed.
        for (var k = 0; k < 4; k++)
        {
            var a = nodes[k];
            var b = nodes[(k + 1) % 4];
            var brush = bridge ? (k % 2 == 0 ? Stretched : Squeezed) : k == 0 ? Stretched : DiagramText.Faint;
            var m1 = new Point(a.X + ((b.X - a.X) * 0.3), a.Y + ((b.Y - a.Y) * 0.3));
            var m2 = new Point(a.X + ((b.X - a.X) * 0.7), a.Y + ((b.Y - a.Y) * 0.7));
            context.DrawLine(wire, a, m1);
            context.DrawLine(wire, m2, b);
            var zig = new List<Point> { m1 };
            var (nx, ny) = (-(b.Y - a.Y) / (r * 1.414), (b.X - a.X) / (r * 1.414));
            for (var i = 1; i < 8; i++)
            {
                var t = 0.3 + (0.4 * i / 8);
                var s = i % 2 == 0 ? -5 : 5;
                zig.Add(new Point(a.X + ((b.X - a.X) * t) + (nx * s), a.Y + ((b.Y - a.Y) * t) + (ny * s)));
            }

            zig.Add(m2);
            context.DrawGeometry(null, new ImmutablePen((IImmutableBrush)brush, 2), Plumbing.PlumbingParts.Polyline(zig));
        }

        foreach (var n in nodes) context.DrawEllipse(DiagramText.Ink, null, n, 3, 3);
        DiagramText.Draw(context, DiagramText.Get("Weighing_Strain_Supply"), new Point(nodes[0].X, nodes[0].Y - 6), 11.5, DiagramText.Ink, 0.5, 1);
        DiagramText.Draw(context, "0 V", new Point(nodes[2].X, nodes[2].Y + 6), 11.5, DiagramText.Ink, 0.5, 0);
        Mark("bridge", new Rect(nodes[3].X, nodes[0].Y, 2 * r, 2 * r));
        Grip("bridge", new Point(nodes[3].X - 20, nodes[3].Y), GripAxis.Tap,
            DiagramText.Get(bridge ? "Weighing_Grip_OneGauge" : "Weighing_Grip_FourGauges"), _ => Bridge = !Bridge);

        // The meter across the middle, and the display it drives.
        var meterAt = new Point(centre.X, centre.Y);
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 1, new ImmutableDashStyle([3, 3], 0)), nodes[3], nodes[1]);
        var millivolts = Gauge.Output(mass, bridge) + (warm && !bridge ? Gauge.Supply * Gauge.Factor * Gauge.DriftPerDegree * Gauge.Warming * 1000 / 4 : 0);
        var tag = DiagramText.Draw(context, DiagramText.Format("Weighing_Strain_Millivolts", Number(millivolts, "0.00")), meterAt, 12, DiagramText.Gold, 0.5, 0.5, bold: true);
        context.DrawRectangle(null, new ImmutablePen(DiagramText.Gold, 1), tag.Inflate(4), 3, 3);

        var reading = Gauge.Reading(mass, warm, bridge);
        var wrong = Math.Abs(reading - mass) > 0.01;
        var screen = new Rect(bounds.Right - 30 - (bounds.Width * 0.3), centre.Y - 26, bounds.Width * 0.3, 52);
        context.DrawLine(wire, nodes[1], new Point(screen.Left - 4, centre.Y));
        Mark("display", DrawDisplay(context, screen, Kilos(reading, "0.00"), wrong));
        if (wrong)
        {
            DiagramText.Draw(context, DiagramText.Format("Weighing_Strain_ErrorLabel", Kilos(reading - mass, "0.00")), new Point(screen.Center.X, screen.Bottom + 12), 12, Warn, 0.5, 0, bold: true);
        }
    }
}
