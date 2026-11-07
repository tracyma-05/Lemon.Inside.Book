namespace Lemon.Inside.Book.Chapters.Weighing;

/// <summary>1. An equal-arm balance: a beam on a knife edge, its centre of mass a little below the edge.</summary>
public static class Balance
{
    /// <summary>Each arm, m; the beam's own mass, kg; the most the picture tilts, degrees.</summary>
    public const double Arm = 0.15;
    public const double Beam = 0.2;
    public const double MostTilt = 40;

    /// <summary>With its centre of mass at or above the knife edge the beam has nothing pulling it back level.</summary>
    public static bool Topples(double drop) => drop <= 0;

    /// <summary>
    /// The tilt, degrees, for <paramref name="difference"/> grams more on the right pan and the beam's centre of mass
    /// <paramref name="drop"/> cm below the knife edge: tan θ = difference × arm / (beam × drop).
    /// </summary>
    public static double Tilt(double difference, double drop)
    {
        if (Topples(drop)) return MostTilt;
        var angle = Math.Atan(difference / 1000 * Arm / (Beam * drop / 100)) * 180 / Math.PI;
        return Math.Min(angle, MostTilt);
    }
}

/// <summary>2. A steelyard: a short arm for the load, a long marked arm with a sliding weight.</summary>
public static class Steelyard
{
    /// <summary>The load's arm, cm; the sliding weight, kg; the long arm's length, cm.</summary>
    public const double ShortArm = 5;
    public const double Poise = 1;
    public const double LongArm = 100;

    /// <summary>What the arm reads with the weight <paramref name="position"/> cm out: kilograms on the hook it balances.</summary>
    public static double Reading(double position) => position * Poise / ShortArm;

    /// <summary>Where the weight balances a sack, cm.</summary>
    public static double BalanceAt(double sack) => sack * ShortArm / Poise;

    public static bool Balanced(double sack, double position) => Math.Abs(Reading(position) - sack) <= 0.1;

    /// <summary>The arm's tilt, degrees: positive when the sack's side goes down.</summary>
    public static double Tilt(double sack, double position) =>
        Balanced(sack, position) ? 0 : Math.Clamp(((sack * ShortArm) - (Poise * position)) * 0.6, -12, 12);
}

/// <summary>3. A spring balance: the stretch grows with the load, until the spring is stretched too far.</summary>
public static class Spring
{
    /// <summary>The stretch per kilogram, cm; the heaviest load it springs back from, kg; the lasting set per kilogram beyond, cm.</summary>
    public const double PerKg = 2;
    public const double Limit = 6;
    public const double SetPerKg = 0.6;

    /// <summary>The stretch it keeps for good after <paramref name="highest"/> kg has hung on it, cm.</summary>
    public static double Set(double highest) => Math.Max(0, highest - Limit) * SetPerKg;

    /// <summary>The stretch with <paramref name="load"/> kg on it, cm.</summary>
    public static double Stretch(double load, double highest) => (load * PerKg) + Set(highest);

    /// <summary>What its pointer reads, kg (marked for a spring that still springs back to zero).</summary>
    public static double Reading(double load, double highest) => Stretch(load, highest) / PerKg;
}

/// <summary>
/// 4. A kitchen scale: the pan on a single arm reads more the farther out the load sits; on two parallel links
/// (Roberval's linkage) the pan stays level and reads the same anywhere.
/// </summary>
public static class Roberval
{
    /// <summary>The load, g; the arm from the pivot to the pan's middle, cm.</summary>
    public const double Load = 500;
    public const double Arm = 10;

    public static double Reading(double offset, bool parallel) => parallel ? Load : Load * (Arm + offset) / Arm;
}

/// <summary>5. A load cell: a bar that bends a little, and gauges whose resistance follows its stretch.</summary>
public static class Gauge
{
    /// <summary>The heaviest load, kg, and the bar's stretch under it; the gauge factor; its resistance, Ω; the supply, V.</summary>
    public const double FullLoad = 5;
    public const double FullStrain = 500e-6;
    public const double Factor = 2;
    public const double Resistance = 350;
    public const double Supply = 5;

    /// <summary>How much a gauge's reading drifts for each degree it warms, as if stretched; and the warming, °C.</summary>
    public const double DriftPerDegree = 10e-6;
    public const double Warming = 10;

    public static double Strain(double mass) => mass / FullLoad * FullStrain;

    /// <summary>A gauge's resistance change, Ω.</summary>
    public static double Change(double mass) => Resistance * Factor * Strain(mass);

    /// <summary>
    /// What the display reads, kg. One gauge also takes warmth for stretch; four gauges in a bridge, two stretched and
    /// two squeezed, all warm alike, so the warmth cancels and only the bending is left.
    /// </summary>
    public static double Reading(double mass, bool warm, bool bridge) =>
        (Strain(mass) + (warm && !bridge ? DriftPerDegree * Warming : 0)) / FullStrain * FullLoad;

    /// <summary>The bridge's output, mV: four gauges give four times one gauge's.</summary>
    public static double Output(double mass, bool bridge) => Supply * Factor * Strain(mass) * 1000 / (bridge ? 1 : 4);
}

/// <summary>6. Weight and mass: a spring measures the pull of gravity, a balance compares masses.</summary>
public static class Worlds
{
    /// <summary>Gravity on the Earth, the Moon and Mars, m/s².</summary>
    public static readonly double[] Gravity = [9.81, 1.62, 3.71];

    public const double Earth = 9.81;

    /// <summary>What a spring scale marked on the Earth reads, kg.</summary>
    public static double SpringReads(double mass, int place) => mass * Gravity[Math.Clamp(place, 0, 2)] / Earth;

    /// <summary>What a balance reads, kg: the same anywhere, because both pans are pulled alike.</summary>
    public static double BalanceReads(double mass) => mass;
}
