using Avalonia;

namespace Lemon.Inside.Book.Chapters.Gears;

/// <summary>
/// The shape and motion of involute spur gears, in units of the module (the tooth size: a gear of z teeth has a
/// rolling, or pitch, circle of radius z / 2). Angles are in radians, counter-clockwise, with y up; the pictures flip y
/// when they draw.
/// </summary>
public static class GearGeometry
{
    /// <summary>How far the teeth reach past the rolling circle (the addendum), in modules.</summary>
    public const double Addendum = 1;

    /// <summary>How far the gaps reach inside the rolling circle (the dedendum), in modules: a little clearance.</summary>
    public const double Dedendum = 1.25;

    /// <summary>Play between meshing teeth, as arc length on the rolling circle taken off each tooth, in modules.</summary>
    public const double Backlash = 0.06;

    /// <summary>Points per involute flank.</summary>
    private const int FlankPoints = 10;

    public static double PitchRadius(int teeth) => teeth / 2.0;

    public static double BaseRadius(int teeth, double pressureAngle) => PitchRadius(teeth) * Math.Cos(pressureAngle);

    /// <summary>The involute function: how far round an involute has come, seen from the centre, at pressure angle a.</summary>
    public static double Involute(double a) => Math.Tan(a) - a;

    /// <summary>
    /// The point at <paramref name="unwound"/> radians of string unwound from a circle of <paramref name="radius"/>,
    /// starting at angle 0 and unwinding clockwise: the string's end draws an involute. Also returns where the string
    /// leaves the circle.
    /// </summary>
    public static (Point End, Point Leaves) Unwind(double radius, double unwound)
    {
        var (sin, cos) = Math.SinCos(unwound);
        var leaves = new Point(radius * cos, radius * sin);
        return (leaves + new Point(radius * unwound * sin, -radius * unwound * cos), leaves);
    }

    /// <summary>
    /// Half a tooth's width, as an angle from its centre line, at <paramref name="radius"/> (not below the base circle).
    /// <paramref name="thinning"/> is arc length on the rolling circle taken off the whole tooth.
    /// </summary>
    public static double HalfToothAngle(int teeth, double pressureAngle, double radius, double thinning = Backlash)
    {
        var rb = BaseRadius(teeth, pressureAngle);
        var r = PitchRadius(teeth);
        var at = Math.Acos(Math.Clamp(rb / Math.Max(radius, rb), -1, 1));
        return (Math.PI / (2 * teeth)) - (thinning / (2 * r)) + Involute(pressureAngle) - Involute(at);
    }

    /// <summary>
    /// The outline of an external gear with tooth 0 centred on angle 0, closed, counter-clockwise. Flanks below the base
    /// circle are radial. <paramref name="tip"/> and <paramref name="root"/> are how far the teeth and the gaps reach
    /// past and inside the rolling circle (an internal gear's space uses them swapped).
    /// </summary>
    public static IReadOnlyList<Point> Outline(int teeth, double pressureAngle,
        double tip = Addendum, double root = Dedendum, double thinning = Backlash)
    {
        var r = PitchRadius(teeth);
        var rb = BaseRadius(teeth, pressureAngle);
        var ra = r + tip;
        var rf = Math.Max(0.2, r - root);
        var start = Math.Max(rb, rf);
        var pitch = 2 * Math.PI / teeth;
        var points = new List<Point>(teeth * ((4 * FlankPoints) + 8));

        static Point Polar(double radius, double angle) => new(radius * Math.Cos(angle), radius * Math.Sin(angle));

        for (var k = 0; k < teeth; k++)
        {
            var centre = k * pitch;
            var halfAtStart = Math.Max(0, HalfToothAngle(teeth, pressureAngle, start, thinning));

            // The gap before this tooth, along the root circle, then up the tooth's trailing (clockwise) flank.
            points.Add(Polar(rf, centre - (pitch / 2)));
            points.Add(Polar(rf, centre - halfAtStart));
            for (var i = 0; i <= FlankPoints; i++)
            {
                var radius = start + ((ra - start) * i / FlankPoints);
                points.Add(Polar(radius, centre - Math.Max(0, HalfToothAngle(teeth, pressureAngle, radius, thinning))));
            }

            // Down the leading flank.
            for (var i = FlankPoints; i >= 0; i--)
            {
                var radius = start + ((ra - start) * i / FlankPoints);
                points.Add(Polar(radius, centre + Math.Max(0, HalfToothAngle(teeth, pressureAngle, radius, thinning))));
            }

            points.Add(Polar(rf, centre + halfAtStart));
        }

        return points;
    }

    /// <summary>
    /// The turn of a gear of <paramref name="teeth"/> that meshes, from outside, with a gear of
    /// <paramref name="driverTeeth"/> turned to <paramref name="driverAngle"/>; <paramref name="towards"/> is the
    /// direction from the driver's centre to this gear's. A tooth of one always sits in a gap of the other.
    /// </summary>
    public static double MeshAngle(int driverTeeth, double driverAngle, int teeth, double towards)
    {
        // Where the driver's teeth are along the line of centres, in tooth pitches (a whole number: a tooth on it).
        var driverPhase = (towards - driverAngle) * driverTeeth / (2 * Math.PI);
        return towards + Math.PI - (2 * Math.PI / teeth * (0.5 - driverPhase));
    }

    /// <summary>
    /// The turn of an internal gear (a ring) of <paramref name="ringTeeth"/> round a gear of <paramref name="teeth"/>
    /// turned to <paramref name="angle"/> inside it, whose centre is in direction <paramref name="towards"/> from the
    /// ring's. The ring's angle is that of its outline's tooth spaces (<see cref="RingSpaces"/>).
    /// </summary>
    public static double RingAngle(int teeth, double angle, int ringTeeth, double towards)
    {
        var phase = (towards - angle) * teeth / (2 * Math.PI);
        return towards - (2 * Math.PI / ringTeeth * phase);
    }

    /// <summary>
    /// The inner outline of a ring with teeth on the inside: its tooth spaces have the shape of an external gear's
    /// teeth, reaching past the rolling circle by the clearance and stopping short of it by the addendum.
    /// </summary>
    public static IReadOnlyList<Point> RingSpaces(int teeth, double pressureAngle) =>
        Outline(teeth, pressureAngle, tip: Dedendum, root: Addendum, thinning: -Backlash);

    /// <summary>
    /// Where pairs of teeth touch on the line of action of gear 1 (at the origin, <paramref name="teeth1"/>, turned to
    /// <paramref name="angle1"/>, driving counter-clockwise) and gear 2 (to the right). Each is a distance along the line
    /// from where it touches gear 1's base circle. Also the stretch of the line where teeth can touch.
    /// </summary>
    public static (IReadOnlyList<double> Contacts, double From, double To) Contacts(
        int teeth1, int teeth2, double pressureAngle, double angle1)
    {
        var (from, to) = ContactPath(teeth1, teeth2, pressureAngle);
        var rb = BaseRadius(teeth1, pressureAngle);
        var basePitch = 2 * Math.PI * rb / teeth1;

        // The leading flank of tooth 0 leaves the base circle at angle1 + half a tooth there; the line touches the base
        // circle at -pressureAngle, and the flank meets it after unrolling that far.
        var leaves = angle1 + HalfToothAngle(teeth1, pressureAngle, rb);
        var first = rb * Wrap(leaves + pressureAngle);
        var contacts = new List<double>();
        for (var s = first % basePitch; s <= to; s += basePitch)
        {
            if (s >= from)
            {
                contacts.Add(s);
            }
        }

        return (contacts, from, to);
    }

    /// <summary>The stretch of the line of action between the two tip circles, as distances from gear 1's base circle.</summary>
    public static (double From, double To) ContactPath(int teeth1, int teeth2, double pressureAngle)
    {
        var rb1 = BaseRadius(teeth1, pressureAngle);
        var rb2 = BaseRadius(teeth2, pressureAngle);
        var ra1 = PitchRadius(teeth1) + Addendum;
        var ra2 = PitchRadius(teeth2) + Addendum;
        var between = (PitchRadius(teeth1) + PitchRadius(teeth2)) * Math.Sin(pressureAngle);
        var to = Math.Sqrt((ra1 * ra1) - (rb1 * rb1));
        var from = Math.Max(0, between - Math.Sqrt((ra2 * ra2) - (rb2 * rb2)));
        return (from, Math.Min(to, between));
    }

    /// <summary>How many pairs of teeth touch on average (the contact ratio).</summary>
    public static double ContactRatio(int teeth1, int teeth2, double pressureAngle)
    {
        var (from, to) = ContactPath(teeth1, teeth2, pressureAngle);
        return (to - from) / (Math.PI * Math.Cos(pressureAngle));
    }

    /// <summary>The point at <paramref name="along"/> on gear 1's line of action (see <see cref="Contacts"/>).</summary>
    public static Point OnLineOfAction(int teeth1, double pressureAngle, double along)
    {
        var rb = BaseRadius(teeth1, pressureAngle);
        var (sin, cos) = Math.SinCos(pressureAngle);
        return new Point((rb * cos) + (along * sin), (-rb * sin) + (along * cos));
    }

    private static double Wrap(double angle)
    {
        angle %= 2 * Math.PI;
        return angle < 0 ? angle + (2 * Math.PI) : angle;
    }
}

/// <summary>Which part of a planetary gear set is held still.</summary>
public enum PlanetHeld
{
    Ring,
    Sun,
    Carrier,
}

/// <summary>
/// A planetary gear set: a sun in the middle, three planets round it on a carrier, and a ring with teeth inside.
/// The ring has <see cref="RingTeeth"/>; the planets fill the space between.
/// </summary>
public static class Planetary
{
    public const int RingTeeth = 60;

    public const int Planets = 3;

    public static int PlanetTeeth(int sunTeeth) => (RingTeeth - sunTeeth) / 2;

    /// <summary>
    /// Turns of (sun, ring, carrier) for one turn of the part that is driven: the sun, or the ring when the sun is held.
    /// From sun teeth × sun turns + ring teeth × ring turns = (sun + ring teeth) × carrier turns.
    /// </summary>
    public static (double Sun, double Ring, double Carrier) Speeds(int sunTeeth, PlanetHeld held)
    {
        double total = sunTeeth + RingTeeth;
        return held switch
        {
            PlanetHeld.Ring => (1, 0, sunTeeth / total),
            PlanetHeld.Sun => (0, 1, RingTeeth / total),
            _ => (1, -(double)sunTeeth / RingTeeth, 0),
        };
    }

    /// <summary>The part turned by hand and the part that comes out, for each choice of held part.</summary>
    public static (PlanetHeld Input, PlanetHeld Output) Parts(PlanetHeld held) => held switch
    {
        PlanetHeld.Ring => (PlanetHeld.Sun, PlanetHeld.Carrier),
        PlanetHeld.Sun => (PlanetHeld.Ring, PlanetHeld.Carrier),
        _ => (PlanetHeld.Sun, PlanetHeld.Ring),
    };

    /// <summary>Turns of the output part for one turn of the input.</summary>
    public static double Ratio(int sunTeeth, PlanetHeld held)
    {
        var (sun, ring, carrier) = Speeds(sunTeeth, held);
        return held switch
        {
            PlanetHeld.Ring => carrier / sun,
            PlanetHeld.Sun => carrier / ring,
            _ => ring / sun,
        };
    }

    /// <summary>
    /// Every part's angle after <paramref name="turns"/> turns of the input: the sun, each planet's centre direction and
    /// spin, and the ring (from planet 0, so its teeth always fit).
    /// </summary>
    public static (double Sun, double Carrier, (double Towards, double Spin)[] Planets, double Ring) Pose(
        int sunTeeth, PlanetHeld held, double turns)
    {
        var (sunSpeed, _, carrierSpeed) = Speeds(sunTeeth, held);
        var sun = 2 * Math.PI * sunSpeed * turns;
        var carrier = 2 * Math.PI * carrierSpeed * turns;
        var planetTeeth = PlanetTeeth(sunTeeth);
        var planets = new (double, double)[Planets];
        for (var i = 0; i < Planets; i++)
        {
            var towards = carrier + (2 * Math.PI * i / Planets);
            planets[i] = (towards, GearGeometry.MeshAngle(sunTeeth, sun, planetTeeth, towards));
        }

        var ring = GearGeometry.RingAngle(planetTeeth, planets[0].Item2, RingTeeth, planets[0].Item1);
        return (sun, carrier, planets, ring);
    }
}
