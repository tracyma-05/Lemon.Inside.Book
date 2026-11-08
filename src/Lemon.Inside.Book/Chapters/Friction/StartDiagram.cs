using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Friction.FrictionParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Friction;

/// <summary>
/// Spread 2: a hand pulling a box along a floor through a rope, the pull and the floor's friction drawn as arrows;
/// below, the friction against the pull: it rises with the pull until the box breaks away, then drops to the smaller
/// sliding friction and stays there.
/// The reader drags the hand back to pull harder (it leans further the harder it pulls) and the box's top up for a
/// heavier box. The pull eases to its new size, so the dot on the chart runs along the line and drops at the breakaway.
/// </summary>
public sealed class StartDiagram : GearPicture
{
    public static readonly StyledProperty<double> BoxProperty =
        AvaloniaProperty.Register<StartDiagram, double>(nameof(Box), 40, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> PullingProperty =
        AvaloniaProperty.Register<StartDiagram, double>(nameof(Pulling), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The chart's widest pull, kg-force.</summary>
    private const double MostPull = 40;

    /// <summary>The lightest and heaviest box (the slider's ends), kg.</summary>
    private const double Lightest = 20;
    private const double Heaviest = 80;

    /// <summary>How far the hand leans back at the hardest pull, pixels.</summary>
    private const double Lean = 70;

    /// <summary>The pull as drawn, easing toward <see cref="Pulling"/>; NaN until the first frame.</summary>
    private double _pull = double.NaN;

    /// <summary>How far the box has slid, pixels (it slides while the pull beats the floor's grip).</summary>
    private double _slid;

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

    /// <summary>The box's side, pixels, for a box of <paramref name="kg"/>.</summary>
    private static double Side(double kg) => 50 + (kg * 0.9);

    /// <summary>The box (to a kilogram) whose side is <paramref name="side"/> pixels.</summary>
    internal static double BoxForSide(double side) => Math.Clamp(Math.Round((side - 50) / 0.9), Lightest, Heaviest);

    /// <summary>The pull (to half a kilogram) for the hand leaning back <paramref name="lean"/> pixels.</summary>
    internal static double PullForLean(double lean) => Math.Clamp(Math.Round(lean / Lean * MostPull * 2) / 2, 0, MostPull);

    protected override void Advance(double seconds)
    {
        var pull = Math.Clamp(Pulling, 0, MostPull);
        _pull = double.IsNaN(_pull) ? pull : Glide(_pull, pull, seconds, 0.1);

        // Once it breaks free the box slides, faster the more the pull beats the sliding friction; it is set back after a while.
        var box = Math.Clamp(Box, Lightest, Heaviest);
        if (Drag.Moving(box, _pull))
        {
            _slid += (20 + ((_pull - Drag.Friction(box, _pull)) * 6)) * seconds;
            if (_slid > 60) _slid = 0;
        }
        else
        {
            _slid = 0;
        }
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var box = Math.Clamp(Box, Lightest, Heaviest);
        var pull = double.IsNaN(_pull) ? Math.Clamp(Pulling, 0, MostPull) : _pull;
        var moving = Drag.Moving(box, pull);
        var friction = Drag.Friction(box, pull);

        // The floor, the box (sliding along once it moves), the rope and the hand.
        var floorY = bounds.Y + (bounds.Height * 0.42);
        var floor = new Rect(bounds.X + 16, floorY, bounds.Width - 32, 14);
        context.DrawRectangle(Wood, Edge, floor, 3, 3);
        Mark("floor", floor);
        var size = Side(box);
        var shift = moving ? _slid : 0;
        var boxRect = new Rect(bounds.X + 50 + shift, floorY - size, size, size);
        context.DrawRectangle(Crate, Edge, boxRect, 4, 4);
        DiagramText.Draw(context, Kgf(box), boxRect.Center, 13, Dark, 0.5, 0.5, bold: true);
        Mark("box", boxRect);
        var ropeY = boxRect.Center.Y + (size * 0.15);
        var handAt = new Point(bounds.Right - 70 - Lean + (pull / MostPull * Lean), ropeY);
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
            DiagramText.Draw(context, DiagramText.Get("Friction_Start_MovingLabel"), new Point(boxRect.Center.X, boxRect.Top - 16), 13, PullBrush, 0.5, 1, bold: true);
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

        // The reader's handles: the hand, leaned back to pull harder, and the box's top corner, raised for a bigger, heavier box.
        var restX = bounds.Right - 70 - Lean;
        Grip("pull", new Point(restX + (Math.Clamp(Pulling, 0, MostPull) / MostPull * Lean), ropeY), GripAxis.Horizontal,
            DiagramText.Get("Friction_Grip_Pull"), p => Pulling = PullForLean(p.X - restX));
        Grip("box", new Point(boxRect.X, boxRect.Top), GripAxis.Vertical, DiagramText.Get("Friction_Grip_Box"),
            p => Box = BoxForSide(floorY - p.Y));
    }
}
