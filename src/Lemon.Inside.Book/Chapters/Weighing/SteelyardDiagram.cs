using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;
using static Lemon.Inside.Book.Chapters.Weighing.WeighingParts;

namespace Lemon.Inside.Book.Chapters.Weighing;

/// <summary>
/// Spread 2: a steelyard hanging from a beam, in centimetres. The sack hangs 5 cm left of the pivot; the 1 kg weight
/// slides along the long arm, which is marked in kilograms. Out of balance, the arm tips toward the heavier side.
/// </summary>
public sealed class SteelyardDiagram : GearPicture
{
    public static readonly StyledProperty<double> SackProperty =
        AvaloniaProperty.Register<SteelyardDiagram, double>(nameof(Sack), 8);

    public static readonly StyledProperty<double> PositionProperty =
        AvaloniaProperty.Register<SteelyardDiagram, double>(nameof(Position), 20);

    /// <summary>The pivot's height, cm.</summary>
    private const double Pivot = 40;

    static SteelyardDiagram()
    {
        AffectsRender<SteelyardDiagram>(SackProperty, PositionProperty);
    }

    /// <summary>The sack's mass, kg.</summary>
    public double Sack
    {
        get => GetValue(SackProperty);
        set => SetValue(SackProperty, value);
    }

    /// <summary>The sliding weight's distance from the pivot, cm.</summary>
    public double Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var sack = Math.Clamp(Sack, 2, 18);
        var position = Math.Clamp(Position, 0, Steelyard.LongArm);
        var tilt = -Steelyard.Tilt(sack, position) * Math.PI / 180; // the sack's side down is the arm turning anticlockwise
        var scale = Math.Min((bounds.Width - 70) / 128, (bounds.Height - 140) / 56);
        var v = new View(new Point(bounds.Center.X - (42 * scale), bounds.Center.Y + (26 * scale)), scale);

        Point OnArm(double x, double y) => v.At((x * Math.Cos(tilt)) + (y * Math.Sin(tilt)), Pivot - (x * Math.Sin(tilt)) + (y * Math.Cos(tilt)));

        // The beam above and the pivot's hanger.
        context.DrawRectangle(Wood, Edge, v.Box(-20, Pivot + 12, 30, Pivot + 15));
        context.DrawLine(new ImmutablePen(SteelBrush, 2.5), v.At(0, Pivot + 12), v.At(0, Pivot + 1));
        var pivot = v.At(0, Pivot);

        // The arm, marked every 5 cm, numbered in kilograms every 10.
        Point[] arm = [OnArm(-9, 1.1), OnArm(Steelyard.LongArm + 2, 0.7), OnArm(Steelyard.LongArm + 2, -0.7), OnArm(-9, -1.1)];
        DrawPolygon(context, SteelBrush, Edge, arm);
        var tick = new ImmutablePen(Dark, 1);
        for (var x = 5; x <= Steelyard.LongArm; x += 5)
        {
            context.DrawLine(tick, OnArm(x, 0.7), OnArm(x, x % 10 == 0 ? -0.2 : 0.2));
            if (x % 20 == 0)
            {
                DiagramText.Draw(context, Number(Steelyard.Reading(x), "0"), OnArm(x, 2.2), 10.5, DiagramText.Faint, 0.5, 1);
            }
        }

        Mark("arm", [arm[0], arm[1]], 8);
        context.DrawEllipse(DiagramText.Gold, Edge, pivot, Math.Max(4, 0.9 * scale), Math.Max(4, 0.9 * scale));
        Mark("pivot", pivot, Math.Max(10, 2 * scale));

        // The sack on its hook, 5 cm the other side of the pivot.
        var hook = OnArm(-Steelyard.ShortArm, 0);
        var sackTop = new Point(hook.X, hook.Y + (9 * scale));
        context.DrawLine(new ImmutablePen(SteelBrush, 1.8), hook, sackTop);
        var size = 8 + (sack * 0.5);
        var sackBox = DrawSack(context, new Rect(sackTop.X - (size * 0.45 * scale), sackTop.Y, size * 0.9 * scale, size * 1.2 * scale), Kilos(sack));
        Mark("hook", sackBox);

        // The sliding weight, hanging from its ring.
        var ring = OnArm(position, 0);
        var weightTop = new Point(ring.X, ring.Y + (4 * scale));
        context.DrawEllipse(null, new ImmutablePen(Brass, 2), ring, 1.2 * scale, 1.2 * scale);
        context.DrawLine(new ImmutablePen(SteelBrush, 1.5), ring, weightTop);
        var weight = new Rect(weightTop.X - (2.5 * scale), weightTop.Y, 5 * scale, 6 * scale);
        context.DrawRectangle(Iron, Edge, weight, 3, 3);
        DiagramText.Draw(context, Kilos(Steelyard.Poise), weight.Center, 10.5, DiagramText.Ink, 0.5, 0.5, bold: true);
        Mark("poise", weight.Union(new Rect(ring, ring).Inflate(1.2 * scale)));

        DiagramText.Draw(context, DiagramText.Format("Weighing_Steelyard_ReadsLabel", Kilos(Steelyard.Reading(position), "0.#")),
            new Point(weight.Center.X, weight.Bottom + 6), 13, Steelyard.Balanced(sack, position) ? DiagramText.Gold : DiagramText.Ink, 0.5, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Weighing_Steelyard_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
