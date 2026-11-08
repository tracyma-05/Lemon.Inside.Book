using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Bicycle;

/// <summary>
/// Spread 3: a rider climbing a road of the chosen slope at a steady pedalling rhythm. The back sprockets show which
/// one the chain is on; a bar shows how hard each push on the pedals is, against what a rider can keep up. The reader
/// drags the top of the slope triangle up for a steeper road and the chain along the cassette to change gear; the chain
/// shifts across the cogs as a derailleur moves it.
/// </summary>
public sealed class HillDiagram : GearPicture
{
    public static readonly StyledProperty<double> SlopeProperty =
        AvaloniaProperty.Register<HillDiagram, double>(nameof(Slope), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> GearProperty =
        AvaloniaProperty.Register<HillDiagram, int>(nameof(Gear), 2, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The slider's steepest road, percent.</summary>
    private const double MostSlope = 15;

    /// <summary>The picture runs at this share of real time, so the legs can be followed.</summary>
    private const double TimeScale = 0.5;

    /// <summary>The push bar runs up to this many kilograms.</summary>
    private const double MostPush = 60;

    private static readonly IImmutableBrush Road = new ImmutableSolidColorBrush(Color.FromRgb(0x2E, 0x34, 0x40));
    private static readonly IPen RoadEdge = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0xA0, 0xE6, 0xEA, 0xF2)), 2);
    private static readonly IPen Dash = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x70, 0xE6, 0xEA, 0xF2)), 2);
    private static readonly IImmutableBrush Track = new ImmutableSolidColorBrush(Color.FromArgb(0x30, 0xE6, 0xEA, 0xF2));
    private static readonly IImmutableBrush Easy = new ImmutableSolidColorBrush(Color.FromArgb(0x50, 0x5F, 0xC2, 0xB0));
    private static readonly IImmutableBrush TooHard = new ImmutableSolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x5A));

    private double _metres;
    private double _crank;

    /// <summary>Where the chain is across the cassette (a gear number, between two while it shifts).</summary>
    private double _shift = 2;

    static HillDiagram()
    {
        AffectsRender<HillDiagram>(SlopeProperty, GearProperty);
    }

    /// <summary>The road's slope, percent.</summary>
    public double Slope
    {
        get => GetValue(SlopeProperty);
        set => SetValue(SlopeProperty, value);
    }

    /// <summary>The gear: 0 for the smallest back sprocket, up to the biggest.</summary>
    public int Gear
    {
        get => GetValue(GearProperty);
        set => SetValue(GearProperty, value);
    }

    /// <summary>The back sprocket the chain is on.</summary>
    public int Sprocket => Climb.Cassette[Math.Clamp(Gear, 0, Climb.Cassette.Count - 1)];

    protected override void Advance(double seconds)
    {
        var t = seconds * TimeScale;
        _metres += Climb.Speed(Sprocket) * t;
        _crank += 2 * Math.PI * Climb.Cadence / 60 * t;
        _shift = Glide(_shift, Math.Clamp(Gear, 0, Climb.Cassette.Count - 1), seconds, 0.1);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var angle = Math.Atan(Slope / 100);
        var scale = Math.Min(bounds.Width * 0.62 / 1.7, bounds.Height * 0.36 / 1.6);
        var (sin, cos) = Math.SinCos(angle);

        // The road through a point below the middle, rising to the right.
        var anchor = new Point(bounds.Width * 0.42, bounds.Height * 0.62);
        Point Along(double metres) => new(anchor.X + (metres * scale * cos), anchor.Y - (metres * scale * sin));
        var left = Along(-bounds.Width / scale);
        var right = Along(bounds.Width / scale);
        var roadShape = new StreamGeometry();
        using (var g = roadShape.Open())
        {
            g.BeginFigure(left, true);
            g.LineTo(right);
            g.LineTo(new Point(right.X, bounds.Bottom + 10));
            g.LineTo(new Point(left.X, bounds.Bottom + 10));
            g.EndFigure(true);
        }

        context.DrawGeometry(Road, null, roadShape);
        context.DrawLine(RoadEdge, left, right);

        // Marks on the road slide back as the bike climbs.
        var spacing = 0.8;
        for (var m = -(_metres % spacing) - (spacing * 20); m < spacing * 20; m += spacing)
        {
            var a = Along(m);
            var b = Along(m + 0.3);
            context.DrawLine(Dash, a + new Point(0, 8), b + new Point(0, 8));
        }

        // The slope's size, as a little rise-over-run triangle on the road behind the bike (ahead of it runs off the
        // page on a narrow picture).
        var start = Along(-1.45);
        var end = Along(-0.55);
        var foot = new Point(end.X, start.Y);
        var faint = new ImmutablePen(DiagramText.Faint, 1, new ImmutableDashStyle([3, 3], 0));
        context.DrawLine(faint, start, foot);
        context.DrawLine(faint, foot, end);
        DiagramText.Draw(context, DiagramText.Format("Bicycle_Percent", Slope.ToString("0.#")), new Point(foot.X + 17, (foot.Y + end.Y) / 2), 13, DiagramText.Ink, 0, 0.5, bold: true);
        Mark("slope", [start, foot, end], 8);

        // The top of the rise is the reader's grip: up for a steeper road, its rise over its run the slope.
        Grip("slope", end, GripAxis.Vertical, DiagramText.Get("Bicycle_Grip_Slope"),
            at => Slope = Math.Round(Math.Clamp((start.Y - at.Y) / Math.Max(1, end.X - start.X) * 100, 0, MostSlope) * 2) / 2);

        // The bike, its back tyre on the road at the anchor.
        Point Map(Point metres) => new(
            anchor.X + (((metres.X * cos) - (metres.Y * sin)) * scale),
            anchor.Y - (((metres.X * sin) + (metres.Y * cos)) * scale));
        var wheelTurn = _metres / Bike.WheelRadius;
        var bike = BicycleParts.DrawBike(context, Map, scale, _crank, wheelTurn, wheelTurn);
        Mark("rider", [bike.Head, bike.Hip, bike.Crank], 10);

        var kmh = Climb.Speed(Sprocket) * 3.6;
        DiagramText.Draw(context, DiagramText.Format("Bicycle_Kmh", kmh.ToString("0")), new Point(bounds.X + 18, bounds.Y + 16), 22, DiagramText.Ink, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Bicycle_Hill_Rhythm", Climb.Cadence.ToString("0")), new Point(bounds.X + 18, bounds.Y + 46), 12, DiagramText.Faint);

        DrawCassette(context, new Rect(bounds.Right - 18 - Math.Min(190, bounds.Width * 0.4), bounds.Y + 58, Math.Min(190, bounds.Width * 0.4), bounds.Height * 0.2));
        DrawPush(context, new Rect(bounds.X + 18, bounds.Bottom - 64, bounds.Width - 36, 44));
    }

    /// <summary>The back sprockets seen from behind, biggest on the left as on a bike, the chain on one of them.</summary>
    private void DrawCassette(DrawingContext context, Rect area)
    {
        var cogs = Climb.Cassette;
        var label = DiagramText.Draw(context, DiagramText.Get("Bicycle_Hill_Cassette"), new Point(area.Center.X, area.Y), 11.5, DiagramText.Faint, 0.5);
        var top = label.Bottom + 6;
        var height = area.Bottom - top - 18;
        var mid = top + (height / 2);
        var step = area.Width / cogs.Count;
        var biggest = cogs[^1];

        // Gear g's cog is drawn in column Count - 1 - g (biggest on the left); its middle is where the chain runs.
        double Column(double gear) => area.X + ((cogs.Count - 1 - gear) * step) + (step * 0.5);
        for (var i = 0; i < cogs.Count; i++)
        {
            var cog = cogs[cogs.Count - 1 - i];
            var h = height * cog / biggest;
            var x = area.X + (i * step) + (step * 0.2);
            var on = cog == Sprocket;
            context.FillRectangle(new ImmutableSolidColorBrush(on ? Gold : Steel, on ? 1 : 0.6), new Rect(x, mid - (h / 2), step * 0.6, h), 2);
            if (on)
            {
                DiagramText.Draw(context, DiagramText.Format("Bicycle_TeethCount", cog), new Point(x + (step * 0.3), mid + (height / 2) + 4), 11, DiagramText.Gold, 0.5);
            }
        }

        // The chain, moving across from cog to cog as the derailleur pushes it, as tall as the cog it is passing.
        var lower = (int)Math.Floor(_shift);
        var upper = Math.Min(lower + 1, cogs.Count - 1);
        var between = cogs[lower] + ((cogs[upper] - cogs[lower]) * (_shift - lower));
        var chainX = Column(_shift);
        var reach = (height * between / biggest / 2) + 6;
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xCE, 0xD8)), 3), new Point(chainX, mid - reach), new Point(chainX, mid + reach));

        Mark("cassette", new Rect(area.X, top, area.Width, height + 16));
        var gear = Math.Clamp(Gear, 0, cogs.Count - 1);
        Grip("gear", new Point(Column(gear), mid), GripAxis.Horizontal, DiagramText.Get("Bicycle_Grip_Gear"),
            at => Gear = Math.Clamp((int)Math.Round(cogs.Count - 1 - ((at.X - area.X - (step * 0.5)) / step)), 0, cogs.Count - 1));
    }

    /// <summary>The push on each pedal, against the most a rider can keep up for a long climb.</summary>
    private void DrawPush(DrawingContext context, Rect area)
    {
        var push = Climb.PedalKilograms(Slope, Sprocket);
        var comfortable = push <= Climb.ComfortableKilograms;
        DiagramText.Draw(context, DiagramText.Format("Bicycle_Hill_Push", push.ToString("0")), new Point(area.X, area.Y), 12.5, DiagramText.Ink, bold: true);
        DiagramText.Draw(context, DiagramText.Get(comfortable ? "Bicycle_Hill_Easy" : "Bicycle_Hill_TooHard"), new Point(area.Right, area.Y), 12.5,
            comfortable ? DiagramText.Ink : TooHard, 1);
        var bar = new Rect(area.X, area.Bottom - 14, area.Width, 12);
        context.FillRectangle(Track, bar, 4);
        context.FillRectangle(Easy, new Rect(bar.X, bar.Y, bar.Width * Climb.ComfortableKilograms / MostPush, bar.Height), 4);
        context.FillRectangle(comfortable ? DiagramText.Gold : TooHard, new Rect(bar.X, bar.Y + 3, bar.Width * Math.Min(1, push / MostPush), bar.Height - 6), 3);
        Mark("push", bar.Inflate(new Thickness(0, 14, 0, 0)));
    }
}
