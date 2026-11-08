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
/// Spread 3: a car braking on a dry road, from the side. Two bars along the road, to the same scale in metres, show
/// where it stops with its wheels still turning and with them locked; the one being used is bright. Below, a tyre's
/// patch on the road: at rest against the road while it rolls, scraping along it once the wheel is locked.
/// The stop plays out in real time: a marker runs along the bright bar, slowing, while the close-up wheel turns with the
/// road (or stays locked as the road scrapes past), then it starts again. The reader drags the end of the bright bar to
/// set the speed that stops there, and taps a wheel to lock or free the brakes.
/// </summary>
public sealed class SkidDiagram : GearPicture
{
    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<SkidDiagram, double>(nameof(Speed), 50, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> LockedProperty =
        AvaloniaProperty.Register<SkidDiagram, bool>(nameof(Locked), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The longest stop drawn, m.</summary>
    private const double Longest = 85;

    /// <summary>The slowest and fastest speeds (the slider's ends), km/h; the pause after each stop, s.</summary>
    private const double Slowest = 20;
    private const double Fastest = 120;
    private const double Pause = 1.2;

    /// <summary>Seconds into the present stop; how far the close-up wheel has turned, radians; how far the road under it has gone, m.</summary>
    private double _time;
    private double _wheel;
    private double _road;

    /// <summary>The close-up wheel's radius, m.</summary>
    private const double Rim = 0.33;

    static SkidDiagram()
    {
        AffectsRender<SkidDiagram>(SpeedProperty, LockedProperty);
    }

    /// <summary>The car's speed, km/h.</summary>
    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    public bool Locked
    {
        get => GetValue(LockedProperty);
        set => SetValue(LockedProperty, value);
    }

    /// <summary>
    /// Where a car braking from <paramref name="speed"/> km/h is, m, and how fast it still goes, m/s,
    /// <paramref name="seconds"/> after the brakes go on: slowing steadily at the tyres' grip × g until it stops.
    /// </summary>
    internal static (double Travelled, double Speed) Stop(double speed, bool locked, double seconds)
    {
        var v = speed / 3.6;
        var slowing = (locked ? Skid.Locked : Skid.Rolling) * Skid.G;
        var t = Math.Clamp(seconds, 0, v / slowing);
        return ((v * t) - (slowing * t * t / 2), v - (slowing * t));
    }

    /// <summary>The speed (to 1 km/h) that stops in <paramref name="metres"/>, the reverse of <see cref="Skid.Distance"/>.</summary>
    internal static double SpeedFor(double metres, bool locked) =>
        Math.Clamp(Math.Round(Math.Sqrt(2 * (locked ? Skid.Locked : Skid.Rolling) * Skid.G * Math.Max(0, metres)) * 3.6), Slowest, Fastest);

    protected override void Advance(double seconds)
    {
        var speed = Math.Clamp(Speed, Slowest, Fastest);
        var locked = Locked;
        _time += seconds;
        var stopping = speed / 3.6 / ((locked ? Skid.Locked : Skid.Rolling) * Skid.G);
        if (_time > stopping + Pause) _time = 0;

        // The close-up wheel (a third of a metre round its rim's middle) turns with the road while it rolls; locked, it
        // stands still and the road slides under it. Drawn an eighth as fast, so the turning can be followed.
        var now = Stop(speed, locked, _time).Speed / 8;
        _road += now * seconds;
        if (!locked) _wheel += now * seconds / Rim;
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var speed = Math.Clamp(Speed, Slowest, Fastest);
        var locked = Locked;
        var left = bounds.X + 30;
        var perMetre = (bounds.Width - 70) / (Longest + 8);
        var roadY = bounds.Y + (bounds.Height * 0.36);

        var road = new Rect(bounds.X + 10, roadY, bounds.Width - 20, 30);
        context.DrawRectangle(Asphalt, null, road);
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 2, new ImmutableDashStyle([6, 6], 0)), new Point(road.Left, road.Center.Y), new Point(road.Right, road.Center.Y));
        Mark("road", road);

        // The car, its front at the start of the bars.
        var car = new Rect(left, roadY - 34, Math.Max(7 * perMetre, 60), 24);
        context.DrawRectangle(Load, Edge, car, 6, 6);
        context.DrawRectangle(Load, Edge, new Rect(car.X + (car.Width * 0.25), car.Y - 14, car.Width * 0.45, 16), 5, 5);
        Mark("car", car);
        var wheels = new[] { new Point(car.X + (car.Width * 0.2), roadY - 8), new Point(car.X + (car.Width * 0.8), roadY - 8) };
        foreach (var w in wheels)
        {
            context.DrawEllipse(Dark, new ImmutablePen(Metal, 2), w, 9, 9);
            if (locked) context.DrawLine(new ImmutablePen(GripBrush, 2), new Point(w.X - 5, w.Y - 5), new Point(w.X + 5, w.Y + 5));
        }

        Mark("tyres", wheels, 12);
        Grip("lock", wheels[1], GripAxis.Tap, DiagramText.Get(locked ? "Friction_Grip_Unlock" : "Friction_Grip_Lock"), _ => Locked = !Locked);

        // The two stops, to scale.
        var start = car.Right;
        var bars = new List<Point>();
        foreach (var (isLocked, y) in new[] { (false, roadY + 46), (true, roadY + 76) })
        {
            var d = Skid.Distance(speed, isLocked);
            var bar = new Rect(start, y, d * perMetre, 18);
            var active = isLocked == locked;
            context.DrawRectangle(isLocked ? GripBrush : PullBrush, null, bar, 3, 3);
            if (!active) context.FillRectangle(new ImmutableSolidColorBrush(Color.FromArgb(0x99, 0x10, 0x14, 0x1E)), bar);
            var text = DiagramText.Format(isLocked ? "Friction_Skid_LockedBar" : "Friction_Skid_RollingBar", Number(d, "0"));
            var inside = bar.Right + 124 > bounds.Right;
            DiagramText.Draw(context, text, new Point(inside ? bar.Right - 20 : bar.Right + 20, bar.Center.Y), 12,
                inside ? Dark : active ? DiagramText.Ink : DiagramText.Faint, inside ? 1 : 0, 0.5, bold: active);
            bars.AddRange([bar.TopLeft, bar.BottomRight]);
            if (active)
            {
                // Where the car has got to in this stop, slowing as it goes.
                var x = start + (Stop(speed, locked, _time).Travelled * perMetre);
                DrawPolygon(context, DiagramText.Gold, Edge, [new Point(x, bar.Y - 1), new Point(x - 7, bar.Y - 11), new Point(x + 7, bar.Y - 11)]);

                // The reader's handle: the end of the stop, dragged out for the speed that stops there.
                Grip("speed", new Point(bar.Right, bar.Center.Y), GripAxis.Horizontal, DiagramText.Get("Friction_Grip_Stop"),
                    p => Speed = SpeedFor((p.X - start) / perMetre, locked));
            }
        }

        Mark("distance", bars, 4);
        for (var m = 0; m <= 80; m += 20)
        {
            var x = start + (m * perMetre);
            context.DrawLine(Axis, new Point(x, roadY + 100), new Point(x, roadY + 106));
            DiagramText.Draw(context, DiagramText.Format("Friction_Metres", Number(m, "0")), new Point(x, roadY + 108), 10.5, DiagramText.Faint, 0.5, 0);
        }

        // The tyre's patch, close up.
        var centre = new Point(bounds.Center.X, bounds.Y + (bounds.Height * 0.78));
        var r = Math.Min(bounds.Height * 0.13, 80);
        context.DrawRectangle(Asphalt, null, new Rect(centre.X - (r * 2.2), centre.Y + r, r * 4.4, 14));
        context.DrawEllipse(Dark, new ImmutablePen(Metal, 3), centre, r, r);
        context.DrawEllipse(null, new ImmutablePen(Metal, 1.5), centre, r * 0.45, r * 0.45);
        for (var k = 0; k < 5; k++)
        {
            var a = _wheel + (k * 2 * Math.PI / 5);
            context.DrawLine(new ImmutablePen(Metal, 2), centre + new Vector(r * 0.45 * Math.Cos(a), r * 0.45 * Math.Sin(a)), centre + new Vector(r * 0.92 * Math.Cos(a), r * 0.92 * Math.Sin(a)));
        }

        // Marks on the road going by under the wheel, at the same pace as its rim.
        var markPen = new ImmutablePen(DiagramText.Faint, 2);
        var gap = r * 0.6;
        for (var k = -4; k <= 4; k++)
        {
            var x = centre.X + (k * gap) - ((_road / Rim * r) % gap);
            if (Math.Abs(x - centre.X) < r * 2.1) context.DrawLine(markPen, new Point(x, centre.Y + r + 10), new Point(x + 6, centre.Y + r + 10));
        }

        var patch = new Rect(centre.X - (r * 0.35), centre.Y + r - 4, r * 0.7, 6);
        context.FillRectangle(locked ? GripBrush : PullBrush, patch);
        if (locked)
        {
            var pen = new ImmutablePen(GripBrush, 1.5);
            for (var k = 1; k <= 4; k++) context.DrawLine(pen, new Point(patch.Left - (k * 10), centre.Y + r + 6), new Point(patch.Left - (k * 10) - 8, centre.Y + r + 6));
        }

        DiagramText.Draw(context, DiagramText.Get(locked ? "Friction_Skid_PatchSliding" : "Friction_Skid_PatchStill"), new Point(centre.X, centre.Y + r + 22), 12.5,
            locked ? GripBrush : PullBrush, 0.5, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Friction_Speed", Number(speed, "0")), new Point(car.Center.X, car.Top - 22), 12.5, DiagramText.Gold, 0.5, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Friction_Skid_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
