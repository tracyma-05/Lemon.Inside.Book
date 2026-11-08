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
/// Spread 2: a bucket lifted by a rope. Fixed, the wheel hangs from the beam and the hand pulls down on the other end;
/// moving, the wheel rides on the bucket, one end of the rope tied to the beam and the hand pulling up the other. Each
/// strand holding the bucket is labelled with what it carries. The reader drags the hand to pull rope and taps the
/// wheel to move it between the beam and the bucket; the rope runs smoothly and the wheel turns as it goes (over a
/// fixed wheel as far as the rope, a moving one rolling up its tied strand only half as far).
/// </summary>
public sealed class PulleyDiagram : GearPicture
{
    public static readonly StyledProperty<bool> MovingProperty =
        AvaloniaProperty.Register<PulleyDiagram, bool>(nameof(Moving), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> PullProperty =
        AvaloniaProperty.Register<PulleyDiagram, double>(nameof(Pull), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The beam's height, m; the bucket's size and where it starts.</summary>
    private const double Beam = 4.2;
    private const double BucketSize = 0.55;
    private const double Floor = 0;
    private const double Wheel = 0.2;

    /// <summary>Where the hand starts, m above the ground: fixed (pulling down) and moving (pulling up).</summary>
    private const double FixedHand = 3.2;
    private const double MovingHand = 1.9;

    /// <summary>The rope pulled as drawn, easing toward the value set.</summary>
    private double _pull = double.NaN;

    static PulleyDiagram()
    {
        AffectsRender<PulleyDiagram>(MovingProperty, PullProperty);
    }

    public bool Moving
    {
        get => GetValue(MovingProperty);
        set => SetValue(MovingProperty, value);
    }

    /// <summary>Rope pulled through the hand, m.</summary>
    public double Pull
    {
        get => GetValue(PullProperty);
        set => SetValue(PullProperty, value);
    }

    private double SetPull => Math.Clamp(Pull, 0, PulleySpread.MostPull);

    protected override void Advance(double seconds) =>
        _pull = double.IsNaN(_pull) ? SetPull : LiftingParts.Ease(_pull, SetPull, Math.Max(0.6, Math.Abs(SetPull - _pull) * 3), seconds);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var pull = double.IsNaN(_pull) ? SetPull : _pull;
        var scale = (bounds.Height - 50) / (Beam + 0.4);
        var v = new View(new Point(bounds.Center.X - (1.6 * scale), bounds.Bottom - 20), scale);
        const double X = 1.6;

        context.FillRectangle(Ground, new Rect(bounds.X, v.At(0, 0).Y, bounds.Width, bounds.Height));
        context.DrawRectangle(Wood, Edge, v.Box(-0.2, Beam, 3.4, Beam + 0.18));

        var rise = Pulleys.Rise(pull, Moving);
        var bucketTop = Floor + BucketSize + rise;
        var bucket = v.Box(X - (BucketSize / 2), bucketTop - BucketSize, X + (BucketSize / 2), bucketTop);
        var r = Wheel * scale;
        var perStrand = Kg(Pulleys.Bucket / Pulleys.Strands(Moving));

        Rect wheel;
        Rect hand;
        Point handAt;
        Point centre;
        double turn;
        if (!Moving)
        {
            // The wheel under the beam; the bucket on its left strand, the hand on its right.
            centre = v.At(X + Wheel, Beam - 0.35);
            context.DrawLine(new ImmutablePen(SteelBrush, 3), v.At(X + Wheel, Beam), centre);
            var left = new Point(centre.X - r, centre.Y);
            var right = new Point(centre.X + r, centre.Y);
            handAt = v.At(X + (2 * Wheel), FixedHand - pull);
            DrawRope(context, left, new Point(left.X, bucket.Top - (0.12 * scale)));
            DrawRope(context, right, handAt);
            wheel = DrawWheel(context, centre, r);
            DrawRopeArc(context, centre, r, over: true);
            DrawHook(context, new Point(left.X, bucket.Top - (0.12 * scale)), 0.1 * scale);
            Mark("rope", [left, new Point(left.X, bucket.Top), right, handAt], 6);
            DiagramText.Draw(context, perStrand, new Point(left.X - 8, (left.Y + bucket.Top) / 2), 12, DiagramText.Gold, 1, 0.5, bold: true);
            hand = DrawHand(context, handAt, 0.28 * scale);
            DrawForce(context, new Point(hand.Center.X + (0.35 * scale), hand.Bottom - (0.2 * scale)), new Point(hand.Center.X + (0.35 * scale), hand.Bottom + (0.45 * scale)), LiftingParts.Push, Kg(Pulleys.Effort(Pulleys.Bucket, false)));

            // The rope runs over the wheel as far as it is pulled: the wheel turns clockwise (the hand's side going down).
            turn = pull / Wheel;
        }
        else
        {
            // The wheel on the bucket's handle; one end tied to the beam, the hand pulling the other up.
            centre = v.At(X, bucketTop + 0.3);
            var left = new Point(centre.X - r, centre.Y);
            var right = new Point(centre.X + r, centre.Y);
            var tie = new Point(left.X, v.At(0, Beam).Y);
            handAt = new Point(right.X, v.At(0, MovingHand + pull).Y);
            context.DrawLine(new ImmutablePen(SteelBrush, 3), centre, new Point(centre.X, bucket.Top));
            DrawRope(context, tie, left);
            DrawRope(context, right, handAt);
            wheel = DrawWheel(context, centre, r);
            DrawRopeArc(context, centre, r, over: false);
            context.DrawEllipse(SteelBrush, Edge, tie, 4, 4);
            Mark("rope", [tie, left, right, handAt], 6);
            DiagramText.Draw(context, perStrand, new Point(left.X - 8, (tie.Y + left.Y) / 2), 12, DiagramText.Gold, 1, 0.5, bold: true);
            DiagramText.Draw(context, perStrand, new Point(right.X + 8, (handAt.Y + right.Y) / 2 + 22), 12, DiagramText.Gold, 0, 0.5, bold: true);
            hand = DrawHand(context, handAt, 0.28 * scale);
            DrawForce(context, new Point(hand.Center.X + (0.35 * scale), hand.Bottom), new Point(hand.Center.X + (0.35 * scale), hand.Top - (0.12 * scale)), LiftingParts.Push, Kg(Pulleys.Effort(Pulleys.Bucket, true)));

            // The wheel rolls up the tied strand: it turns by how far it has risen, clockwise.
            turn = rise / Wheel;
        }

        // A spoke on the wheel, so its turning shows.
        var (s, c) = Math.SinCos(turn);
        context.DrawLine(new ImmutablePen(Dark, 2.2), new Point(centre.X - (c * r * 0.7), centre.Y - (s * r * 0.7)), new Point(centre.X + (c * r * 0.7), centre.Y + (s * r * 0.7)));

        Mark("wheel", wheel);
        Mark("hand", hand);
        Mark("bucket", DrawWeight(context, bucket, Kg(Pulleys.Bucket)));

        // How far the bucket has come up, and how much rope went through the hand.
        var start = v.At(0, Floor + BucketSize).Y;
        context.DrawLine(Dashed, new Point(bucket.Left - (0.8 * scale), start), new Point(bucket.Left, start));
        DiagramText.Draw(context, DiagramText.Format("Lifting_Up", (rise * 100).ToString("0", CultureInfo.CurrentCulture)),
            new Point(bucket.Left - 10, bucket.Top), 13, DiagramText.Ink, 1, 0.5, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Lifting_Pulled", (pull * 100).ToString("0", CultureInfo.CurrentCulture)),
            new Point(hand.Right + 46, hand.Center.Y), 13, DiagramText.Ink, 0, 0.5, bold: true);
        DiagramText.Draw(context, DiagramText.Get(Moving ? "Lifting_Pulley_MovingLabel" : "Lifting_Pulley_FixedLabel"),
            new Point(bounds.X + 16, bounds.Y + 16), 13, DiagramText.Faint);

        // The reader's handles: the hand pulls the rope (down past a fixed wheel, up from a moving one), and a tap on
        // the wheel moves it between the beam and the bucket.
        var moving = Moving;
        Grip("pull", hand.Center, GripAxis.Vertical, DiagramText.Get("Lifting_Grip_Pull"),
            at => Pull = PullFor(moving, v.Y(at)));
        Grip("wheel", centre, GripAxis.Tap, DiagramText.Get("Lifting_Grip_Wheel"), _ => Moving = !Moving);
    }

    /// <summary>The rope pulled that puts the hand <paramref name="handY"/> m above the ground, snapped like the slider.</summary>
    internal static double PullFor(bool moving, double handY) =>
        Math.Clamp(Math.Round((moving ? handY - MovingHand : FixedHand - handY) / 0.05) * 0.05, 0, PulleySpread.MostPull);
}
