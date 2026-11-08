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
/// Spread 4: the machine from the front, its body cut away round the thread's path. The red thread comes off the spool,
/// between the tension discs, up through the eye of the take-up lever, down to the needle and under the plate, where
/// the hook spreads it round the bobbin case. The lever drops to let that loop down and climbs to pull it all back,
/// setting the stitch and drawing a little fresh thread off the spool. Held still, nothing takes the loop back and the
/// thread piles up in a tangle under the cloth. Time runs slow. The reader taps the lever to hold it and drags the stitch
/// length lever up or down.
/// </summary>
public sealed class TakeUpDiagram : GearPicture
{
    public static readonly StyledProperty<double> LengthProperty =
        AvaloniaProperty.Register<TakeUpDiagram, double>(nameof(Length), 2.5, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> HeldProperty =
        AvaloniaProperty.Register<TakeUpDiagram, bool>(nameof(Held), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Handwheel degrees a second.</summary>
    private const double Pace = 90;

    private double _angle = 60;

    /// <summary>Thread drawn off the spool so far, mm (it turns the spool); thread piled up under the cloth, mm.</summary>
    private double _drawn;
    private double _nest;

    static TakeUpDiagram()
    {
        AffectsRender<TakeUpDiagram>(LengthProperty, HeldProperty);
    }

    /// <summary>The stitch length, mm.</summary>
    public double Length
    {
        get => GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    /// <summary>Is the take-up lever held still?</summary>
    public bool Held
    {
        get => GetValue(HeldProperty);
        set => SetValue(HeldProperty, value);
    }

    protected override void Advance(double seconds)
    {
        var before = TakeUp.Down(_angle);
        _angle = Needle.Wrap(_angle + (seconds * Pace));
        var after = TakeUp.Down(_angle);
        if (Held)
        {
            // Every bit the hook pulls down comes off the spool and stays down.
            var pulled = Math.Max(0, after - before) * TakeUp.Loop;
            _nest = Math.Min(_nest + pulled, 2000);
            _drawn += pulled;
        }
        else
        {
            _nest = Math.Max(0, _nest - (seconds * 600));
            _drawn += seconds * Pace / 360 * TakeUp.PerStitch(Seam.Clamp(Length));
        }
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var length = Seam.Clamp(Length);
        var w = bounds.Width;
        var h = bounds.Height;
        var thread = Math.Clamp(w * 0.006, 2.2, 3.5);
        var pen = UpperPen(thread);

        // The body: an arm across the top, the head on the left over the plate, the bed along the bottom.
        var head = new Rect(bounds.X + (w * 0.1), bounds.Y + (h * 0.16), w * 0.22, h * 0.5);
        var arm = new Rect(head.Right - 4, bounds.Y + (h * 0.16), w * 0.56, h * 0.16);
        var pillar = new Rect(arm.Right - (w * 0.14), arm.Top, w * 0.14, h * 0.62);
        var bed = new Rect(bounds.X + (w * 0.04), bounds.Y + (h * 0.74), w * 0.92, h * 0.1);
        foreach (var part in new[] { head, arm, pillar, bed })
        {
            context.DrawRectangle(Body, Lifting.LiftingParts.Edge, part, 8, 8);
        }

        var plateY = bed.Top;
        var nx = head.Center.X + (w * 0.02);

        // The spool on its pin on top of the arm, turning as thread comes off.
        var spool = new Point(arm.Left + (arm.Width * 0.62), arm.Top - (h * 0.06));
        var spoolR = h * 0.045;
        context.DrawEllipse(UpperBrush, Lifting.LiftingParts.Edge, spool, spoolR, spoolR);
        var turned = _drawn / 8;
        for (var k = 0; k < 3; k++)
        {
            var a = turned + (k * 2.1);
            context.DrawLine(new ImmutablePen(Lifting.LiftingParts.Dark, 1.2), spool, spool + (new Vector(Math.Cos(a), Math.Sin(a)) * spoolR * 0.8));
        }

        Mark("spool", spool, spoolR + 4);

        // The tension discs on the head, the lever's slot above them, the needle bar below.
        var discs = new Point(head.Left + (head.Width * 0.3), head.Top + (head.Height * 0.42));
        context.DrawEllipse(Metal, Lifting.LiftingParts.Edge, discs, h * 0.02, h * 0.02);
        var lever = Held ? 1 : TakeUp.Lever(_angle);
        var slotTop = head.Top + (head.Height * 0.08);
        var slotBottom = head.Top + (head.Height * 0.36);
        var leverEye = new Point(head.Left + (head.Width * 0.5), slotBottom - ((slotBottom - slotTop) * lever));
        context.DrawRectangle(Lifting.LiftingParts.Dark, null, new Rect(leverEye.X - 4, slotTop - 4, 8, slotBottom - slotTop + 8), 4, 4);
        var pivot = new Point(head.Right - (head.Width * 0.12), head.Top + (head.Height * 0.25));
        context.DrawLine(new ImmutablePen(Metal, Math.Max(5, w * 0.012), lineCap: PenLineCap.Round), pivot, leverEye);
        context.DrawEllipse(Lifting.LiftingParts.Dark, Lifting.LiftingParts.Edge, leverEye, 5, 5);
        Mark("lever", [pivot, leverEye, new Point(leverEye.X, slotTop), new Point(leverEye.X, slotBottom)], 6);

        // The needle at its height, through the cloth on the plate.
        var stroke = h * 0.1;
        var tip = new Point(nx, plateY + (stroke * 0.35) - (stroke * Needle.Rise(_angle) / 30));
        var guide = new Point(nx - 3, head.Bottom - 6);
        context.FillRectangle(Metal, new Rect(nx - 3, head.Bottom - 10, 6, tip.Y - head.Bottom));
        DrawCloth(context, bed.Left + 10, bed.Right - 10, plateY - 7, plateY);
        var eye = DrawNeedle(context, tip, stroke * 0.8, 6);
        Mark("needle", new Rect(nx - 8, head.Bottom - 10, 16, tip.Y - head.Bottom + 14));

        // Under the plate: the bobbin case and the loop round it.
        var bobbinAt = new Point(nx, bed.Bottom + (h * 0.07));
        var bobbinR = h * 0.04;
        context.FillRectangle(new ImmutableSolidColorBrush(Color.FromRgb(0x14, 0x1A, 0x26)), new Rect(bed.Left, bed.Bottom, bed.Width, bounds.Bottom - bed.Bottom));
        context.DrawEllipse(DarkMetal, Lifting.LiftingParts.Edge, bobbinAt, bobbinR, bobbinR);
        context.DrawEllipse(LowerBrush, null, bobbinAt, bobbinR * 0.55, bobbinR * 0.55);
        Mark("bobbin", bobbinAt, bobbinR + 6);

        // The thread's path: spool, discs, lever, guide, needle, and down round the bobbin.
        DrawLine(context, pen, [spool + new Vector(-spoolR, 0), new Point(arm.Left + 10, arm.Top + 10), discs, leverEye, guide, eye]);
        var down = Held ? Math.Max(0.6, TakeUp.Down(_angle)) : TakeUp.Down(_angle);
        if (down > 0.02)
        {
            var r = bobbinR + (4 + (bobbinR * 1.2 * down));
            var loop = new List<Point> { eye, new Point(nx - 4, bed.Bottom + 4) };
            for (var d = -40.0; d <= 220 * down + 40; d += 12)
            {
                var a = d * Math.PI / 180;
                loop.Add(bobbinAt + (new Vector(-Math.Cos(a), -Math.Sin(a)) * r));
            }

            loop.Add(new Point(nx + 4, bed.Bottom + 4));
            DrawLine(context, pen, loop, smooth: true);
        }

        // Held still, the slack piles up into a tangle under the cloth.
        if (_nest > 1)
        {
            var size = Math.Min(h * 0.12, 4 * Math.Sqrt(_nest));
            var tangle = new List<Point>();
            for (var s = 0.0; s < Math.Min(60, 6 + (_nest / 20)); s += 0.5)
            {
                tangle.Add(new Point(nx + (size * 0.8 * Math.Sin(s * 1.7)), bed.Bottom + 10 + (size * 0.5 * (1 + Math.Sin(s * 2.3 + 1)))));
            }

            DrawLine(context, UpperPen(thread * 0.8), tangle, smooth: true);
            DiagramText.Draw(context, DiagramText.Get("Sewing_TakeUp_Nest"), new Point(nx + size + 16, bed.Bottom + 14), 12, UpperBrush);
        }

        // The numbers, and the stitch length lever on the pillar.
        DiagramText.Draw(context, DiagramText.Format("Sewing_TakeUp_Label", Number(TakeUp.Loop / 10, "0.0"), Number(TakeUp.PerStitch(length), "0.0"),
            Number(TakeUp.Passes(length), "0")), new Point(bounds.Center.X, bounds.Bottom - 30), 13, DiagramText.Gold, 0.5, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Sewing_TakeUp_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
        DiagramText.Draw(context, DiagramText.Get("Sewing_TakeUp_Slow"), new Point(bounds.X + 16, bounds.Y + 34), 11, DiagramText.Faint);

        var slider = new Point(pillar.Center.X, 0);
        var low = pillar.Bottom - 20;
        var high = pillar.Top + (pillar.Height * 0.35);
        context.DrawLine(new ImmutablePen(Lifting.LiftingParts.Dark, 6, lineCap: PenLineCap.Round), new Point(slider.X, high), new Point(slider.X, low));
        var knob = new Point(slider.X, low - ((low - high) * (length - 1) / 4));
        DiagramText.Draw(context, DiagramText.Format("Sewing_Mm", Number(length, "0.0")), new Point(slider.X - 14, knob.Y), 11.5, DiagramText.Ink, 1, 0.5);
        Grip("length", knob, GripAxis.Vertical, DiagramText.Get("Sewing_Grip_Length"),
            at => Length = Math.Round(1 + (4 * Math.Clamp((low - at.Y) / (low - high), 0, 1)), 1));
        Grip("hold", leverEye, GripAxis.Tap, DiagramText.Get("Sewing_Grip_Hold"), _ => Held = !Held);
    }
}
