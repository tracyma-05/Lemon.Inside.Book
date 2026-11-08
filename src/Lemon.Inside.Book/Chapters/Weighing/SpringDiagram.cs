using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;
using static Lemon.Inside.Book.Chapters.Weighing.WeighingParts;
using Bounce = Lemon.Inside.Book.Chapters.Bridges.Bounce;

namespace Lemon.Inside.Book.Chapters.Weighing;

/// <summary>
/// Spread 3: a spring balance, in centimetres. The coil hangs from the top ring; its bottom carries the pointer along a
/// scale marked 2 cm to the kilogram, and the hook below it. Past 6 kg the coil is stretched for good: it is drawn red,
/// and with the load taken off its pointer no longer comes back to zero. Hang a load on or take it off and it bobs
/// up and down on the coil before it settles, a heavy load more slowly than a light one. The reader drags the load's
/// bottom down to hang more on, and taps the hook to lift the load off or hang it back.
/// </summary>
public sealed class SpringDiagram : GearPicture
{
    public static readonly StyledProperty<double> MassProperty =
        AvaloniaProperty.Register<SpringDiagram, double>(nameof(Mass), 2, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> OffProperty =
        AvaloniaProperty.Register<SpringDiagram, bool>(nameof(Off), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> HighestProperty =
        AvaloniaProperty.Register<SpringDiagram, double>(nameof(Highest), 2);

    /// <summary>The coil's top and its bottom unloaded, cm; the scale's zero is at the unloaded bottom.</summary>
    private const double Top = 46;
    private const double Zero = 37;

    /// <summary>The drawn stretch, cm, bobbing toward the coil's stretch; null until the first frame.</summary>
    private Bounce? _stretch;

    static SpringDiagram()
    {
        AffectsRender<SpringDiagram>(MassProperty, OffProperty, HighestProperty);
    }

    public double Mass
    {
        get => GetValue(MassProperty);
        set => SetValue(MassProperty, value);
    }

    public bool Off
    {
        get => GetValue(OffProperty);
        set => SetValue(OffProperty, value);
    }

    /// <summary>The heaviest load it has carried, kg.</summary>
    public double Highest
    {
        get => GetValue(HighestProperty);
        set => SetValue(HighestProperty, value);
    }

    private double Load => Off ? 0 : Math.Clamp(Mass, 0, 8);

    private double Settled => Spring.Stretch(Load, Math.Max(Highest, Load));

    /// <summary>
    /// A weight on a spring bobs at √(stiffness / mass): the coil stretches 2 cm a kilogram, so a 1 kg load would bob
    /// about three and a half times a second, slowed here to be seen. A heavier load bobs more slowly; the coil alone, quickly.
    /// </summary>
    protected override void Advance(double seconds)
    {
        var frequency = Math.Clamp(Math.Sqrt(9.81 / (Spring.PerKg / 100)) / 4 / Math.Sqrt(Math.Max(Load, 0.3)), 2, 10);
        _stretch = (_stretch ?? Bounce.At(Settled)).Step(Settled, seconds, frequency, 0.08);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var load = Load;
        var highest = Math.Max(Highest, load);
        var stretch = Math.Max(-2, (_stretch ??= Bounce.At(Settled)).Value);
        var bottom = Zero - stretch;
        var scale = Math.Min((bounds.Width - 60) / 40, (bounds.Height - 130) / 52);
        var v = new View(new Point(bounds.Center.X, bounds.Center.Y + (24 * scale)), scale);

        // The ring it hangs from, and the scale plate beside the coil.
        context.DrawEllipse(null, new ImmutablePen(SteelBrush, 2.5), v.At(0, Top + 2), 1.6 * scale, 1.6 * scale);
        var plate = v.Box(3, Zero - 18, 11, Top);
        context.DrawRectangle(Face, Edge, plate, 3, 3);
        var tick = new ImmutablePen(Dark, 1);
        for (var kg = 0; kg <= 8; kg++)
        {
            var y = Zero - (kg * Spring.PerKg);
            context.DrawLine(tick, v.At(3, y), v.At(5, y));
            DiagramText.Draw(context, Number(kg, "0"), v.At(5.6, y), 10.5, Dark, 0, 0.5);
        }

        // Up to here it springs back: a mark at the limit.
        var limit = Zero - (Spring.Limit * Spring.PerKg);
        context.DrawLine(new ImmutablePen(Warn, 2), v.At(3, limit), v.At(11, limit));
        DiagramText.Draw(context, DiagramText.Get("Weighing_Spring_LimitLabel"), v.At(11.8, limit), 11, Warn, 0, 0.5);
        Mark("scale", plate);

        // The coil: red once it has been stretched past its limit.
        var damaged = Spring.Set(highest) > 0;
        var coil = DrawCoil(context, v.At(0, Top), v.At(0, bottom), 3.2 * scale, 12, damaged ? Warn : SteelBrush);
        Mark("spring", coil, 4);

        // The pointer at the coil's foot, and the hook and the load below.
        var foot = v.At(0, bottom);
        var tip = v.At(3.2, bottom);
        context.DrawLine(new ImmutablePen(DiagramText.Gold, 2.5, null, PenLineCap.Round), foot, tip);
        Mark("pointer", tip, Math.Max(10, 1.4 * scale));
        var hookTop = v.At(0, bottom - 3);
        context.DrawLine(new ImmutablePen(SteelBrush, 2), foot, hookTop);
        DrawHook(context, new Point(hookTop.X + (0.6 * scale), hookTop.Y), 1.2 * scale);
        Mark("hook", new Rect(hookTop.X - (1.5 * scale), foot.Y, 3 * scale, hookTop.Y - foot.Y + (1.4 * scale)));
        if (load > 0.01)
        {
            var size = 5 + (load * 0.6);
            var top = new Point(hookTop.X, hookTop.Y + (1.2 * scale));
            var box = new Rect(top.X - (size * 0.5 * scale), top.Y, size * scale, size * 0.8 * scale);
            context.DrawRectangle(Iron, Edge, box, 3, 3);
            DiagramText.Draw(context, Kilos(load, "0.#"), new Point(top.X, top.Y + (size * 0.4 * scale)), 12, DiagramText.Ink, 0.5, 0.5, bold: true);

            // Its bottom, dragged down to hang more on: the load settles with its bottom where the pointer is.
            double BottomY(double kg) =>
                v.At(0, Zero - Spring.Stretch(kg, Math.Max(Highest, kg)) - 3).Y + (1.2 * scale) + ((5 + (kg * 0.6)) * 0.8 * scale);
            Grip("load", new Point(box.Center.X, box.Bottom), GripAxis.Vertical, DiagramText.Get("Weighing_Grip_Load"),
                at => Mass = Math.Round(Solve(BottomY, at.Y, 0, 8) * 10) / 10);
        }

        // The hook's rod, tapped to lift the load off or hang it back.
        Grip("off", new Point(foot.X, (foot.Y + hookTop.Y) / 2), GripAxis.Tap, DiagramText.Get(Off ? "Weighing_Grip_HangOn" : "Weighing_Grip_LiftOff"),
            _ => Off = !Off);

        DiagramText.Draw(context, DiagramText.Format("Weighing_Spring_ReadsLabel", Kilos(Math.Max(0, stretch / Spring.PerKg), "0.#")), new Point(tip.X - (5.8 * scale), tip.Y), 13,
            damaged ? Warn : DiagramText.Gold, 1, 0.5, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Weighing_Spring_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
