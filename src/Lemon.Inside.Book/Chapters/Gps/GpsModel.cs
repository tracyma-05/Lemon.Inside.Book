namespace Lemon.Inside.Book.Chapters.Gps;

/// <summary>A point on a flat map, in kilometres east and north of its middle.</summary>
public readonly record struct MapPoint(double X, double Y)
{
    public static MapPoint operator +(MapPoint a, MapPoint b) => new(a.X + b.X, a.Y + b.Y);

    public static MapPoint operator -(MapPoint a, MapPoint b) => new(a.X - b.X, a.Y - b.Y);

    public static MapPoint operator *(MapPoint a, double k) => new(a.X * k, a.Y * k);

    public double Length => Math.Sqrt((X * X) + (Y * Y));

    public double DistanceTo(MapPoint o) => (this - o).Length;

    public override string ToString() => FormattableString.Invariant($"({X:0.##}, {Y:0.##})");
}

/// <summary>1. Radio signals travel at the speed of light: a delay is a distance.</summary>
public static class Radio
{
    /// <summary>The speed of light, km a second.</summary>
    public const double Speed = 299_792.458;

    /// <summary>How far a signal goes in this many milliseconds, km.</summary>
    public static double Km(double milliseconds) => Speed * milliseconds / 1000;

    /// <summary>How long a signal takes to go this far, ms.</summary>
    public static double Milliseconds(double km) => km / Speed * 1000;

    /// <summary>How far a signal goes in a microsecond, metres (about 300).</summary>
    public static double MetresPerMicrosecond => Speed / 1000;
}

/// <summary>Satellites going round the Earth: heights in km above the ground.</summary>
public static class Orbit
{
    public const double EarthRadius = 6371;

    /// <summary>The Earth's pull, km³/s² (its mass times the gravitational constant).</summary>
    public const double Mu = 398_600.4418;

    /// <summary>A GPS satellite's height, km.</summary>
    public const double Gps = 20_200;

    /// <summary>The time for one round, seconds.</summary>
    public static double Period(double height)
    {
        var a = EarthRadius + Math.Max(0, height);
        return 2 * Math.PI * Math.Sqrt(a * a * a / Mu);
    }

    public static double Hours(double height) => Period(height) / 3600;

    /// <summary>Its speed along the orbit, km a second.</summary>
    public static double Speed(double height) => Math.Sqrt(Mu / (EarthRadius + Math.Max(0, height)));

    /// <summary>The share of the Earth's surface it can see (the cap inside its horizon).</summary>
    public static double Seen(double height) => (1 - (EarthRadius / (EarthRadius + Math.Max(0, height)))) / 2;

    /// <summary>How far off a satellite at this height is when it sits on your horizon, km.</summary>
    public static double HorizonDistance(double height)
    {
        var a = EarthRadius + height;
        return Math.Sqrt((a * a) - (EarthRadius * EarthRadius));
    }

    /// <summary>
    /// In a cut through the Earth's middle, with the satellite straight up from it at <paramref name="height"/>: the
    /// angle round the Earth from below the satellite, degrees, of the places <paramref name="distance"/> km from it (or
    /// null if no place on the ground is that far).
    /// </summary>
    public static double? GroundAngle(double height, double distance)
    {
        var a = EarthRadius + height;
        var c = ((a * a) + (EarthRadius * EarthRadius) - (distance * distance)) / (2 * a * EarthRadius);
        return c is < -1 or > 1 ? null : Math.Acos(c) * 180 / Math.PI;
    }
}

/// <summary>Circles on a flat map: where two distances agree.</summary>
public static class Circles
{
    /// <summary>The points where two circles cross (none, one if they just touch, or two).</summary>
    public static IReadOnlyList<MapPoint> Cross(MapPoint c1, double r1, MapPoint c2, double r2)
    {
        var d = c1.DistanceTo(c2);
        if (d < 1e-9 || d > r1 + r2 + 1e-9 || d < Math.Abs(r1 - r2) - 1e-9) return [];

        var a = ((r1 * r1) - (r2 * r2) + (d * d)) / (2 * d);
        var h = Math.Sqrt(Math.Max(0, (r1 * r1) - (a * a)));
        var u = (c2 - c1) * (1 / d);
        var mid = c1 + (u * a);
        var normal = new MapPoint(-u.Y, u.X);
        return h < 1e-6 ? [mid] : [mid + (normal * h), mid - (normal * h)];
    }

    /// <summary>
    /// Where two circles come closest to agreeing near <paramref name="near"/>: the crossing nearest it, or, if they do
    /// not cross, the point between them on the line through their centres.
    /// </summary>
    public static MapPoint Nearest(MapPoint c1, double r1, MapPoint c2, double r2, MapPoint near)
    {
        var cross = Cross(c1, r1, c2, r2);
        if (cross.Count > 0) return cross.OrderBy(p => p.DistanceTo(near)).First();

        var d = c1.DistanceTo(c2);
        var u = (c2 - c1) * (1 / d);
        var gap = d - r1 - r2;
        return gap > 0 ? c1 + (u * (r1 + (gap / 2))) : c1 + (u * (r1 > r2 ? r1 : -r1));
    }
}

/// <summary>2 and 3. Three satellites over a flat map (their spots straight below, really), km from its middle.</summary>
public static class Map
{
    public static readonly MapPoint[] Satellites = [new(-150, 120), new(160, 100), new(10, -170)];

    /// <summary>Where the receiver really is in spread 3.</summary>
    public static readonly MapPoint Here = new(30, 10);

    /// <summary>
    /// Where you could be, knowing your distance from the first <paramref name="count"/> satellites: for one, null (a
    /// whole circle); for two, the two crossings; for three, the one place all three agree.
    /// </summary>
    public static IReadOnlyList<MapPoint>? Places(MapPoint you, int count)
    {
        var r = Satellites.Select(s => s.DistanceTo(you)).ToArray();
        return count switch
        {
            <= 1 => null,
            2 => Circles.Cross(Satellites[0], r[0], Satellites[1], r[1]),
            _ => [you],
        };
    }

    /// <summary>
    /// The distances a receiver whose clock is <paramref name="microseconds"/> slow works out: every one too long by the
    /// same amount (the signal seems to have taken longer).
    /// </summary>
    public static double[] Measured(double microseconds) =>
        Satellites.Select(s => s.DistanceTo(Here) + (Radio.MetresPerMicrosecond * microseconds / 1000)).ToArray();

    /// <summary>How far apart the three circles' meeting points are, km: 0 when the clock is right.</summary>
    public static double Miss(double microseconds)
    {
        var r = Measured(microseconds);
        var p = new[]
        {
            Circles.Nearest(Satellites[0], r[0], Satellites[1], r[1], Here),
            Circles.Nearest(Satellites[1], r[1], Satellites[2], r[2], Here),
            Circles.Nearest(Satellites[2], r[2], Satellites[0], r[0], Here),
        };
        return Math.Max(p[0].DistanceTo(p[1]), Math.Max(p[1].DistanceTo(p[2]), p[2].DistanceTo(p[0])));
    }
}

/// <summary>4. The satellites' clocks: weaker gravity makes them run fast, their speed slow.</summary>
public static class Clocks
{
    /// <summary>Microseconds a day a GPS satellite's clock gains from sitting higher in the Earth's gravity.</summary>
    public const double Gravity = 45.7;

    /// <summary>Microseconds a day it loses from moving fast.</summary>
    public const double Motion = 7.2;

    public static double PerDay => Gravity - Motion;

    /// <summary>How far ahead the satellite's clock is after this many days, µs (none once corrected).</summary>
    public static double Ahead(double days, bool corrected) => corrected ? 0 : PerDay * Math.Max(0, days);

    /// <summary>How far a position worked out with that clock would drift, km.</summary>
    public static double DriftKm(double days, bool corrected) => Ahead(days, corrected) * Radio.MetresPerMicrosecond / 1000;
}

/// <summary>
/// 6. How satellites spread round the sky set how good a fix is: each distance is only good to ±<c>error</c> metres, so
/// each circle is a band, and two bands crossing at <c>angle</c> degrees overlap in a diamond.
/// </summary>
public static class Patch
{
    /// <summary>The diamond's longer diagonal, metres.</summary>
    public static double Longest(double error, double angle)
    {
        var half = Math.Clamp(angle, 1, 179) * Math.PI / 360;
        return 2 * error / Math.Min(Math.Sin(half), Math.Cos(half));
    }

    /// <summary>The diamond's area, square metres.</summary>
    public static double Area(double error, double angle) => 4 * error * error / Math.Sin(Math.Clamp(angle, 1, 179) * Math.PI / 180);

    /// <summary>The smallest diamond, satellites at right angles.</summary>
    public static double Best(double error) => Longest(error, 90);
}
