namespace Lemon.Inside.Book.Chapters.Lifting;

// The arithmetic behind the Lifting chapter's pictures. Forces are given as "kilograms held": how hard the reader
// pushes or pulls, said as the weight they would hold up with that force, so no newtons appear in the words.

/// <summary>A lever: a 4 m bar on a pivot, a stone on the short end, a hand on the long one.</summary>
public static class Seesaw
{
    /// <summary>The bar's length, m.</summary>
    public const double Length = 4;

    /// <summary>The pivot's height above the ground, m.</summary>
    public const double PivotHeight = 0.3;

    /// <summary>The stone's mass, kg.</summary>
    public const double Stone = 200;

    /// <summary>How hard the hand pushes (kg held) with the pivot <paramref name="pivot"/> m from the stone's end.</summary>
    public static double Effort(double load, double pivot) => load * pivot / (Length - pivot);

    /// <summary>How far the hand goes down for each unit the stone rises.</summary>
    public static double Travel(double pivot) => (Length - pivot) / pivot;

    /// <summary>
    /// The bar's tilt, radians: how far the stone's end is turned up (and the hand's end down): <paramref name="push"/> 0 is
    /// the stone's end resting on the ground, 1 the hand's end down on the ground.
    /// </summary>
    public static double Tilt(double pivot, double push)
    {
        var low = -Math.Asin(Math.Min(1, PivotHeight / pivot));
        var high = Math.Asin(Math.Min(1, PivotHeight / (Length - pivot)));
        return low + ((high - low) * Math.Clamp(push, 0, 1));
    }

    /// <summary>How far the stone's end is above where it started, m.</summary>
    public static double Rise(double pivot, double push) =>
        pivot * (Math.Sin(Tilt(pivot, push)) - Math.Sin(Tilt(pivot, 0)));

    /// <summary>How far the hand's end has gone down, m.</summary>
    public static double Drop(double pivot, double push) =>
        (Length - pivot) * (Math.Sin(Tilt(pivot, push)) - Math.Sin(Tilt(pivot, 0)));
}

/// <summary>One pulley, fixed to a beam or riding on the load.</summary>
public static class Pulleys
{
    /// <summary>The bucket's mass, kg.</summary>
    public const double Bucket = 40;

    /// <summary>Strands of rope holding the load: one over a fixed pulley, two round a moving one.</summary>
    public static int Strands(bool moving) => moving ? 2 : 1;

    public static double Effort(double load, bool moving) => load / Strands(moving);

    /// <summary>How far the load rises for <paramref name="pulled"/> of rope pulled.</summary>
    public static double Rise(double pulled, bool moving) => pulled / Strands(moving);
}

/// <summary>A block and tackle: several strands of one rope between a fixed block and a moving one.</summary>
public static class Tackle
{
    /// <summary>The load, kg (an engine on its way out of a car).</summary>
    public const double Load = 300;

    /// <summary>What a strand keeps of the pull on the strand before it, past one wheel's rubbing.</summary>
    public const double Efficiency = 0.95;

    public const int MostStrands = 6;

    /// <summary>
    /// How many times the load is the pull. The hand's rope goes over a wheel into the first strand, every wheel losing
    /// a little: the strands carry T·e, T·e², … T·eⁿ, and together they hold the load.
    /// </summary>
    public static double Advantage(int strands, bool friction)
    {
        if (!friction) return strands;
        var sum = 0.0;
        for (var k = 1; k <= strands; k++) sum += Math.Pow(Efficiency, k);
        return sum;
    }

    public static double Effort(double load, int strands, bool friction) => load / Advantage(strands, friction);

    /// <summary>Rope to pull for each metre the load rises: one for every strand that must shorten.</summary>
    public static double RopePerMetre(int strands) => strands;

    /// <summary>The wheels in the top block (the first over which the hand's rope runs) and the bottom one.</summary>
    public static (int Top, int Bottom) Wheels(int strands) => (1 + ((strands - 1) / 2), strands / 2);
}

/// <summary>A hand winch: a crank turns a drum the rope winds on, straight or through a worm.</summary>
public static class Winch
{
    /// <summary>The load, kg.</summary>
    public const double Load = 100;

    /// <summary>The drum's radius, m.</summary>
    public const double Drum = 0.05;

    /// <summary>Teeth on the worm wheel: one turn of the worm moves it one tooth.</summary>
    public const int WormRatio = 20;

    /// <summary>The worm thread's slope, degrees, and the friction between thread and teeth.</summary>
    public const double Lead = 4;

    public const double Friction = 0.1;

    /// <summary>Of the work the hand puts into a worm, what reaches the wheel (the rest warms the thread).</summary>
    public static double WormEfficiency
    {
        get
        {
            var lead = Lead * Math.PI / 180;
            return Math.Tan(lead) / Math.Tan(lead + Math.Atan(Friction));
        }
    }

    /// <summary>
    /// A worm cannot be turned by its wheel when its thread slopes less than friction allows: the load can never run
    /// it backwards.
    /// </summary>
    public static bool SelfLocking(double leadDegrees, double friction) => Math.Tan(leadDegrees * Math.PI / 180) < friction;

    /// <summary>How hard the hand pushes on a crank of radius <paramref name="handle"/> m.</summary>
    public static double Effort(double load, double handle, bool worm) =>
        worm ? load * Drum / handle / (WormRatio * WormEfficiency) : load * Drum / handle;

    /// <summary>Turns of the handle to wind up one metre.</summary>
    public static double TurnsPerMetre(bool worm) => (worm ? WormRatio : 1) / (2 * Math.PI * Drum);

    /// <summary>Whether the load stays up once the handle is let go.</summary>
    public static bool Holds(bool worm) => worm && SelfLocking(Lead, Friction);
}

/// <summary>A hydraulic jack: a pump piston pushes oil under a wide ram.</summary>
public static class Jack
{
    /// <summary>The weight on the ram, kg (one side of a car).</summary>
    public const double Load = 1000;

    /// <summary>The handle's own lever: the hand is this many times as far from its pivot as the pump piston.</summary>
    public const double HandleLever = 6;

    /// <summary>One pump stroke, mm.</summary>
    public const double Stroke = 20;

    /// <summary>How many times the ram's area is the pump's, the ram <paramref name="diameter"/> times as wide.</summary>
    public static double AreaRatio(double diameter) => diameter * diameter;

    /// <summary>How hard the hand presses (kg held).</summary>
    public static double Effort(double load, double diameter) => load / AreaRatio(diameter) / HandleLever;

    /// <summary>How far the ram rises for one stroke, mm: the oil pushed out of the pump spread under the wide ram.</summary>
    public static double RisePerStroke(double diameter) => Stroke / AreaRatio(diameter);

    public static double Rise(double diameter, int strokes) => strokes * RisePerStroke(diameter);
}

/// <summary>A traction lift: a car and a counterweight hung from either end of ropes over a driven wheel.</summary>
public static class Elevator
{
    /// <summary>The empty car, kg; what it is built to carry; one rider.</summary>
    public const double Car = 1200;

    public const double Rated = 1000;

    public const double Rider = 75;

    public const int MostRiders = 13;

    /// <summary>The counterweight: the car and a little under half its rated load.</summary>
    public const double Counterweight = Car + (0.45 * Rated);

    /// <summary>Between the cables' tensions on either side of the wheel, the most its grip can hold (friction 0.3 over half a turn).</summary>
    public static readonly double Grip = Math.Exp(0.3 * Math.PI);

    public static double CarSide(int riders) => Car + (riders * Rider);

    /// <summary>What the motor must hold (kg): the difference between the sides, or the whole car with nothing against it.</summary>
    public static double Motor(int riders, bool counterweight) =>
        counterweight ? Math.Abs(CarSide(riders) - Counterweight) : CarSide(riders);

    /// <summary>The heavier side: +1 the car's, −1 the counterweight's, 0 when they are even.</summary>
    public static int Heavier(int riders, bool counterweight) => counterweight ? Math.Sign(CarSide(riders) - Counterweight) : 1;

    /// <summary>Whether the ropes would slip on the wheel: one side more than <see cref="Grip"/> times the other.</summary>
    public static bool Slips(int riders, bool counterweight)
    {
        if (!counterweight) return false; // then the motor holds the rope end itself, a drum, not a grip
        var (a, b) = (CarSide(riders), Counterweight);
        return Math.Max(a, b) / Math.Min(a, b) > Grip;
    }
}
