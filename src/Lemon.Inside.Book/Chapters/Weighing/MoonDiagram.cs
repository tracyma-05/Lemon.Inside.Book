using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;
using static Lemon.Inside.Book.Chapters.Weighing.WeighingParts;

namespace Lemon.Inside.Book.Chapters.Weighing;

/// <summary>
/// Spread 6: the same bag weighed two ways, on the Earth, the Moon or Mars, in centimetres. On the left a spring scale
/// stretches as far as gravity pulls; on the right a balance compares the bag with iron weights, both pulled alike, and
/// stays level wherever it is.
/// </summary>
public sealed class MoonDiagram : GearPicture
{
    public static readonly StyledProperty<double> MassProperty =
        AvaloniaProperty.Register<MoonDiagram, double>(nameof(Mass), 1);

    public static readonly StyledProperty<int> PlaceProperty =
        AvaloniaProperty.Register<MoonDiagram, int>(nameof(Place));

    private static readonly IImmutableBrush[] Grounds =
    [
        new ImmutableSolidColorBrush(Color.FromRgb(0x4A, 0x6A, 0x3A)),
        new ImmutableSolidColorBrush(Color.FromRgb(0x84, 0x84, 0x8A)),
        new ImmutableSolidColorBrush(Color.FromRgb(0x9A, 0x52, 0x32)),
    ];

    static MoonDiagram()
    {
        AffectsRender<MoonDiagram>(MassProperty, PlaceProperty);
    }

    /// <summary>The bag's mass, kg.</summary>
    public double Mass
    {
        get => GetValue(MassProperty);
        set => SetValue(MassProperty, value);
    }

    /// <summary>0 the Earth, 1 the Moon, 2 Mars.</summary>
    public int Place
    {
        get => GetValue(PlaceProperty);
        set => SetValue(PlaceProperty, value);
    }

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var mass = Math.Clamp(Mass, 0.5, 5);
        var place = Math.Clamp(Place, 0, 2);
        var springReads = Worlds.SpringReads(mass, place);
        var scale = Math.Min((bounds.Width - 50) / 84, (bounds.Height - 150) / 54);
        var v = new View(new Point(bounds.Center.X, bounds.Center.Y + (24 * scale)), scale);
        var bagSize = 6 + (mass * 1.2);

        // The ground of the world we are on.
        var ground = new Rect(bounds.X, v.At(0, 0).Y, bounds.Width, bounds.Bottom - v.At(0, 0).Y);
        context.FillRectangle(Grounds[place], ground);
        Mark("ground", new Rect(ground.X, ground.Y, ground.Width, Math.Min(ground.Height, 40)));
        DiagramText.Draw(context, DiagramText.Format("Weighing_Moon_GroundLabel", DiagramText.Get(MoonSpread.PlaceKey(place)), Number(Worlds.Gravity[place], "0.##")),
            new Point(bounds.Center.X, ground.Top + 10), 13, DiagramText.Ink, 0.5, 0, bold: true);

        // Left: a spring scale on a stand.
        context.DrawRectangle(Wood, Edge, v.Box(-38, 0, -36, 46));
        context.DrawRectangle(Wood, Edge, v.Box(-38, 44, -20, 46));
        var bottom = 38 - (springReads * Spring.PerKg);
        var coil = DrawCoil(context, v.At(-26, 44), v.At(-26, bottom), 3 * scale, 10, SteelBrush);
        Mark("spring", coil, 4);
        var plate = v.Box(-20, 38 - (10 * Spring.PerKg) - 1, -16, 44);
        context.DrawRectangle(Face, Edge, plate, 2, 2);
        for (var kg = 0; kg <= 10; kg += 2) context.DrawLine(new ImmutablePen(Dark, 1), v.At(-20, 38 - (kg * Spring.PerKg)), v.At(-18.5, 38 - (kg * Spring.PerKg)));
        context.DrawLine(new ImmutablePen(DiagramText.Gold, 2.5, null, PenLineCap.Round), v.At(-26, bottom), v.At(-19, bottom));
        var hook = v.At(-26, bottom - 2.5);
        context.DrawLine(new ImmutablePen(SteelBrush, 2), v.At(-26, bottom), hook);
        var bag = DrawSack(context, new Rect(hook.X - (bagSize * 0.4 * scale), hook.Y, bagSize * 0.8 * scale, bagSize * 1.1 * scale), Kilos(mass, "0.#"));
        Mark("bag", bag);
        DiagramText.Draw(context, DiagramText.Format("Weighing_Moon_SpringLabel", Kilos(springReads, "0.##")), new Point(v.At(-26, 0).X, v.At(0, 52).Y), 13,
            place == 0 ? DiagramText.Ink : Warn, 0.5, 0, bold: true);

        // Right: a balance, the same bag against iron weights, level whatever the gravity.
        const double cx = 22;
        context.DrawRectangle(Wood, Edge, v.Box(cx - 9, 0, cx + 9, 1.5), 2, 2);
        context.DrawRectangle(Brass, Edge, v.Box(cx - 0.8, 1.5, cx + 0.8, 32));
        context.DrawRectangle(SteelBrush, Edge, v.Box(cx - 13, 32, cx + 13, 33.2));
        DrawPolygon(context, DiagramText.Gold, Edge, [v.At(cx, 33.2), v.At(cx - 1, 31.6), v.At(cx + 1, 31.6)]);
        foreach (var side in new[] { -1, 1 })
        {
            var hang = v.At(cx + (side * 12), 32);
            var rim = v.At(cx + (side * 12), 20);
            context.DrawLine(ThreadPen, hang, new Point(rim.X - (4 * scale), rim.Y));
            context.DrawLine(ThreadPen, hang, new Point(rim.X + (4 * scale), rim.Y));
            DrawPan(context, rim, 10 * scale);
            var size = Math.Min(bagSize, 9);
            if (side < 0)
            {
                DrawSack(context, new Rect(rim.X - (size * 0.4 * scale), rim.Y - (size * scale), size * 0.8 * scale, size * scale), Kilos(mass, "0.#"));
            }
            else
            {
                var weight = new Rect(rim.X - (size * 0.4 * scale), rim.Y - (size * 0.7 * scale), size * 0.8 * scale, size * 0.7 * scale);
                context.DrawRectangle(Iron, Edge, weight, 3, 3);
                DiagramText.Draw(context, Kilos(mass, "0.#"), weight.Center, 11, DiagramText.Ink, 0.5, 0.5, bold: true);
            }
        }

        Mark("balance", v.Box(cx - 14, 0, cx + 14, 34));
        DiagramText.Draw(context, DiagramText.Format("Weighing_Moon_BalanceLabel", Kilos(Worlds.BalanceReads(mass), "0.##")), new Point(v.At(cx, 0).X, v.At(0, 52).Y), 13,
            DiagramText.Gold, 0.5, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Weighing_Moon_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
