using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Lifting;

/// <summary>
/// Spread 2: a bucket lifted by a rope. Fixed, the wheel hangs from the beam and the hand pulls down on the other end;
/// moving, the wheel rides on the bucket, one end of the rope tied to the beam and the hand pulling up the other. Each
/// strand holding the bucket is labelled with what it carries.
/// </summary>
public sealed class PulleyDiagram : GearPicture
{
    public static readonly StyledProperty<bool> MovingProperty =
        AvaloniaProperty.Register<PulleyDiagram, bool>(nameof(Moving));

    public static readonly StyledProperty<double> PullProperty =
        AvaloniaProperty.Register<PulleyDiagram, double>(nameof(Pull));

    /// <summary>The beam's height, m; the bucket's size and where it starts.</summary>
    private const double Beam = 4.2;
    private const double BucketSize = 0.55;
    private const double Floor = 0;
    private const double Wheel = 0.2;

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

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var pull = Math.Clamp(Pull, 0, PulleySpread.MostPull);
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
        if (!Moving)
        {
            // The wheel under the beam; the bucket on its left strand, the hand on its right.
            var centre = v.At(X + Wheel, Beam - 0.35);
            context.DrawLine(new Avalonia.Media.Immutable.ImmutablePen(SteelBrush, 3), v.At(X + Wheel, Beam), centre);
            var left = new Point(centre.X - r, centre.Y);
            var right = new Point(centre.X + r, centre.Y);
            handAt = v.At(X + (2 * Wheel), 3.2 - pull);
            DrawRope(context, left, new Point(left.X, bucket.Top - (0.12 * scale)));
            DrawRope(context, right, handAt);
            wheel = DrawWheel(context, centre, r);
            DrawRopeArc(context, centre, r, over: true);
            DrawHook(context, new Point(left.X, bucket.Top - (0.12 * scale)), 0.1 * scale);
            Mark("rope", [left, new Point(left.X, bucket.Top), right, handAt], 6);
            DiagramText.Draw(context, perStrand, new Point(left.X - 8, (left.Y + bucket.Top) / 2), 12, DiagramText.Gold, 1, 0.5, bold: true);
            hand = DrawHand(context, handAt, 0.28 * scale);
            DrawForce(context, new Point(hand.Center.X + (0.35 * scale), hand.Bottom - (0.2 * scale)), new Point(hand.Center.X + (0.35 * scale), hand.Bottom + (0.45 * scale)), LiftingParts.Push, Kg(Pulleys.Effort(Pulleys.Bucket, false)));
        }
        else
        {
            // The wheel on the bucket's handle; one end tied to the beam, the hand pulling the other up.
            var centre = v.At(X, bucketTop + 0.3);
            var left = new Point(centre.X - r, centre.Y);
            var right = new Point(centre.X + r, centre.Y);
            var tie = new Point(left.X, v.At(0, Beam).Y);
            handAt = new Point(right.X, v.At(0, 1.9 + pull).Y);
            context.DrawLine(new Avalonia.Media.Immutable.ImmutablePen(SteelBrush, 3), centre, new Point(centre.X, bucket.Top));
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
        }

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
    }
}
