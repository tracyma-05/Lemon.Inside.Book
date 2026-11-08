using Avalonia.Media.Immutable;
using Avalonia.Media;
using Avalonia;
using Avalonia.Data;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Gears;

/// <summary>
/// Spread 6: a planetary gear set seen face on. The part the reader chose is held still (and says so); the input is
/// turned steadily and the output follows at its own speed. The reader taps a part to hold it, and drags the sun's rim
/// to pick a bigger or smaller sun (the planets grow or shrink to fill the ring).
/// </summary>
public sealed class PlanetaryDiagram : GearPicture
{
    public static readonly StyledProperty<int> SunStepProperty =
        AvaloniaProperty.Register<PlanetaryDiagram, int>(nameof(SunStep), 2, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<PlanetHeld> HeldProperty =
        AvaloniaProperty.Register<PlanetaryDiagram, PlanetHeld>(nameof(Held), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Turns per second of the input part.</summary>
    private const double InputSpeed = 0.12;

    /// <summary>How far the ring reaches past its rolling circle, in modules.</summary>
    private const double RingRim = 3.5;

    private static readonly double Pressure = 20 * Math.PI / 180;

    private double _turns;

    static PlanetaryDiagram()
    {
        AffectsRender<PlanetaryDiagram>(SunStepProperty, HeldProperty);
    }

    /// <summary>Which of <see cref="PlanetarySpread.SunChoices"/> the sun has, as the slider steps through them.</summary>
    public int SunStep
    {
        get => GetValue(SunStepProperty);
        set => SetValue(SunStepProperty, value);
    }

    public int SunTeeth => PlanetarySpread.SunChoices[Math.Clamp(SunStep, 0, PlanetarySpread.SunChoices.Count - 1)];

    public PlanetHeld Held
    {
        get => GetValue(HeldProperty);
        set => SetValue(HeldProperty, value);
    }

    protected override void Advance(double seconds) => _turns += InputSpeed * seconds;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var sunTeeth = SunTeeth;
        var planetTeeth = Planetary.PlanetTeeth(sunTeeth);
        var ringRadius = GearGeometry.PitchRadius(Planetary.RingTeeth);
        var outer = ringRadius + RingRim;
        var scale = Math.Min(bounds.Width * 0.9, bounds.Height * 0.8) / (2 * outer + 2);
        var centre = new Point(bounds.Center.X, bounds.Height * 0.47);
        var (sun, carrier, planets, ring) = Planetary.Pose(sunTeeth, Held, _turns);
        var orbit = GearGeometry.PitchRadius(sunTeeth) + GearGeometry.PitchRadius(planetTeeth);

        // The ring, with a tab on its rim so its turning shows.
        var (input, output) = Planetary.Parts(Held);
        var ringColor = ColorOf(PlanetHeld.Ring, input, output);
        using (context.PushTransform(Placement(centre, scale, ring)))
        {
            context.DrawGeometry(new ImmutableSolidColorBrush(Shade(ringColor, 0.8)),
                new ImmutablePen(new ImmutableSolidColorBrush(Shade(ringColor, 0.45)), 1.2 / scale), RingShape(Planetary.RingTeeth, Pressure, outer));
        }

        var tab = ToScreen(new Point(Math.Cos(ring) * (outer - 1.2), Math.Sin(ring) * (outer - 1.2)), centre, scale);
        context.DrawEllipse(new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E)), null, tab, Math.Max(3, scale * 0.7), Math.Max(3, scale * 0.7));

        // The planets on their carrier (a triangle through their axles).
        var axles = planets.Select(p => ToScreen(new Point(Math.Cos(p.Towards) * orbit, Math.Sin(p.Towards) * orbit), centre, scale)).ToArray();
        foreach (var (p, at) in planets.Zip(axles))
        {
            DrawGear(context, planetTeeth, Pressure, at, scale, p.Spin, Steel, mark: false);
        }

        var frame = new StreamGeometry();
        using (var g = frame.Open())
        {
            g.BeginFigure(axles[0], true);
            g.LineTo(axles[1]);
            g.LineTo(axles[2]);
            g.EndFigure(true);
        }

        var carrierColor = ColorOf(PlanetHeld.Carrier, input, output);
        context.DrawGeometry(new ImmutableSolidColorBrush(Shade(carrierColor, 1, 0.35)), new ImmutablePen(new ImmutableSolidColorBrush(carrierColor), 3, lineJoin: PenLineJoin.Round), frame);
        foreach (var at in axles)
        {
            context.DrawEllipse(new ImmutableSolidColorBrush(carrierColor), null, at, Math.Max(4, scale * 1.1), Math.Max(4, scale * 1.1));
        }

        DrawGear(context, sunTeeth, Pressure, centre, scale, sun, ColorOf(PlanetHeld.Sun, input, output));
        Mark("ring", centre, outer * scale);
        Mark("sun", centre, (GearGeometry.PitchRadius(sunTeeth) + 1) * scale);
        Mark("carrier", axles, pad: 10);

        // Which part is held, which is turned and which comes out.
        var text = 12.5;
        var held = Held switch
        {
            PlanetHeld.Ring => ToScreen(new Point(0, -outer), centre, scale) + new Point(0, 6),
            PlanetHeld.Sun => centre + new Point(0, Math.Max(GearGeometry.PitchRadius(sunTeeth) * scale * 0.45, 18)),
            _ => axles.OrderByDescending(a => a.Y).First() + new Point(0, Math.Max(8, scale * 1.6)),
        };
        DiagramText.Draw(context, DiagramText.Get("Gears_Planet_HeldLabel"), held, text, DiagramText.Ink, 0.5, 0, bold: true);
        var legend = new Point(bounds.X + 16, bounds.Y + 14);
        var r = DiagramText.Draw(context, DiagramText.Format("Gears_Planet_InLabel", DiagramText.Get($"Gears_Planet_{input}Name")), legend, text, DiagramText.Gold);
        DiagramText.Draw(context, DiagramText.Format("Gears_Planet_OutLabel", DiagramText.Get($"Gears_Planet_{output}Name")), new Point(legend.X, r.Bottom + 4), text, new ImmutableSolidColorBrush(Copper));

        // Tap a part to hold it: the sun at its hub, the ring on its rim (low on the right), the carrier at an axle.
        Grip("holdSun", centre, GripAxis.Tap, DiagramText.Get("Gears_Grip_HoldSun"), _ => Held = PlanetHeld.Sun);
        var rim = -40 * Math.PI / 180;
        Grip("holdRing", ToScreen(new Point(Math.Cos(rim) * (ringRadius + (RingRim / 2)), Math.Sin(rim) * (ringRadius + (RingRim / 2))), centre, scale),
            GripAxis.Tap, DiagramText.Get("Gears_Grip_HoldRing"), _ => Held = PlanetHeld.Ring);
        Grip("holdCarrier", axles[0], GripAxis.Tap, DiagramText.Get("Gears_Grip_HoldCarrier"), _ => Held = PlanetHeld.Carrier);

        // The sun's rim, straight up from its middle: drag it out for a bigger sun, in for a smaller one.
        Grip("sunTeeth", centre - new Vector(0, GearGeometry.PitchRadius(sunTeeth) * scale), GripAxis.Vertical, DiagramText.Get("Gears_Grip_SunTeeth"),
            at => SunStep = GearsGrips.Nearest(0, PlanetarySpread.SunChoices.Count - 1, centre.Y - at.Y,
                i => GearGeometry.PitchRadius(PlanetarySpread.SunChoices[i]) * scale));
    }

    /// <summary>Gold for the part turned by hand, copper for the one that comes out, grey for the one held.</summary>
    private static Color ColorOf(PlanetHeld part, PlanetHeld input, PlanetHeld output) =>
        part == input ? Gold : part == output ? Copper : Color.FromRgb(0x6A, 0x74, 0x84);
}
