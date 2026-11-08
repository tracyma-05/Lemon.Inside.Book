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
/// Spread 2: the needle close up, going through the cloth, with the handwheel that drives it. The red thread runs
/// down the needle's long groove, through the eye just above the point, and back up the other side. Turn the wheel
/// past the bottom and, as the needle starts back up, the cloth holds the thread on the short side so it bulges into a
/// loop, widest about a sixth of a turn past the bottom: that is where the hook's point will pass. With a hand-sewing
/// needle's eye at the top no loop forms. The reader turns the wheel by its handle and taps the needle to swap it.
/// </summary>
public sealed class NeedleDiagram : GearPicture
{
    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<NeedleDiagram, double>(nameof(Angle), 150, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> HandNeedleProperty =
        AvaloniaProperty.Register<NeedleDiagram, bool>(nameof(HandNeedle), defaultBindingMode: BindingMode.TwoWay);

    static NeedleDiagram()
    {
        AffectsRender<NeedleDiagram>(AngleProperty, HandNeedleProperty);
    }

    /// <summary>The handwheel's angle, degrees: 0 with the needle at the top.</summary>
    public double Angle
    {
        get => GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    public bool HandNeedle
    {
        get => GetValue(HandNeedleProperty);
        set => SetValue(HandNeedleProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var angle = Needle.Wrap(Angle);
        var mm = Math.Min((bounds.Height - 150) / 52, (bounds.Width - 60) / 34);
        var nx = bounds.X + (bounds.Width * 0.38);
        var lowest = bounds.Bottom - 70 - (6 * mm);
        var tip = new Point(nx, lowest - (Needle.Rise(angle) * mm));
        var clothTop = lowest - (Needle.ClothHeight * mm);
        var clothBottom = clothTop + (2 * mm);
        var width = Math.Max(10, 1.2 * mm);
        var thread = Math.Clamp(mm * 0.28, 2.5, 4.5);
        var pen = UpperPen(thread);

        // The cloth, with the needle through it (or the hole it left).
        DrawCloth(context, bounds.X + 20, nx - (width * 0.8), clothTop, clothBottom);
        DrawCloth(context, nx + (width * 0.8), bounds.Right - 20, clothTop, clothBottom);
        Mark("cloth", new Rect(bounds.X + 20, clothTop, nx - bounds.X - 60, clothBottom - clothTop));
        DiagramText.Draw(context, DiagramText.Get("Sewing_Needle_Cloth"), new Point(bounds.X + 24, clothTop - 4), 11.5, DiagramText.Faint, 0, 1);

        // Where the hook's point passes, a little above the eye's lowest place.
        var eyeDrop = width * 2.2 * 1.3;
        var hookY = lowest - eyeDrop - ((Hook.RiseAt(0) + 1.5) * mm);
        var dash = new ImmutablePen(DiagramText.Faint, 1, new ImmutableDashStyle([4, 4], 0));
        context.DrawLine(dash, new Point(nx - (9 * mm), hookY), new Point(nx + (7 * mm), hookY));
        DiagramText.DrawArrowAlong(context, new Point(nx - (9 * mm), hookY), new Point(nx - (6 * mm), hookY), DiagramText.Faint, 8);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Needle_HookPath"), new Point(nx - (9 * mm), hookY + 4), 11, DiagramText.Faint);

        // The thread: down the long groove on the left, through the eye, up the short side and off to the last stitch.
        var length = 40 * mm;
        var eyeAt = HandNeedle ? new Point(nx, tip.Y - length + (width * 1.4)) : new Point(nx, tip.Y - eyeDrop);
        var loop = HandNeedle ? 0 : Needle.Loop(angle);
        if (HandNeedle)
        {
            // A hand needle's thread trails from the eye at the top and only follows the needle through.
            DrawLine(context, pen, [eyeAt, new Point(nx + (width * 2), eyeAt.Y + (4 * mm)), new Point(nx + (width * 0.6), clothTop - (thread / 2)),
                new Point(bounds.Right - 20, clothTop - (thread / 2))], smooth: true);
        }
        else
        {
            var side = nx + (width * 0.55);
            var up = new List<Point> { eyeAt };
            if (eyeAt.Y > clothBottom + thread)
            {
                // Below the cloth: up the short side, bulging into the loop on the way up, and out through the hole.
                up.Add(new Point(side, eyeAt.Y - (0.4 * mm)));
                if (loop > 0.05)
                {
                    var bulge = side + (loop * mm);
                    up.AddRange([new Point(bulge, eyeAt.Y - (1.6 * mm)), new Point((bulge * 0.7) + (side * 0.3), eyeAt.Y - (3.2 * mm)),
                        new Point(side, eyeAt.Y - (4 * mm))]);
                }

                if (up[^1].Y > clothBottom + thread) up.Add(new Point(side, clothBottom + thread));
            }
            else
            {
                // Above the cloth: the thread hangs from the eye down to the last stitch.
                up.Add(new Point(side, eyeAt.Y + (0.6 * mm)));
            }

            up.AddRange([new Point(side, clothTop - (thread / 2)), new Point(side + (2 * mm), clothTop - (thread / 2)),
                new Point(bounds.Right - 20, clothTop - (thread / 2))]);
            DrawLine(context, pen, up, smooth: true);
        }

        var drawnEye = DrawNeedle(context, tip, length, width, eyeAtPoint: !HandNeedle);
        if (!HandNeedle)
        {
            // The thread lies in the needle's long groove, down to the eye.
            DrawLine(context, UpperPen(thread * 0.8), [new Point(nx - (width * 0.2), bounds.Y + 50), new Point(nx - (width * 0.2), eyeAt.Y)]);
        }
        Mark("needle", new Rect(nx - width, tip.Y - length, 2 * width, length));
        Mark("eye", drawnEye, Math.Max(10, width));
        Mark("loop", new Rect(nx + (width * 0.5), eyeAt.Y - (4.5 * mm), Math.Max(4, loop) * mm, 4.5 * mm));

        // Which way the needle is going, how far up it is and how wide the loop.
        var rising = Needle.Rising(angle);
        var arrowFrom = new Point(nx - (width * 2.2), tip.Y - (12 * mm));
        DiagramText.DrawArrowAlong(context, arrowFrom, arrowFrom + new Vector(0, rising ? -16 : 16), DiagramText.Ink, 9);
        DiagramText.Draw(context, DiagramText.Format("Sewing_Needle_Label", Number(Needle.Rise(angle), "0.0"), Number(loop, "0.0")),
            new Point(bounds.Center.X, bounds.Bottom - 40), 13, DiagramText.Gold, 0.5, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Get(HandNeedle ? "Sewing_Needle_RuleHand" : "Sewing_Needle_Rule"),
            new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Needle_Bigger"), new Point(bounds.X + 16, bounds.Y + 34), 11, DiagramText.Faint);

        // The handwheel, top right: the reader turns it by its handle.
        var radius = Math.Min(bounds.Width * 0.16, 70);
        var centre = new Point(bounds.Right - radius - 24, bounds.Y + radius + 60);
        var handle = DrawHandwheel(context, centre, radius, angle);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Needle_Wheel"), new Point(centre.X, centre.Y + radius + 6), 11.5, DiagramText.Faint, 0.5);
        Grip("wheel", handle, GripAxis.Free, DiagramText.Get("Sewing_Grip_Wheel"), at => Angle = Math.Round(AngleAround(centre, at)));
        Grip("swap", new Point(nx, tip.Y - (length * 0.75)), GripAxis.Tap, DiagramText.Get("Sewing_Grip_Swap"), _ => HandNeedle = !HandNeedle);
    }
}
