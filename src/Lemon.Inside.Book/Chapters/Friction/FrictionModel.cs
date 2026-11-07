namespace Lemon.Inside.Book.Chapters.Friction;

/// <summary>1. A block on a ramp: it stays while the grip it needs is less than the grip it has.</summary>
public static class Ramp
{
    /// <summary>The block's mass, kg; the grip (static friction coefficient) of a wooden block on ice, wood and rubber.</summary>
    public const double Block = 10;
    public static readonly double[] Grip = [0.05, 0.4, 0.8];

    public static double Surface(int surface) => Grip[Math.Clamp(surface, 0, 2)];

    /// <summary>The pull down the slope, kg-force: weight × sin(angle).</summary>
    public static double Downhill(double angle) => Block * Math.Sin(angle * Math.PI / 180);

    /// <summary>The most the surface can hold back, kg-force: grip × weight × cos(angle).</summary>
    public static double MostGrip(double angle, int surface) => Surface(surface) * Block * Math.Cos(angle * Math.PI / 180);

    /// <summary>It slides once the slope's steepness (tan) passes the grip, whatever the block weighs.</summary>
    public static bool Slides(double angle, int surface) => Math.Tan(angle * Math.PI / 180) > Surface(surface);

    /// <summary>The steepest angle it stays at, degrees.</summary>
    public static double Steepest(int surface) => Math.Atan(Surface(surface)) * 180 / Math.PI;
}

/// <summary>2. Pulling a box: friction matches the pull until it breaks free, then drops to the smaller sliding friction.</summary>
public static class Drag
{
    /// <summary>Grip at rest and sliding (static and kinetic coefficients) for a wooden box on a floor.</summary>
    public const double AtRest = 0.5;
    public const double Sliding = 0.35;

    public static double BreakAway(double box) => AtRest * box;

    public static bool Moving(double box, double pull) => pull > BreakAway(box) + 1e-9;

    /// <summary>The floor's friction on the box, kg-force.</summary>
    public static double Friction(double box, double pull) => Moving(box, pull) ? Sliding * box : Math.Max(0, pull);
}

/// <summary>3. Braking: a rolling tyre grips better than a sliding one.</summary>
public static class Skid
{
    /// <summary>A tyre's grip on a dry road, rolling (its patch not sliding) and skidding.</summary>
    public const double Rolling = 0.9;
    public const double Locked = 0.7;
    public const double G = 9.81;

    /// <summary>The distance to stop from <paramref name="speed"/> km/h, m: v² / (2 × grip × g).</summary>
    public static double Distance(double speed, bool locked)
    {
        var v = speed / 3.6;
        return v * v / (2 * (locked ? Locked : Rolling) * G);
    }
}

/// <summary>4. A rope round a post: every bit of the wrap adds grip, so the hold shrinks as e^(grip × angle).</summary>
public static class Capstan
{
    /// <summary>The boat's pull on the rope, kg-force; the rope's grip on the post, dry and wet.</summary>
    public const double Boat = 1000;
    public const double Dry = 0.3;
    public const double Wet = 0.15;

    /// <summary>How many times the hold is multiplied by the wrap.</summary>
    public static double Gain(double turns, bool wet) => Math.Exp((wet ? Wet : Dry) * 2 * Math.PI * Math.Max(0, turns));

    /// <summary>The pull the hand must hold, kg-force.</summary>
    public static double Hold(double turns, bool wet) => Boat / Gain(turns, wet);
}

/// <summary>5. Dragging, rolling on logs, and wheels on ball bearings.</summary>
public static class Rolling
{
    /// <summary>The force to keep a load moving, as a share of its weight: dragged, on rollers, on wheels with bearings.</summary>
    public static readonly double[] Share = [0.4, 0.04, 0.005];

    public static double Force(double crate, int way) => crate * Share[Math.Clamp(way, 0, 2)];
}

/// <summary>6. Oil between two moving surfaces: a film that thickens with speed and keeps the bumps apart.</summary>
public static class Lubrication
{
    /// <summary>The surfaces' bumps, µm; the film at full speed, µm; friction dry and in the film's limits.</summary>
    public const double Roughness = 1;
    public const double FullFilm = 2.5;
    public const double Dry = 0.4;

    /// <summary>The oil film's thickness, µm, at <paramref name="speed"/> % of full speed.</summary>
    public static double Film(double speed) => FullFilm * Math.Sqrt(Math.Clamp(speed, 0, 100) / 100);

    /// <summary>Friction as a share of the load: the bumps rubbing while the film is thin, the oil's own drag when it is thick.</summary>
    public static double Friction(double speed, bool oiled)
    {
        if (!oiled) return Dry;
        var ratio = Film(speed) / Roughness;
        return (0.1 / (1 + Math.Pow(ratio, 4))) + 0.002 + (0.006 * Math.Clamp(speed, 0, 100) / 100);
    }

    /// <summary>Do the bumps still touch through the oil?</summary>
    public static bool Touching(double speed, bool oiled) => !oiled || Film(speed) < Roughness;
}
