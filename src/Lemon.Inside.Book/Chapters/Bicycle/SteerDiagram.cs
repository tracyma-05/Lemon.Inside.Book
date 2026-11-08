using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Bicycle;

/// <summary>
/// Spread 6: the front of a bike from the side, with its steering axis drawn down to the ground and the trail between
/// that point and the tyre's touch. Below, the same wheel from above, knocked by a bump every few seconds: with trail
/// it swings back straight; without, it does not; with negative trail it swings away to full lock. The reader leans the
/// head tube by its top (to the right stands it up steeper) and drags the fork's bend forwards or back for its offset.
/// </summary>
public sealed class SteerDiagram : GearPicture
{
    public static readonly StyledProperty<double> HeadAngleProperty =
        AvaloniaProperty.Register<SteerDiagram, double>(nameof(HeadAngle), 72, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> OffsetProperty =
        AvaloniaProperty.Register<SteerDiagram, double>(nameof(Offset), 45, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The sliders' ranges: degrees of head angle, millimetres of offset.</summary>
    private const double LeastAngle = 62;
    private const double MostAngle = 80;
    private const double MostOffset = 80;

    /// <summary>Seconds between bumps.</summary>
    private const double BumpEvery = 3.5;

    private static readonly IPen Ground = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0xA0, 0xE6, 0xEA, 0xF2)), 2);
    private static readonly IPen Axis = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x6F, 0xD8, 0xFF)), 1.6, new ImmutableDashStyle([6, 4], 0));
    private static readonly IImmutableBrush Danger = new ImmutableSolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x5A));

    private readonly Steering _steering = new();
    private double _sinceBump = BumpEvery - 0.6;
    private double _roll;

    static SteerDiagram()
    {
        AffectsRender<SteerDiagram>(HeadAngleProperty, OffsetProperty);
    }

    /// <summary>The steering axis's angle up from the ground, degrees.</summary>
    public double HeadAngle
    {
        get => GetValue(HeadAngleProperty);
        set => SetValue(HeadAngleProperty, value);
    }

    /// <summary>How far the fork bends the axle forward of the steering axis, millimetres.</summary>
    public double Offset
    {
        get => GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    private double Trail => Steering.Trail(HeadAngle, Offset / 1000);

    /// <summary>
    /// The side view's steering geometry, in metres from the tyre's touch (x forwards, y up), for a head angle in
    /// degrees and an offset in metres: the axis's direction up, the point on it nearest the axle, and the head tube's top.
    /// </summary>
    internal static (Point Up, Point Nearest, Point HeadTop) Geometry(double headAngle, double offset)
    {
        var r = Bike.WheelRadius;
        var a = headAngle * Math.PI / 180;
        var up = new Point(-Math.Cos(a), Math.Sin(a));
        var forward = new Point(Math.Sin(a), Math.Cos(a));
        var nearest = new Point(0, r) - (forward * offset);
        var crown = nearest + (up * ((2 * r) + 0.04 - nearest.Y) / up.Y);
        return (up, nearest, crown + (up * 0.16));
    }

    /// <summary>
    /// The head angle (degrees, in the slider's range) that puts the head tube's top <paramref name="x"/> metres ahead
    /// of the tyre's touch: the top moves forwards as the tube stands up, so halving the range finds it.
    /// </summary>
    internal static double AngleFor(double x, double offset)
    {
        double low = LeastAngle, high = MostAngle;
        for (var i = 0; i < 30; i++)
        {
            var mid = (low + high) / 2;
            if (Geometry(mid, offset).HeadTop.X < x) low = mid;
            else high = mid;
        }

        return (low + high) / 2;
    }

    protected override void Advance(double seconds)
    {
        _roll += seconds * 2;
        _sinceBump += seconds;
        if (_sinceBump >= BumpEvery)
        {
            _sinceBump = 0;
            _steering.Reset();
            _steering.Nudge();
        }

        _steering.Step(seconds, Trail);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var r = Bike.WheelRadius;
        var offset = Offset / 1000;
        var trail = Trail;

        // Side view, in metres from the tyre's touch on the ground (x forwards, y up).
        var scale = Math.Min(bounds.Width * 0.85 / 1.1, bounds.Height * 0.5 / 1.0);
        var floor = (bounds.Height * 0.06) + (0.98 * scale);
        var origin = new Point(bounds.Width * 0.56, floor);
        Point Map(Point m) => new(origin.X + (m.X * scale), origin.Y - (m.Y * scale));
        var (up, nearest, headTop) = Geometry(HeadAngle, offset);
        var axle = new Point(0, r);
        var ground = nearest - (up * (nearest.Y / up.Y));
        var crown = headTop - (up * 0.16);

        context.DrawLine(Ground, new Point(bounds.X + 8, floor), new Point(bounds.Right - 8, floor));
        BicycleParts.DrawWheel(context, Map(axle), r * scale, _roll, spokes: 24);

        // The frame's tubes reaching back, the head tube and stem, and the fork bending forward to the axle.
        var frame = new ImmutablePen(new ImmutableSolidColorBrush(BicycleParts.Frame), Math.Max(3, scale * 0.03), lineCap: PenLineCap.Round);
        var headBottom = crown + (up * 0.02);
        context.DrawLine(frame, Map(headTop - (up * 0.02)), Map(headTop - (up * 0.02) + new Point(-0.55, -0.02)));
        context.DrawLine(frame, Map(headBottom + (up * 0.02)), Map(headBottom + new Point(-0.5, -0.3)));
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(BicycleParts.Frame), Math.Max(5, scale * 0.05), lineCap: PenLineCap.Round), Map(headBottom), Map(headTop));
        var stem = headTop + (up * 0.05);
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x50, 0x58, 0x68)), Math.Max(3, scale * 0.025), lineCap: PenLineCap.Round), Map(headTop), Map(stem));
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x50, 0x58, 0x68)), Math.Max(3, scale * 0.025), lineCap: PenLineCap.Round), Map(stem), Map(stem + new Point(0.1, 0.01)));

        var bend = nearest + (up * 0.12);
        var fork = new StreamGeometry();
        using (var g = fork.Open())
        {
            g.BeginFigure(Map(crown), false);
            g.LineTo(Map(bend));
            g.QuadraticBezierTo(Map(nearest), Map(axle));
            g.EndFigure(false);
        }

        context.DrawGeometry(null, new ImmutablePen(new ImmutableSolidColorBrush(GearPicture.Steel), Math.Max(3, scale * 0.024), lineCap: PenLineCap.Round), fork);
        context.DrawEllipse(BicycleParts.Dark, new ImmutablePen(new ImmutableSolidColorBrush(GearPicture.Steel), 1.5), Map(axle), 4, 4);
        Mark("fork", [Map(crown), Map(bend), Map(axle)], 6);

        // The reader's handles: the fork's bend, forwards or back for its offset (its line from the axle runs at right
        // angles to the axis), and the head tube's top, leaned back or stood up.
        var forward = new Point(up.Y, -up.X);
        Grip("offset", Map(bend), GripAxis.Horizontal, DiagramText.Get("Bicycle_Grip_Offset"),
            at => Offset = Math.Round(Math.Clamp((((at.X - origin.X) / scale) - (up.X * 0.12)) / -forward.X * 1000, 0, MostOffset)));
        Grip("angle", Map(headTop - (up * 0.06)), GripAxis.Horizontal, DiagramText.Get("Bicycle_Grip_HeadAngle"),
            at => HeadAngle = Math.Round(AngleFor(((at.X - origin.X) / scale) + (up.X * 0.06), offset) * 2) / 2);

        // The steering axis, dashed, from above the bars down to the ground.
        var axisTop = headTop + (up * 0.12);
        context.DrawLine(Axis, Map(axisTop), Map(ground));
        DiagramText.Draw(context, DiagramText.Get("Bicycle_Steer_Axis"), Map(axisTop) + new Point(6, 0), 12, DiagramText.Cyan, 0, 0.5);
        Mark("axis", [Map(axisTop), Map((axisTop + ground) * 0.5)], 6);

        // The trail along the ground, and the tyre's touch.
        var touch = Map(new Point(0, 0));
        var meets = Map(ground);
        var trailBrush = trail < 0 ? Danger : DiagramText.Gold;
        context.DrawLine(new ImmutablePen(trailBrush, 5, lineCap: PenLineCap.Round), touch + new Point(0, 8), meets + new Point(0, 8));
        context.DrawEllipse(DiagramText.Cyan, null, meets, 4, 4);
        DiagramText.Draw(context, DiagramText.Format("Bicycle_Steer_TrailLabel", (trail * 1000).ToString("0")), new Point((touch.X + meets.X) / 2, touch.Y + 14), 12.5, trailBrush, 0.5, 0, bold: true);
        Mark("trail", [touch + new Point(0, 8), meets + new Point(0, 8)], 4);
        context.DrawEllipse(new ImmutableSolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2)), new ImmutablePen(BicycleParts.Dark, 1.5), touch, 5, 5);
        Mark("contact", touch, 6);

        DrawTopView(context, new Rect(bounds.X + 18, floor + 64, bounds.Width - 36, bounds.Bottom - floor - 84), trail);
    }

    /// <summary>The wheel from above, turning about where the steering axis meets the ground.</summary>
    private void DrawTopView(DrawingContext context, Rect area, double trail)
    {
        if (area.Height < 40) return;

        DiagramText.Draw(context, DiagramText.Get("Bicycle_Steer_FromAbove"), new Point(area.X, area.Y), 12, DiagramText.Faint);
        var mid = new Point(area.Center.X + (area.Width * 0.1), area.Center.Y + 8);
        var scale = Math.Min(area.Width * 0.75 / (2 * Bike.WheelRadius), area.Height * 0.9 / (2 * Bike.WheelRadius));

        // The steering axis's foot is the pivot; the tyre's touch trails it. Trail is drawn three times its size to show.
        var pivot = mid;
        var (sin, cos) = Math.SinCos(_steering.Angle);
        Point Turned(double along, double across) => new(pivot.X + (((along * cos) - (across * sin)) * scale), pivot.Y + (((along * sin) + (across * cos)) * scale));
        var touch = -3 * trail;
        var half = Bike.WheelRadius;
        var width = 0.025;
        var tyre = new StreamGeometry();
        using (var g = tyre.Open())
        {
            g.BeginFigure(Turned(touch - half, -width), true);
            g.LineTo(Turned(touch + half, -width));
            g.LineTo(Turned(touch + half, width));
            g.LineTo(Turned(touch - half, width));
            g.EndFigure(true);
        }

        context.DrawGeometry(new ImmutableSolidColorBrush(BicycleParts.Tyre), new ImmutablePen(new ImmutableSolidColorBrush(GearPicture.Steel), 1.5), tyre);
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x60, 0xE6, 0xEA, 0xF2)), 1, new ImmutableDashStyle([3, 3], 0)),
            pivot - new Point(half * 1.4 * scale, 0), pivot + new Point(half * 1.4 * scale, 0));
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(GearPicture.Steel), 3), Turned(touch, 0), pivot);
        context.DrawEllipse(DiagramText.Cyan, null, pivot, 4, 4);
        context.DrawEllipse(Brushes.White, null, Turned(touch, 0), 3.5, 3.5);
        Mark("top", [Turned(touch - half, 0), Turned(touch + half, 0), pivot], 10);

        DiagramText.Draw(context, DiagramText.Get("Bicycle_Steer_Ahead"), new Point(area.Right, pivot.Y - (half * scale) - 4), 11.5, DiagramText.Faint, 1, 1);
        if (_sinceBump < 0.6)
        {
            DiagramText.Draw(context, DiagramText.Get("Bicycle_Steer_Bump"), new Point(area.X, pivot.Y), 14, DiagramText.Gold, 0, 0.5, bold: true);
        }

        var degrees = _steering.Angle * 180 / Math.PI;
        DiagramText.Draw(context, DiagramText.Format("Bicycle_Steer_Turned", Math.Abs(degrees).ToString("0")), new Point(area.X, area.Bottom), 12, DiagramText.Ink, 0, 1);
    }
}
