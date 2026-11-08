using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Sewing.SewingParts;

namespace Lemon.Inside.Book.Chapters.Sewing;

/// <summary>
/// Spread 5: the needle plate from the side, cut through the slots. The presser foot holds the cloth down on the plate;
/// under it the feed dogs' teeth go round a flat loop: up through the slots while the needle is out of the cloth,
/// along by a stitch carrying the cloth with them, down below the plate and back. The stitches the needle has made
/// ride away on the cloth. Time runs slow. The reader drags the stitch length marker along its scale and taps the
/// reverse button.
/// </summary>
public sealed class FeedDiagram : GearPicture
{
    public static readonly StyledProperty<double> LengthProperty =
        AvaloniaProperty.Register<FeedDiagram, double>(nameof(Length), 2.5, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> ReverseProperty =
        AvaloniaProperty.Register<FeedDiagram, bool>(nameof(Reverse), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Handwheel degrees a second.</summary>
    private const double Pace = 120;

    private double _angle = 120;

    /// <summary>How far the cloth has been carried, mm (forward is to the left, away from the sewer).</summary>
    private double _carried;

    /// <summary>Where the needle went in, in the cloth's own millimetres.</summary>
    private readonly List<double> _holes = [];

    static FeedDiagram()
    {
        AffectsRender<FeedDiagram>(LengthProperty, ReverseProperty);
    }

    /// <summary>The stitch length, mm.</summary>
    public double Length
    {
        get => GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    public bool Reverse
    {
        get => GetValue(ReverseProperty);
        set => SetValue(ReverseProperty, value);
    }

    protected override void Advance(double seconds)
    {
        var length = Math.Clamp(Length, 0, 5);
        var before = Feed.Dog(_angle, length, Reverse);
        var wasDown = Needle.Wrap(_angle) >= 180;
        _angle = Needle.Wrap(_angle + (seconds * Pace));
        var after = Feed.Dog(_angle, length, Reverse);

        // Teeth above the plate carry the cloth with them.
        if (after.Y > 0 && before.Y > 0) _carried += after.X - before.X;

        // The needle goes in at the bottom of its stroke: a new hole where the cloth is now.
        if (!wasDown && Needle.Wrap(_angle) >= 180)
        {
            _holes.Add(-_carried);
            if (_holes.Count > 40) _holes.RemoveAt(0);
        }
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var length = Math.Clamp(Length, 0, 5);
        var mm = Math.Min((bounds.Width - 60) / 42, (bounds.Height - 200) / 34);
        var nx = bounds.Center.X + (2 * mm);
        var plateY = bounds.Center.Y - (2 * mm);
        var thread = Math.Clamp(mm * 0.3, 2.5, 4.5);

        // The plate, with slots for the teeth.
        var plate = new Rect(bounds.X + 20, plateY, bounds.Width - 40, 1.5 * mm);
        context.FillRectangle(Metal, plate);
        context.FillRectangle(Lifting.LiftingParts.Dark, new Rect(nx - (9 * mm), plateY, 6.5 * mm, 1.5 * mm));
        context.FillRectangle(Lifting.LiftingParts.Dark, new Rect(nx + (2.5 * mm), plateY, 6.5 * mm, 1.5 * mm));
        Mark("plate", plate);

        // The feed dogs: a toothed bar going round its flat loop, its path drawn faint.
        var dog = Feed.Dog(_angle, length, Reverse);
        var dogCentre = new Point(nx - (dog.X * mm), plateY + (1.5 * mm) - (dog.Y * mm));
        var path = new List<Point>();
        for (var a = 0.0; a <= 360; a += 10)
        {
            var d = Feed.Dog(a, Math.Max(length, 0.2), Reverse);
            path.Add(new Point(nx - (d.X * mm), plateY + (3 * mm) - (d.Y * mm)));
        }

        DrawLine(context, new ImmutablePen(DiagramText.Faint, 1, new ImmutableDashStyle([3, 3], 0)), path);
        var teeth = new List<Point>();
        var bar = 18 * mm;
        teeth.Add(new Point(dogCentre.X - (bar / 2), dogCentre.Y + (3 * mm)));
        for (var k = 0; k <= 8; k++)
        {
            var x = dogCentre.X - (bar / 2) + (k * bar / 8);
            teeth.Add(new Point(x, dogCentre.Y));
            if (k < 8) teeth.Add(new Point(x + (bar / 16), dogCentre.Y - (1 * mm)));
        }

        teeth.Add(new Point(dogCentre.X + (bar / 2), dogCentre.Y + (3 * mm)));
        Lifting.LiftingParts.DrawPolygon(context, DarkMetal, Lifting.LiftingParts.Edge, teeth);
        Mark("dogs", teeth, 4);

        // The cloth on the plate, carried along, with the stitches already sewn.
        var clothTop = plateY - (1.4 * mm);
        DrawCloth(context, bounds.X + 20, bounds.Right - 20, clothTop, plateY);
        Mark("cloth", new Rect(bounds.X + 20, clothTop, nx - bounds.X - 120, plateY - clothTop));
        var pen = UpperPen(thread);
        for (var i = 1; i < _holes.Count; i++)
        {
            var a = nx - ((_holes[i - 1] + _carried) * mm);
            var b = nx - ((_holes[i] + _carried) * mm);
            if (Math.Max(a, b) < bounds.X + 20 || Math.Min(a, b) > bounds.Right - 20) continue;
            if (Math.Abs(a - b) < 1) continue;
            context.DrawLine(pen, new Point(Math.Clamp(a, bounds.X + 20, bounds.Right - 20) + 2, clothTop - (thread / 2)),
                new Point(Math.Clamp(b, bounds.X + 20, bounds.Right - 20) - 2, clothTop - (thread / 2)));
        }

        // The presser foot, pressing down round the needle.
        var footTop = clothTop - (3 * mm);
        Lifting.LiftingParts.DrawPolygon(context, Metal, Lifting.LiftingParts.Edge,
        [
            new Point(nx - (12 * mm), clothTop - (0.2 * mm)), new Point(nx + (8 * mm), clothTop - (0.2 * mm)), new Point(nx + (10 * mm), footTop),
            new Point(nx + (3 * mm), footTop), new Point(nx + (3 * mm), footTop - (12 * mm)), new Point(nx - (3 * mm), footTop - (12 * mm)),
            new Point(nx - (3 * mm), footTop), new Point(nx - (12 * mm), footTop),
        ]);
        context.DrawLine(new ImmutablePen(Lifting.LiftingParts.Push, 3), new Point(nx - (6 * mm), footTop - (10 * mm)), new Point(nx - (6 * mm), footTop - (2 * mm)));
        DiagramText.DrawArrowAlong(context, new Point(nx - (6 * mm), footTop - (10 * mm)), new Point(nx - (6 * mm), footTop - (2 * mm)),
            Lifting.LiftingParts.Push, 8);
        Mark("foot", new Rect(nx - (12 * mm), footTop - (12 * mm), 22 * mm, 12 * mm));

        // The needle, out of the cloth while the teeth move it.
        var tip = new Point(nx, clothTop + (5 * mm) - (Needle.Rise(_angle) * 0.6 * mm));
        DrawNeedle(context, tip, 24 * mm, Math.Max(6, 0.9 * mm));

        // The words, and the reader's handles: a stitch-length scale and the reverse button.
        DiagramText.Draw(context, DiagramText.Get(Reverse ? "Sewing_Feed_RuleBack" : "Sewing_Feed_Rule"), new Point(bounds.X + 16, bounds.Y + 16),
            12, DiagramText.Faint);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Feed_Slow"), new Point(bounds.X + 16, bounds.Y + 34), 11, DiagramText.Faint);
        var forward = new Point(bounds.X + 70, clothTop - (8 * mm));
        var (from, to) = (forward + new Vector(Reverse ? -24 : 24, 0), forward + new Vector(Reverse ? 24 : -24, 0));
        context.DrawLine(new ImmutablePen(DiagramText.Ink, 2), from, to);
        DiagramText.DrawArrowAlong(context, from, to, DiagramText.Ink, 9);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Feed_Way"), forward + new Vector(0, -10), 11.5, DiagramText.Faint, 0.5, 1);
        var scaleY = plateY + (10 * mm);
        var per = Math.Min(40, (bounds.Width - 120) / 5);
        var x0 = bounds.Center.X - (2.5 * per);
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 1), new Point(x0, scaleY), new Point(x0 + (5 * per), scaleY));
        for (var k = 0; k <= 5; k++)
        {
            DiagramText.Draw(context, Number(k, "0"), new Point(x0 + (k * per), scaleY + 10), 11, DiagramText.Faint, 0.5);
        }

        DiagramText.Draw(context, DiagramText.Format("Sewing_Feed_Label", Number(length, "0.0"),
            Number(Feed.MetresPerMinute(length) * 100 / 60, "0.0")), new Point(bounds.Center.X, scaleY - 14), 13, DiagramText.Gold, 0.5, 1, bold: true);
        Grip("length", new Point(x0 + (length * per), scaleY), GripAxis.Horizontal, DiagramText.Get("Sewing_Grip_Length"),
            at => Length = Math.Round(Math.Clamp((at.X - x0) / per, 0, 5), 1));
        var button = new Point(bounds.Right - 50, scaleY);
        context.DrawEllipse(Reverse ? Lifting.LiftingParts.Push : DarkMetal, Lifting.LiftingParts.Edge, button, 13, 13);
        DiagramText.Draw(context, "↺", button, 14, DiagramText.Ink, 0.5, 0.5);
        Grip("reverse", button, GripAxis.Tap, DiagramText.Get("Sewing_Grip_Reverse"), _ => Reverse = !Reverse);
    }
}
