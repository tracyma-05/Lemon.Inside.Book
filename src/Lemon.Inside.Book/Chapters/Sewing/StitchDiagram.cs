using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Sewing.SewingParts;

namespace Lemon.Inside.Book.Chapters.Sewing;

/// <summary>
/// Spread 1: a seam cut along its length, the cloth drawn much thicker than life. Sewn as a lockstitch, a red thread
/// runs along the top and a blue one along the underside, hooked round each other half-way through the cloth at every
/// hole. Sewn as a chain stitch, one red thread goes down each hole and loops along the underside through the next
/// loop. Pull the loose end and the chain comes undone stitch after stitch; the lockstitch only gives up its last bit.
/// The reader taps the seam to switch stitch, drags the second hole for longer or shorter stitches, and taps the loose
/// end to pull it.
/// </summary>
public sealed class StitchDiagram : GearPicture
{
    public static readonly StyledProperty<bool> ChainProperty =
        AvaloniaProperty.Register<StitchDiagram, bool>(nameof(Chain), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> LengthProperty =
        AvaloniaProperty.Register<StitchDiagram, double>(nameof(Length), 2.5, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> PulledProperty =
        AvaloniaProperty.Register<StitchDiagram, bool>(nameof(Pulled), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>How many stitches have come undone, and how many the picture holds.</summary>
    private double _undone;
    private int _count = 8;

    static StitchDiagram()
    {
        AffectsRender<StitchDiagram>(ChainProperty, LengthProperty, PulledProperty);
    }

    public bool Chain
    {
        get => GetValue(ChainProperty);
        set => SetValue(ChainProperty, value);
    }

    /// <summary>The stitch length, mm.</summary>
    public double Length
    {
        get => GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    public bool Pulled
    {
        get => GetValue(PulledProperty);
        set => SetValue(PulledProperty, value);
    }

    protected override void Advance(double seconds)
    {
        // A pulled chain comes undone a few stitches a second; let go (or a lockstitch) and it is sewn up again at once.
        var target = Chain && Pulled ? _count : 0;
        _undone = target > _undone ? Math.Min(target, _undone + (seconds * 2.5)) : target;
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var length = Seam.Clamp(Length);
        var mm = Math.Min(30, (bounds.Width - 80) / 16);
        var pitch = length * mm;
        var thick = 2.4 * Seam.Cloth * mm;
        var top = bounds.Center.Y - (thick / 2) - 10;
        var bottom = top + thick;
        var left = bounds.X + 24;
        var right = bounds.Right - 24;
        var x0 = left + (pitch * 0.8);
        _count = Math.Max(2, (int)Math.Floor((right - 40 - x0) / pitch) + 1);
        var thread = Math.Clamp(mm * 0.22, 3, 6);

        DrawCloth(context, left, right, top, bottom);
        var cloth = new Rect(left, top, right - left, bottom - top);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Stitch_Top"), new Point(right, top - 26), 11.5, DiagramText.Faint, 1, 1);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Stitch_Under"), new Point(right, bottom + 30), 11.5, DiagramText.Faint, 1, 0);

        // The first few holes stand for them all: a ring round the whole seam would sit under the threads' rings.
        var holes = new List<Point>();
        for (var i = 0; i < Math.Min(3, _count); i++)
        {
            holes.AddRange([new Point(x0 + (i * pitch), top), new Point(x0 + (i * pitch), bottom)]);
        }

        Point end;
        if (Chain)
        {
            end = DrawChainSeam(context, x0, pitch, _count, top, bottom, _undone, thread, Seconds);
            Mark("upper", [new Point(left, top - thread), new Point(x0 + ((_count - 1) * pitch), bottom + (pitch * 0.4))], 4);
        }
        else
        {
            var knots = DrawLockSeam(context, x0, pitch, _count, top, bottom, 0.5, thread);
            Mark("knots", knots, 7);
            Mark("upper", [new Point(left, top - thread), new Point(x0 + ((_count - 0.5) * pitch), top)], 4);
            Mark("lower", [new Point(left, bottom), new Point(x0 + ((_count - 0.5) * pitch), bottom + thread)], 4);
            end = new Point(x0 + ((_count - 0.5) * pitch), top - (thread / 2));
            if (Pulled)
            {
                // The lockstitch gives up only the free end: a short tail, the stitches all still locked.
                var tail = new List<Point>();
                for (var s = 0.0; s <= 50; s += 5)
                {
                    tail.Add(new Point(end.X + (s * 0.6), end.Y - (s * 0.6) + (3 * Math.Sin((s / 10) + (Seconds * 2)))));
                }

                DrawLine(context, UpperPen(thread), tail, smooth: true);
                end = tail[^1];
            }
        }

        Mark("holes", holes, 5);
        context.DrawEllipse(UpperBrush, null, end, thread * 0.9, thread * 0.9);

        // The rule for what is drawn, and how much thread ten centimetres of it take.
        var key = Chain ? "Sewing_Stitch_RuleChain" : "Sewing_Stitch_RuleLock";
        DiagramText.Draw(context, DiagramText.Get(key), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Stitch_Thicker"), new Point(bounds.X + 16, bounds.Y + 34), 11, DiagramText.Faint);
        var used = Seam.Thread(length, Chain) / Seam.Length;
        DiagramText.Draw(context, DiagramText.Format("Sewing_Stitch_Used", Number(used, "0.0")), new Point(cloth.Center.X, bottom + 80), 13,
            DiagramText.Gold, 0.5, 0, bold: true);
        if (Chain && Pulled && _undone >= _count - 0.01)
        {
            DiagramText.Draw(context, DiagramText.Get("Sewing_Stitch_AllUndone"), new Point(cloth.Center.X, top - 70), 13, DiagramText.Ink, 0.5, 1);
        }

        // The reader's handles: tap the cloth to switch stitch, drag the second hole, tap the loose end.
        Grip("kind", new Point(left + 20, cloth.Center.Y), GripAxis.Tap, DiagramText.Get("Sewing_Grip_Kind"), _ => Chain = !Chain);
        Grip("length", new Point(x0 + pitch, top - 18), GripAxis.Horizontal, DiagramText.Get("Sewing_Grip_Length"),
            at => Length = Math.Round(Math.Clamp((at.X - x0) / mm, 1, 5), 1));
        Grip("pull", end, GripAxis.Tap, DiagramText.Get("Sewing_Grip_Pull"), _ => Pulled = !Pulled);
    }
}
