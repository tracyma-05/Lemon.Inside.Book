using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Lifting;

/// <summary>
/// Spread 3: a block and tackle with its wheels drawn side by side (in a real block they share one axle), so every
/// strand can be seen. The hand's rope comes down on the left; the strands run between the top block and the bottom
/// one, the last tied to whichever block it ends at. Each strand is labelled with what it carries; with rubbing
/// wheels the strands nearer the hand carry more.
/// </summary>
public sealed class TackleDiagram : GearPicture
{
    public static readonly StyledProperty<int> StrandsProperty =
        AvaloniaProperty.Register<TackleDiagram, int>(nameof(Strands), 1);

    public static readonly StyledProperty<bool> FrictionProperty =
        AvaloniaProperty.Register<TackleDiagram, bool>(nameof(Friction));

    public static readonly StyledProperty<double> PullProperty =
        AvaloniaProperty.Register<TackleDiagram, double>(nameof(Pull));

    private const double Beam = 4.4;
    private const double Top = 3.95;
    private const double Bottom = 1.05;
    private const double Gap = 0.26;

    static TackleDiagram()
    {
        AffectsRender<TackleDiagram>(StrandsProperty, FrictionProperty, PullProperty);
    }

    public int Strands
    {
        get => GetValue(StrandsProperty);
        set => SetValue(StrandsProperty, value);
    }

    public bool Friction
    {
        get => GetValue(FrictionProperty);
        set => SetValue(FrictionProperty, value);
    }

    public double Pull
    {
        get => GetValue(PullProperty);
        set => SetValue(PullProperty, value);
    }

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var n = Math.Clamp(Strands, 1, Tackle.MostStrands);
        var pull = Math.Clamp(Pull, 0, TackleSpread.MostPull);
        var rise = pull / n;
        var scale = (bounds.Height - 50) / (Beam + 0.3);
        var x0 = 1.0;
        var v = new View(new Point(bounds.Center.X - ((x0 + (n * Gap / 2)) * scale), bounds.Bottom - 20), scale);
        var r = Gap / 2 * scale;
        var bottom = Bottom + rise;

        context.FillRectangle(Ground, new Rect(bounds.X, v.At(0, 0).Y, bounds.Width, bounds.Height));
        context.DrawRectangle(Wood, Edge, new Rect(bounds.X + 10, v.At(0, Beam + 0.18).Y, bounds.Width - 20, 0.18 * scale));

        double X(int k) => x0 + (k * Gap); // k = 0 is the hand's rope, 1..n the strands

        // The blocks: plates round the wheels.
        var (topWheels, bottomWheels) = Tackle.Wheels(n);
        var topBlock = v.Box(X(0) - 0.12, Top - 0.2, X(Math.Max(1, (2 * topWheels) - 1)) + 0.12, Top + 0.2);
        context.DrawLine(new ImmutablePen(SteelBrush, 3), new Point(topBlock.Center.X, topBlock.Top), new Point(topBlock.Center.X, v.At(0, Beam).Y));
        context.DrawRectangle(DarkSteelBrush, Edge, topBlock, 6, 6);
        Mark("blocks", topBlock);
        var bottomBlock = bottomWheels > 0
            ? v.Box(X(1) - 0.12, bottom - 0.2, X(2 * bottomWheels) + 0.12, bottom + 0.2)
            : v.Box(X(1) - 0.08, bottom - 0.12, X(1) + 0.08, bottom);
        context.DrawRectangle(DarkSteelBrush, Edge, bottomBlock, 6, 6);

        // Tensions along the rope from the hand: each wheel passed keeps e of it.
        var hand = Tackle.Effort(Tackle.Load, n, Friction);
        var e = Friction ? Tackle.Efficiency : 1;

        // The strands, then the wheels on top of them with the rope round each.
        var strandPoints = new List<Point>();
        for (var k = 1; k <= n; k++)
        {
            var a = v.At(X(k), Top);
            var b = v.At(X(k), bottom);
            DrawRope(context, a, b);
            strandPoints.Add(a);
            strandPoints.Add(b);
            var tension = hand * Math.Pow(e, k);
            DiagramText.Draw(context, Kg(tension), new Point(a.X, ((a.Y + b.Y) / 2) + ((k % 2 == 0 ? 1 : -1) * 12)), 11,
                DiagramText.Gold, 0.5, 0.5, bold: true);
        }

        for (var k = 0; k < n; k += 2)
        {
            var c = v.At((X(k) + X(k + 1)) / 2, Top);
            DrawWheel(context, c, r * 0.92);
            DrawRopeArc(context, c, r, over: true);
        }

        for (var k = 1; k < n; k += 2)
        {
            var c = v.At((X(k) + X(k + 1)) / 2, bottom);
            DrawWheel(context, c, r * 0.92);
            DrawRopeArc(context, c, r, over: false);
        }

        // The rope's dead end, tied to the block the last strand reaches.
        context.DrawEllipse(DiagramText.Gold, Edge, v.At(X(n), n % 2 == 1 ? bottom : Top), 4, 4);
        Mark("strands", strandPoints, 6);

        // The load under the bottom block.
        DrawHook(context, new Point(bottomBlock.Center.X, bottomBlock.Bottom), 0.1 * scale);
        var load = v.Box(((X(1) + X(n)) / 2) - 0.4, bottom - 0.95, ((X(1) + X(n)) / 2) + 0.4, bottom - 0.32);
        Mark("load", DrawWeight(context, load, Kg(Tackle.Load), LiftingParts.Load));

        // The hand's rope and the hand.
        var handAt = v.At(X(0), 3.2 - pull);
        DrawRope(context, v.At(X(0), Top), handAt);
        var handBox = DrawHand(context, handAt, 0.26 * scale);
        Mark("hand", handBox);
        DrawForce(context, new Point(handBox.Left - 12, handBox.Top), new Point(handBox.Left - 12, handBox.Top + (Math.Clamp(hand / Tackle.Load, 0.12, 1.1) * 0.9 * scale)),
            LiftingParts.Push, Kg(hand), -1);

        DiagramText.Draw(context, DiagramText.Format("Lifting_Up", (rise * 100).ToString("0", CultureInfo.CurrentCulture)),
            new Point(load.Right + 12, load.Center.Y), 13, DiagramText.Ink, 0, 0.5, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Lifting_Pulled", (pull * 100).ToString("0", CultureInfo.CurrentCulture)),
            new Point(handBox.Left - 12, handBox.Bottom + 6), 13, DiagramText.Ink, 1, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Lifting_Tackle_SideBySide"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
