using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Lifting;

/// <summary>
/// Spread 3: a block and tackle with its wheels drawn side by side (in a real block they share one axle), so every
/// strand can be seen. The hand's rope comes down on the left; the strands run between the top block and the bottom
/// one, the last tied to whichever block it ends at. Each strand is labelled with what it carries; with rubbing
/// wheels the strands nearer the hand carry more. The reader drags the hand to pull rope, drags the last strand
/// sideways for more strands and taps the top block to make the wheels rub. The rope runs smoothly and every wheel
/// turns as it passes: the first as fast as the hand's rope, each one after more slowly, the last barely at all.
/// </summary>
public sealed class TackleDiagram : GearPicture
{
    public static readonly StyledProperty<int> StrandsProperty =
        AvaloniaProperty.Register<TackleDiagram, int>(nameof(Strands), 1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> FrictionProperty =
        AvaloniaProperty.Register<TackleDiagram, bool>(nameof(Friction), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> PullProperty =
        AvaloniaProperty.Register<TackleDiagram, double>(nameof(Pull), defaultBindingMode: BindingMode.TwoWay);

    private const double Beam = 4.4;
    private const double Top = 3.95;
    private const double Bottom = 1.05;
    private const double Gap = 0.26;
    private const double HandStart = 3.2;

    /// <summary>The rope pulled as drawn, easing toward the value set.</summary>
    private double _pull = double.NaN;

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

    private double SetPull => Math.Clamp(Pull, 0, TackleSpread.MostPull);

    protected override void Advance(double seconds) =>
        _pull = double.IsNaN(_pull) ? SetPull : LiftingParts.Ease(_pull, SetPull, Math.Max(0.6, Math.Abs(SetPull - _pull) * 3), seconds);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var n = Math.Clamp(Strands, 1, Tackle.MostStrands);
        var pull = double.IsNaN(_pull) ? SetPull : _pull;
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

        // Wheel j (j = 0 the first, under the hand's rope) passes the rope at (n − j)/n of the hand's speed; the top
        // wheels turn one way, the bottom ones the other.
        var spoke = new ImmutablePen(Dark, 1.8);
        void Turning(Point c, int j, int way)
        {
            DrawWheel(context, c, r * 0.92);
            var (s, co) = Math.SinCos(way * WheelTurn(n, j, pull) / (Gap / 2));
            context.DrawLine(spoke, new Point(c.X - (co * r * 0.6), c.Y - (s * r * 0.6)), new Point(c.X + (co * r * 0.6), c.Y + (s * r * 0.6)));
        }

        for (var k = 0; k < n; k += 2)
        {
            var c = v.At((X(k) + X(k + 1)) / 2, Top);
            Turning(c, k, -1);
            DrawRopeArc(context, c, r, over: true);
        }

        for (var k = 1; k < n; k += 2)
        {
            var c = v.At((X(k) + X(k + 1)) / 2, bottom);
            Turning(c, k, 1);
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
        var handAt = v.At(X(0), HandStart - pull);
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

        // The reader's handles: the hand pulls, the last strand is dragged out sideways for more strands (or in for
        // fewer), and a tap on the top block makes its wheels rub or run free.
        var centreX = bounds.Center.X;
        Grip("pull", handBox.Center, GripAxis.Vertical, DiagramText.Get("Lifting_Grip_Pull"),
            at => Pull = Math.Clamp(Math.Round((HandStart - v.Y(at)) / 0.05) * 0.05, 0, TackleSpread.MostPull));
        var lastTop = v.At(X(n), Top);
        var lastBottom = v.At(X(n), bottom);
        Grip("strands", new Point(lastTop.X, lastTop.Y + ((lastBottom.Y - lastTop.Y) * 0.25)), GripAxis.Horizontal,
            DiagramText.Get("Lifting_Grip_Strands"), at => Strands = StrandsAt(at.X - centreX, scale));
        Grip("friction", new Point(topBlock.Left - 16, topBlock.Center.Y), GripAxis.Tap, DiagramText.Get("Lifting_Grip_Friction"),
            _ => Friction = !Friction);
    }

    /// <summary>
    /// How far the rope has run round wheel <paramref name="wheel"/> (0 the first, under the hand's rope) of a tackle of
    /// <paramref name="strands"/>, m, for <paramref name="pulled"/> m pulled: the first as far as the hand's rope, each
    /// one after a strand's worth less, since its block moves with the rope.
    /// </summary>
    internal static double WheelTurn(int strands, int wheel, double pulled) => pulled * (strands - wheel) / strands;

    /// <summary>
    /// The strands for the last strand dragged to <paramref name="fromCentre"/> pixels right of the picture's middle:
    /// the picture keeps the tackle centred, so the last strand of n sits n·gap/2 right of the middle.
    /// </summary>
    internal static int StrandsAt(double fromCentre, double scale) =>
        Math.Clamp((int)Math.Round(fromCentre / (Gap / 2 * scale)), 1, Tackle.MostStrands);
}
