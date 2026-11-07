using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Friction.FrictionParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Friction;

/// <summary>
/// Spread 2: a hand pulling a box along a floor through a rope, the pull and the floor's friction drawn as arrows;
/// below, the friction against the pull: it rises with the pull until the box breaks away, then drops to the smaller
/// sliding friction and stays there.
/// </summary>
public sealed class StartDiagram : GearPicture
{
    public static readonly StyledProperty<double> BoxProperty =
        AvaloniaProperty.Register<StartDiagram, double>(nameof(Box), 40);

    public static readonly StyledProperty<double> PullingProperty =
        AvaloniaProperty.Register<StartDiagram, double>(nameof(Pulling));

    /// <summary>The chart's widest pull, kg-force.</summary>
    private const double MostPull = 40;

    static StartDiagram()
    {
        AffectsRender<StartDiagram>(BoxProperty, PullingProperty);
    }

    /// <summary>The box's mass, kg.</summary>
    public double Box
    {
        get => GetValue(BoxProperty);
        set => SetValue(BoxProperty, value);
    }

    /// <summary>The hand's pull, kg-force.</summary>
    public double Pulling
    {
        get => GetValue(PullingProperty);
        set => SetValue(PullingProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var box = Math.Clamp(Box, 20, 80);
        var pull = Math.Clamp(Pulling, 0, MostPull);
        var moving = Drag.Moving(box, pull);
        var friction = Drag.Friction(box, pull);

        // The floor, the box (sliding along once it moves), the rope and the hand.
        var floorY = bounds.Y + (bounds.Height * 0.42);
        var floor = new Rect(bounds.X + 16, floorY, bounds.Width - 32, 14);
        context.DrawRectangle(Wood, Edge, floor, 3, 3);
        Mark("floor", floor);
        var size = 50 + (box * 0.9);
        var shift = moving ? (Seconds * 40) % 60 : 0;
        var boxRect = new Rect(bounds.X + 50 + shift, floorY - size, size, size);
        context.DrawRectangle(Crate, Edge, boxRect, 4, 4);
        DiagramText.Draw(context, Kgf(box), boxRect.Center, 13, Dark, 0.5, 0.5, bold: true);
        Mark("box", boxRect);
        var ropeY = boxRect.Center.Y + (size * 0.15);
        var handAt = new Point(bounds.Right - 70, ropeY);
        context.DrawLine(Rope, new Point(boxRect.Right, ropeY), handAt);
        var hand = DrawHand(context, handAt, 34);
        Mark("hand", hand);

        if (pull > 0.05)
        {
            var length = pull / MostPull * 140;
            Arrow(context, new Point(boxRect.Right + 8, ropeY - 26), new Point(boxRect.Right + 8 + length, ropeY - 26), PullBrush, Kgf(pull), 0.5, 1.4);
        }

        if (friction > 0.05)
        {
            var length = friction / MostPull * 140;
            Arrow(context, new Point(boxRect.Left, floorY - 8), new Point(boxRect.Left - length, floorY - 8), GripBrush, Kgf(friction), 0.5, 1.4);
        }

        if (moving)
        {
            DiagramText.Draw(context, DiagramText.Get("Friction_Start_MovingLabel"), new Point(boxRect.Center.X, boxRect.Top - 10), 13, PullBrush, 0.5, 1, bold: true);
        }

        // The chart: friction against pull.
        var chartBox = new Rect(bounds.X + 24, floorY + 40, bounds.Width - 48, bounds.Bottom - floorY - 100);
        var plot = DrawChartFrame(context, chartBox, DiagramText.Get("Friction_Start_ChartPull"), DiagramText.Get("Friction_Start_ChartFriction"));
        Point P(double x, double y) => new(plot.X + (x / MostPull * plot.Width), plot.Bottom - (y / MostPull * plot.Height));
        var breakAway = Drag.BreakAway(box);
        var sliding = Drag.Sliding * box;
        var line = new List<Point> { P(0, 0), P(breakAway, breakAway), P(breakAway, sliding), P(MostPull, sliding) };
        context.DrawGeometry(null, new ImmutablePen(GripBrush, 2.5, null, PenLineCap.Round, PenLineJoin.Round), Plumbing.PlumbingParts.Polyline(line));
        context.DrawEllipse(DiagramText.Gold, Edge, P(pull, friction), 6, 6);
        DiagramText.Draw(context, DiagramText.Format("Friction_Start_PeakLabel", Kgf(breakAway)), new Point(P(breakAway, breakAway).X + 8, P(breakAway, breakAway).Y), 11.5,
            DiagramText.Ink, 0, 0.5);
        DiagramText.Draw(context, DiagramText.Format("Friction_Start_SlidingLabel", Kgf(sliding)), new Point(plot.Right - 4, P(0, sliding).Y - 4), 11.5, DiagramText.Ink, 1, 1);
        Mark("chart", chartBox);
        DiagramText.Draw(context, DiagramText.Get("Friction_Start_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
