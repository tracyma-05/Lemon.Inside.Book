namespace Lemon.Inside.Book.Chapters.Watch;

/// <summary>
/// The going train of the book's watch: the barrel drives the centre wheel (one turn an hour: the minute hand), then
/// the third, fourth (one turn a minute: the seconds hand) and escape wheels. Each wheel drives the next one's pinion.
/// The balance beats <see cref="BeatsPerHour"/> times an hour, two beats per escape-wheel tooth.
/// </summary>
public static class GoingTrain
{
    /// <summary>The wheels from the barrel to the escape wheel: each one's own teeth and the teeth of the pinion it drives.</summary>
    public static IReadOnlyList<(string Key, int Wheel, int NextPinion)> Wheels { get; } =
    [
        ("Barrel", 80, 10),
        ("Centre", 64, 8),
        ("Third", 60, 8),
        ("Fourth", 80, 8),
        ("Escape", 15, 0),
    ];

    /// <summary>Teeth on the escape wheel.</summary>
    public const int EscapeTeeth = 15;

    /// <summary>The balance's beats an hour (two per swing to and fro): 18,000 is a common rate.</summary>
    public const double BeatsPerHour = 18000;

    /// <summary>Swings to and fro a second (2.5 Hz).</summary>
    public const double Frequency = BeatsPerHour / 3600 / 2;

    /// <summary>Turns an hour of each wheel in <see cref="Wheels"/>, from the centre wheel's one turn an hour.</summary>
    public static double[] TurnsPerHour()
    {
        var turns = new double[Wheels.Count];
        turns[1] = 1;
        turns[0] = (double)Wheels[0].NextPinion / Wheels[0].Wheel;
        for (var i = 2; i < Wheels.Count; i++)
        {
            turns[i] = turns[i - 1] * Wheels[i - 1].Wheel / Wheels[i - 1].NextPinion;
        }

        return turns;
    }

    /// <summary>Hours a fully wound mainspring runs for.</summary>
    public const double ReserveHours = 40;

    /// <summary>Turns of the barrel arbor that wind the spring fully: the barrel turns this many times in the reserve.</summary>
    public static double SpringTurns => ReserveHours * TurnsPerHour()[0];

    /// <summary>The hour, minute and seconds hands' angles, clockwise from 12, in degrees.</summary>
    public static (double Hour, double Minute, double Second) Hands(double seconds)
    {
        var hours = seconds / 3600;
        return (hours % 12 * 30, hours % 1 * 360, seconds % 60 * 6);
    }
}

/// <summary>The balance wheel and its hairspring: a mass on a spring, swinging at a rate set by the two.</summary>
public static class Balance
{
    /// <summary>
    /// Swings a second for a spring <paramref name="stiffness"/> times as stiff and a wheel <paramref name="inertia"/>
    /// times as heavy as the book's watch: f = √(k / I) / 2π, so it does not depend on how wide the swing is.
    /// </summary>
    public static double Frequency(double stiffness = 1, double inertia = 1) => GoingTrain.Frequency * Math.Sqrt(stiffness / inertia);

    /// <summary>The balance's angle in degrees, <paramref name="seconds"/> into a swing of <paramref name="amplitude"/> degrees.</summary>
    public static double Angle(double seconds, double amplitude, double frequency) => amplitude * Math.Sin(2 * Math.PI * frequency * seconds);

    /// <summary>Seconds a day a watch gains (positive) or loses whose balance swings at <paramref name="frequency"/>.</summary>
    public static double DailyError(double frequency) => ((frequency / GoingTrain.Frequency) - 1) * 86400;
}

/// <summary>
/// The lever escapement, as a function of time. The balance swings freely, except near the middle of each swing, when
/// its pin flicks the lever across: the lever unlocks the escape wheel, which turns half a tooth and gives the lever
/// (and through it the balance) a push, then is locked again by the lever's other stone. That moment is the tick.
/// </summary>
public static class Escapement
{
    /// <summary>The balance's angle either side of the middle while its pin is in the lever's fork, in degrees.</summary>
    public const double LiftAngle = 30;

    /// <summary>The balance's swing either side, in degrees.</summary>
    public const double Amplitude = 270;

    /// <summary>
    /// Where everything is after <paramref name="seconds"/>: the balance's angle (degrees), the lever's side (+1 one
    /// way, -1 the other, between while it moves) and how many half teeth the escape wheel has turned.
    /// </summary>
    public static (double Balance, double Lever, double HalfTeeth, bool Ticking) At(double seconds, double frequency = GoingTrain.Frequency)
    {
        var balance = Balance.Angle(seconds, Amplitude, frequency);

        // The balance passes the middle every half swing; around each pass, the beat.
        var beat = Math.Round(2 * frequency * seconds);
        var halfWindow = Math.Asin(LiftAngle / Amplitude) / (2 * Math.PI * frequency);
        var from = (seconds - (beat / (2 * frequency))) / halfWindow;
        var ticking = from is > -1 and < 1;
        var done = from <= -1 ? 0 : from >= 1 ? 1 : SmoothStep((from + 1) / 2);
        var side = beat % 2 == 0 ? 1 : -1;
        return (balance, side * (1 - (2 * done)), beat + done, ticking);
    }

    private static double SmoothStep(double t) => t * t * (3 - (2 * t));
}

/// <summary>
/// An automatic watch's winding: a half-moon weight (the rotor) swings as the wrist moves and winds the mainspring
/// through a reverser, which turns both ways of swinging into winding (without it, only one way winds). A slipping
/// spring at the barrel stops it winding past full.
/// </summary>
public sealed class AutoWinding
{
    /// <summary>Spring wound (0 to 1) per radian the rotor swings, in the book's sped-up picture.</summary>
    public const double WindPerRadian = 0.004;

    /// <summary>Spring used a second while the watch runs, in the sped-up picture.</summary>
    public const double UsePerSecond = 0.006;

    /// <summary>How full the mainspring is, 0 to 1.</summary>
    public double Wound { get; private set; } = 0.35;

    /// <summary>Radians the winding wheel has turned (only ever forwards).</summary>
    public double WheelTurned { get; private set; }

    public bool Reverser { get; set; } = true;

    /// <summary>The rotor swung by <paramref name="radians"/> (either way) over <paramref name="seconds"/>.</summary>
    public void Step(double radians, double seconds)
    {
        var winds = Reverser ? Math.Abs(radians) : Math.Max(0, radians);
        WheelTurned += winds;
        Wound = Math.Clamp(Wound + (winds * WindPerRadian) - (seconds * UsePerSecond), 0, 1);
    }

    /// <summary>The rotor's angle (radians from hanging straight down) for a wrist this lively (0 to 1).</summary>
    public static double RotorAngle(double seconds, double activity) =>
        activity * ((1.3 * Math.Sin(1.7 * seconds)) + (0.8 * Math.Sin((3.1 * seconds) + 1)) + (0.5 * Math.Sin((0.6 * seconds) + 2)));
}
