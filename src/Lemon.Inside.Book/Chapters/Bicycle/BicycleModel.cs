namespace Lemon.Inside.Book.Chapters.Bicycle;

/// <summary>Numbers shared by the chapter's models: a grown-up's road bike with its rider.</summary>
public static class Bike
{
    public const double Gravity = 9.81;

    /// <summary>How far the bike rolls for one turn of its wheel, in metres (a 700c road tyre).</summary>
    public const double WheelCircumference = 2.1;

    public static double WheelRadius => WheelCircumference / (2 * Math.PI);

    /// <summary>The pedal crank's length, in metres.</summary>
    public const double CrankLength = 0.17;

    /// <summary>Bike plus rider, in kilograms.</summary>
    public const double Mass = 85;
}

/// <summary>
/// Spread 1: a spoked wheel standing under a load. A spoke is a thin wire: it can pull but not push. The wheel is built
/// with every spoke already pulling hard; a load presses the hub down, and the spokes below it (where the rim flattens a
/// little on the road) lose some of that pull, while the others hardly change. The hub is held up by the difference.
/// </summary>
public static class SpokedWheel
{
    public const int Spokes = 32;

    /// <summary>How sharply the change gathers at the bottom: the rim bends only near the road.</summary>
    private const int Spread = 4;

    /// <summary>
    /// Each spoke's pull in newtons, spoke 0 pointing at <paramref name="turned"/> radians from straight down (the wheel
    /// turned that far), built to <paramref name="tension"/> newtons and loaded with <paramref name="kilograms"/>. A spoke
    /// that would have to push goes slack instead (0).
    /// </summary>
    public static double[] Tensions(double tension, double kilograms, double turned = 0)
    {
        // Shared out so that, at this turn of the wheel, the pull given up adds up to the load exactly.
        var share = kilograms * Bike.Gravity / Normaliser(turned);
        var result = new double[Spokes];
        for (var i = 0; i < Spokes; i++)
        {
            var fromDown = turned + (i * 2 * Math.PI / Spokes);
            result[i] = Math.Max(0, tension - (share * Weight(fromDown)));
        }

        return result;
    }

    /// <summary>The pull lost by the spoke pointing straight down, in newtons.</summary>
    public static double BottomLoss(double kilograms) => kilograms * Bike.Gravity / Normaliser(0) * Weight(0);

    /// <summary>
    /// How much of the load spoke at <paramref name="fromDown"/> radians from straight down gives up: only the lower half,
    /// most of all straight down.
    /// </summary>
    private static double Weight(double fromDown)
    {
        var c = Math.Cos(fromDown);
        return c > 0 ? Math.Pow(c, Spread) : 0;
    }

    /// <summary>Upward pull summed over the spokes for each unit given up: makes the lost pull add up to the load.</summary>
    private static double Normaliser(double turned) => Enumerable.Range(0, Spokes)
        .Select(i => turned + (i * 2 * Math.PI / Spokes))
        .Sum(a => Weight(a) * Math.Cos(a));

    /// <summary>
    /// The upward force on the hub from all the spokes together, in newtons: each pulls the hub towards the rim, so the
    /// spokes above pull it up and those below pull it down.
    /// </summary>
    public static double UpwardPull(IReadOnlyList<double> tensions, double turned = 0)
    {
        var up = 0.0;
        for (var i = 0; i < tensions.Count; i++)
        {
            up -= tensions[i] * Math.Cos(turned + (i * 2 * Math.PI / tensions.Count));
        }

        return up;
    }
}

/// <summary>Spread 2: pedals, chain and wheel. The chain moves the same number of teeth past both sprockets.</summary>
public static class Drive
{
    /// <summary>A chain link's length (half an inch), in metres.</summary>
    public const double Pitch = 0.0127;

    /// <summary>Turns of the back wheel for one turn of the pedals.</summary>
    public static double Ratio(int chainring, int sprocket) => (double)chainring / sprocket;

    /// <summary>Metres rolled for one turn of the pedals.</summary>
    public static double Development(int chainring, int sprocket) => Ratio(chainring, sprocket) * Bike.WheelCircumference;

    /// <summary>The radius in metres of the circle through a sprocket's chain rollers.</summary>
    public static double PitchRadius(int teeth) => Pitch / (2 * Math.Sin(Math.PI / teeth));
}

/// <summary>
/// Spread 3: climbing. The rider keeps the same pedalling rhythm; the gear decides how fast that carries them, and the
/// road's pull back (the slope, the tyres, the air) decides how hard each push must be.
/// </summary>
public static class Climb
{
    /// <summary>The chainring the cassette's sprockets are used with.</summary>
    public const int Chainring = 40;

    /// <summary>The back sprockets, smallest (the fastest gear) first.</summary>
    public static IReadOnlyList<int> Cassette { get; } = [11, 13, 15, 17, 19, 21, 24, 28, 32, 36];

    /// <summary>Pedal turns per minute the rider keeps to.</summary>
    public const double Cadence = 80;

    /// <summary>A push the rider can keep up for a long climb, in kilograms' weight on the pedal.</summary>
    public const double ComfortableKilograms = 25;

    private const double Rolling = 0.005;
    private const double DragArea = 0.4;
    private const double AirDensity = 1.2;

    /// <summary>Road speed in metres per second at the rider's cadence in this sprocket.</summary>
    public static double Speed(int sprocket) => Cadence / 60 * Drive.Development(Chainring, sprocket);

    /// <summary>The road's pull back on the bike, in newtons, on a slope of <paramref name="percent"/> % at <paramref name="speed"/>.</summary>
    public static double Resistance(double percent, double speed)
    {
        var angle = Math.Atan(percent / 100);
        return (Bike.Mass * Bike.Gravity * (Math.Sin(angle) + (Rolling * Math.Cos(angle)))) + (0.5 * AirDensity * DragArea * speed * speed);
    }

    /// <summary>The average push on the pedals, in newtons, to hold the rider's cadence in this sprocket.</summary>
    public static double PedalForce(double percent, int sprocket)
    {
        var wheelTorque = Resistance(percent, Speed(sprocket)) * Bike.WheelRadius;
        var crankTorque = wheelTorque * Drive.Ratio(Chainring, sprocket);
        return crankTorque / Bike.CrankLength;
    }

    /// <summary>The same push as kilograms' weight standing on the pedal.</summary>
    public static double PedalKilograms(double percent, int sprocket) => PedalForce(percent, sprocket) / Bike.Gravity;

    /// <summary>The rider's power, in watts.</summary>
    public static double Power(double percent, int sprocket) => Resistance(percent, Speed(sprocket)) * Speed(sprocket);
}

/// <summary>
/// Spread 4: the freewheel in the back hub. The sprocket turns an inner driver carrying two spring-loaded pawls; the
/// wheel's shell has ratchet teeth inside. Driving forwards the pawls catch a tooth and turn the wheel; when the wheel
/// runs ahead (coasting, or pedalling backwards) the pawls ride up each tooth's slope and drop off its edge with a click.
/// </summary>
public sealed class Freewheel
{
    /// <summary>Ratchet teeth inside the shell.</summary>
    public const int Teeth = 24;

    /// <summary>How fast a coasting wheel slows, in turns per second, each second.</summary>
    public const double Slowing = 0.05;

    /// <summary>Turns of the driver (with the sprocket and pedals) so far.</summary>
    public double Driver { get; private set; }

    /// <summary>Turns of the wheel so far.</summary>
    public double Wheel { get; private set; }

    /// <summary>The wheel's speed, turns per second.</summary>
    public double WheelSpeed { get; private set; }

    /// <summary>Whether the pawls are pushing on a tooth (the pedals drive the wheel).</summary>
    public bool Engaged { get; private set; }

    /// <summary>Clicks heard so far (a pawl dropping off a tooth).</summary>
    public int Clicks { get; private set; }

    /// <summary>How far ahead the wheel has run of the driver, in turns: the pawls slide over this.</summary>
    public double Overrun => Wheel - Driver;

    /// <summary>
    /// How high the pawls are lifted, 0 (down in a gap) to 1 (on a tooth's tip): they climb each tooth's gentle slope
    /// and drop off its steep edge.
    /// </summary>
    public double Lift
    {
        get
        {
            // Against a tooth the overrun is a whole number of teeth, give or take rounding: that is down, not up.
            var phase = Overrun * Teeth;
            return Math.Clamp(phase - Math.Floor(phase + 1e-9), 0, 1);
        }
    }

    /// <summary>Moves on <paramref name="seconds"/> with the pedals turning the driver at <paramref name="pedalSpeed"/> turns per second.</summary>
    public void Step(double seconds, double pedalSpeed)
    {
        // The tooth the pawls last dropped past: they push on its steep edge once the driver catches up with it.
        var passed = Math.Floor((Overrun * Teeth) + 1e-9);
        var tooth = passed / Teeth;
        Driver += pedalSpeed * seconds;
        var coasting = Math.Max(0, WheelSpeed - (Slowing * seconds));
        var wheel = Wheel + (coasting * seconds);
        if (wheel - Driver <= tooth + 1e-12)
        {
            // Caught: the pawls hold the tooth and the wheel turns with the driver.
            Wheel = Driver + tooth;
            WheelSpeed = Math.Max(coasting, pedalSpeed);
            Engaged = true;
        }
        else
        {
            // The wheel runs ahead: the pawls slide up the teeth and drop off each one.
            Wheel = wheel;
            WheelSpeed = coasting;
            Engaged = false;
            Clicks += (int)Math.Max(0, Math.Floor((Overrun * Teeth) + 1e-9) - passed);
        }
    }
}

/// <summary>Which brakes the rider squeezes.</summary>
public enum BrakeUse
{
    Both,
    Front,
    Rear,
}

/// <summary>What happens when the rider brakes.</summary>
public enum BrakeOutcome
{
    /// <summary>The bike slows as hard as asked.</summary>
    Stops,

    /// <summary>The back wheel loses its grip and skids: the bike slows only as hard as that wheel can grip.</summary>
    RearSkids,

    /// <summary>Braking so hard with the front that the back wheel lifts: the rider would tip over the handlebars.</summary>
    Flips,
}

/// <summary>
/// Spread 5: braking. Slowing down tips the rider's weight forwards, onto the front wheel and off the back one, so the
/// front brake can do most of the work and the back wheel skids easily; too hard on the front and the bike tips over.
/// </summary>
public static class Braking
{
    /// <summary>The share of the weight on the front wheel while riding steadily.</summary>
    public const double FrontShare = 0.4;

    /// <summary>The height of the centre of mass over the wheelbase (front to back axle).</summary>
    public const double HeightOverWheelbase = 1.0;

    /// <summary>How well a tyre grips the road: the most it can slow, as a share of the weight on it.</summary>
    public const double Grip = 0.8;

    /// <summary>The share of the weight on the front wheel while slowing at <paramref name="g"/> (in g).</summary>
    public static double FrontLoad(double g) => Math.Min(1, FrontShare + (g * HeightOverWheelbase));

    /// <summary>The hardest slowing, in g, before the back wheel lifts.</summary>
    public static double TipOver => (1 - FrontShare) / HeightOverWheelbase;

    /// <summary>The hardest slowing, in g, the back brake alone can give before its wheel skids.</summary>
    public static double RearLimit => Grip * (1 - FrontShare) / (1 + (Grip * HeightOverWheelbase));

    /// <summary>What happens, and how hard the bike really slows (in g), when the rider asks for <paramref name="asked"/> g.</summary>
    public static (BrakeOutcome Outcome, double Slowing) Brake(BrakeUse use, double asked)
    {
        if (use == BrakeUse.Rear && asked > RearLimit)
        {
            return (BrakeOutcome.RearSkids, RearLimit);
        }

        if (use != BrakeUse.Rear && asked > TipOver)
        {
            return (BrakeOutcome.Flips, TipOver);
        }

        return (BrakeOutcome.Stops, asked);
    }

    /// <summary>Metres to stop from <paramref name="kmh"/> slowing at <paramref name="g"/>.</summary>
    public static double StoppingDistance(double kmh, double g)
    {
        var v = kmh / 3.6;
        return v * v / (2 * Math.Max(0.01, g) * Bike.Gravity);
    }
}

/// <summary>
/// Spread 6: steering. The fork turns about a tilted axis; where that axis meets the ground lies ahead of where the
/// tyre touches it, by the trail. Like a trolley's wheel trailing its pivot, the tyre is dragged straight again after a
/// bump; with the touch point ahead of the axis (negative trail) it swings away instead.
/// </summary>
public sealed class Steering
{
    /// <summary>How strongly a metre of trail pulls the wheel straight (per second squared).</summary>
    private const double Straightening = 660;

    /// <summary>How quickly a swing dies away (per second).</summary>
    private const double Damping = 3;

    /// <summary>The furthest the bars turn, in radians.</summary>
    public const double Lock = 0.7;

    /// <summary>
    /// Trail in metres: from where the tyre touches the ground back to where the steering axis does.
    /// <paramref name="headAngle"/> is the axis's angle up from the ground in degrees; <paramref name="offset"/> how far
    /// the fork bends the axle forward of the axis, in metres.
    /// </summary>
    public static double Trail(double headAngle, double offset)
    {
        var a = headAngle * Math.PI / 180;
        return ((Bike.WheelRadius * Math.Cos(a)) - offset) / Math.Sin(a);
    }

    /// <summary>The wheel's steering angle, radians (0 straight ahead).</summary>
    public double Angle { get; private set; }

    /// <summary>How fast it is turning, radians per second.</summary>
    public double Rate { get; private set; }

    /// <summary>A bump knocks the wheel sideways.</summary>
    public void Nudge(double rate = 1.2) => Rate += rate;

    public void Reset() => (Angle, Rate) = (0, 0);

    /// <summary>Moves on <paramref name="seconds"/> with this much <paramref name="trail"/> (metres), in small steps.</summary>
    public void Step(double seconds, double trail)
    {
        const double Dt = 0.004;
        for (var t = 0.0; t < seconds; t += Dt)
        {
            var dt = Math.Min(Dt, seconds - t);
            var accel = (-Straightening * trail * Angle) - (Damping * Rate);
            Rate += accel * dt;
            Angle += Rate * dt;
            if (Math.Abs(Angle) > Lock)
            {
                Angle = Math.Sign(Angle) * Lock;
                Rate = 0;
            }
        }
    }
}
