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
/// Spread 4: a mooring post seen from above, a boat pulling on a rope that wraps round it and a hand holding the end.
/// The rope is drawn as thick as the pull in it: full at the boat, thinner round every bit of the wrap, a thread at the
/// hand. Each turn is drawn a little wider so the turns can be counted.
/// The reader drags the rope's end round the post to wrap it (back to unwrap) and taps the rope from the boat to wet it;
/// the rope winds on and off smoothly, its thickness and the hand's hold following.
/// </summary>
public sealed class CapstanDiagram : GearPicture
{
    public static readonly StyledProperty<double> TurnsProperty =
        AvaloniaProperty.Register<CapstanDiagram, double>(nameof(Turns), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> WetProperty =
        AvaloniaProperty.Register<CapstanDiagram, bool>(nameof(Wet), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The most turns the slider gives.</summary>
    private const double MostTurns = 3;

    /// <summary>The wrap as drawn, easing toward <see cref="Turns"/>; NaN until the first frame.</summary>
    private double _turns = double.NaN;

    static CapstanDiagram()
    {
        AffectsRender<CapstanDiagram>(TurnsProperty, WetProperty);
    }

    public double Turns
    {
        get => GetValue(TurnsProperty);
        set => SetValue(TurnsProperty, value);
    }

    public bool Wet
    {
        get => GetValue(WetProperty);
        set => SetValue(WetProperty, value);
    }

    protected override void Advance(double seconds)
    {
        var turns = Math.Clamp(Turns, 0, MostTurns);
        _turns = double.IsNaN(_turns) ? turns : Glide(_turns, turns, seconds, 0.1);
    }

    /// <summary>
    /// The wrap (to a twentieth of a turn) that ends at <paramref name="angle"/> (screen radians, as atan2 gives) round the
    /// post: of all the wraps that end there, the one nearest <paramref name="now"/>, so a drag winds on round and round.
    /// The rope meets the post at its top and wraps anticlockwise on screen.
    /// </summary>
    internal static double TurnsAt(double angle, double now)
    {
        var part = (((-Math.PI / 2) - angle) / (2 * Math.PI)) % 1;
        if (part < 0) part += 1;
        var turns = Math.Round(now - part) + part;
        return Math.Clamp(Math.Round(turns * 20) / 20, 0, MostTurns);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var turns = double.IsNaN(_turns) ? Math.Clamp(Turns, 0, MostTurns) : _turns;
        var wet = Wet;
        var centre = new Point(bounds.X + (bounds.Width * 0.45), bounds.Center.Y - 10);
        var post = Math.Min(bounds.Width, bounds.Height) * 0.11;
        var spacing = 7.0;

        // The water to the right, and the boat on it.
        context.FillRectangle(Plumbing.PlumbingParts.WaterBrush, new Rect(bounds.Right - (bounds.Width * 0.22), bounds.Y + 40, bounds.Width * 0.22, bounds.Height - 80));
        var boatAt = new Point(bounds.Right - (bounds.Width * 0.11), centre.Y - post);
        Point[] hull = [new(boatAt.X - 26, boatAt.Y - 40), new(boatAt.X + 26, boatAt.Y - 40), new(boatAt.X + 26, boatAt.Y + 40), new(boatAt.X, boatAt.Y + 70), new(boatAt.X - 26, boatAt.Y + 40)];
        DrawPolygon(context, Crate, Edge, hull);
        Mark("boat", hull, 4);
        DiagramText.Draw(context, DiagramText.Format("Friction_Capstan_BoatLabel", Kgf(Capstan.Boat)), new Point(boatAt.X, boatAt.Y + 80), 12, DiagramText.Ink, 0.5, 0, bold: true);

        // The rope: from the boat to the top of the post, round it, then off to the hand.
        double Width(double tension) => 1.5 + (5.5 * Math.Log(Math.Max(1, tension)) / Math.Log(Capstan.Boat));
        var mu = wet ? Capstan.Wet : Capstan.Dry;
        var touch = new Point(centre.X, centre.Y - post);
        context.DrawLine(new ImmutablePen(Rope.Brush as IImmutableBrush, Width(Capstan.Boat), null, PenLineCap.Round), new Point(hull[0].X, touch.Y), touch);
        var rope = new List<Point> { new(hull[0].X, touch.Y), touch };
        var total = turns * 2 * Math.PI;
        var steps = Math.Max(1, (int)(total / 0.08));
        var previous = touch;
        for (var i = 1; i <= steps && total > 0; i++)
        {
            var phi = total * i / steps;
            var r = post + (spacing * phi / (2 * Math.PI));
            var a = (-Math.PI / 2) - phi;
            var p = new Point(centre.X + (r * Math.Cos(a)), centre.Y + (r * Math.Sin(a)));
            context.DrawLine(new ImmutablePen(Rope.Brush as IImmutableBrush, Width(Capstan.Boat * Math.Exp(-mu * phi)), null, PenLineCap.Round), previous, p);
            rope.Add(p);
            previous = p;
        }

        var end = (-Math.PI / 2) - total;
        var tangent = new Point(Math.Sin(end), -Math.Cos(end));
        var hold = Capstan.Hold(turns, wet);
        var handAt = new Point(previous.X + (tangent.X * bounds.Width * 0.22), previous.Y + (tangent.Y * bounds.Width * 0.22));
        context.DrawLine(new ImmutablePen(Rope.Brush as IImmutableBrush, Width(hold), null, PenLineCap.Round), previous, handAt);
        rope.Add(handAt);
        Mark("rope", rope, 4);

        // The post, drawn over the rope's far side.
        context.DrawEllipse(Wood, Edge, centre, post * 0.92, post * 0.92);
        context.DrawEllipse(null, new ImmutablePen(Dark, 1), centre, post * 0.6, post * 0.6);
        Mark("post", centre, post);

        var hand = DrawHand(context, handAt, 30);
        Mark("hand", hand);
        DiagramText.Draw(context, DiagramText.Format("Friction_Capstan_HoldLabel", Kgf(hold)), new Point(hand.Center.X, hand.Bottom + 6), 13, PullBrush, 0.5, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Friction_Turns", Number(turns, "0.##")), new Point(centre.X, centre.Y - post - 30), 13, DiagramText.Gold, 0.5, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Friction_Capstan_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);

        // The reader's handles: where the rope leaves the post, dragged round it to wrap more; the rope from the boat, tapped to wet it.
        Grip("turns", previous, GripAxis.Free, DiagramText.Get("Friction_Grip_Turns"),
            p => Turns = TurnsAt(Math.Atan2(p.Y - centre.Y, p.X - centre.X), Math.Clamp(Turns, 0, MostTurns)));
        Grip("wet", new Point((hull[0].X + touch.X + post) / 2, touch.Y), GripAxis.Tap, DiagramText.Get(wet ? "Friction_Grip_Dry" : "Friction_Grip_Wet"),
            _ => Wet = !Wet);
    }
}
