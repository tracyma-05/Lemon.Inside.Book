using Avalonia;

namespace Lemon.Inside.Book.Chapters.Boats;

/// <summary>Water and the pull of gravity, for the chapter's models.</summary>
public static class Water
{
    public const double Gravity = 9.81;

    /// <summary>Fresh water, kilograms per cubic metre.</summary>
    public const double Fresh = 1000;

    /// <summary>Sea water, a little heavier for its salt.</summary>
    public const double Sea = 1025;

    /// <summary>The pressure (above the air's) <paramref name="depth"/> metres down, in pascals.</summary>
    public static double Pressure(double depth, double density = Fresh) => density * Gravity * Math.Max(0, depth);
}

/// <summary>
/// Spread 1: a block put in water. It sinks until the water it pushes aside weighs as much as the block; a block
/// heavier than its own volume of water never gets there and goes to the bottom.
/// </summary>
public sealed class FloatingBlock
{
    /// <summary>How quickly a bob dies away (per second).</summary>
    private const double Damping = 1.6;

    /// <summary>The block's height, in its own units; the picture scales it.</summary>
    public const double Height = 1;

    /// <summary>How deep the block's bottom is below the surface, in block heights (negative: above the water).</summary>
    public double Depth { get; private set; }

    public double Speed { get; private set; }

    /// <summary>The share of the block under water at rest: its density over the water's, or all of it if it sinks.</summary>
    public static double SubmergedShare(double density) => Math.Min(1, density / Water.Fresh);

    public static bool Sinks(double density) => density > Water.Fresh;

    /// <summary>The share of the block under water now.</summary>
    public double Submerged => Math.Clamp(Depth / Height, 0, 1);

    /// <summary>
    /// Moves on <paramref name="seconds"/>: gravity pulls the block down, the water pushes up with the weight of the
    /// water it has pushed aside, and the water's drag slows it. The floor <paramref name="floor"/> block heights down
    /// stops it.
    /// </summary>
    public void Step(double seconds, double density, double floor)
    {
        const double Dt = 0.004;
        var heaviness = density / Water.Fresh;
        for (var t = 0.0; t < seconds; t += Dt)
        {
            var dt = Math.Min(Dt, seconds - t);
            var accel = (Water.Gravity * (1 - (Submerged / heaviness))) - (Damping * Speed * (Submerged > 0 ? 1 : 0.1));
            Speed += accel * dt;
            Depth += Speed * dt;
            if (Depth >= floor)
            {
                Depth = floor;
                Speed = Math.Min(0, Speed);
            }
        }
    }
}

/// <summary>
/// Spread 3: a steel boat. As a hull it holds a great deal of air inside the same steel, so it pushes aside far more
/// water than the steel alone; loaded, it settles deeper, until the water comes over its sides.
/// </summary>
public static class Hull
{
    public const double Length = 12;
    public const double Beam = 4;
    public const double Depth = 1.5;

    /// <summary>The empty hull's steel, tonnes.</summary>
    public const double Steel = 6;

    /// <summary>Steel's density, kilograms per cubic metre.</summary>
    public const double SteelDensity = 7850;

    /// <summary>How deep the hull sits with <paramref name="cargo"/> tonnes aboard, metres (past <see cref="Depth"/>, it sinks).</summary>
    public static double Draft(double cargo) => (Steel + cargo) * 1000 / (Water.Sea * Length * Beam);

    public static bool Sinks(double cargo) => Draft(cargo) >= Depth;

    /// <summary>The most cargo the hull can carry before the sea reaches its deck, tonnes.</summary>
    public static double MostCargo => (Depth * Water.Sea * Length * Beam / 1000) - Steel;

    /// <summary>The side of a cube made of the hull's steel, metres.</summary>
    public static double LumpSide => Math.Cbrt(Steel * 1000 / SteelDensity);
}

/// <summary>
/// Spreads 4 and 5: a box-shaped hull seen end-on, <see cref="Beam"/> wide and <see cref="HullDepth"/> deep, floating
/// <see cref="Draft"/> deep when upright, its weight centred <see cref="CentreHeight"/> above the keel. Heeled over,
/// the water it pushes aside changes shape; its centre (the centre of buoyancy) moves to the low side, and the push of
/// the water and the pull of the weight make a pair that turns the boat. Heel is in radians, the right side going down.
/// </summary>
public sealed class Section(double beam, double hullDepth, double draft, double centreHeight)
{
    public double Beam { get; } = beam;

    public double HullDepth { get; } = hullDepth;

    public double Draft { get; } = draft;

    /// <summary>The height of the centre of the weight above the keel (KG).</summary>
    public double CentreHeight { get; } = centreHeight;

    /// <summary>
    /// How far above the weight's centre the push of the water seems to come from, for small tilts: the metacentric
    /// height GM = KB + BM − KG. Negative means top-heavy: the boat will not stay upright.
    /// </summary>
    public double MetacentricHeight => (Draft / 2) + (Beam * Beam / (12 * Draft)) - CentreHeight;

    /// <summary>The hull's corners in the boat's own frame (metres, x across, y up from the middle of the keel).</summary>
    public IReadOnlyList<Point> Corners =>
    [
        new(-Beam / 2, 0), new(Beam / 2, 0), new(Beam / 2, HullDepth), new(-Beam / 2, HullDepth),
    ];

    /// <summary>A point of the boat's frame, heeled over by <paramref name="heel"/> about the middle of the keel.</summary>
    public static Point Heeled(Point p, double heel)
    {
        var (sin, cos) = Math.SinCos(-heel);
        return new Point((p.X * cos) - (p.Y * sin), (p.X * sin) + (p.Y * cos));
    }

    /// <summary>
    /// The boat heeled over by <paramref name="heel"/>: the height of the water's surface (in the heeled frame), the
    /// shape under water and its centre (the centre of buoyancy), and the centre of the weight.
    /// </summary>
    public (double Waterline, IReadOnlyList<Point> Under, Point Buoyancy, Point Weight) At(double heel)
    {
        var hull = Corners.Select(c => Heeled(c, heel)).ToList();
        var area = Beam * Draft;
        double low = hull.Min(p => p.Y), high = hull.Max(p => p.Y);
        for (var i = 0; i < 60; i++)
        {
            var mid = (low + high) / 2;
            if (Area(Below(hull, mid)) < area) low = mid;
            else high = mid;
        }

        var level = (low + high) / 2;
        var under = Below(hull, level);
        return (level, under, Centroid(under), Heeled(new Point(0, CentreHeight), heel));
    }

    /// <summary>
    /// The righting lever GZ, metres: how far the push of the water acts to the low side of the weight. Positive turns
    /// the boat back upright; negative tips it further.
    /// </summary>
    public double RightingLever(double heel) => Math.Sign(heel) * Lever(heel);

    /// <summary>How far right of the weight's centre the water pushes up, metres: it turns the boat towards the left.</summary>
    public double Lever(double heel)
    {
        var (_, _, buoyancy, weight) = At(heel);
        return buoyancy.X - weight.X;
    }

    /// <summary>The part of a polygon at or below the height <paramref name="level"/>.</summary>
    private static List<Point> Below(IReadOnlyList<Point> polygon, double level)
    {
        var result = new List<Point>();
        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            var aIn = a.Y <= level;
            var bIn = b.Y <= level;
            if (aIn) result.Add(a);
            if (aIn != bIn)
            {
                var t = (level - a.Y) / (b.Y - a.Y);
                result.Add(new Point(a.X + ((b.X - a.X) * t), level));
            }
        }

        return result;
    }

    private static double Area(IReadOnlyList<Point> p)
    {
        var sum = 0.0;
        for (var i = 0; i < p.Count; i++)
        {
            var (a, b) = (p[i], p[(i + 1) % p.Count]);
            sum += (a.X * b.Y) - (b.X * a.Y);
        }

        return Math.Abs(sum / 2);
    }

    private static Point Centroid(IReadOnlyList<Point> p)
    {
        double sum = 0, cx = 0, cy = 0;
        for (var i = 0; i < p.Count; i++)
        {
            var (a, b) = (p[i], p[(i + 1) % p.Count]);
            var cross = (a.X * b.Y) - (b.X * a.Y);
            sum += cross;
            cx += (a.X + b.X) * cross;
            cy += (a.Y + b.Y) * cross;
        }

        return Math.Abs(sum) < 1e-12 ? p.Count > 0 ? p[0] : default : new Point(cx / (3 * sum), cy / (3 * sum));
    }
}

/// <summary>
/// Spread 5: a boat rolling in waves. The righting lever pulls it back upright, the water damps the swing, and every
/// few seconds a wave pushes it over. Tipped past <see cref="Capsize"/>, it has capsized.
/// </summary>
public sealed class Roll
{
    /// <summary>How quickly a swing dies away (per second).</summary>
    private const double Damping = 0.5;

    /// <summary>Past this heel (radians) the boat has turned over.</summary>
    public const double Capsize = 80 * Math.PI / 180;

    public double Angle { get; private set; }

    public double Rate { get; private set; }

    public bool Capsized => Math.Abs(Angle) >= Capsize;

    /// <summary>A wave knocks the boat over a little.</summary>
    public void Nudge(double rate) => Rate += rate;

    public void Reset() => (Angle, Rate) = (0, 0);

    /// <summary>Moves on <paramref name="seconds"/>; the boat's swing is set by its width (its radius of gyration ~0.4 beam).</summary>
    public void Step(double seconds, Section section)
    {
        if (Capsized) return;

        var gyration = 0.4 * section.Beam;
        const double Dt = 0.01;
        for (var t = 0.0; t < seconds && !Capsized; t += Dt)
        {
            var dt = Math.Min(Dt, seconds - t);
            var accel = (-Water.Gravity * section.Lever(Angle) / (gyration * gyration)) - (Damping * Rate);
            Rate += accel * dt;
            Angle += Rate * dt;
        }

        if (Capsized) Angle = Math.Sign(Angle) * Capsize;
    }
}

/// <summary>
/// Spread 6: a submarine. Its hull weighs a little less than the water it pushes aside, so empty it floats; letting
/// sea into its ballast tanks makes it heavier. Exactly as heavy as its water, it hangs where it is, but only just: the
/// smallest difference makes it drift up or down.
/// </summary>
public sealed class Submarine
{
    /// <summary>The hull's height, metres.</summary>
    public const double Height = 8;

    /// <summary>How deep the sea is, metres.</summary>
    public const double SeaDepth = 60;

    /// <summary>The submarine's weight with empty tanks, as a share of the water it pushes aside when under.</summary>
    public const double DryWeight = 0.9;

    /// <summary>The ballast tanks' size, as a share of the hull.</summary>
    public const double Tanks = 0.2;

    private const double Drag = 0.25;

    /// <summary>A gentle slowing at any speed, so a bob at the surface dies away (per second).</summary>
    private const double Settle = 0.4;

    /// <summary>The ballast where the submarine weighs just what its water does.</summary>
    public static double Neutral => (1 - DryWeight) / Tanks;

    /// <summary>How deep its keel is, metres.</summary>
    public double Depth { get; private set; } = Height * DryWeight;

    /// <summary>Its speed downwards, metres per second.</summary>
    public double Speed { get; private set; }

    /// <summary>The share of the hull under water.</summary>
    public double Submerged => Math.Clamp(Depth / Height, 0, 1);

    public bool OnSeabed => Depth >= SeaDepth - 1e-9;

    /// <summary>Its weight with the tanks <paramref name="ballast"/> full (0–1), over the weight of the water it pushes aside fully under.</summary>
    public static double Weight(double ballast) => DryWeight + (Tanks * Math.Clamp(ballast, 0, 1));

    /// <summary>Moves on <paramref name="seconds"/> with the tanks <paramref name="ballast"/> full.</summary>
    public void Step(double seconds, double ballast)
    {
        const double Dt = 0.01;
        var weight = Weight(ballast);
        for (var t = 0.0; t < seconds; t += Dt)
        {
            var dt = Math.Min(Dt, seconds - t);

            // Pushed by gravity and the water, with the water around it to move too (about as much again).
            var accel = (Water.Gravity * (weight - Submerged) / (2 * weight)) - (Drag * Speed * Math.Abs(Speed)) - (Settle * Speed);
            Speed += accel * dt;
            Depth += Speed * dt;
            if (Depth >= SeaDepth)
            {
                Depth = SeaDepth;
                Speed = Math.Min(0, Speed);
            }
        }
    }
}
