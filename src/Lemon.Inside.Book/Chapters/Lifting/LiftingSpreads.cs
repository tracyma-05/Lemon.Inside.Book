using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Lifting;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Lifting_</c>.</summary>
public abstract class LiftingSpread(string key) : Spread("Lifting", key)
{
    protected static string Number(double value, string format = "0") => value.ToString(format, CultureInfo.CurrentCulture);

    /// <summary>A force said as the weight it would hold up: "50 kg".</summary>
    protected static string Kg(double value) => Text.Format("Lifting_Kg", Number(value, value < 10 ? "0.0" : "N0"));
}

/// <summary>1. The lever: a long arm for a short one.</summary>
public sealed partial class SeesawSpread() : LiftingSpread("Seesaw")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PivotText), nameof(ReadoutText), nameof(Effort))]
    private double _pivot = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PushText), nameof(ReadoutText))]
    private double _push;

    public double Effort => Seesaw.Effort(Seesaw.Stone, Pivot);

    public string PivotText => Text.Format("Lifting_Metres", Number(Pivot, "0.0"));

    public string PushText => Text.Format("Lifting_Percent", Number(Push * 100));

    public string ReadoutText => Text.Format("Lifting_Seesaw_Readout", Kg(Effort), Number(Seesaw.Travel(Pivot), "0.#"),
        Number(Seesaw.Rise(Pivot, Push) * 100));

    public override IReadOnlyList<string> Parts => ["stone", "pivot", "bar", "hand"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "stone", () => (Pivot, Push) = (2, 0)),
        Say(2, "pivot"),
        TryIt(3, () => Effort <= 50.001, () => Pivot = 0.8, highlight: "pivot"),
        Say(4, "hand", () => Push = 1),
        Quiz(5, answer: 1, highlight: "bar"),
    ];
}

/// <summary>2. One pulley: fixed, it turns the pull round; riding on the load, it halves it.</summary>
public sealed partial class PulleySpread() : LiftingSpread("Pulley")
{
    /// <summary>The most rope the hand pulls, m.</summary>
    public const double MostPull = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Effort))]
    private bool _moving;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PullText), nameof(ReadoutText))]
    private double _pull;

    public double Effort => Pulleys.Effort(Pulleys.Bucket, Moving);

    public string PullText => Text.Format("Lifting_Metres", Number(Pull, "0.0"));

    public string ReadoutText => Text.Format("Lifting_Pulley_Readout", Kg(Effort), Number(Pulleys.Rise(Pull, Moving) * 100),
        Number(Pull * 100));

    public override IReadOnlyList<string> Parts => ["wheel", "rope", "bucket", "hand"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "wheel", () => (Moving, Pull) = (false, 0)),
        Say(2, "hand", () => Pull = 1),
        TryIt(3, () => Moving, () => Moving = true, highlight: "wheel"),
        Say(4, "rope", () => Pull = MostPull),
        Quiz(5, answer: 0, highlight: "rope"),
    ];
}

/// <summary>3. Block and tackle: more strands, less pull, more rope.</summary>
public sealed partial class TackleSpread() : LiftingSpread("Tackle")
{
    /// <summary>The most rope the hand pulls, m.</summary>
    public const double MostPull = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StrandsText), nameof(ReadoutText), nameof(Effort))]
    private int _strands = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Effort))]
    private bool _friction;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PullText), nameof(ReadoutText))]
    private double _pull;

    public double Effort => Tackle.Effort(Tackle.Load, Strands, Friction);

    public string StrandsText => Number(Strands);

    public string PullText => Text.Format("Lifting_Metres", Number(Pull, "0.0"));

    public string ReadoutText => Text.Format("Lifting_Tackle_Readout", Kg(Effort), Number(Tackle.RopePerMetre(Strands)),
        Number(Pull / Strands * 100));

    public override IReadOnlyList<string> Parts => ["load", "strands", "blocks", "hand"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "load", () => (Strands, Friction, Pull) = (1, false, 0)),
        Say(2, "strands", () => Strands = 2),
        TryIt(3, () => Strands >= 4, () => Strands = 4, highlight: "strands"),
        Say(4, "blocks", () => Friction = true),
        Quiz(5, answer: 2, highlight: "hand"),
    ];
}

/// <summary>4. The winch: a crank, a drum, and a worm that will not run backwards.</summary>
public sealed partial class WinchSpread() : LiftingSpread("Winch")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HandleText), nameof(ReadoutText), nameof(Effort))]
    private double _handle = 0.3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Effort), nameof(Holds))]
    private bool _worm;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _letGo;

    public double Effort => Winch.Effort(Winch.Load, Handle, Worm);

    public bool Holds => Winch.Holds(Worm);

    public string HandleText => Text.Format("Lifting_Centimetres", Number(Handle * 100));

    public string ReadoutText => Text.Format("Lifting_Winch_Readout", Kg(Effort), Number(Winch.TurnsPerMetre(Worm)))
        + " " + Text.GetString(!LetGo ? "Lifting_Winch_Holding" : Holds ? "Lifting_Winch_Stays" : "Lifting_Winch_Falls");

    public override IReadOnlyList<string> Parts => ["handle", "drum", "rope", "load"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "drum", () => (Handle, Worm, LetGo) = (0.3, false, false)),
        Say(2, "handle"),
        TryIt(3, () => LetGo, () => LetGo = true, highlight: "load"),
        Say(4, "drum", () => (Worm, LetGo) = (true, true)),
        Quiz(5, answer: 0, highlight: "drum"),
    ];
}

/// <summary>5. The hydraulic jack: a narrow piston, a wide one, and oil between.</summary>
public sealed partial class JackSpread() : LiftingSpread("Jack")
{
    public const int MostStrokes = 40;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RamText), nameof(ReadoutText), nameof(Effort), nameof(Rise))]
    private double _ram = 4;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StrokesText), nameof(ReadoutText), nameof(Rise))]
    private int _strokes;

    public double Effort => Jack.Effort(Jack.Load, Ram);

    /// <summary>How far the car is up, mm.</summary>
    public double Rise => Jack.Rise(Ram, Strokes);

    public string RamText => Text.Format("Lifting_Times", Number(Ram, "0.#"));

    public string StrokesText => Number(Strokes);

    public string ReadoutText => Text.Format("Lifting_Jack_Readout", Kg(Effort), Number(Jack.RisePerStroke(Ram), "0.0#"),
        Number(Rise, "0.#"));

    public override IReadOnlyList<string> Parts => ["pump", "ram", "oil", "valves"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "oil", () => (Ram, Strokes) = (4, 0)),
        Say(2, "pump"),
        Say(3, "ram"),
        TryIt(4, () => Rise >= 10, () => (Ram, Strokes) = (4, 8), highlight: "ram"),
        Quiz(5, answer: 1, highlight: "valves"),
    ];
}

/// <summary>6. The lift: a car, a counterweight, and a wheel the ropes grip.</summary>
public sealed partial class ElevatorSpread() : LiftingSpread("Elevator")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RidersText), nameof(ReadoutText), nameof(Motor))]
    private int _riders;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Motor))]
    private bool _counterweight;

    public double Motor => Elevator.Motor(Riders, Counterweight);

    public string RidersText => Number(Riders);

    public string ReadoutText
    {
        get
        {
            var side = Elevator.Heavier(Riders, Counterweight) switch
            {
                > 0 => Text.GetString("Lifting_Elevator_CarHeavier"),
                < 0 => Text.GetString("Lifting_Elevator_WeightHeavier"),
                _ => Text.GetString("Lifting_Elevator_Even"),
            };
            return Counterweight
                ? Text.Format("Lifting_Elevator_Readout", Kg(Motor), Kg(Elevator.Motor(Riders, false))) + " " + side
                : Text.Format("Lifting_Elevator_Alone", Kg(Motor));
        }
    }

    public override IReadOnlyList<string> Parts => ["car", "weight", "sheave", "ropes"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "car", () => (Riders, Counterweight) = (0, false)),
        Say(2, "sheave"),
        TryIt(3, () => Counterweight, () => Counterweight = true, highlight: "weight"),
        Say(4, "ropes", () => Riders = 6),
        Quiz(5, answer: 2, highlight: "weight"),
    ];
}
