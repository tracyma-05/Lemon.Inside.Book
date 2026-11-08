using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;
using static Lemon.Inside.Book.Chapters.Plumbing.PlumbingParts;

namespace Lemon.Inside.Book.Chapters.Plumbing;

/// <summary>
/// Spread 2: a screw-down tap cut in half, in millimetres. Water under pressure fills the inlet and the chamber below
/// the seat; the handle screws the spindle up, lifting the rubber washer off the seat, and water runs round it and out
/// of the spout. A gauge beside it shows the flow, which stops growing once the gap is as big as the hole. A worn
/// washer cannot close the seat, and the spout drips.
/// </summary>
public sealed class TapDiagram : GearPicture
{
    public static readonly StyledProperty<double> TurnsProperty =
        AvaloniaProperty.Register<TapDiagram, double>(nameof(Turns), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> WornProperty =
        AvaloniaProperty.Register<TapDiagram, bool>(nameof(Worn), defaultBindingMode: BindingMode.TwoWay);

    static TapDiagram()
    {
        AffectsRender<TapDiagram>(TurnsProperty, WornProperty);
    }

    public double Turns
    {
        get => GetValue(TurnsProperty);
        set => SetValue(TurnsProperty, value);
    }

    public bool Worn
    {
        get => GetValue(WornProperty);
        set => SetValue(WornProperty, value);
    }

    /// <summary>The turns as drawn: the handle winds round to a new setting rather than jumping (a grip's drag turns it at once).</summary>
    private double _shown = double.NaN;

    private double Shown => double.IsNaN(_shown) ? _shown = Math.Clamp(Turns, 0, 3) : _shown;

    protected override void Advance(double seconds) => _shown = Glide(Shown, Math.Clamp(Turns, 0, 3), seconds, 0.2);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var turns = Shown;
        var lift = Tap.Lift(turns);
        var opening = Tap.Opening(turns);
        var flow = Tap.Flow(turns, Worn);
        var scale = Math.Min((bounds.Width - 40) / 128, (bounds.Height - 70) / 108);
        var v = new View(new Point(bounds.Center.X - (2 * scale), bounds.Center.Y + (6 * scale)), scale);
        Rect Box(double x0, double y0, double x1, double y1) => v.Box(x0, y0, x1, y1);

        // The brass body round its passages, and the head the spindle screws through.
        (double, double, double, double)[] full = [(-58, -18, -10, -6), (-14, -20, 14, -3)];
        (double, double, double, double)[] after = [(-16, 0, 16, 24), (16, 6, 46, 16), (36, -38, 46, 16)];
        foreach (var (x0, y0, x1, y1) in full.Concat(after)) context.FillRectangle(Brass, Box(x0 - 4, y0 - 4, x1 + 4, y1 + 4));
        context.DrawRectangle(Brass, Edge, Box(-12, 24, 12, 41), 2, 2);
        context.DrawRectangle(Brass, Edge, Box(-62, -22, -54, -2));

        // Water under pressure below the seat; above it, water only while the tap is open.
        foreach (var (x0, y0, x1, y1) in full) context.FillRectangle(WaterBrush, Box(x0, y0, x1, y1));
        var running = opening > 0;
        context.FillRectangle(running || Worn ? WaterBrush : AirBrush, Box(-6, -3, 6, 0));
        foreach (var (x0, y0, x1, y1) in after) context.FillRectangle(running ? WaterBrush : AirBrush, Box(x0, y0, x1, y1));
        var seat = Box(-14, -3.5, 14, 0.5);
        Mark("seat", seat);
        var seatTag = v.At(-34, 8);
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 1), seatTag, v.At(-6.5, -1.5));
        DiagramText.Draw(context, DiagramText.Get("Plumbing_Tap_SeatLabel"), new Point(seatTag.X, seatTag.Y - 2), 11.5, DiagramText.Ink, 0.5, 1);

        if (running)
        {
            // Water moving along: in, up through the seat, round the washer, out and down the spout.
            var path = new[] { v.At(-56, -12), v.At(0, -12), v.At(0, -1.5), v.At(12, Math.Max(0.5, lift * 0.5)), v.At(12, 11), v.At(41, 11), v.At(41, -37) };
            DrawFlow(context, path, 3.2 * scale, Seconds * 60 * opening);
            var stream = Box(41 - (4.5 * opening), -38, 41 + (4.5 * opening), -38);
            context.FillRectangle(WaterBrush, new Rect(stream.X, stream.Y, Math.Max(2, stream.Width), bounds.Bottom - stream.Y - 30));
        }
        else if (Worn)
        {
            // A drop swells at the spout's lip, then falls.
            var t = Seconds % 1.3;
            var lip = v.At(41, -38);
            if (t < 0.9)
            {
                var r = (0.8 + (1.6 * t / 0.9)) * scale;
                context.DrawEllipse(WaterBrush, null, new Point(lip.X, lip.Y + r), r * 0.85, r);
            }
            else
            {
                var fallen = 0.5 * 9810 * Math.Pow(t - 0.9, 2) * scale * 0.08;
                context.DrawEllipse(WaterBrush, null, new Point(lip.X, lip.Y + (2.4 * scale) + fallen), 1.9 * scale, 2.3 * scale);
            }
        }

        // The spindle, its thread, the washer on its jumper plate, and the crosshead handle.
        var spindle = Box(-3, lift + 5, 3, lift + 50);
        context.DrawRectangle(SteelBrush, Edge, spindle);
        var threadPen = new ImmutablePen(DarkSteelBrush, 1);
        for (var y = 26 + (lift % Tap.Pitch); y < 39; y += Tap.Pitch)
        {
            context.DrawLine(threadPen, v.At(-3, y), v.At(3, y + (Tap.Pitch / 2)));
        }

        Mark("spindle", spindle);
        context.DrawRectangle(Brass, Edge, Box(-7, lift + 3, 7, lift + 5.5));
        var washer = Box(-9, lift, 9, lift + 3);
        context.DrawRectangle(Rubber, Edge, washer);
        if (Worn)
        {
            DrawPolygon(context, running ? WaterBrush : AirBrush, null, [v.At(-9.2, lift - 0.1), v.At(-3, lift - 0.1), v.At(-9.2, lift + 2)]);
        }

        Mark("washer", washer);
        var handle = Box(-24, lift + 50, 24, lift + 56);
        context.DrawRectangle(SteelBrush, Edge, handle, 3, 3);
        context.DrawRectangle(SteelBrush, Edge, Box(-6, lift + 56, 6, lift + 60), 2, 2);
        var angle = turns * 2 * Math.PI;
        context.DrawEllipse(Math.Cos(angle) >= 0 ? DiagramText.Gold : DiagramText.Faint, Edge, v.At(20 * Math.Sin(angle), lift + 53), 3, 3);
        Mark("handle", handle);

        DiagramText.Draw(context, DiagramText.Format("Plumbing_Tap_LiftLabel", Number(lift, "0.0")), v.At(-60, 34), 12.5, DiagramText.Gold, 0, 1, bold: true);

        // The gauge: litres a minute against what the tap gives fully open.
        var gauge = Box(52, -38, 58, 24);
        context.DrawRectangle(AirBrush, Edge, gauge, 2, 2);
        var share = Math.Clamp(flow / Tap.FullFlow, 0, 1);
        context.FillRectangle(WaterBrush, new Rect(gauge.X + 1, gauge.Bottom - (gauge.Height * share), gauge.Width - 2, gauge.Height * share));
        DiagramText.Draw(context, DiagramText.Format("Plumbing_LitresPerMinute", Number(flow, "0.#")), new Point(gauge.Center.X, gauge.Top - 6), 12.5,
            DiagramText.Ink, 0.5, 1, bold: true);

        DiagramText.Draw(context, DiagramText.Get("Plumbing_Tap_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);

        // The reader's handles: the crosshead turned by dragging along it (left shut, right three turns open), and the
        // washer tapped to wear it out or fit a new one.
        Grip("handle", v.At(-20 + (40 * turns / 3), lift + 53), GripAxis.Horizontal, DiagramText.Get("Plumbing_Grip_Handle"), at =>
        {
            Turns = Snap((v.X(at) + 20) / 40 * 3, 0.05, 0, 3);
            _shown = Turns;
        });
        Grip("washer", v.At(9, lift + 1.5), GripAxis.Tap, DiagramText.Get("Plumbing_Grip_Washer"), _ => Worn = !Worn);
    }
}
