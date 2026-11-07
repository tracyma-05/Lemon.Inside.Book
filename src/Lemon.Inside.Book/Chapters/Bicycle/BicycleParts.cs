using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Lemon.Inside.Book.Chapters.Bicycle;

/// <summary>Where the parts of a drawn bike landed on screen, for the picture to mark and label them.</summary>
internal readonly record struct BikePoints(
    Point RearAxle, Point FrontAxle, Point RearContact, Point FrontContact, Point Crank, Point CentreOfMass,
    Point FrontPads, Point Head, Point Hip, double WheelRadius);

/// <summary>
/// Drawing the chapter's own things: a spoked wheel, a sprocket, a chain's rollers, and a side view of a bike with its
/// rider. Bike drawings are laid out in metres (y up, the back tyre's touch on the road at the origin, the bike facing
/// right) and placed on screen by a map.
/// </summary>
internal static class BicycleParts
{
    public static readonly Color Frame = Color.FromRgb(0x5F, 0xC2, 0xB0);
    public static readonly Color Steel = Color.FromRgb(0x9F, 0xB0, 0xC8);
    public static readonly Color Tyre = Color.FromRgb(0x2A, 0x2E, 0x36);
    public static readonly Color Jersey = Color.FromRgb(0xE0, 0x7A, 0x4F);
    public static readonly Color Skin = Color.FromRgb(0xE8, 0xC2, 0x9A);
    public static readonly Color Shorts = Color.FromRgb(0x34, 0x3C, 0x52);
    public static readonly IImmutableBrush Dark = new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E));

    // The bike, in metres from the back tyre's touch on the road.
    private static readonly Point RearAxle = new(0, Bike.WheelRadius);
    private static readonly Point FrontAxle = new(1.0, Bike.WheelRadius);
    private static readonly Point BottomBracket = new(0.42, 0.27);
    private static readonly Point SeatTop = new(0.31, 0.80);
    private static readonly Point HeadTop = new(0.90, 0.80);
    private static readonly Point HeadBottom = new(0.93, 0.68);
    private static readonly Point Saddle = new(0.29, 0.88);
    private static readonly Point Bars = new(0.94, 0.92);
    private static readonly Point Hip = new(0.29, 0.94);
    private static readonly Point Shoulder = new(0.66, 1.28);
    private static readonly Point HeadCentre = new(0.79, 1.40);

    /// <summary>Where the rider's weight is centred: 40 % of the way from the back axle, a wheelbase up.</summary>
    public static readonly Point CentreOfMass = new(Braking.FrontShare, Braking.HeightOverWheelbase);

    /// <summary>A point <paramref name="radius"/> from <paramref name="centre"/> at <paramref name="angle"/> radians (screen: clockwise from 3 o'clock).</summary>
    public static Point At(Point centre, double radius, double angle) =>
        new(centre.X + (radius * Math.Cos(angle)), centre.Y + (radius * Math.Sin(angle)));

    /// <summary>
    /// A wheel at <paramref name="centre"/>: a tyre, a rim and <paramref name="spokes"/> spokes, turned
    /// <paramref name="angle"/> radians clockwise.
    /// </summary>
    public static void DrawWheel(DrawingContext context, Point centre, double radius, double angle, int spokes = 18, double opacity = 1)
    {
        var tyre = Math.Max(2, radius * 0.08);
        context.DrawEllipse(null, new ImmutablePen(new ImmutableSolidColorBrush(Tyre, opacity), tyre), centre, radius - (tyre / 2), radius - (tyre / 2));
        var rim = radius - tyre;
        context.DrawEllipse(null, new ImmutablePen(new ImmutableSolidColorBrush(Steel, opacity), Math.Max(1, radius * 0.025)), centre, rim, rim);
        var spoke = new ImmutablePen(new ImmutableSolidColorBrush(Steel, 0.55 * opacity), Math.Max(0.6, radius * 0.008));
        for (var i = 0; i < spokes; i++)
        {
            var a = angle + (i * 2 * Math.PI / spokes);
            context.DrawLine(spoke, At(centre, radius * 0.08, a), At(centre, rim, a + 0.3));
        }

        context.DrawEllipse(new ImmutableSolidColorBrush(Steel, opacity), null, centre, Math.Max(2, radius * 0.08), Math.Max(2, radius * 0.08));
    }

    /// <summary>A sprocket with <paramref name="teeth"/> whose rollers sit on a circle of <paramref name="radius"/> pixels, turned <paramref name="angle"/> clockwise.</summary>
    public static void DrawSprocket(DrawingContext context, Point centre, int teeth, double radius, double angle, Color color, double pitch)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            var step = 2 * Math.PI / teeth;
            for (var k = 0; k < teeth; k++)
            {
                // A gap (where a roller sits) at k, a tooth halfway to the next.
                var a = angle + (k * step);
                var gap = At(centre, radius - (pitch * 0.32), a);
                if (k == 0) g.BeginFigure(gap, true);
                else g.LineTo(gap);
                g.LineTo(At(centre, radius + (pitch * 0.18), a + (step * 0.28)));
                g.LineTo(At(centre, radius + (pitch * 0.36), a + (step * 0.5)));
                g.LineTo(At(centre, radius + (pitch * 0.18), a + (step * 0.72)));
            }

            g.EndFigure(true);
        }

        context.DrawGeometry(new ImmutableSolidColorBrush(Color.FromRgb((byte)(color.R * 0.85), (byte)(color.G * 0.85), (byte)(color.B * 0.85))),
            new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb((byte)(color.R * 0.45), (byte)(color.G * 0.45), (byte)(color.B * 0.45))), 1), geometry);
        context.DrawEllipse(Dark, null, centre, radius * 0.55, radius * 0.55);
        context.DrawEllipse(new ImmutableSolidColorBrush(color), null, centre, Math.Max(3, radius * 0.18), Math.Max(3, radius * 0.18));
    }

    /// <summary>
    /// A bike and its rider. <paramref name="map"/> takes metres to the screen; <paramref name="crank"/> is the pedals'
    /// angle and <paramref name="frontTurn"/>, <paramref name="rearTurn"/> how far each wheel has turned (radians, forwards).
    /// </summary>
    public static BikePoints DrawBike(DrawingContext context, Func<Point, Point> map, double scale, double crank, double frontTurn, double rearTurn)
    {
        var r = Bike.WheelRadius * scale;
        var frame = new ImmutablePen(new ImmutableSolidColorBrush(Frame), Math.Max(2, scale * 0.028), lineCap: PenLineCap.Round);
        var thin = new ImmutablePen(new ImmutableSolidColorBrush(Frame), Math.Max(1.5, scale * 0.018), lineCap: PenLineCap.Round);
        var dark = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x50, 0x58, 0x68)), Math.Max(1.5, scale * 0.02), lineCap: PenLineCap.Round);
        var spinDirection = Angle(map, new Point(0, 0), new Point(1, 0)); // the screen's way along the bike

        // The pedal on the far side first, then its leg, so the near side covers them.
        var crankArm = Bike.CrankLength;
        Point Pedal(double a) => new(BottomBracket.X + (crankArm * Math.Cos(a)), BottomBracket.Y + (crankArm * Math.Sin(a)));
        var near = -crank;
        var far = near + Math.PI;
        DrawLeg(context, map, scale, Pedal(far), 0.6);

        DrawWheel(context, map(RearAxle), r, spinDirection + rearTurn);
        DrawWheel(context, map(FrontAxle), r, spinDirection + frontTurn);

        context.DrawLine(frame, map(BottomBracket), map(RearAxle));
        context.DrawLine(thin, map(SeatTop), map(RearAxle));
        context.DrawLine(frame, map(BottomBracket), map(SeatTop));
        context.DrawLine(frame, map(SeatTop), map(HeadTop));
        context.DrawLine(frame, map(BottomBracket), map(HeadBottom));
        context.DrawLine(frame, map(HeadTop), map(HeadBottom));
        context.DrawLine(thin, map(HeadBottom), map(FrontAxle));
        context.DrawLine(dark, map(SeatTop), map(Saddle));
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x50, 0x58, 0x68)), Math.Max(3, scale * 0.045), lineCap: PenLineCap.Round),
            map(Saddle - new Point(0.08, 0)), map(Saddle + new Point(0.06, 0.005)));
        context.DrawLine(dark, map(HeadTop), map(new Point(0.88, 0.90)));
        context.DrawLine(dark, map(new Point(0.88, 0.90)), map(Bars));

        // The brake calipers over each tyre.
        var pads = new Point(FrontAxle.X - 0.02, (2 * Bike.WheelRadius) - 0.005);
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x30, 0x34, 0x3E)), Math.Max(2, scale * 0.03), lineCap: PenLineCap.Round),
            map(pads - new Point(0.04, 0.0)), map(pads + new Point(0.04, 0.0)));
        var rearPads = new Point(RearAxle.X + 0.02, (2 * Bike.WheelRadius) - 0.005);
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x30, 0x34, 0x3E)), Math.Max(2, scale * 0.03), lineCap: PenLineCap.Round),
            map(rearPads - new Point(0.04, 0.0)), map(rearPads + new Point(0.04, 0.0)));

        // Chainring and the near crank.
        var bb = map(BottomBracket);
        context.DrawEllipse(null, new ImmutablePen(new ImmutableSolidColorBrush(Steel), Math.Max(1.5, scale * 0.012)), bb, 0.1 * scale, 0.1 * scale);

        // The rider: torso, head, arms, then the near leg and crank on top.
        var torso = new ImmutablePen(new ImmutableSolidColorBrush(Jersey), Math.Max(4, scale * 0.13), lineCap: PenLineCap.Round);
        context.DrawLine(torso, map(Hip), map(Shoulder));
        var head = map(HeadCentre);
        context.DrawEllipse(new ImmutableSolidColorBrush(Skin), null, head, 0.095 * scale, 0.095 * scale);
        var helmet = new StreamGeometry();
        using (var g = helmet.Open())
        {
            g.BeginFigure(map(HeadCentre + new Point(-0.11, 0.0)), true);
            g.ArcTo(map(HeadCentre + new Point(0.11, 0.0)), new Size(0.11 * scale, 0.1 * scale), 0, false, SweepDirection.Clockwise);
            g.EndFigure(true);
        }

        context.DrawGeometry(new ImmutableSolidColorBrush(Frame), null, helmet);
        var elbow = Joint(Shoulder, Bars, 0.31, 0.31, bendForward: false);
        var arm = new ImmutablePen(new ImmutableSolidColorBrush(Skin), Math.Max(2.5, scale * 0.055), lineCap: PenLineCap.Round);
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Jersey), Math.Max(3, scale * 0.07), lineCap: PenLineCap.Round), map(Shoulder), map(elbow));
        context.DrawLine(arm, map(elbow), map(Bars));
        DrawLeg(context, map, scale, Pedal(near), 1);
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Steel), Math.Max(2, scale * 0.022), lineCap: PenLineCap.Round), bb, map(Pedal(near)));

        return new BikePoints(map(RearAxle), map(FrontAxle), map(new Point(0, 0)), map(new Point(FrontAxle.X, 0)), bb, map(CentreOfMass),
            map(pads), head, map(Hip), r);
    }

    /// <summary>The centre-of-mass sign: a circle in four quarters.</summary>
    public static void DrawCentreOfMass(DrawingContext context, Point at, double radius)
    {
        context.DrawEllipse(Brushes.White, new ImmutablePen(Dark, 1.5), at, radius, radius);
        foreach (var start in new[] { 0.0, Math.PI })
        {
            var quarter = new StreamGeometry();
            using (var g = quarter.Open())
            {
                g.BeginFigure(at, true);
                g.LineTo(At(at, radius, start));
                g.ArcTo(At(at, radius, start + (Math.PI / 2)), new Size(radius, radius), 0, false, SweepDirection.Clockwise);
                g.EndFigure(true);
            }

            context.DrawGeometry(Dark, null, quarter);
        }
    }

    private static void DrawLeg(DrawingContext context, Func<Point, Point> map, double scale, Point pedal, double light)
    {
        var knee = Joint(Hip, pedal, 0.44, 0.44, bendForward: true);
        var thigh = new ImmutablePen(new ImmutableSolidColorBrush(Shade(Shorts, light)), Math.Max(4, scale * 0.1), lineCap: PenLineCap.Round);
        var shin = new ImmutablePen(new ImmutableSolidColorBrush(Shade(Skin, light)), Math.Max(3, scale * 0.065), lineCap: PenLineCap.Round);
        context.DrawLine(thigh, map(Hip), map(knee));
        context.DrawLine(shin, map(knee), map(pedal));
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Shade(Color.FromRgb(0x22, 0x26, 0x30), light)), Math.Max(3, scale * 0.05), lineCap: PenLineCap.Round),
            map(pedal - new Point(0.05, 0)), map(pedal + new Point(0.06, 0)));
    }

    /// <summary>The middle joint of a two-part limb from <paramref name="from"/> to <paramref name="to"/> (metres, y up).</summary>
    private static Point Joint(Point from, Point to, double first, double second, bool bendForward)
    {
        var d = to - from;
        var length = Math.Clamp(Math.Sqrt((d.X * d.X) + (d.Y * d.Y)), 1e-6, first + second - 1e-6);
        var cos = Math.Clamp(((first * first) + (length * length) - (second * second)) / (2 * first * length), -1, 1);
        var bend = Math.Acos(cos) * (bendForward ? 1 : -1);
        var direction = Math.Atan2(d.Y, d.X) + bend;
        return new Point(from.X + (first * Math.Cos(direction)), from.Y + (first * Math.Sin(direction)));
    }

    private static double Angle(Func<Point, Point> map, Point a, Point b)
    {
        var d = map(b) - map(a);
        return Math.Atan2(d.Y, d.X);
    }

    private static Color Shade(Color color, double light) =>
        Color.FromRgb((byte)(color.R * light), (byte)(color.G * light), (byte)(color.B * light));
}
