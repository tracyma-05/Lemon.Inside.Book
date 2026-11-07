using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Lifting;

/// <summary>
/// Spread 6: a lift shaft cut open. At the top the motor turns a grooved wheel; the ropes run over it, the car hanging
/// on one side and the counterweight on the other (shown as an outline when it is taken away). Along the bottom, a bar
/// for what the motor must hold, against the whole car's weight.
/// </summary>
public sealed class ElevatorDiagram : GearPicture
{
    public static readonly StyledProperty<int> RidersProperty =
        AvaloniaProperty.Register<ElevatorDiagram, int>(nameof(Riders));

    public static readonly StyledProperty<bool> CounterweightProperty =
        AvaloniaProperty.Register<ElevatorDiagram, bool>(nameof(Counterweight));

    private static readonly IPen Ghost = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0xE6, 0xEA, 0xF2)), 1.4, new ImmutableDashStyle([4, 3], 0));

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

    protected override bool Animates => false;

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

        // The car on the left rope, the counterweight on the right.
        var left = wheelCentre.X - r;
        var right = wheelCentre.X + r;
        var carRight = Math.Min(left + (w * 0.12), right - (w * 0.045) - 8);
        var car = new Rect(carRight - (w * 0.24), shaft.Y + (h * 0.22), w * 0.24, h * 0.24);
        var weight = new Rect(right - (w * 0.045), shaft.Y + (h * 0.08), w * 0.09, h * 0.2);
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
        var motorLoad = Elevator.Motor(riders, Counterweight);
        context.FillRectangle(motorLoad > 0 ? DiagramText.Gold : LiftingParts.Push, new Rect(bar.X, bar.Y, Math.Max(4, bar.Width * motorLoad / most), bar.Height), 5);
        DiagramText.Draw(context, DiagramText.Format("Lifting_Elevator_Bar", Kg(motorLoad)), new Point(bar.X, bar.Y - 4), 12.5, DiagramText.Ink, 0, 1, bold: true);
    }
}
