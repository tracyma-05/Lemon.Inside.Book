namespace Lemon.Inside.Book.Chapters.Car;

/// <summary>
/// Spread 1: the crank and connecting rod that turn the piston's up-and-down into turning. Angles are the crank's,
/// from the top (the piston highest) in radians; lengths are in units of the crank's radius.
/// </summary>
public static class SliderCrank
{
    /// <summary>How far the piston has come down from the top, with a rod <paramref name="rod"/> crank radii long.</summary>
    public static double Travel(double angle, double rod)
    {
        var (sin, cos) = Math.SinCos(angle);
        return 1 + rod - (cos + Math.Sqrt((rod * rod) - (sin * sin)));
    }

    /// <summary>The travel a plain wave would give (an endless rod): 1 − cos.</summary>
    public static double Wave(double angle) => 1 - Math.Cos(angle);

    /// <summary>The piston's speed, crank radii per radian of turn.</summary>
    public static double Speed(double angle, double rod) => (Travel(angle + 1e-5, rod) - Travel(angle - 1e-5, rod)) / 2e-5;

    /// <summary>The crank angle, degrees from the top, where the piston moves fastest on its way down.</summary>
    public static double FastestAt(double rod)
    {
        var best = 0.0;
        var fastest = 0.0;
        for (var d = 0.0; d <= 180; d += 0.25)
        {
            var v = Speed(d * Math.PI / 180, rod);
            if (v > fastest) (best, fastest) = (d, v);
        }

        return best;
    }
}

/// <summary>
/// Spread 2: a cam lifts a valve. The camshaft turns once for every two turns of the crank; the valve's lift follows
/// the bump on the cam, a smooth rise and fall.
/// </summary>
public static class Cam
{
    /// <summary>The cam's round part, millimetres.</summary>
    public const double BaseRadius = 16;

    /// <summary>The valve's lift, mm, at <paramref name="crankDegrees"/> of crank turn (0–720) for a cam with this <paramref name="lift"/> and <paramref name="duration"/> (crank degrees open), centred on <paramref name="centre"/>.</summary>
    public static double Lift(double crankDegrees, double lift, double duration, double centre = 360)
    {
        var from = Math.IEEERemainder(crankDegrees - centre, 720);
        if (Math.Abs(from) >= duration / 2) return 0;
        return lift * (1 + Math.Cos(2 * Math.PI * from / duration)) / 2;
    }

    /// <summary>The cam's radius, mm, at <paramref name="camDegrees"/> round from its nose.</summary>
    public static double Radius(double camDegrees, double lift, double duration) =>
        BaseRadius + Lift(360 + (2 * camDegrees), lift, duration);

    /// <summary>Milliseconds the valve is open at <paramref name="rpm"/> crank turns a minute.</summary>
    public static double OpenMilliseconds(double duration, double rpm) => duration / 360 / (rpm / 60) * 1000;
}

/// <summary>
/// Spread 3: several cylinders on one crank, each pushing in turn, and a flywheel to smooth what is left. Each cylinder
/// pushes hard for half a turn out of every two (its power stroke), and the others take a little to squeeze their gas.
/// </summary>
public static class Cylinders
{
    /// <summary>The cylinder counts the slider steps through.</summary>
    public static IReadOnlyList<int> Choices { get; } = [1, 2, 3, 4, 6, 8];

    /// <summary>One cylinder's turning force (as a share of its peak) at <paramref name="degrees"/> of its own 720° cycle, 0 being the top before its power stroke.</summary>
    public static double One(double degrees)
    {
        var d = ((degrees % 720) + 720) % 720;
        if (d < 180)
        {
            // Burning gas pushes the piston down: strong early, fading as it spreads.
            var t = d / 180;
            return Math.Sin(Math.PI * t) * Math.Exp(-1.8 * t) * 2.2;
        }

        if (d >= 540)
        {
            // Squeezing the next charge takes a little back, most of all near the top.
            var t = (d - 540) / 180;
            return -0.28 * Math.Sin(Math.PI * t) * t;
        }

        return -0.03; // pushing out spent gas and drawing in fresh
    }

    /// <summary>The crank's turning force at <paramref name="degrees"/> with <paramref name="count"/> cylinders firing evenly.</summary>
    public static double Total(double degrees, int count)
    {
        var sum = 0.0;
        for (var k = 0; k < count; k++)
        {
            sum += One(degrees - (k * 720.0 / count));
        }

        return sum;
    }

    /// <summary>The average over a whole cycle.</summary>
    public static double Mean(int count)
    {
        var sum = 0.0;
        const int Steps = 1440;
        for (var i = 0; i < Steps; i++) sum += Total(i * 720.0 / Steps, count);
        return sum / Steps;
    }

    /// <summary>
    /// How much the crank's speed wobbles over a cycle, as ± a share of its speed, with a flywheel of
    /// <paramref name="flywheel"/> (1 = an ordinary one). The surplus and shortfall of turning force speed it up and slow
    /// it down; a heavier flywheel feels them less.
    /// </summary>
    public static double Wobble(int count, double flywheel)
    {
        var mean = Mean(count);
        double energy = 0, low = 0, high = 0;
        const int Steps = 1440;
        for (var i = 0; i < Steps; i++)
        {
            energy += (Total(i * 720.0 / Steps, count) - mean) * (4 * Math.PI / Steps);
            low = Math.Min(low, energy);
            high = Math.Max(high, energy);
        }

        // Energy swing over the flywheel's energy (½Iω², with I·ω² scaled so an ordinary flywheel on one cylinder wobbles ~6 %).
        return (high - low) / (2 * Stiffness * flywheel);
    }

    /// <summary>
    /// The crank's speed through a cycle, as a share above or below its average, at <paramref name="steps"/> even
    /// points of the 720° cycle (the surplus energy so far, less its middle, over the flywheel's).
    /// </summary>
    public static double[] SpeedThroughCycle(int count, double flywheel, int steps = 360)
    {
        var mean = Mean(count);
        var energy = new double[steps];
        var e = 0.0;
        for (var i = 0; i < steps; i++)
        {
            energy[i] = e;
            e += (Total(i * 720.0 / steps, count) - mean) * (4 * Math.PI / steps);
        }

        var middle = (energy.Min() + energy.Max()) / 2;
        return energy.Select(x => (x - middle) / (Stiffness * flywheel)).ToArray();
    }

    /// <summary>An ordinary flywheel's I·ω², in the units of <see cref="One"/> × radians.</summary>
    private const double Stiffness = 25;
}

/// <summary>
/// Spread 4: the clutch. Springs squeeze a friction plate between the engine's flywheel and a pressure plate; the
/// pedal pulls the springs back. Squeezed hard enough, the two sides grip and turn together; less, and they slip,
/// rubbing their difference in speed away as heat.
/// </summary>
public sealed class Clutch
{
    /// <summary>The engine's idle speed it tries to hold, turns per second.</summary>
    public const double EngineTarget = 25;

    /// <summary>The most turning force the clutch can pass with the pedal up, newton-metres.</summary>
    public const double Capacity = 300;

    private const double EngineMost = 160;
    private const double EngineInertia = 0.25;
    private const double CarInertia = 4;
    private const double Drag = 1.2;

    /// <summary>The engine's speed and the gearbox side's speed, turns per second.</summary>
    public double Engine { get; private set; } = EngineTarget;

    public double Gearbox { get; private set; }

    /// <summary>Whether the plates turn together.</summary>
    public bool Gripping { get; private set; }

    /// <summary>Heat the plates make now, kilowatts.</summary>
    public double Heat { get; private set; }

    /// <summary>How hard the clutch can grip with the pedal <paramref name="pedal"/> down (0 up, 1 to the floor).</summary>
    public static double Grip(double pedal) => Capacity * Math.Clamp(1 - (pedal * 1.25), 0, 1);

    public void Step(double seconds, double pedal)
    {
        const double Dt = 0.002;
        var grip = Grip(pedal);
        for (var t = 0.0; t < seconds; t += Dt)
        {
            var dt = Math.Min(Dt, seconds - t);
            var push = Math.Clamp(60 * (EngineTarget - Engine), 0, EngineMost);
            var load = Drag * Gearbox * 2 * Math.PI;
            if (Gripping)
            {
                // Together: the clutch passes whatever turns both at one speed, if it can.
                var common = (push - load) / (EngineInertia + CarInertia);
                var passed = push - (EngineInertia * common);
                if (Math.Abs(passed) <= grip)
                {
                    var w = (Engine * 2 * Math.PI) + (common * dt);
                    Engine = Gearbox = w / (2 * Math.PI);
                    Heat = 0;
                    continue;
                }

                Gripping = false;
            }

            var slip = Engine - Gearbox;
            var torque = Math.Sign(slip) * grip;
            Engine += (push - torque) / EngineInertia * dt / (2 * Math.PI);
            Gearbox += (torque - load) / CarInertia * dt / (2 * Math.PI);
            Gearbox = Math.Max(0, Gearbox);
            Heat = Math.Abs(torque * slip * 2 * Math.PI) / 1000;
            if (Math.Sign(Engine - Gearbox) != Math.Sign(slip) || Math.Abs(Engine - Gearbox) < 0.02)
            {
                Gripping = grip > 0;
                if (Gripping) Engine = Gearbox = (Engine + Gearbox) / 2;
            }
        }
    }
}

/// <summary>
/// Spreads 5 and 6: the differential. Its case turns with the big ring gear; inside, small spider gears carried round
/// with it mesh with a side gear for each wheel. Whatever the wheels do, the case turns at their average: on a bend the
/// outer wheel can turn faster and the inner slower, and the spiders spin to let them. It always shares the turning
/// force equally between the wheels.
/// </summary>
public static class Differential
{
    /// <summary>From one rear tyre to the other, metres.</summary>
    public const double Track = 1.6;

    /// <summary>The case's turns for wheels turning <paramref name="left"/> and <paramref name="right"/>: their average.</summary>
    public static double Case(double left, double right) => (left + right) / 2;

    /// <summary>The spider gears' turns relative to the case, side gears and spiders being the same size.</summary>
    public static double Spider(double left, double right) => (right - left) / 2;

    /// <summary>The wheels' speeds, as shares of the car's, round a bend of <paramref name="radius"/> metres to the left (inner, outer).</summary>
    public static (double Inner, double Outer) OnBend(double radius) =>
        ((radius - (Track / 2)) / radius, (radius + (Track / 2)) / radius);

    /// <summary>
    /// The push forward, as a share of the engine's best, when each rear tyre can grip at most
    /// <paramref name="leftGrip"/> and <paramref name="rightGrip"/> (shares of the engine's turning force each could take).
    /// An open differential gives each wheel the same turning force, so the slippier wheel limits both; a locked one
    /// turns both together and each wheel pushes what it can.
    /// </summary>
    public static double Push(double leftGrip, double rightGrip, bool locked) => locked
        ? Math.Min(1, leftGrip + rightGrip)
        : Math.Min(1, 2 * Math.Min(leftGrip, rightGrip));

    /// <summary>Whether the slippier wheel spins (the engine's force is more than it can take).</summary>
    public static bool Spins(double leftGrip, double rightGrip, bool locked) => locked
        ? leftGrip + rightGrip < 1
        : Math.Min(leftGrip, rightGrip) < 0.5;
}
