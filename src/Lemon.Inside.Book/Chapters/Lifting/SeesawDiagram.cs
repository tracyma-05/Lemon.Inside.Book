using Avalonia;
using Avalonia.Media;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Lifting;

/// <summary>
/// Spread 1: a bar over a pivot, a stone on its short end and a hand pressing the long one. The arrows give the
/// stone's weight and the hand's push; under the bar, each arm's length; beside each end, how far it has moved.
/// </summary>
public sealed class SeesawDiagram : GearPicture
{
    public static readonly StyledProperty<double> PivotProperty =
        AvaloniaProperty.Register<SeesawDiagram, double>(nameof(Pivot), 2);

    public static readonly StyledProperty<double> PushProperty =
        AvaloniaProperty.Register<SeesawDiagram, double>(nameof(Push));

    /// <summary>Where the pivot stands, m left of which the stone's end sits: fixed on the picture.</summary>
    private const double PivotX = 2.3;

    static SeesawDiagram()
    {
        AffectsRender<SeesawDiagram>(PivotProperty, PushProperty);
    }

    /// <summary>The pivot's distance from the stone's end of the bar, m.</summary>
    public double Pivot
    {
        get => GetValue(PivotProperty);
        set => SetValue(PivotProperty, value);
    }

    /// <summary>How far the hand has pressed its end down, 0–1.</summary>
    public double Push
    {
        get => GetValue(PushProperty);
        set => SetValue(PushProperty, value);
    }

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var pivot = Math.Clamp(Pivot, 0.2, Seesaw.Length - 0.2);
        var tilt = Seesaw.Tilt(pivot, Push);
        var scale = Math.Min((bounds.Width - 40) / 6.2, (bounds.Height - 40) / 3.2);
        var v = new View(new Point(bounds.X + 20 + (0.1 * scale), bounds.Y + (bounds.Height * 0.68)), scale);

        context.FillRectangle(Ground, new Rect(bounds.X, v.At(0, 0).Y, bounds.Width, bounds.Height));

        // The pivot: a wedge standing on the ground, its tip under the bar.
        var top = v.At(PivotX, Seesaw.PivotHeight);
        Point[] wedge = [top, v.At(PivotX - 0.28, 0), v.At(PivotX + 0.28, 0)];
        DrawPolygon(context, SteelBrush, Edge, wedge);
        Mark("pivot", wedge, 8);

        // The bar, turned about the pivot: the stone's end up and left as the tilt grows, the hand's end down and right.
        var (sin, cos) = Math.SinCos(tilt);
        Point Along(double d, double above) => v.At(PivotX + (d * cos) + (above * sin), Seesaw.PivotHeight - (d * sin) + (above * cos));
        const double Thick = 0.09;
        Point[] bar = [Along(-pivot, 0), Along(Seesaw.Length - pivot, 0), Along(Seesaw.Length - pivot, Thick), Along(-pivot, Thick)];
        DrawPolygon(context, Wood, Edge, bar);
        Mark("bar", bar, 6);

        // The stone sits on the short end.
        var stoneBase = Along(-pivot + 0.28, Thick);
        var size = 0.5 * scale;
        var stone = new Rect(stoneBase.X - (size / 2), stoneBase.Y - (size * 0.8), size, size * 0.8);
        Mark("stone", DrawWeight(context, stone, Kg(Seesaw.Stone), Stone));
        DrawForce(context, new Point(stone.Left - 10, stone.Top), new Point(stone.Left - 10, stone.Top + (0.55 * scale)), Weight, null);

        // The hand on the long end, and how hard it presses.
        var handAt = Along(Seesaw.Length - pivot - 0.15, Thick);
        var hand = DrawHand(context, new Point(handAt.X, handAt.Y - (0.13 * scale)), 0.22 * scale);
        Mark("hand", hand);
        var effort = Seesaw.Effort(Seesaw.Stone, pivot);
        var length = Math.Clamp(effort / Seesaw.Stone, 0.08, 1.6) * 0.55 * scale;
        DrawForce(context, new Point(hand.Center.X, hand.Top - 4 - length), new Point(hand.Center.X, hand.Top - 4), LiftingParts.Push, Kg(effort), -1);

        // The arms' lengths along the ground, and how far each end has moved.
        var y = v.At(0, -0.25).Y;
        var stoneX = v.At(PivotX - pivot, 0).X;
        var handX = v.At(PivotX + Seesaw.Length - pivot, 0).X;
        context.DrawLine(Dashed, new Point(stoneX, y), new Point(top.X, y));
        context.DrawLine(new Avalonia.Media.Immutable.ImmutablePen(DiagramText.Gold, 1.4), new Point(top.X, y), new Point(handX, y));
        DiagramText.Draw(context, DiagramText.Format("Lifting_Metres", pivot.ToString("0.0", System.Globalization.CultureInfo.CurrentCulture)),
            new Point((stoneX + top.X) / 2, y + 4), 12, DiagramText.Faint, 0.5);
        DiagramText.Draw(context, DiagramText.Format("Lifting_Metres", (Seesaw.Length - pivot).ToString("0.0", System.Globalization.CultureInfo.CurrentCulture)),
            new Point((top.X + handX) / 2, y + 4), 12, DiagramText.Gold, 0.5);

        var rise = Seesaw.Rise(pivot, Push) * 100;
        var drop = Seesaw.Drop(pivot, Push) * 100;
        DiagramText.Draw(context, DiagramText.Format("Lifting_Up", rise.ToString("0", System.Globalization.CultureInfo.CurrentCulture)),
            new Point(stone.Center.X, stone.Top - 6), 13, DiagramText.Ink, 0.5, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Lifting_Down", drop.ToString("0", System.Globalization.CultureInfo.CurrentCulture)),
            new Point(Math.Min(hand.Right + 8, bounds.Right - 6), hand.Bottom + 8), 13, DiagramText.Ink, hand.Right + 80 > bounds.Right ? 1 : 0, 0, bold: true);

        DiagramText.Draw(context, DiagramText.Get("Lifting_Seesaw_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 13, DiagramText.Faint);

        // The work done at each end: force times distance, the same on both sides.
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        DiagramText.Draw(context, DiagramText.Format("Lifting_Seesaw_WorkStone", Kg(Seesaw.Stone), rise.ToString("0", culture)),
            new Point(bounds.X + 16, bounds.Y + 44), 13, DiagramText.Ink, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Lifting_Seesaw_WorkHand", Kg(effort), drop.ToString("0", culture)),
            new Point(bounds.X + 16, bounds.Y + 66), 13, LiftingParts.Push, bold: true);
    }
}
