using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Bicycle;

/// <summary>
/// Spread 5: a rider brakes from the chosen speed, over and over. Arrows at the tyres show how the weight shifts onto
/// the front wheel while slowing; a ruler shows how far the bike goes. With only the back brake the back wheel skids
/// (a black mark); too hard with the front and the back wheel lifts.
/// </summary>
public sealed class BrakeDiagram : GearPicture
{
    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<BrakeDiagram, double>(nameof(Speed), 25);

    public static readonly StyledProperty<double> StrengthProperty =
        AvaloniaProperty.Register<BrakeDiagram, double>(nameof(Strength), 0.3);

    public static readonly StyledProperty<BrakeUse> UseProperty =
        AvaloniaProperty.Register<BrakeDiagram, BrakeUse>(nameof(Use));

    /// <summary>Seconds rolling up to the brake line, and seconds standing still after stopping.</summary>
    private const double Approach = 0.9;
    private const double Rest = 1.6;

    /// <summary>How far the bike tips forwards when it goes over, radians.</summary>
    private const double TipAngle = 0.5;

    private static readonly IPen Ground = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0xA0, 0xE6, 0xEA, 0xF2)), 2);
    private static readonly IPen Ruler = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0xC0, 0xE6, 0xEA, 0xF2)), 1.2);
    private static readonly IPen SkidMark = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x05, 0x06, 0x08)), 4, lineCap: PenLineCap.Round);
    private static readonly IImmutableBrush Danger = new ImmutableSolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x5A));
    private static readonly IImmutableBrush LoadBrush = new ImmutableSolidColorBrush(GearPicture.Copper);

    private double _clock;
    private double _front;
    private double _rear;
    private double _crank;

    static BrakeDiagram()
    {
        AffectsRender<BrakeDiagram>(SpeedProperty, StrengthProperty, UseProperty);
        SpeedProperty.Changed.AddClassHandler<BrakeDiagram>((d, _) => d._clock = 0);
        StrengthProperty.Changed.AddClassHandler<BrakeDiagram>((d, _) => d._clock = 0);
        UseProperty.Changed.AddClassHandler<BrakeDiagram>((d, _) => d._clock = 0);
    }

    /// <summary>The speed the rider brakes from, km/h.</summary>
    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    /// <summary>How hard the rider brakes, in g.</summary>
    public double Strength
    {
        get => GetValue(StrengthProperty);
        set => SetValue(StrengthProperty, value);
    }

    public BrakeUse Use
    {
        get => GetValue(UseProperty);
        set => SetValue(UseProperty, value);
    }

    /// <summary>Real seconds to stop, and the seconds the picture takes to show it.</summary>
    private (double Real, double Shown) Durations(double slowing)
    {
        var real = Speed / 3.6 / (slowing * Bike.Gravity);
        return (real, Math.Clamp(real, 1.2, 3.5));
    }

    protected override void Advance(double seconds)
    {
        var (outcome, slowing) = Braking.Brake(Use, Strength);
        var (real, shown) = Durations(slowing);
        var before = Travelled(_clock, slowing, real, shown);
        _clock = (_clock + seconds) % (Approach + shown + Rest);
        var moved = Math.Max(0, Travelled(_clock, slowing, real, shown) - before);
        var rolling = _clock < Approach ? Speed / 3.6 * seconds * 0.6 : moved;
        _front += rolling / Bike.WheelRadius;
        if (!(outcome == BrakeOutcome.RearSkids && _clock >= Approach))
        {
            _rear += rolling / Bike.WheelRadius;
        }

        _crank += _clock < Approach ? seconds * 2 : 0;
    }

    /// <summary>Metres from the brake line at <paramref name="clock"/> into the loop.</summary>
    private double Travelled(double clock, double slowing, double real, double shown)
    {
        if (clock < Approach) return 0;
        var t = Math.Min(clock - Approach, shown) * real / shown;
        var v = Speed / 3.6;
        return Math.Min((v * t) - (0.5 * slowing * Bike.Gravity * t * t), Braking.StoppingDistance(Speed, slowing));
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var (outcome, slowing) = Braking.Brake(Use, Strength);
        var (real, shown) = Durations(slowing);
        var distance = Braking.StoppingDistance(Speed, slowing);
        var travelled = Travelled(_clock, slowing, real, shown);
        var braking = _clock >= Approach && _clock < Approach + shown;
        var stopped = _clock >= Approach + shown;

        var scale = Math.Min(bounds.Width * 0.3 / 1.5, bounds.Height * 0.3 / 1.6);
        var floor = bounds.Height * 0.56;
        context.DrawLine(Ground, new Point(bounds.X + 8, floor), new Point(bounds.Right - 8, floor));

        // The brake line, and the ground from it to where the bike stops, to the ruler's scale.
        var start = bounds.X + 20 + (0.35 * scale);
        var span = bounds.Width - 40 - (1.5 * scale);
        var perMetre = span / Math.Max(distance, 1);
        var x = start + (travelled * perMetre);
        context.DrawLine(new ImmutablePen(DiagramText.Gold, 1.5, new ImmutableDashStyle([4, 3], 0)), new Point(start, floor + 4), new Point(start, bounds.Y + 70));
        DiagramText.Draw(context, DiagramText.Get("Bicycle_Brakes_Line"), new Point(start + 4, bounds.Y + 70), 12, DiagramText.Gold);
        if (outcome == BrakeOutcome.RearSkids && _clock >= Approach)
        {
            context.DrawLine(SkidMark, new Point(start, floor - 2), new Point(x, floor - 2));
        }

        // How far the bike tips: only when it goes over, growing through the last part of the stop.
        var tip = outcome == BrakeOutcome.Flips && _clock >= Approach
            ? TipAngle * Smooth(stopped ? 1 : ((_clock - Approach) / shown - 0.45) / 0.55)
            : 0;
        var pivot = new Point(1.0, 0);
        Point Map(Point metres)
        {
            // Turn about the front tyre's touch (the back rising), then place the back tyre's touch at x on the floor.
            var (sin, cos) = Math.SinCos(-tip);
            var d = metres - pivot;
            var turned = new Point(pivot.X + (d.X * cos) - (d.Y * sin), pivot.Y + (d.X * sin) + (d.Y * cos));
            return new Point(x + (turned.X * scale), floor - (turned.Y * scale));
        }

        var bike = BicycleParts.DrawBike(context, Map, scale, _crank, _front, _rear);
        BicycleParts.DrawCentreOfMass(context, bike.CentreOfMass, Math.Max(6, scale * 0.05));
        Mark("centre", bike.CentreOfMass, Math.Max(8, scale * 0.06));
        Mark("front", bike.FrontAxle, bike.WheelRadius);
        Mark("rear", bike.RearAxle, bike.WheelRadius);
        Mark("pads", bike.FrontPads, Math.Max(8, scale * 0.06));

        // The weight on each tyre: shifting forwards while slowing, all on the front when going over.
        var front = braking ? (outcome == BrakeOutcome.Flips && tip > 0.02 ? 1 : Braking.FrontLoad(slowing)) : (tip > 0.02 ? 1 : Braking.FrontShare);
        DrawLoad(context, bike.FrontContact, front, scale);
        DrawLoad(context, bike.RearContact, 1 - front, scale);

        // The ruler under the floor, in metres.
        var ruler = floor + (scale * 0.9) + 74;
        var end = start + (distance * perMetre);
        context.DrawLine(Ruler, new Point(start, ruler), new Point(end, ruler));
        var step = NiceStep(distance);
        for (var m = 0.0; m <= distance + 1e-9; m += step)
        {
            var tx = start + (m * perMetre);
            context.DrawLine(Ruler, new Point(tx, ruler - 4), new Point(tx, ruler + 4));
            DiagramText.Draw(context, m.ToString("0"), new Point(tx, ruler + 6), 10.5, DiagramText.Faint, 0.5);
        }

        context.DrawLine(new ImmutablePen(DiagramText.Gold, 2), new Point(end, ruler - 7), new Point(end, ruler + 7));
        DiagramText.Draw(context, DiagramText.Format("Bicycle_Brakes_Metres", distance.ToString("0.0")), new Point(end, ruler - 9), 12.5, DiagramText.Gold, 0.5, 1, bold: true);
        Mark("distance", new Rect(start, ruler - 10, Math.Max(20, end - start), 30));

        // What is happening now.
        var v = _clock < Approach ? Speed : Math.Max(0, Speed - (slowing * Bike.Gravity * 3.6 * Math.Min(_clock - Approach, shown) * real / shown));
        var info = new Point(bounds.X + 18, bounds.Y + 16);
        var line = DiagramText.Draw(context, DiagramText.Format("Bicycle_Kmh", v.ToString("0")), info, 22, DiagramText.Ink, bold: true);
        var words = outcome switch
        {
            BrakeOutcome.RearSkids when !(_clock < Approach) => DiagramText.Get("Bicycle_Brakes_Skidding"),
            BrakeOutcome.Flips when tip > 0.02 => DiagramText.Get("Bicycle_Brakes_Over"),
            _ => braking ? DiagramText.Get("Bicycle_Brakes_Braking") : stopped ? DiagramText.Get("Bicycle_Brakes_Stopped") : string.Empty,
        };
        DiagramText.Draw(context, words, new Point(line.Right + 12, line.Center.Y), 13, outcome == BrakeOutcome.Stops ? DiagramText.Ink : Danger, 0, 0.5, bold: true);

        var legend = new Point(bounds.X + 18, bounds.Bottom - 60);
        DiagramText.Draw(context, DiagramText.Get("Bicycle_Brakes_LoadKey"), legend, 11.5, LoadBrush);
    }

    /// <summary>An upward arrow at a tyre's touch, as long as its share of the weight, labelled in percent.</summary>
    private static void DrawLoad(DrawingContext context, Point contact, double share, double scale)
    {
        if (share < 0.01) return;

        var length = share * scale * 0.9;
        var from = contact + new Point(0, 18 + length);
        var to = contact + new Point(0, 12);
        context.DrawLine(new ImmutablePen(LoadBrush, 4), from, to + new Point(0, 6));
        DiagramText.DrawArrowAlong(context, from, to, LoadBrush, 11);
        DiagramText.Draw(context, (share * 100).ToString("0") + "%", from + new Point(0, 2), 11.5, LoadBrush, 0.5);
    }

    private static double NiceStep(double metres) => metres switch
    {
        <= 6 => 1,
        <= 15 => 2,
        <= 30 => 5,
        _ => 10,
    };

    private static double Smooth(double t)
    {
        t = Math.Clamp(t, 0, 1);
        return t * t * (3 - (2 * t));
    }
}
