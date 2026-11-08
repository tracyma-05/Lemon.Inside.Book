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
/// Spread 5: a crate pushed along the ground, dragged, on log rollers, or on a cart whose wheels turn on ball bearings
/// (one wheel cut open to show them). The push needed is drawn as an arrow and written beside it; the logs and wheels
/// turn as the crate moves; marks on the ground go by, so the logs and wheels can be seen to roll on it, not slide.
/// The reader taps under the crate for the next way of moving it and drags its top up for a bigger, heavier crate.
/// </summary>
public sealed class RollingDiagram : GearPicture
{
    public static readonly StyledProperty<int> WayProperty =
        AvaloniaProperty.Register<RollingDiagram, int>(nameof(Way), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> CrateProperty =
        AvaloniaProperty.Register<RollingDiagram, double>(nameof(Crate), 200, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The lightest and heaviest crate (the slider's ends), kg.</summary>
    private const double Lightest = 50;
    private const double Heaviest = 500;

    /// <summary>The logs' radius, pixels; how fast the logs turn, radians a second.</summary>
    private const double LogRadius = 15;
    private const double Spin = 2;

    static RollingDiagram()
    {
        AffectsRender<RollingDiagram>(WayProperty, CrateProperty);
    }

    /// <summary>0 dragged, 1 on logs, 2 on wheels with ball bearings.</summary>
    public int Way
    {
        get => GetValue(WayProperty);
        set => SetValue(WayProperty, value);
    }

    /// <summary>The crate's mass, kg.</summary>
    public double Crate
    {
        get => GetValue(CrateProperty);
        set => SetValue(CrateProperty, value);
    }

    /// <summary>The crate (to ten kilograms) whose side is <paramref name="side"/> pixels.</summary>
    internal static double CrateForSide(double side) => Math.Clamp(Math.Round((side - 90) / 0.12 / 10) * 10, Lightest, Heaviest);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var way = Math.Clamp(Way, 0, 2);
        var crate = Math.Clamp(Crate, Lightest, Heaviest);
        var force = Rolling.Force(crate, way);
        var groundY = bounds.Y + (bounds.Height * 0.62);
        var ground = new Rect(bounds.X, groundY, bounds.Width, bounds.Bottom - groundY);
        context.FillRectangle(Ground, ground);
        Mark("ground", new Rect(ground.X, ground.Y, ground.Width, 30));

        // Marks on the ground going by as the crate moves on: at the logs' rim speed, so a log's foot keeps pace with the ground.
        var groundPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x50, 0xE6, 0xEA, 0xF2)), 2);
        var passed = (Seconds * Spin * LogRadius) % 40;
        for (var x = bounds.X - passed + 40; x < bounds.Right; x += 40)
        {
            context.DrawLine(groundPen, new Point(x, groundY + 4), new Point(x - 8, groundY + 12));
        }

        // Under the crate: nothing, logs, or a cart on wheels.
        var size = 90 + (crate * 0.12);
        var lift = way switch { 1 => 30.0, 2 => 46.0, _ => 0.0 };
        var crateRect = new Rect(bounds.Center.X - (size / 2) - 20, groundY - lift - size, size, size);
        var spin = Seconds * Spin;
        var rollers = new List<Point>();
        if (way == 1)
        {
            for (var k = 0; k < 3; k++)
            {
                var c = new Point(crateRect.X + (size * (0.18 + (0.32 * k))), groundY - 15);
                context.DrawEllipse(Wood, Edge, c, LogRadius, LogRadius);
                context.DrawLine(new ImmutablePen(Dark, 1.5), c, new Point(c.X + (LogRadius * Math.Cos(spin)), c.Y + (LogRadius * Math.Sin(spin))));
                rollers.AddRange([new Point(c.X - LogRadius, c.Y - LogRadius), new Point(c.X + LogRadius, c.Y + LogRadius)]);
            }
        }
        else if (way == 2)
        {
            context.DrawRectangle(Metal, Edge, new Rect(crateRect.X - 6, groundY - lift, size + 12, 10), 3, 3);
            foreach (var x in new[] { crateRect.X + (size * 0.2), crateRect.X + (size * 0.8) })
            {
                var c = new Point(x, groundY - 18);
                context.DrawEllipse(Dark, new ImmutablePen(Metal, 3), c, 18, 18);
                context.DrawEllipse(Metal, Edge, c, 9, 9);
                for (var k = 0; k < 8; k++)
                {
                    var a = spin + (k * Math.PI / 4);
                    context.DrawEllipse(DiagramText.Gold, null, new Point(c.X + (12.5 * Math.Cos(a)), c.Y + (12.5 * Math.Sin(a))), 2.6, 2.6);
                }

                context.DrawEllipse(Dark, null, c, 4, 4);
                rollers.AddRange([new Point(c.X - 18, c.Y - 18), new Point(c.X + 18, c.Y + 18)]);
            }

            DiagramText.Draw(context, DiagramText.Get("Friction_Rolling_BearingLabel"), new Point(crateRect.X + (size * 0.8), groundY + 8), 11.5, DiagramText.Gold, 0.5, 0);
        }
        else
        {
            var pen = new ImmutablePen(DiagramText.Faint, 1.2, new ImmutableDashStyle([3, 3], 0));
            foreach (var x in new[] { 0.25, 0.75 }) context.DrawEllipse(null, pen, new Point(crateRect.X + (size * x), groundY - 15), 15, 15);
            rollers.AddRange([new Point(crateRect.X, groundY - 30), new Point(crateRect.Right, groundY)]);
            var scrape = new ImmutablePen(GripBrush, 1.5);
            for (var k = 1; k <= 4; k++) context.DrawLine(scrape, new Point(crateRect.X - (k * 12), groundY - 2), new Point(crateRect.X - (k * 12) - 8, groundY - 2));
            DiagramText.Draw(context, DiagramText.Get("Friction_Rolling_ScrapeLabel"), new Point(crateRect.X - 30, groundY + 8), 11.5, GripBrush, 0.5, 0);
        }

        Mark("rollers", rollers, 4);
        context.DrawRectangle(FrictionParts.Crate, Edge, crateRect, 4, 4);
        DiagramText.Draw(context, Kgf(crate), crateRect.Center, 14, Dark, 0.5, 0.5, bold: true);
        Mark("crate", crateRect);

        // The hand pushing, with the force it needs.
        var handAt = new Point(crateRect.X - 30, crateRect.Center.Y);
        var hand = DrawHand(context, handAt, 34);
        Mark("hand", hand);
        var length = Math.Max(14, force / 200 * 120);
        Arrow(context, new Point(hand.Left - length - 6, crateRect.Top - 18), new Point(hand.Left + 30, crateRect.Top - 18), PullBrush, Kgf(force), 0.5, 1.4);
        DiagramText.Draw(context, DiagramText.Get(RollingSpread.WayKey(way)), new Point(bounds.Center.X, groundY + 60), 14, DiagramText.Ink, 0.5, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Friction_Rolling_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);

        // The reader's handles: under the crate's front, tapped for the next way of moving it (dragged, logs, wheels,
        // round again); the crate's top, raised for a bigger, heavier crate.
        Grip("way", new Point(crateRect.Right + 26, groundY - 16), GripAxis.Tap, DiagramText.Get("Friction_Grip_Way"), _ => Way = (way + 1) % 3);
        Grip("crate", new Point(crateRect.Center.X, crateRect.Top), GripAxis.Vertical, DiagramText.Get("Friction_Grip_Crate"),
            p => Crate = CrateForSide(groundY - lift - p.Y));
    }
}
