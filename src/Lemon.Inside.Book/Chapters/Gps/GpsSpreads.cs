using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Gps;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Gps_</c>.</summary>
public abstract class GpsSpread(string key) : Spread("Gps", key)
{
    protected static string Number(double value, string format = "0") => value.ToString(format, CultureInfo.CurrentCulture);

    protected static string Km(double km) => Text.Format("Gps_Km", Number(km, km < 100 ? "0.#" : "N0"));
}

/// <summary>1. A delay is a distance: the signal's travel time says how far away the satellite is.</summary>
public sealed partial class EchoSpread() : GpsSpread("Echo")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DelayText), nameof(ReadoutText))]
    private double _delay = 75;

    public string DelayText => Text.Format("Gps_Ms", Number(Delay, "0.0"));

    /// <summary>The delay from a satellite straight overhead, ms.</summary>
    public static double Overhead => Radio.Milliseconds(Orbit.Gps);

    public string ReadoutText
    {
        get
        {
            var km = Radio.Km(Delay);
            var said = Text.Format("Gps_Echo_Readout", Number(Delay, "0.0"), Km(km));
            if (Math.Abs(Delay - Overhead) < 0.3) return $"{said} {Text.GetString("Gps_Echo_Overhead")}";
            return Orbit.GroundAngle(Orbit.Gps, km) is null ? $"{said} {Text.GetString("Gps_Echo_Nowhere")}" : said;
        }
    }

    public override IReadOnlyList<string> Parts => ["satellite", "pulse", "circle", "places"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "satellite", () => Delay = 75),
        Say(2, "pulse"),
        TryIt(3, () => Math.Abs(Delay - Overhead) < 0.3, () => Delay = Math.Round(Overhead, 1), highlight: "circle"),
        Say(4, "places", () => Delay = 75),
        Quiz(5, answer: 2, highlight: "pulse"),
    ];
}

/// <summary>2. Circles crossing: each distance puts you on a circle, and where they cross is where you are.</summary>
public sealed partial class FixSpread() : GpsSpread("Fix")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CountText), nameof(ReadoutText), nameof(Used))]
    private double _count = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EastText))]
    private double _east = 30;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NorthText))]
    private double _north = 10;

    /// <summary>How many satellites' distances are used, 1 to 3.</summary>
    public int Used => (int)Math.Round(Math.Clamp(Count, 1, 3));

    public string CountText => Number(Used);

    public string EastText => Km(East);

    public string NorthText => Km(North);

    public string ReadoutText => Text.GetString(Used switch { 1 => "Gps_Fix_One", 2 => "Gps_Fix_Two", _ => "Gps_Fix_Three" });

    public override IReadOnlyList<string> Parts => ["satellites", "circle", "you", "crossing"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "circle", () => (Count, East, North) = (1, 30, 10)),
        Say(2, "crossing", () => Count = 2),
        TryIt(3, () => Used == 3, () => Count = 3, highlight: "satellites"),
        Say(4, "you"),
        Quiz(5, answer: 1, highlight: "crossing"),
    ];
}

/// <summary>3. The clock problem: a receiver's clock that is a little off makes every distance wrong by the same amount.</summary>
public sealed partial class ClockSpread() : GpsSpread("Clock")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OffsetText), nameof(ReadoutText), nameof(Miss))]
    private double _offset = 120;

    public double Miss => Map.Miss(Offset);

    public string OffsetText => Text.Format("Gps_Us", Number(Offset));

    public string ReadoutText
    {
        get
        {
            if (Math.Abs(Offset) < 2) return Text.GetString("Gps_Clock_Right");

            var km = Math.Abs(Offset) * Radio.MetresPerMicrosecond / 1000;
            return Text.Format(Offset > 0 ? "Gps_Clock_Long" : "Gps_Clock_Short", Number(Math.Abs(Offset)), Km(km), Km(Miss));
        }
    }

    public override IReadOnlyList<string> Parts => ["circles", "gap", "clock", "you"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "circles", () => Offset = 120),
        Say(2, "gap"),
        TryIt(3, () => Math.Abs(Offset) < 10, () => Offset = 0, highlight: "clock"),
        Say(4, "you"),
        Quiz(5, answer: 1, highlight: "clock"),
    ];
}

/// <summary>4. Clocks in orbit run fast: weaker gravity speeds them up more than their speed slows them down.</summary>
public sealed partial class DriftSpread() : GpsSpread("Drift")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DaysText), nameof(ReadoutText))]
    private double _days = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _corrected;

    public string DaysText => Text.Format(Math.Abs(Days - 1) < 0.05 ? "Gps_Day" : "Gps_Days", Number(Days, "0.#"));

    public string ReadoutText => Corrected
        ? Text.GetString("Gps_Drift_Corrected")
        : Text.Format("Gps_Drift_Readout", DaysText, Number(Clocks.Ahead(Days, false)), Km(Clocks.DriftKm(Days, false)));

    public override IReadOnlyList<string> Parts => ["ground", "orbit", "gravity", "motion"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "ground", () => (Days, Corrected) = (1, false)),
        Say(2, "gravity"),
        TryIt(3, () => Corrected, () => Corrected = true, highlight: "orbit"),
        Say(4, "motion"),
        Quiz(5, answer: 1, highlight: "gravity"),
    ];
}

/// <summary>5. Orbits: the higher a satellite, the slower it goes round and the more of the Earth it sees.</summary>
public sealed partial class OrbitSpread() : GpsSpread("Orbit")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AltitudeText), nameof(ReadoutText))]
    private double _altitude = 20_200;

    public string AltitudeText => Km(Altitude);

    public static string Period(double height)
    {
        var minutes = (int)Math.Round(Orbit.Period(height) / 60);
        return Text.Format("Gps_Period", minutes / 60, minutes % 60);
    }

    public string ReadoutText => Text.Format("Gps_Orbit_Readout", Km(Altitude), Period(Altitude), Number(Orbit.Speed(Altitude), "0.0"),
        Text.Format("Gps_Percent", Number(100 * Orbit.Seen(Altitude))));

    public override IReadOnlyList<string> Parts => ["earth", "orbit", "satellite", "seen"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "earth", () => Altitude = 400),
        Say(2, "seen"),
        TryIt(3, () => Math.Abs(Orbit.Hours(Altitude) - 12) < 0.2, () => Altitude = 20_200, highlight: "orbit"),
        Say(4, "satellite"),
        Quiz(5, answer: 1, highlight: "seen"),
    ];
}

/// <summary>6. Where the satellites are in the sky matters: bands crossing at a shallow angle leave a long patch.</summary>
public sealed partial class AngleSpread() : GpsSpread("Angle")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AngleText), nameof(ReadoutText))]
    private double _angle = 25;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErrorText), nameof(ReadoutText))]
    private double _error = 5;

    public string AngleText => Text.Format("Gps_Degrees", Number(Angle));

    public string ErrorText => Text.Format("Gps_Metres", Number(Error, "0.#"));

    public string ReadoutText => Text.Format("Gps_Angle_Readout", Number(Error, "0.#"), Number(Angle), Number(Patch.Longest(Error, Angle)));

    public override IReadOnlyList<string> Parts => ["band", "patch", "satellite", "angle"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "band", () => (Angle, Error) = (25, 5)),
        Say(2, "patch"),
        TryIt(3, () => Patch.Longest(Error, Angle) <= 1.25 * Patch.Best(Error), () => Angle = 90, highlight: "satellite"),
        Say(4, "angle"),
        Quiz(5, answer: 1, highlight: "patch"),
    ];
}
