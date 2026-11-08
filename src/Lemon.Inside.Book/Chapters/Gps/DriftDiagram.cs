using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Gps.GpsParts;

namespace Lemon.Inside.Book.Chapters.Gps;

/// <summary>
/// Spread 4: two clocks, one on the ground and one in orbit, each showing microseconds. The orbit clock creeps ahead
/// day by day: weaker gravity speeds it up (a long gold bar), its speed slows it a little (a short blue bar). Corrected
/// before launch, it keeps step with the ground. The reader drags the days bar and taps the correction switch.
/// </summary>
public sealed class DriftDiagram : GearPicture
{
    public static readonly StyledProperty<double> DaysProperty =
        AvaloniaProperty.Register<DriftDiagram, double>(nameof(Days), 1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> CorrectedProperty =
        AvaloniaProperty.Register<DriftDiagram, bool>(nameof(Corrected), defaultBindingMode: BindingMode.TwoWay);

    static DriftDiagram()
    {
        AffectsRender<DriftDiagram>(DaysProperty, CorrectedProperty);
    }

    public double Days
    {
        get => GetValue(DaysProperty);
        set => SetValue(DaysProperty, value);
    }

    public bool Corrected
    {
        get => GetValue(CorrectedProperty);
        set => SetValue(CorrectedProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var days = Math.Clamp(Days, 0, 7);
        var ahead = Clocks.Ahead(days, Corrected);
        var w = bounds.Width;

        // The two clocks: one turn of the hand is 100 µs.
        var radius = Math.Min(70, w * 0.13);
        var y = bounds.Y + 90 + radius;
        var ground = new Point(bounds.X + (w * 0.28), y);
        var orbit = new Point(bounds.X + (w * 0.72), y);
        DrawClock(context, ground, radius, 0, PlaceInk);
        DrawClock(context, orbit, radius, ahead / 100, ahead > 0.5 ? WarnInk : PlaceInk);
        DiagramText.Draw(context, DiagramText.Get("Gps_Drift_Ground"), new Point(ground.X, y - radius - 10), 12, DiagramText.Ink, 0.5, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Gps_Drift_Orbit"), new Point(orbit.X, y - radius - 10), 12, DiagramText.Ink, 0.5, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Gps_Us", "0"), new Point(ground.X, y + radius + 8), 12, DiagramText.Faint, 0.5);
        DiagramText.Draw(context, DiagramText.Format("Gps_Drift_Ahead", Number(ahead)), new Point(orbit.X, y + radius + 8), 12,
            ahead > 0.5 ? WarnInk : DiagramText.Faint, 0.5);
        DrawSatellite(context, orbit + new Vector(radius + 18, -radius + 6), 10);
        Mark("ground", ground, radius);
        Mark("orbit", orbit, radius);

        // A day's gain from gravity, and loss from speed, as bars from a middle line.
        var mid = bounds.Center.X;
        var perUs = (w * 0.4) / Clocks.Gravity;
        var by = y + radius + 70;
        DiagramText.Draw(context, DiagramText.Get("Gps_Drift_PerDay"), new Point(mid, by - 28), 12, DiagramText.Faint, 0.5);
        var gravity = new Rect(mid, by, Clocks.Gravity * perUs, 16);
        var motion = new Rect(mid - (Clocks.Motion * perUs), by + 30, Clocks.Motion * perUs, 16);
        context.FillRectangle(PlaceInk, gravity);
        context.FillRectangle(SignalInk, motion);
        context.DrawLine(Line(DiagramText.Faint, 1), new Point(mid, by - 8), new Point(mid, by + 54));
        DiagramText.Draw(context, DiagramText.Format("Gps_Drift_Gravity", Number(Clocks.Gravity, "0.0")), new Point(gravity.Right - 4, gravity.Bottom + 2),
            11.5, PlaceInk, 1, 0);
        DiagramText.Draw(context, DiagramText.Format("Gps_Drift_Motion", Number(Clocks.Motion, "0.0")), new Point(motion.X - 8, motion.Center.Y), 11.5,
            SignalInk, 1, 0.5);
        Mark("gravity", gravity.Inflate(4));
        Mark("motion", motion.Inflate(4));
        DiagramText.Draw(context, DiagramText.Format("Gps_Drift_Net", Number(Clocks.PerDay, "0.0")), new Point(mid, by + 66), 12, DiagramText.Ink, 0.5);

        // What that does to a position.
        DiagramText.Draw(context, DiagramText.Format(Corrected ? "Gps_Drift_Kept" : "Gps_Drift_Off", Km(Clocks.DriftKm(days, Corrected))),
            new Point(mid, by + 100), 14, Corrected ? PlaceInk : WarnInk, 0.5, 0, bold: true);

        // The days bar and the correction switch.
        var barLeft = new Point(bounds.X + 30, Math.Min(bounds.Bottom - 110, by + 170));
        var barWidth = w - 60;
        var knob = Tesseract.TesseractParts.DrawBar(context, barLeft, barWidth, days / 7, DiagramText.Get("Gps_Drift_Days"));
        Grip("days", knob, GripAxis.Horizontal, DiagramText.Get("Gps_Grip_Days"),
            at => Days = Math.Round(Tesseract.TesseractParts.Along(at, barLeft, barWidth) * 70) / 10);
        var sw = Colour.ColourParts.DrawSwitch(context, new Point(mid, barLeft.Y + 40),
            DiagramText.Get(Corrected ? "Gps_Drift_SwitchOn" : "Gps_Drift_SwitchOff"), Corrected);
        Grip("corrected", new Point(sw.Left - 2, sw.Center.Y), GripAxis.Tap, DiagramText.Get("Gps_Grip_Corrected"), _ => Corrected = !Corrected);
        DiagramText.Draw(context, DiagramText.Get("Gps_Drift_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
