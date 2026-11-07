using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Bicycle;

/// <summary>
/// Spread 2: the drive side of a bike, true to size. The pedals turn the chainring; the chain carries its teeth's turn
/// back to the sprocket on the wheel. Counters show the turns of each.
/// </summary>
public sealed class ChainDiagram : GearPicture
{
    public static readonly StyledProperty<int> ChainringProperty =
        AvaloniaProperty.Register<ChainDiagram, int>(nameof(Chainring), 44);

    public static readonly StyledProperty<int> SprocketProperty =
        AvaloniaProperty.Register<ChainDiagram, int>(nameof(Sprocket), 22);

    /// <summary>From the crank's axle to the wheel's, metres.</summary>
    private const double Chainstay = 0.41;

    /// <summary>Pedal turns a second (slowed down so the chain can be followed).</summary>
    private const double PedalSpeed = 0.3;

    private static readonly IImmutableBrush Roller = new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xCE, 0xD8));
    private static readonly Color LinkPlate = Color.FromRgb(0x6A, 0x74, 0x86);

    private double _pedalTurns;
    private double _wheelTurns;

    static ChainDiagram()
    {
        AffectsRender<ChainDiagram>(ChainringProperty, SprocketProperty);
    }

    public int Chainring
    {
        get => GetValue(ChainringProperty);
        set => SetValue(ChainringProperty, value);
    }

    public int Sprocket
    {
        get => GetValue(SprocketProperty);
        set => SetValue(SprocketProperty, value);
    }

    /// <summary>Chain moved so far, in links.</summary>
    private double _links;

    protected override void Advance(double seconds)
    {
        _pedalTurns += PedalSpeed * seconds;
        _links += PedalSpeed * seconds * Chainring;
        _wheelTurns += PedalSpeed * seconds * Drive.Ratio(Chainring, Sprocket);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var scale = Math.Min(bounds.Width / (Bike.WheelRadius + Chainstay + Bike.CrankLength + 0.12), bounds.Height * 0.62 / (2 * Bike.WheelRadius));
        var y = Math.Max((bounds.Height * 0.06) + ((Bike.WheelRadius + 0.02) * scale), bounds.Height * 0.4);
        var wheel = new Point(bounds.X + ((Bike.WheelRadius + 0.03) * scale), y);
        var ring = new Point(wheel.X + (Chainstay * scale), y);
        var pitch = Drive.Pitch * scale;
        var ra = Drive.PitchRadius(Sprocket) * scale;
        var rb = Drive.PitchRadius(Chainring) * scale;

        // The back wheel behind it all.
        var wheelAngle = 2 * Math.PI * _wheelTurns;
        BicycleParts.DrawWheel(context, wheel, Bike.WheelRadius * scale, wheelAngle, spokes: 24, opacity: 0.55);
        Mark("wheel", wheel, Bike.WheelRadius * scale);

        // Both sprockets turn clockwise; tooth gaps move at the chain's speed.
        BicycleParts.DrawSprocket(context, wheel, Sprocket, ra, 2 * Math.PI * _links / Sprocket, Steel, pitch);
        BicycleParts.DrawSprocket(context, ring, Chainring, rb, 2 * Math.PI * _links / Chainring, Gold, pitch);
        Mark("sprocket", wheel, ra + (pitch * 0.4));
        Mark("chainring", ring, rb + (pitch * 0.4));

        // The chain: rollers spaced a link apart round the loop, moving with it.
        var path = new ChainPath(wheel, ra, ring, rb);
        var count = Math.Max(8, (int)Math.Round(path.Length / pitch));
        var spacing = path.Length / count;
        var shift = _links % 1 * spacing;
        var rollers = new Point[count];
        for (var k = 0; k < count; k++)
        {
            rollers[k] = path.At((k * spacing) + shift);
        }

        var plate = new ImmutablePen(new ImmutableSolidColorBrush(LinkPlate), Math.Max(2, pitch * 0.55), lineCap: PenLineCap.Round);
        for (var k = 0; k < count; k++)
        {
            context.DrawLine(plate, rollers[k], rollers[(k + 1) % count]);
        }

        var dot = Math.Max(1.2, pitch * 0.22);
        foreach (var p in rollers)
        {
            context.DrawEllipse(Roller, null, p, dot, dot);
        }

        Mark("chain", [path.TopStart, path.TopEnd], 4);

        // The crank and its pedal, on top.
        var crank = (2 * Math.PI * _pedalTurns) - (Math.PI / 2);
        var pedal = BicycleParts.At(ring, Bike.CrankLength * scale, crank);
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xB8, 0xC2, 0xD2)), Math.Max(4, scale * 0.025), lineCap: PenLineCap.Round), ring, pedal);
        context.DrawRectangle(new ImmutableSolidColorBrush(Color.FromRgb(0x3A, 0x40, 0x4E)), new ImmutablePen(DiagramText.Gold, 1.5),
            new Rect(pedal.X - (scale * 0.05), pedal.Y - (scale * 0.012), scale * 0.1, scale * 0.024), 2, 2);
        Mark("pedal", pedal, Math.Max(10, scale * 0.05));

        DiagramText.Draw(context, DiagramText.Get("Bicycle_Chain_Ring"), new Point(ring.X, ring.Y + rb + 10), 12, DiagramText.Gold, 0.5, 0);
        DiagramText.Draw(context, DiagramText.Get("Bicycle_Chain_Cog"), new Point(wheel.X, wheel.Y - ra - 8), 12, DiagramText.Ink, 0.5, 1);

        // Turns so far: the pedals' and the wheel's.
        var counters = new Point(bounds.X + 18, Math.Min(bounds.Bottom - 46, y + (Bike.WheelRadius * scale) + 18));
        var first = DiagramText.Draw(context, DiagramText.Format("Bicycle_Chain_PedalTurns", _pedalTurns.ToString("0.0")), counters, 13, DiagramText.Gold, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Bicycle_Chain_WheelTurns", _wheelTurns.ToString("0.0")), new Point(first.X, first.Bottom + 4), 13, DiagramText.Ink, bold: true);
    }

    /// <summary>
    /// The loop a chain makes round two sprockets (screen coordinates, the small one on the left): along the top from
    /// left to right, clockwise round the right one, back along the bottom, clockwise round the left one.
    /// </summary>
    private sealed class ChainPath
    {
        private readonly Point _a;
        private readonly Point _b;
        private readonly double _ra;
        private readonly double _rb;
        private readonly double _upper;
        private readonly double _lower;
        private readonly double _top;
        private readonly double _arcB;
        private readonly double _bottom;

        public ChainPath(Point a, double ra, Point b, double rb)
        {
            (_a, _b, _ra, _rb) = (a, b, ra, rb);
            var distance = b.X - a.X;
            var nx = Math.Clamp((ra - rb) / distance, -1, 1);
            var ny = Math.Sqrt(1 - (nx * nx));
            _upper = Math.Atan2(-ny, nx);
            _lower = Math.Atan2(ny, nx);
            TopStart = BicycleParts.At(a, ra, _upper);
            TopEnd = BicycleParts.At(b, rb, _upper);
            _top = Distance(TopStart, TopEnd);
            _arcB = rb * (_lower - _upper);
            _bottom = _top;
            Length = _top + _arcB + _bottom + (ra * ((2 * Math.PI) - (_lower - _upper)));
        }

        public Point TopStart { get; }

        public Point TopEnd { get; }

        public double Length { get; }

        public Point At(double s)
        {
            s %= Length;
            if (s < 0) s += Length;
            if (s < _top) return Lerp(TopStart, TopEnd, s / _top);
            s -= _top;
            if (s < _arcB) return BicycleParts.At(_b, _rb, _upper + (s / _rb));
            s -= _arcB;
            if (s < _bottom) return Lerp(BicycleParts.At(_b, _rb, _lower), BicycleParts.At(_a, _ra, _lower), s / _bottom);
            s -= _bottom;
            return BicycleParts.At(_a, _ra, _lower + (s / _ra));
        }

        private static Point Lerp(Point a, Point b, double t) => new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));

        private static double Distance(Point a, Point b) => Math.Sqrt(((b.X - a.X) * (b.X - a.X)) + ((b.Y - a.Y) * (b.Y - a.Y)));
    }
}
