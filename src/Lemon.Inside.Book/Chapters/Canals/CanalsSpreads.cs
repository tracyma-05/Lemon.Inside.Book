using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Canals;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Canals_</c>.</summary>
public abstract class CanalsSpread(string key) : Spread("Canals", key)
{
    protected static string Number(double value, string format = "0") => value.ToString(format, CultureInfo.CurrentCulture);

    protected static string Metres(double value) => Text.Format("Canals_Metres", Number(value, "0.#"));

    protected static string Tonnes(double value) => Text.Format("Canals_Tonnes", Number(value, value < 10 ? "0.#" : "N0"));

    protected static string Cubic(double value) => Text.Format("Canals_Cubic", Number(value, "N0"));

    protected static string Minutes(double seconds) => Text.Format("Canals_Minutes", Number(seconds / 60, "0.#"));
}

/// <summary>1. The lock: a chamber filling until its water meets the upper canal's.</summary>
public sealed partial class LockSpread() : CanalsSpread("Lock")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PaddlesText), nameof(ReadoutText))]
    private int _paddles = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _filling;

    public string PaddlesText => Number(Paddles);

    public string ReadoutText => Text.Format("Canals_Lock_Readout", Minutes(Chamber.FillTime(Paddles)),
        Minutes(Chamber.MetreTime(Paddles, false)), Minutes(Chamber.MetreTime(Paddles, true)));

    public override IReadOnlyList<string> Parts => ["boat", "gates", "paddles", "chamber"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "boat", () => (Paddles, Filling) = (1, false)),
        Say(2, "gates"),
        TryIt(3, () => Filling, () => Filling = true, highlight: "paddles"),
        Say(4, "paddles", () => Paddles = 4),
        Quiz(5, answer: 1, highlight: "chamber"),
    ];
}

/// <summary>2. Mitre gates: pointed into the water's push, which only closes them tighter.</summary>
public sealed partial class GatesSpread() : CanalsSpread("Gates")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeadText), nameof(ReadoutText), nameof(Holds))]
    private double _head = 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Holds))]
    private bool _downstream;

    public bool Holds => Gates.Hold(Head, Downstream);

    public string HeadText => Metres(Head);

    public string ReadoutText => Head < 0.01
        ? Text.GetString("Canals_Gates_Level")
        : Text.Format(Holds ? "Canals_Gates_Readout" : "Canals_Gates_Burst", Tonnes(Gates.Push(Head)));

    public override IReadOnlyList<string> Parts => ["leaves", "mitre", "water", "beam"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "leaves", () => (Head, Downstream) = (3, false)),
        Say(2, "water"),
        TryIt(3, () => Downstream, () => Downstream = true, highlight: "mitre"),
        Say(4, "beam", () => (Downstream, Head) = (false, 0)),
        Quiz(5, answer: 2, highlight: "mitre"),
    ];
}

/// <summary>3. Water: every boat sends a chamberful downhill, less with side ponds.</summary>
public sealed partial class WaterSpread() : CanalsSpread("Water")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SidePondsText), nameof(ReadoutText))]
    private int _sidePonds;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BoatsText), nameof(ReadoutText))]
    private double _boats = 40;

    public string SidePondsText => Number(SidePonds);

    public string BoatsText => Number(Boats);

    public string ReadoutText => Text.Format("Canals_Water_Readout", Cubic(Ponds.PerBoat(SidePonds)), Number(Ponds.Share(SidePonds) * 100),
        Cubic(Ponds.PerBoat(SidePonds) * Boats));

    public override IReadOnlyList<string> Parts => ["chamber", "ponds", "summit", "bar"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "chamber", () => (SidePonds, Boats) = (0, 40)),
        Say(2, "summit"),
        TryIt(3, () => SidePonds >= 2, () => SidePonds = 2, highlight: "ponds"),
        Say(4, "bar", () => SidePonds = 3),
        Quiz(5, answer: 0, highlight: "ponds"),
    ];
}

/// <summary>4. A flight of locks: the same hill in smaller steps.</summary>
public sealed partial class FlightSpread() : CanalsSpread("Flight")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LocksText), nameof(ReadoutText))]
    private int _locks = 1;

    public string LocksText => Number(Locks);

    public string ReadoutText => Text.Format("Canals_Flight_Readout", Metres(Flight.Step(Locks)), Cubic(Flight.Water(Locks)),
        Number(Flight.Minutes(Locks)));

    public override IReadOnlyList<string> Parts => ["hill", "locks", "boat", "water"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "hill", () => Locks = 1),
        Say(2, "locks", () => Locks = 2),
        TryIt(3, () => Locks >= 6, () => Locks = 6, highlight: "locks"),
        Say(4, "water"),
        Quiz(5, answer: 2, highlight: "boat"),
    ];
}

/// <summary>5. The boat lift: two tanks that balance, boat or no boat.</summary>
public sealed partial class LiftSpread() : CanalsSpread("Lift")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Imbalance))]
    private bool _boat;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExtraText), nameof(ReadoutText), nameof(Imbalance))]
    private double _extra;

    public double Imbalance => BoatLift.Imbalance(Boat, Extra);

    public string ExtraText => Text.Format("Canals_Centimetres", Number(Extra));

    public string ReadoutText => Imbalance < 0.05
        ? Text.Format("Canals_Lift_Even", Tonnes(BoatLift.Water))
        : Text.Format("Canals_Lift_Uneven", Tonnes(Imbalance));

    public override IReadOnlyList<string> Parts => ["tanks", "boat", "wheel", "motor"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "tanks", () => (Boat, Extra) = (false, 0)),
        Say(2, "wheel"),
        TryIt(3, () => Boat, () => Boat = true, highlight: "boat"),
        Say(4, "motor", () => Extra = 10),
        Quiz(5, answer: 1, highlight: "tanks"),
    ];
}

/// <summary>6. Towing: a horse, a barge, and drag that grows with the square of the speed.</summary>
public sealed partial class TowingSpread() : CanalsSpread("Towing")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoadText), nameof(ReadoutText), nameof(Drag))]
    private double _load = 30;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedText), nameof(ReadoutText), nameof(Drag))]
    private double _speed = 4;

    public double Drag => Towing.Drag(Load, Speed);

    public string LoadText => Tonnes(Load);

    public string SpeedText => Text.Format("Canals_Speed", Number(Speed, "0.#"));

    public string ReadoutText => Text.Format(Towing.Manages(Load, Speed) ? "Canals_Towing_Readout" : "Canals_Towing_TooFast",
        Text.Format("Canals_Kgf", Number(Drag)), Text.Format("Canals_Kgf", Number(Towing.Horse)), Tonnes(Towing.OnRoad));

    public override IReadOnlyList<string> Parts => ["barge", "horse", "rope", "wake"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "barge", () => (Load, Speed) = (30, 4)),
        Say(2, "horse"),
        TryIt(3, () => Speed >= 8, () => Speed = 8, highlight: "rope"),
        Say(4, "wake", () => Speed = 4),
        Quiz(5, answer: 0, highlight: "barge"),
    ];
}
