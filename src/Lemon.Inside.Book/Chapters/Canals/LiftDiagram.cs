using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Canals.CanalsParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Canals;

/// <summary>
/// Spread 5: a boat lift drawn as two water-filled tanks hung from the ends of ropes over one big wheel, a motor at
/// its axle. A boat floating in the left tank leaves it no heavier, because it pushed its own weight of water out over
/// the gate; extra water in the left tank (drawn ten times deeper) is weight the motor must hold. The lift keeps
/// working, one tank going up as the other comes down and the wheel turning with them: evenly balanced it glides; with
/// extra water the motor strains and the lift crawls. The reader taps beside the left tank to float a boat in or out and
/// drags its water's surface for the extra.
/// </summary>
public sealed class LiftDiagram : GearPicture
{
    public static readonly StyledProperty<bool> BoatProperty =
        AvaloniaProperty.Register<LiftDiagram, bool>(nameof(Boat), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> ExtraProperty =
        AvaloniaProperty.Register<LiftDiagram, double>(nameof(Extra), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>How far through its up-and-down the lift is, radians: it moves on, slower the heavier one side.</summary>
    private double _travel;

    static LiftDiagram()
    {
        AffectsRender<LiftDiagram>(BoatProperty, ExtraProperty);
    }

    public bool Boat
    {
        get => GetValue(BoatProperty);
        set => SetValue(BoatProperty, value);
    }

    /// <summary>Extra water in the left tank, cm.</summary>
    public double Extra
    {
        get => GetValue(ExtraProperty);
        set => SetValue(ExtraProperty, value);
    }

    /// <summary>How fast the lift works, 1 when balanced, slower as the motor has more to hold.</summary>
    internal static double Pace(double imbalance) => 1 / (1 + (imbalance / 6));

    protected override void Advance(double seconds) =>
        _travel += seconds * 0.6 * Pace(BoatLift.Imbalance(Boat, Math.Clamp(Extra, 0, 20)));

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var extra = Math.Clamp(Extra, 0, 20);
        var imbalance = BoatLift.Imbalance(Boat, extra);
        var radius = Math.Min(bounds.Width * 0.24, bounds.Height * 0.2);
        var wheel = new Point(bounds.Center.X, bounds.Y + 50 + radius);
        var tankWidth = radius * 1.1;
        var tankTop = Math.Max(bounds.Y + (bounds.Height * 0.5), wheel.Y + 50);
        var tankHeight = bounds.Height * 0.2;

        // The left tank goes down as far as the right one goes up; the wheel turns with the ropes.
        var shift = Math.Sin(_travel) * tankHeight * 0.3;
        var turn = shift / radius;
        var shake = imbalance > 0.05 ? Math.Sin(Seconds * 47) * Math.Min(2, imbalance / 10) : 0;

        // The wheel and the motor at its axle.
        context.DrawEllipse(null, new ImmutablePen(Metal, 6), wheel, radius, radius);
        for (var k = 0; k < 6; k++)
        {
            var a = (k * Math.PI / 3) + turn;
            context.DrawLine(new ImmutablePen(Metal, 2), wheel, new Point(wheel.X + (radius * Math.Cos(a)), wheel.Y + (radius * Math.Sin(a))));
        }

        Mark("wheel", new Point(wheel.X, wheel.Y - radius), 14);
        var motor = new Rect(wheel.X - 22 + shake, wheel.Y - 16, 44, 32);
        context.DrawRectangle(imbalance > 0.05 ? Warn : Iron, Edge, motor, 4, 4);
        Mark("motor", motor);
        DiagramText.Draw(context, imbalance > 0.05 ? DiagramText.Format("Canals_Lift_MotorLabel", Number(imbalance, "0")) : DiagramText.Get("Canals_Lift_EvenLabel"),
            new Point(wheel.X, wheel.Y + 26), 12.5, imbalance > 0.05 ? Warn : DiagramText.Gold, 0.5, 0, bold: true);

        // The tanks hanging from each side, their water, and the boat in the left one.
        var tanks = new List<Point>();
        foreach (var side in new[] { -1, 1 })
        {
            var x = wheel.X + (side * radius);
            var hang = tankTop - (side * shift);
            context.DrawLine(new ImmutablePen(SteelBrush, 2.5), new Point(x, wheel.Y), new Point(x, hang));
            var tank = new Rect(x - (tankWidth / 2), hang, tankWidth, tankHeight);
            var depth = tankHeight * 0.7;
            var extraDrawn = side < 0 ? extra / 100 * 10 / BoatLift.Depth * depth : 0;
            context.FillRectangle(Water, new Rect(tank.X, tank.Bottom - depth - extraDrawn, tank.Width, depth + extraDrawn));
            DrawRipples(context, tank.Left + 3, tank.Right - 3, tank.Bottom - depth - extraDrawn, 0.6, Seconds + side, 14);
            context.DrawRectangle(null, new ImmutablePen(Masonry, 4), tank);
            tanks.AddRange([tank.TopLeft, tank.BottomRight]);
            DiagramText.Draw(context, DiagramText.Format("Canals_Tonnes", Number(BoatLift.Water + (side < 0 ? imbalance : 0), "N0")), new Point(tank.Center.X, tank.Bottom + 8), 12,
                DiagramText.Ink, 0.5, 0, bold: true);
            if (side < 0)
            {
                var surface = tank.Bottom - depth - extraDrawn;
                var level = tank.Bottom - depth;
                var perCm = 10.0 / 100 / BoatLift.Depth * depth;
                Grip("extra", new Point(tank.Right + 16, surface), GripAxis.Vertical, DiagramText.Get("Canals_Grip_Extra"),
                    at => Extra = Math.Round(Math.Clamp((level - at.Y) / perCm, 0, 20) * 2) / 2);
                Grip("boat", new Point(tank.Left - 20, tank.Center.Y), GripAxis.Tap, DiagramText.Get("Canals_Grip_Boat"), _ => Boat = !Boat);
                if (Boat)
                {
                    var boat = DrawBoat(context, new Point(tank.Center.X, surface + 9), tankWidth * 0.6, 14);
                    Mark("boat", boat);
                    DiagramText.Draw(context, DiagramText.Get("Canals_Lift_SpilledLabel"), new Point(tank.Left, tank.Top - 6), 11, Flow, 0, 1);
                }
                else
                {
                    var ghost = new Rect(tank.Center.X - (tankWidth * 0.3), surface - 18, tankWidth * 0.6, 22);
                    context.DrawRectangle(null, new ImmutablePen(DiagramText.Faint, 1.2, new ImmutableDashStyle([4, 4], 0)), ghost, 3, 3);
                    Mark("boat", ghost);
                }
            }
        }

        Mark("tanks", tanks, 4);
        DiagramText.Draw(context, DiagramText.Get("Canals_Lift_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }

    private static readonly IImmutableBrush Warn = new ImmutableSolidColorBrush(Color.FromRgb(0xF0, 0x6A, 0x5A));
    private static readonly IImmutableBrush Iron = new ImmutableSolidColorBrush(Color.FromRgb(0x5A, 0x60, 0x6C));
    private static readonly IImmutableBrush Metal = new ImmutableSolidColorBrush(Color.FromRgb(0x8F, 0xA3, 0xBF));
}
