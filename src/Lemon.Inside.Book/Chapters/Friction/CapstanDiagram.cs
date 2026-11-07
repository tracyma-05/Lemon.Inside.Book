using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Friction.FrictionParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Friction;

/// <summary>
/// Spread 4: a mooring post seen from above, a boat pulling on a rope that wraps round it and a hand holding the end.
/// The rope is drawn as thick as the pull in it: full at the boat, thinner round every bit of the wrap, a thread at the
/// hand. Each turn is drawn a little wider so the turns can be counted.
/// </summary>
public sealed class CapstanDiagram : GearPicture
{
    public static readonly StyledProperty<double> TurnsProperty =
        AvaloniaProperty.Register<CapstanDiagram, double>(nameof(Turns));

    public static readonly StyledProperty<bool> WetProperty =
        AvaloniaProperty.Register<CapstanDiagram, bool>(nameof(Wet));

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

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var turns = Math.Clamp(Turns, 0, 3);
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
    }
}
