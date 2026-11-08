namespace Lemon.Inside.Book.Chapters.Canals;

/// <summary>1. A lock chamber filling through the paddles in its upper gate.</summary>
public static class Chamber
{
    /// <summary>The chamber's length and width, m; the lock's rise, m; one paddle hole, m²; the hole's discharge coefficient.</summary>
    public const double Length = 30;
    public const double Width = 6;
    public const double Rise = 3;
    public const double Paddle = 0.5;
    public const double Discharge = 0.6;
    public const double G = 9.81;

    public static double Area => Length * Width;

    /// <summary>
    /// How fast the chamber's water rises, m/s, with <paramref name="level"/> m of the rise already done: water pushed
    /// through the paddles by the difference left, √(2 g Δh), so it slows as the levels close.
    /// </summary>
    public static double RiseRate(double level, int paddles) =>
        Discharge * Paddle * Math.Clamp(paddles, 1, 4) * Math.Sqrt(2 * G * Math.Max(0, Rise - level)) / Area;

    /// <summary>The time to fill from empty to level, s: 2 A √Δh / (C a √2g).</summary>
    public static double FillTime(int paddles) =>
        2 * Area * Math.Sqrt(Rise) / (Discharge * Paddle * Math.Clamp(paddles, 1, 4) * Math.Sqrt(2 * G));

    /// <summary>How long the first and the last metre take, s.</summary>
    public static double MetreTime(int paddles, bool last) =>
        last ? FillTime(paddles) * Math.Sqrt(1 / Rise) : FillTime(paddles) * (1 - Math.Sqrt((Rise - 1) / Rise));
}

/// <summary>2. Mitre gates: two leaves meeting at an angle, pointed upstream so the water presses them shut.</summary>
public static class Gates
{
    /// <summary>Each leaf's width, m.</summary>
    public const double Leaf = 3.5;

    /// <summary>The water's push on one leaf, tonnes-force, for a difference of <paramref name="head"/> m: ½ ρ g h² w.</summary>
    public static double Push(double head) => 0.5 * 1000 * Math.Max(0, head) * Math.Max(0, head) * Leaf / 1000;

    /// <summary>Pointed upstream the push closes them; pointed downstream it swings them open.</summary>
    public static bool Hold(double head, bool pointedDownstream) => !pointedDownstream || head < 0.01;
}

/// <summary>3. The water a boat sends downhill, and side ponds that save some of it.</summary>
public static class Ponds
{
    /// <summary>The chamberful a boat uses without side ponds, m³.</summary>
    public static double Chamberful => Chamber.Area * Chamber.Rise;

    /// <summary>The share used with <paramref name="ponds"/> side ponds as large as the chamber: 2 / (n + 2).</summary>
    public static double Share(int ponds) => 2.0 / (Math.Clamp(ponds, 0, 3) + 2);

    public static double PerBoat(int ponds) => Chamberful * Share(ponds);
}

/// <summary>4. A flight of locks up a hill: more locks, smaller rises.</summary>
public static class Flight
{
    /// <summary>The hill, m; the time to work one lock, min.</summary>
    public const double Hill = 24;
    public const double PerLock = 12;

    public static double Step(int locks) => Hill / Math.Clamp(locks, 1, 8);

    /// <summary>The water a boat takes down from the top, m³: one chamberful of one lock's rise, carried down every lock.</summary>
    public static double Water(int locks) => Chamber.Area * Step(locks);

    public static double Minutes(int locks) => PerLock * Math.Clamp(locks, 1, 8);
}

/// <summary>5. A boat lift: two water-filled tanks balanced against each other.</summary>
public static class BoatLift
{
    /// <summary>Each tank's water depth, m; and its water's mass, tonnes.</summary>
    public const double Depth = 3;

    public static double Water => Chamber.Area * Depth;

    /// <summary>
    /// How much heavier the left tank is, tonnes: a floating boat pushes out its own weight of water, so it adds nothing;
    /// only extra water does.
    /// </summary>
    public static double Imbalance(bool boat, double extraCm) => Chamber.Area * extraCm / 100;
}

/// <summary>6. Towing a barge: water's drag is tiny at a walking pace and grows with the square of the speed.</summary>
public static class Towing
{
    /// <summary>The drag's scale, kg-force per (tonne^⅔ (km/h)²); a horse's steady pull, kg-force; a road cart's rolling share.</summary>
    public const double Scale = 0.39;
    public const double Horse = 80;
    public const double RoadShare = 0.05;

    /// <summary>The pull to keep a barge of <paramref name="load"/> tonnes moving at <paramref name="speed"/> km/h, kg-force.</summary>
    public static double Drag(double load, double speed) => Scale * Math.Pow(Math.Max(1, load), 2.0 / 3) * speed * speed;

    /// <summary>What the same horse could pull on a road, tonnes.</summary>
    public static double OnRoad => Horse / RoadShare / 1000;

    public static bool Manages(double load, double speed) => Drag(load, speed) <= Horse;
}
