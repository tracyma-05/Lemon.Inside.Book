namespace Lemon.Inside.Book.Chapters.Plumbing;

/// <summary>Water standing still pushes harder the deeper it is: about one bar for every ten metres.</summary>
public static class Water
{
    /// <summary>Gravity, m/s².</summary>
    public const double G = 9.81;

    /// <summary>The height of a column of water the air holds up at sea level, m (one atmosphere).</summary>
    public const double Atmosphere = 10.3;

    /// <summary>The pressure under <paramref name="head"/> metres of water, bar.</summary>
    public static double Pressure(double head) => Math.Max(0, head) * 1000 * G / 100_000;

    /// <summary>The fastest water can leave an opening <paramref name="head"/> metres below its surface, m/s.</summary>
    public static double Speed(double head) => Math.Sqrt(2 * G * Math.Max(0, head));
}

/// <summary>1. A water tower: its water's height above a tap sets the tap's pressure.</summary>
public static class Tower
{
    /// <summary>One storey, m.</summary>
    public const double Storey = 3;

    /// <summary>How far the tower's water stands above a tap at <paramref name="tap"/> m; below zero, none comes out.</summary>
    public static double Head(double surface, double tap) => surface - tap;

    public static bool Flows(double surface, double tap) => Head(surface, tap) > 0.05;
}

/// <summary>2. A screw-down tap: the handle lifts a washer off a round seat.</summary>
public static class Tap
{
    /// <summary>The seat's opening, mm; how far one turn lifts the washer, mm; the flow fully open, litres a minute.</summary>
    public const double Seat = 12;
    public const double Pitch = 1.5;
    public const double FullFlow = 12;

    /// <summary>A worn washer's leak when shut, litres a minute (a drip every second or so).</summary>
    public const double Drip = 0.03;

    public static double Lift(double turns) => Math.Max(0, turns) * Pitch;

    /// <summary>
    /// How open the tap is, 0..1: the gap round the washer (seat's rim × lift) against the seat's own area. Once the
    /// lift is a quarter of the seat's width the gap is as big as the hole, and lifting further gives nothing more.
    /// </summary>
    public static double Opening(double turns) => Math.Min(1, Lift(turns) / (Seat / 4));

    /// <summary>The turns that open the tap fully.</summary>
    public static double FullTurns => Seat / 4 / Pitch;

    public static double Flow(double turns, bool worn) => Math.Max(Opening(turns) * FullFlow, worn ? Drip : 0);
}

/// <summary>3. A siphon: a full hose over a crest, emptying a tank to a lower outlet.</summary>
public static class Siphon
{
    /// <summary>The tank's water surface and the hose's crest above the floor, m.</summary>
    public const double Surface = 2;
    public const double Crest = 3.3;

    /// <summary>Water flows while the hose is full and its outlet is below the tank's surface.</summary>
    public static bool Flows(bool primed, double outlet) => primed && outlet < Surface - 0.01;

    /// <summary>How fast water leaves the outlet, m/s (at best: the hose's rubbing slows it).</summary>
    public static double Speed(bool primed, double outlet) => Flows(primed, outlet) ? Water.Speed(Surface - outlet) : 0;

    /// <summary>The highest a crest can stand above the tank's water before the water in it boils away, m.</summary>
    public static double HighestCrest => Water.Atmosphere;
}

/// <summary>4. A toilet cistern: a float on an arm shuts the inlet valve as the water rises.</summary>
public static class Cistern
{
    /// <summary>The tank's depth and the overflow pipe's top, cm; how far the float rises from fully open to shut, cm.</summary>
    public const double Depth = 40;
    public const double Overflow = 34;
    public const double Band = 4;

    /// <summary>The fastest the valve fills the tank and the flush empties it, cm a second (a picture's time, sped up).</summary>
    public const double FillRate = 3;
    public const double DrainRate = 18;

    /// <summary>How far open the valve is, 0..1, with the water at <paramref name="level"/> and the float set to shut at <paramref name="set"/>.</summary>
    public static double Opening(double level, double set) => Math.Clamp((set - level) / Band, 0, 1);

    /// <summary>Where the water settles: at the float's level, or at the overflow if that is lower.</summary>
    public static double Settles(double set) => Math.Min(set, Overflow);

    /// <summary>Set above the overflow, the float never shuts the valve and the water runs away down the overflow.</summary>
    public static bool Spills(double set) => Opening(Overflow, set) > 0;

    /// <summary>The water's level after <paramref name="seconds"/>, flushing or not.</summary>
    public static double Step(double level, double set, bool flushing, double seconds)
    {
        level += FillRate * Opening(level, set) * seconds;
        if (flushing) level -= DrainRate * seconds;
        return Math.Clamp(level, 0, Overflow);
    }
}

/// <summary>5. A trap: the U-bend under a sink holds a plug of water between the room and the drain.</summary>
public static class Trap
{
    /// <summary>The seal: how far the water stands above the bend's dip, cm.</summary>
    public const double Seal = 5;

    /// <summary>A vent pipe lets air into the drain, leaving only this share of the suction.</summary>
    public const double Vented = 0.1;

    /// <summary>The suction the trap feels, cm of water.</summary>
    public static double Felt(double suction, bool vent) => Math.Max(0, suction) * (vent ? Vented : 1);

    /// <summary>
    /// How far the sink side falls, cm: the drain side cannot rise above its outlet (the extra spills away), so the
    /// whole difference shows on the sink side, until it reaches the dip.
    /// </summary>
    public static double Drop(double suction, bool vent) => Math.Min(Felt(suction, vent), Seal);

    public static double Left(double suction, bool vent) => Seal - Drop(suction, vent);

    public static bool Broken(double suction, bool vent) => Felt(suction, vent) >= Seal;
}

/// <summary>6. A hand pump: the air outside pushes the water up, so it cannot lift it more than about ten metres.</summary>
public static class Pump
{
    /// <summary>How high the water comes up the pipe, m: a pump at the top only lowers the pressure above the water.</summary>
    public static double Rise(double depth, bool down) => down ? depth : Math.Min(depth, Water.Atmosphere);

    /// <summary>Does water reach the spout?</summary>
    public static bool Works(double depth, bool down) => down || depth < Water.Atmosphere;
}
