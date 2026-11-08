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
/// Spread 4: a canal climbing a 24 m hill, from the side, in metres (the height drawn three times steeper than the
/// length so the steps show). Each lock is a step of water between two gates; more locks make smaller steps. A boat
/// climbs the flight, a lock at a time: it sails into each chamber, the chamber fills and lifts it, and on it goes.
/// A bar shows the water one boat takes down from the top, a chamberful of one step. The reader drags the top of the
/// first step up or down: the hill splits into as many locks as fit.
/// </summary>
public sealed class FlightDiagram : GearPicture
{
    public static readonly StyledProperty<int> LocksProperty =
        AvaloniaProperty.Register<FlightDiagram, int>(nameof(Locks), 1, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The flight's length along the ground, m; how much steeper than life the heights are drawn.</summary>
    private const double Span = 120;
    private const double Steep = 3;

    /// <summary>Seconds the boat takes to sail into a chamber, to be lifted in it, and to wait at the start and the top.</summary>
    private const double Sail = 0.9;
    private const double Lift = 1.3;
    private const double Wait = 1.2;

    static FlightDiagram()
    {
        AffectsRender<FlightDiagram>(LocksProperty);
    }

    public int Locks
    {
        get => GetValue(LocksProperty);
        set => SetValue(LocksProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var locks = Math.Clamp(Locks, 1, 8);
        var step = Flight.Step(locks);
        var scale = Math.Min((bounds.Width - 60) / (Span + 40), (bounds.Height - 200) / ((Flight.Hill * Steep) + 12));
        var v = new View(new Point(bounds.X + 30 + (20 * scale), bounds.Y + 60 + (((Flight.Hill * Steep) + 8) * scale)), scale);
        double Y(double height) => height * Steep;

        // The hill, under the water.
        Point[] hill = [v.At(-20, -4), v.At(-20, Y(0) - 1), v.At(0, Y(0) - 1), v.At(Span, Y(Flight.Hill) - 1), v.At(Span + 20, Y(Flight.Hill) - 1), v.At(Span + 20, -4)];
        DrawPolygon(context, Ground, null, hill);
        Mark("hill", [hill[2], hill[3]], 10);

        // The pounds and chambers: each lock a step of water; the gates at each step's edges.
        var width = Span / locks;
        context.FillRectangle(Water, v.Box(-20, Y(0) - 1, 0, Y(0)));
        context.FillRectangle(Water, v.Box(Span, Y(Flight.Hill) - 1, Span + 20, Y(Flight.Hill)));
        var gatePen = new ImmutablePen(Timber, 4);
        var gates = new List<Point>();
        var trip = Trip(locks, Seconds);
        for (var k = 0; k < locks; k++)
        {
            var x0 = k * width;
            var level = (k + 1) * step;

            // The chamber the boat is in stands at the water it has risen to; the others are full to their upper gates.
            var water = k == trip.Chamber ? trip.Level : level;
            context.FillRectangle(Water, v.Box(x0, Y(k * step) - 1, x0 + width, Y(water)));
            DrawRipples(context, v.At(x0, 0).X + 2, v.At(x0 + width, 0).X - 2, v.At(0, Y(water)).Y, 0.6, Seconds + k, 16);
            if (k == trip.Chamber && trip.Rising)
            {
                Plumbing.PlumbingParts.DrawFlow(context, [v.At(x0 + width + 0.5, Y(level) - 1.5), v.At(x0 + width - 4, Y(water) - 1)], 0.6 * scale * Steep, Seconds * 90);
            }

            context.DrawLine(gatePen, v.At(x0, Y(k * step) - 1), v.At(x0, Y(level) + 0.6));
            gates.AddRange([v.At(x0, Y(k * step) - 1), v.At(x0, Y(level))]);
        }

        context.DrawLine(gatePen, v.At(Span, Y(Flight.Hill) - 1), v.At(Span, Y(Flight.Hill) + 0.6));
        Mark("locks", gates, 6);

        var boat = DrawBoat(context, v.At(trip.X, Y(trip.Level) - 0.4), Math.Min(14, width * 0.8) * scale, Math.Max(scale * 2.5, 6));
        Mark("boat", boat);
        DiagramText.Draw(context, DiagramText.Format("Canals_Flight_StepLabel", Number(step)), new Point(v.At(width, 0).X - 18, v.At(0, Y(step) + 2).Y), 12, DiagramText.Gold, 1, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Canals_Metres", Number(Flight.Hill, "0")), v.At(Span + 10, Y(Flight.Hill) + 2), 12, DiagramText.Ink, 0.5, 1);

        // The bar: water taken down per boat, against what a single 24 m lock would take.
        var bar = new Rect(bounds.X + 30, v.At(0, -4).Y + 40, bounds.Width - 60, 24);
        context.DrawRectangle(null, new ImmutablePen(DiagramText.Faint, 1), bar, 3, 3);
        context.FillRectangle(DeepWater, new Rect(bar.X, bar.Y, bar.Width * Flight.Water(locks) / Flight.Water(1), bar.Height), 3);
        DiagramText.Draw(context, DiagramText.Format("Canals_Flight_BarLabel", Number(Flight.Water(locks), "N0")), new Point(bar.X + 8, bar.Center.Y), 12, DiagramText.Ink, 0, 0.5, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Canals_Flight_TimeLabel", Number(Flight.Minutes(locks), "0")), new Point(bar.X, bar.Bottom + 10), 12.5, DiagramText.Gold, 0, 0, bold: true);
        Mark("water", bar);
        DiagramText.Draw(context, DiagramText.Get("Canals_Flight_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);

        // The top of the first step, dragged up for fewer, taller locks or down for more, shorter ones.
        Grip("step", v.At(width, Y(step) + 0.6), GripAxis.Vertical, DiagramText.Get("Canals_Grip_Step"),
            at => Locks = Steps(v.Y(at) / Steep));
    }

    /// <summary>The locks a first step <paramref name="height"/> m high splits the hill into, 1 to 8.</summary>
    internal static int Steps(double height) => height <= Flight.Hill / 8 ? 8 : Math.Clamp((int)Math.Round(Flight.Hill / height), 1, 8);

    /// <summary>
    /// Where the boat is after <paramref name="seconds"/> of its climb: along the flight and at what water height (m),
    /// the chamber it is in (-1 for none), and whether that chamber is filling under it. Then it starts again.
    /// </summary>
    internal static (double X, double Level, int Chamber, bool Rising) Trip(int locks, double seconds)
    {
        locks = Math.Clamp(locks, 1, 8);
        var width = Span / locks;
        var step = Flight.Step(locks);
        var t = seconds % ((locks * (Sail + Lift)) + Sail + (2 * Wait));
        if (t < Wait) return (-10, 0, -1, false);

        t -= Wait;
        var x = -10.0;
        for (var k = 0; k < locks; k++)
        {
            var middle = (k + 0.5) * width;
            if (t < Sail) return (x + ((middle - x) * Smooth(t / Sail)), k * step, k, false);

            t -= Sail;
            if (t < Lift) return (middle, (k + Smooth(t / Lift)) * step, k, true);

            t -= Lift;
            x = middle;
        }

        return (x + ((Span + 10 - x) * Smooth(Math.Min(1, t / Sail))), Flight.Hill, -1, false);
    }

    private static double Smooth(double t) => t * t * (3 - (2 * t));
}
