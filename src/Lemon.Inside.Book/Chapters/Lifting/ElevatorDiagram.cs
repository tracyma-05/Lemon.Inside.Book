using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Lifting;

/// <summary>
/// Spread 6: a lift shaft cut open. At the top the motor turns a grooved wheel; the ropes run over it, the car hanging
/// on one side and the counterweight on the other (shown as an outline when it is taken away). Along the bottom, a bar
/// for what the motor must hold, against the whole car's weight. The lift runs up and down the shaft: as the car
/// rises the counterweight comes down by as much, and the wheel turns with the ropes. The reader drags along the car's
/// roof to let riders in or out and taps the counterweight to hang it on or take it off; the motor's bar eases to its
/// new load.
/// </summary>
public sealed class ElevatorDiagram : GearPicture
{
    public static readonly StyledProperty<int> RidersProperty =
        AvaloniaProperty.Register<ElevatorDiagram, int>(nameof(Riders), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> CounterweightProperty =
        AvaloniaProperty.Register<ElevatorDiagram, bool>(nameof(Counterweight), defaultBindingMode: BindingMode.TwoWay);

    private static readonly IPen Ghost = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0xE6, 0xEA, 0xF2)), 1.4, new ImmutableDashStyle([4, 3], 0));

    /// <summary>One trip down and back up the shaft, s.</summary>
    private const double Trip = 14;

    /// <summary>The motor's load as drawn on the bar, kg, easing toward the true one.</summary>
    private double _bar = double.NaN;

    static ElevatorDiagram()
    {
        AffectsRender<ElevatorDiagram>(RidersProperty, CounterweightProperty);
    }

    public int Riders
    {
        get => GetValue(RidersProperty);
        set => SetValue(RidersProperty, value);
    }

    public bool Counterweight
    {
        get => GetValue(CounterweightProperty);
        set => SetValue(CounterweightProperty, value);
    }

    private double MotorLoad => Elevator.Motor(Math.Clamp(Riders, 0, Elevator.MostRiders), Counterweight);

    protected override void Advance(double seconds) =>
        _bar = double.IsNaN(_bar) ? MotorLoad : Glide(_bar, MotorLoad, seconds, 0.1);

    /// <summary>
    /// Where the car is on its trip, 0 (at the top) to 1 (at the bottom), <paramref name="seconds"/> in: it sets off
    /// gently, runs, and slows to stop at each end, waiting there a moment.
    /// </summary>
    internal static double Travel(double seconds)
    {
        var t = (seconds / Trip % 1 + 1) % 1;
        var leg = t < 0.5 ? t * 2 : (1 - t) * 2; // 0 → 1 → 0
        var moving = Math.Clamp((leg - 0.1) / 0.8, 0, 1); // a pause at each end
        return 0.5 - (0.5 * Math.Cos(Math.PI * moving));
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var riders = Math.Clamp(Riders, 0, Elevator.MostRiders);
        var w = bounds.Width;
        var h = bounds.Height;
        var cx = bounds.Center.X;
        var shaft = new Rect(cx - (w * 0.3), bounds.Y + (h * 0.2), w * 0.6, h * 0.62);
        context.DrawRectangle(Wall, null, shaft);
        context.DrawRectangle(DarkSteelBrush, Edge, new Rect(shaft.X - 6, bounds.Y + (h * 0.04), shaft.Width + 12, h * 0.16 - 4), 4, 4); // the machine room's floor and walls

        // The wheel and its motor.
        var r = Math.Min(w * 0.09, h * 0.075);
        var wheelCentre = new Point(cx, bounds.Y + (h * 0.12));
        var motor = new Rect(wheelCentre.X + r + 10, wheelCentre.Y - (r * 0.6), w * 0.12, r * 1.2);
        context.DrawRectangle(SteelBrush, Edge, motor, 4, 4);
        context.DrawLine(new ImmutablePen(DarkSteelBrush, 5), wheelCentre, new Point(motor.X, wheelCentre.Y));
        DiagramText.Draw(context, DiagramText.Get("Lifting_Elevator_Motor"), motor.Center, 11, Dark, 0.5, 0.5, bold: true);

        // The car on the left rope, the counterweight on the right, each going down as far as the other comes up.
        var travel = Travel(Seconds);
        var run = h * 0.28;
        var left = wheelCentre.X - r;
        var right = wheelCentre.X + r;
        var carRight = Math.Min(left + (w * 0.12), right - (w * 0.045) - 8);
        var car = new Rect(carRight - (w * 0.24), shaft.Y + (h * 0.06) + (run * travel), w * 0.24, h * 0.24);
        var weight = new Rect(right - (w * 0.045), shaft.Y + (h * 0.06) + (run * (1 - travel)), w * 0.09, h * 0.2);
        var ropes = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xE9, 0xD8, 0xA6)), 2);
        context.DrawLine(ropes, new Point(left, wheelCentre.Y), new Point(left, car.Top));
        context.DrawLine(ropes, new Point(left - 3, wheelCentre.Y), new Point(left - 3, car.Top));
        if (Counterweight)
        {
            context.DrawLine(ropes, new Point(right, wheelCentre.Y), new Point(right, weight.Top));
            context.DrawLine(ropes, new Point(right + 3, wheelCentre.Y), new Point(right + 3, weight.Top));
        }

        Mark("ropes", [new Point(left - 3, wheelCentre.Y), new Point(left, car.Top)], 6);
        DrawWheel(context, wheelCentre, r);
        context.DrawEllipse(null, ropes, wheelCentre, r * 0.98, r * 0.98);

        // Spokes on the wheel: it turns by the rope that has run over it (the car going down turns it anticlockwise).
        var turn = -run * travel / r;
        var spoke = new ImmutablePen(Dark, 2.2);
        for (var k = 0; k < 3; k++)
        {
            var (s, c) = Math.SinCos(turn + (k * Math.PI / 3));
            context.DrawLine(spoke, new Point(wheelCentre.X - (c * r * 0.6), wheelCentre.Y - (s * r * 0.6)), new Point(wheelCentre.X + (c * r * 0.6), wheelCentre.Y + (s * r * 0.6)));
        }

        Mark("sheave", wheelCentre, r);

        // The car with its riders standing in rows.
        context.DrawRectangle(new ImmutableSolidColorBrush(Color.FromRgb(0x5F, 0x7A, 0x9A)), Edge, car, 4, 4);
        context.DrawRectangle(Dark, null, car.Deflate(6), 2, 2);
        var perRow = 5;
        var person = car.Width / (perRow + 1);
        for (var i = 0; i < riders; i++)
        {
            var row = i / perRow;
            var col = i % perRow;
            var x = car.X + (person * (col + 1)) + (row % 2 * person / 2) - (row % 2 == 1 && col == perRow - 1 ? person : 0);
            var feet = car.Bottom - 8 - (row * person * 0.3);
            context.DrawRectangle(Skin, null, new Rect(x - (person * 0.18), feet - (person * 0.9), person * 0.36, person * 0.9), 3, 3);
            context.DrawEllipse(Skin, null, new Point(x, feet - (person * 1.08)), person * 0.17, person * 0.17);
        }

        Mark("car", car);
        DrawForce(context, new Point(car.Center.X, car.Bottom + 4), new Point(car.Center.X, car.Bottom + 4 + (h * 0.07)), Weight, Kg(Elevator.CarSide(riders)), -1);

        if (Counterweight)
        {
            DrawWeight(context, weight, "", Stone);
            DrawForce(context, new Point(weight.Center.X, weight.Bottom + 4), new Point(weight.Center.X, weight.Bottom + 4 + (h * 0.07)), Weight, Kg(Elevator.Counterweight));
        }
        else
        {
            context.DrawRectangle(null, Ghost, weight, 4, 4);
            DiagramText.Draw(context, DiagramText.Get("Lifting_Elevator_NoWeight"), new Point(weight.Center.X, weight.Bottom + 6), 11, DiagramText.Faint, 0.5);
        }

        Mark("weight", weight);

        // What the motor holds, against the full car with nothing to balance it.
        var most = Elevator.Motor(Elevator.MostRiders, false);
        var bar = new Rect(bounds.X + (w * 0.1), bounds.Bottom - 30, w * 0.8, 10);
        context.FillRectangle(new ImmutableSolidColorBrush(Color.FromArgb(0x30, 0xE6, 0xEA, 0xF2)), bar, 5);
        var motorLoad = MotorLoad;
        var shown = double.IsNaN(_bar) ? motorLoad : _bar;
        context.FillRectangle(motorLoad > 0 ? DiagramText.Gold : LiftingParts.Push, new Rect(bar.X, bar.Y, Math.Max(4, bar.Width * shown / most), bar.Height), 5);
        DiagramText.Draw(context, DiagramText.Format("Lifting_Elevator_Bar", Kg(motorLoad)), new Point(bar.X, bar.Y - 4), 12.5, DiagramText.Ink, 0, 1, bold: true);

        // The reader's handles: a slider along the car's roof (more riders to the right), and a tap on the
        // counterweight's place to hang it on or take it off.
        var roofY = car.Top + 3;
        var (roofLeft, roofRight) = (car.Left + 12, car.Right - 12);
        Grip("riders", new Point(roofLeft + ((roofRight - roofLeft) * riders / Elevator.MostRiders), roofY), GripAxis.Horizontal,
            DiagramText.Get("Lifting_Grip_Riders"),
            at => Riders = Math.Clamp((int)Math.Round((at.X - roofLeft) / (roofRight - roofLeft) * Elevator.MostRiders), 0, Elevator.MostRiders));
        Grip("counterweight", weight.Center, GripAxis.Tap, DiagramText.Get(Counterweight ? "Lifting_Grip_TakeWeight" : "Lifting_Grip_HangWeight"),
            _ => Counterweight = !Counterweight);
    }
}
