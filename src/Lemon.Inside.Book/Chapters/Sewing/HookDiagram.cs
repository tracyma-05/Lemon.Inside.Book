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
/// Spread 3: under the needle plate, seen from the front. The needle comes down beside a hook that turns twice for
/// every stitch round the bobbin and its case. Set right, the hook's point passes the needle just as the loop opens,
/// carries it round the bobbin, and lets it go to be pulled up with the blue thread inside it; set too early or too late
/// the point slips past an empty needle and the stitch is skipped. Below, the last stitches sewn: a skipped one is a long
/// float with no hole. Time runs slow. The reader drags the timing marker along its scale.
/// </summary>
public sealed class HookDiagram : GearPicture
{
    public static readonly StyledProperty<double> TimingProperty =
        AvaloniaProperty.Register<HookDiagram, double>(nameof(Timing), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Handwheel degrees a second: a stitch every four seconds.</summary>
    private const double Pace = 90;

    private double _angle = 150;

    /// <summary>The last stitches sewn: was each one caught?</summary>
    private readonly List<bool> _sewn = [true, true, true, true, true, true, true, true];

    static HookDiagram()
    {
        AffectsRender<HookDiagram>(TimingProperty);
    }

    /// <summary>How late the hook is set, degrees of the handwheel (negative: early).</summary>
    public double Timing
    {
        get => GetValue(TimingProperty);
        set => SetValue(TimingProperty, value);
    }

    protected override void Advance(double seconds)
    {
        var before = Needle.Wrap(_angle - Hook.Right);
        _angle = Needle.Wrap(_angle + (seconds * Pace));
        var after = Needle.Wrap(_angle - Hook.Right);

        // A stitch is finished once the point has been past the needle: it went in, or it did not.
        if (after < before)
        {
            _sewn.Add(Hook.Catches(Timing));
            if (_sewn.Count > 12) _sewn.RemoveAt(0);
        }
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var timing = Math.Clamp(Timing, -30, 30);
        var caught = Hook.Catches(timing);
        var mm = Math.Min((bounds.Width - 60) / 46, (bounds.Height - 200) / 50);
        var nx = bounds.Center.X - (2 * mm);
        var width = Math.Max(8, 1.0 * mm);
        var eyeDrop = width * 2.2 * 1.3;
        var lowestTip = bounds.Y + 70 + (26 * mm);
        var lowestEye = lowestTip - eyeDrop;
        var radius = 14 * mm;
        var hc = new Point(nx, lowestEye - (4 * mm) + radius);
        var plateY = lowestTip - (Needle.ClothHeight * mm);
        var thread = Math.Clamp(mm * 0.3, 2.5, 4.5);

        // The needle plate and the cloth on it.
        context.FillRectangle(Metal, new Rect(bounds.X + 20, plateY, bounds.Width - 40, 1.2 * mm));
        DrawCloth(context, bounds.X + 20, bounds.Right - 20, plateY - (1.6 * mm), plateY);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Hook_Plate"), new Point(bounds.Right - 24, plateY + (1.4 * mm)), 11.5, DiagramText.Faint, 1);

        // The bobbin case and the bobbin's blue thread, up through the plate to the cloth.
        var bobbin = TakeUp.Bobbin / 2 * mm * 0.85;
        context.DrawEllipse(DarkMetal, Lifting.LiftingParts.Edge, hc, bobbin, bobbin);
        context.DrawEllipse(LowerBrush, Lifting.LiftingParts.Edge, hc, bobbin * 0.6, bobbin * 0.6);
        context.DrawEllipse(DarkMetal, null, hc, bobbin * 0.2, bobbin * 0.2);
        DrawLine(context, LowerPen(thread), [hc + new Vector(bobbin * 0.5, -bobbin * 0.3), new Point(nx + (3 * mm), hc.Y - bobbin - (2 * mm)),
            new Point(nx + (1.5 * mm), plateY + (1.2 * mm)), new Point(nx + (1 * mm), plateY)], smooth: true);
        Mark("bobbin", hc, bobbin);

        // The hook: a ring round the case with its point, turning twice a stitch.
        var turn = Hook.Turn(_angle, timing);
        var ringPen = new ImmutablePen(Metal, Math.Max(4, 1.4 * mm));
        context.DrawEllipse(null, ringPen, hc, radius - (0.7 * mm), radius - (0.7 * mm));
        var t = turn * Math.PI / 180;
        Point Rim(double degrees, double r)
        {
            var a = degrees * Math.PI / 180;
            return hc + (new Vector(Math.Sin(a), -Math.Cos(a)) * r);
        }

        var point = Rim(turn, radius);
        Lifting.LiftingParts.DrawPolygon(context, Metal, Lifting.LiftingParts.Edge,
            [Rim(turn - 28, radius - (1.4 * mm)), Rim(turn - 22, radius + (0.6 * mm)), point + new Vector(Math.Cos(t), Math.Sin(t)) * 2.5 * mm,
                Rim(turn, radius - (1.4 * mm))]);
        Mark("hook", hc, radius + (1.5 * mm));

        // The loop: opening beside the rising needle, then (if the point took it) carried round and drawn back up.
        var down = TakeUp.Down(_angle);
        var p = Needle.Wrap(_angle - Hook.Right - timing);
        var tip = new Point(nx, lowestTip - (Needle.Rise(_angle) * mm));
        var eye = new Point(nx, tip.Y - eyeDrop);
        var pen = UpperPen(thread);
        var side = nx + (width * 0.55);
        if (caught && p < 190)
        {
            // Taken: the two sides of the loop run from the eye round the rim, behind the point.
            var reach = Math.Min(turn > 300 && p > 100 ? 360 : turn, 330);
            var round = new List<Point> { eye };
            for (var d = 0.0; d <= reach; d += 10) round.Add(Rim(d, radius + (2 * mm * (0.4 + (0.6 * down)))));
            if (p > 120)
            {
                // Let go: the loop slides off the case and is drawn back up, tighter and tighter round the bobbin.
                round.Clear();
                var r = bobbin + ((radius + (2 * mm) - bobbin) * down);
                round.Add(eye);
                for (var d = -20.0; d <= 380; d += 12) round.Add(hc + (new Vector(Math.Sin(d * Math.PI / 180), -Math.Cos(d * Math.PI / 180)) * r));
            }

            round.Add(new Point(side, eye.Y - (1 * mm)));
            round.Add(new Point(side, plateY));
            DrawLine(context, pen, round, smooth: true);
        }
        else
        {
            var loop = Needle.Loop(_angle);
            var up = new List<Point> { eye, new Point(side, eye.Y - (0.4 * mm)) };
            if (loop > 0.05)
            {
                var bulge = side + (loop * mm);
                up.AddRange([new Point(bulge, eye.Y - (1.6 * mm)), new Point(side, eye.Y - (4 * mm))]);
            }

            up.Add(new Point(side, plateY));
            DrawLine(context, pen, up, smooth: true);
        }

        DrawNeedle(context, tip, 36 * mm, width);

        // The thread lies in the needle's long groove, down to the eye.
        DrawLine(context, UpperPen(thread * 0.8), [new Point(nx - (width * 0.2), bounds.Y + 50), new Point(nx - (width * 0.2), eye.Y)]);
        Mark("loop", new Rect(nx - (2 * mm), lowestEye - (6 * mm), 6 * mm, 6 * mm));

        // The seam so far, seen from above: a hole for every caught stitch, a long float where one was skipped.
        var seamY = hc.Y + radius + (7 * mm);
        var pitch = Math.Min(3 * mm, (bounds.Width - 80) / _sewn.Count);
        var x = bounds.Center.X - (pitch * _sewn.Count / 2);
        var seam = new Rect(x - 6, seamY - 10, (pitch * _sewn.Count) + 12, 20);
        context.FillRectangle(Cloth, seam);
        var run = x;
        for (var i = 0; i < _sewn.Count; i++)
        {
            var next = x + ((i + 1) * pitch);
            if (_sewn[i] || i == _sewn.Count - 1)
            {
                context.DrawLine(pen, new Point(run + 2, seamY), new Point(next - 2, seamY));
                run = next;
            }
            else
            {
                DiagramText.Draw(context, "×", new Point(next, seamY + 6), 12, UpperBrush, 0.5, 0);
            }
        }

        Mark("seam", seam);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Hook_Seam"), new Point(seam.Left, seamY - 14), 11.5, DiagramText.Faint, 0, 1);

        // The timing scale below: the window where the loop is wide enough, and the reader's marker on it.
        var scaleY = seamY + 52;
        var per = Math.Min(5, (bounds.Width - 100) / 60);
        var centreX = bounds.Center.X;
        var good = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0x7A, 0xD9, 0x8C)), 8, lineCap: PenLineCap.Round);
        var (from, to) = Window();
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 1), new Point(centreX - (30 * per), scaleY), new Point(centreX + (30 * per), scaleY));
        context.DrawLine(good, new Point(centreX + (from * per), scaleY), new Point(centreX + (to * per), scaleY));
        DiagramText.Draw(context, DiagramText.Get("Sewing_Hook_ScaleEarly"), new Point(centreX - (30 * per), scaleY + 12), 11, DiagramText.Faint, 0);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Hook_ScaleLate"), new Point(centreX + (30 * per), scaleY + 12), 11, DiagramText.Faint, 1);
        DiagramText.Draw(context, DiagramText.Format(caught ? "Sewing_Hook_Caught" : "Sewing_Hook_Skipped", Number(Hook.LoopAt(timing), "0.0")),
            new Point(centreX, scaleY - 12), 13, caught ? DiagramText.Gold : UpperBrush, 0.5, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Hook_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
        DiagramText.Draw(context, DiagramText.Get("Sewing_Hook_Slow"), new Point(bounds.X + 16, bounds.Y + 34), 11, DiagramText.Faint);
        Grip("timing", new Point(centreX + (timing * per), scaleY), GripAxis.Horizontal, DiagramText.Get("Sewing_Grip_Timing"),
            at => Timing = Math.Round(Math.Clamp((at.X - centreX) / per, -30, 30)));
    }

    /// <summary>The timings, degrees, at which the point finds a loop wide enough.</summary>
    internal static (double From, double To) Window()
    {
        double from = 0, to = 0;
        while (from > -60 && Hook.Catches(from - 0.5)) from -= 0.5;
        while (to < 60 && Hook.Catches(to + 0.5)) to += 0.5;
        return (from, to);
    }
}
