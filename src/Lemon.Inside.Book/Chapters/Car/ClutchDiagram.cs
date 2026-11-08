using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Car;

/// <summary>
/// Spread 4: the clutch cut in half, edge-on: the engine's flywheel, the friction plate on the gearbox shaft, and the
/// pressure plate pushed by the springs. The pedal draws the pressure plate back. Below, the two sides' speeds and the
/// heat while they slip.
/// The reader drags the pressure plate back against its springs, as the pedal does; let go of the pedal on the left
/// page and the plate slides home rather than jumping.
/// </summary>
public sealed class ClutchDiagram : GearPicture
{
    public static readonly StyledProperty<double> PedalProperty =
        AvaloniaProperty.Register<ClutchDiagram, double>(nameof(Pedal), 1, defaultBindingMode: BindingMode.TwoWay);

    private static readonly IImmutableBrush Lining = new ImmutableSolidColorBrush(Color.FromRgb(0x8A, 0x6E, 0x4E));
    private static readonly IImmutableBrush Stripe = new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0x10, 0x14, 0x1E));
    private static readonly IPen SpringPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x8F, 0xD8, 0xF0)), 2);

    private readonly Clutch _clutch = new();
    private double _engineTurns;
    private double _gearboxTurns;

    /// <summary>How far apart the plates are drawn, 0 to 1 of the widest gap, easing toward the pedal's; NaN until the first frame.</summary>
    private double _apart = double.NaN;

    static ClutchDiagram()
    {
        AffectsRender<ClutchDiagram>(PedalProperty);
    }

    /// <summary>How far the pedal is pressed, 0 (up) to 1 (to the floor).</summary>
    public double Pedal
    {
        get => GetValue(PedalProperty);
        set => SetValue(PedalProperty, value);
    }

    internal Clutch Model => _clutch;

    protected override void Advance(double seconds)
    {
        _clutch.Step(seconds, Pedal);

        // Drawn turning a tenth as fast, so the stripes can be followed.
        _engineTurns += _clutch.Engine * seconds * 0.1;
        _gearboxTurns += _clutch.Gearbox * seconds * 0.1;
        _apart = double.IsNaN(_apart) ? Apart(Pedal) : Glide(_apart, Apart(Pedal), seconds, 0.08);
    }

    /// <summary>The share of the widest gap the pedal opens: the gap is full a little before the pedal reaches the floor.</summary>
    private static double Apart(double pedal) => Math.Clamp(pedal * 1.25, 0, 1);

    /// <summary>The pedal (to a fiftieth) that draws the pressure plate back <paramref name="travel"/> of the widest gap.</summary>
    internal static double PedalForGap(double travel) => Math.Clamp(Math.Round(travel / 1.25 * 50) / 50, 0, 1);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var mid = new Point(bounds.Width * 0.5, bounds.Height * 0.34);
        var tall = bounds.Height * 0.44;
        var unit = Math.Min(bounds.Width / 14, 40);
        var widest = unit * 0.6;
        var gap = (double.IsNaN(_apart) ? Apart(Pedal) : _apart) * widest;

        // Shafts: the engine's from the left, the gearbox's to the right.
        context.DrawLine(new ImmutablePen(CarParts.DarkMetalBrush, unit * 0.5), new Point(bounds.X + 10, mid.Y), new Point(mid.X - (unit * 1.5), mid.Y));
        context.DrawLine(new ImmutablePen(CarParts.MetalBrush, unit * 0.35), new Point(mid.X - (unit * 0.5), mid.Y), new Point(bounds.Right - 10, mid.Y));

        // The flywheel (engine side), the friction plate with its linings, the pressure plate and springs.
        var flywheel = new Rect(mid.X - (unit * 2.4), mid.Y - (tall / 2), unit * 1.1, tall);
        CarParts.DrawEdgeOn(context, flywheel, _engineTurns, CarParts.DarkMetalBrush, Stripe, 8);
        var plateX = flywheel.Right + (gap * 0.5);
        var plate = new Rect(plateX, mid.Y - (tall * 0.42), unit * 0.5, tall * 0.84);
        CarParts.DrawEdgeOn(context, plate, _gearboxTurns, Lining, Stripe, 8, 2);
        var pressure = new Rect(plate.Right + (gap * 0.5), mid.Y - (tall * 0.44), unit * 0.7, tall * 0.88);
        CarParts.DrawEdgeOn(context, pressure, _engineTurns, CarParts.DarkMetalBrush, Stripe, 8);

        // A cover bolted to the flywheel holds the springs, which press the pressure plate towards the flywheel.
        var cover = new Rect(flywheel.Right + (unit * 3.3), mid.Y - (tall * 0.5), unit * 0.3, tall);
        context.FillRectangle(CarParts.DarkMetalBrush, cover);
        var springs = new List<Point>();
        foreach (var y in new[] { mid.Y - (tall * 0.3), mid.Y + (tall * 0.3) })
        {
            DrawSpring(context, pressure.Right, cover.X, y, unit * 0.25);
            springs.Add(new Point(pressure.Right, y - (unit * 0.3)));
            springs.Add(new Point(cover.X, y + (unit * 0.3)));
        }

        // The reader's handle on the pressure plate: drawn back against the springs as the pedal goes down. Past the
        // widest gap the pedal still goes on to the floor.
        Grip("pedal", new Point(pressure.Center.X, mid.Y), GripAxis.Horizontal, DiagramText.Get("Car_Grip_Pedal"),
            at => Pedal = PedalForGap((at.X - (pressure.Width / 2) - flywheel.Right - (unit * 0.5)) / widest));

        Mark("flywheel", flywheel);
        Mark("plate", plate);
        Mark("springs", springs, 2);
        DiagramText.Draw(context, DiagramText.Get("Car_Clutch_EngineSide"), new Point(flywheel.Center.X, flywheel.Y - 8), 12, DiagramText.Ink, 0.5, 1);
        DiagramText.Draw(context, DiagramText.Get("Car_Clutch_PlateLabel"), new Point(plate.Center.X, plate.Bottom + 8), 12, DiagramText.Gold, 0.5, 0);
        DiagramText.Draw(context, DiagramText.Get("Car_Clutch_ToGearbox"), new Point(bounds.Right - 14, mid.Y + (unit * 0.4)), 11.5, DiagramText.Faint, 1, 0);

        // The speeds of the two sides, and the heat while they slip.
        var area = new Rect(bounds.X + 24, bounds.Height * 0.66, bounds.Width - 48, bounds.Height * 0.28);
        var rows = new[]
        {
            (DiagramText.Format("Car_Clutch_EngineSpeed", (_clutch.Engine * 60).ToString("0")), _clutch.Engine, DiagramText.Ink),
            (DiagramText.Format("Car_Clutch_GearboxSpeed", (_clutch.Gearbox * 60).ToString("0")), _clutch.Gearbox, DiagramText.Gold),
        };
        var y0 = area.Y;
        foreach (var (words, speed, brush) in rows)
        {
            DiagramText.Draw(context, words, new Point(area.X, y0), 12.5, brush);
            var bar = new Rect(area.X, y0 + 20, area.Width, 8);
            context.FillRectangle(CarParts.Track, bar, 3);
            context.FillRectangle(brush, new Rect(bar.X, bar.Y, bar.Width * Math.Clamp(speed / (Clutch.EngineTarget * 1.2), 0, 1), bar.Height), 3);
            y0 += 42;
        }

        Mark("speeds", new Rect(area.X, area.Y, area.Width, 70));
        var state = Pedal >= 0.8 ? "Car_Clutch_StateApart" : _clutch.Gripping ? "Car_Clutch_StateGrip" : "Car_Clutch_StateSlip";
        DiagramText.Draw(context, DiagramText.Get(state), new Point(area.X, y0 + 4), 14, _clutch.Gripping ? DiagramText.Ink : CarParts.Hot, bold: true);
        if (_clutch.Heat > 0.05)
        {
            DiagramText.Draw(context, DiagramText.Format("Car_Clutch_Heat", _clutch.Heat.ToString("0.0")), new Point(area.Right, y0 + 6), 12.5, CarParts.Hot, 1);
        }
    }

    private static void DrawSpring(DrawingContext context, double from, double to, double y, double half)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            const int Turns = 6;
            g.BeginFigure(new Point(from, y), false);
            for (var i = 1; i < Turns * 2; i++)
            {
                g.LineTo(new Point(from + ((to - from) * i / (Turns * 2)), y + (i % 2 == 0 ? -half : half)));
            }

            g.LineTo(new Point(to, y));
            g.EndFigure(false);
        }

        context.DrawGeometry(null, SpringPen, geometry);
    }
}
