using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Locks;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Locks_</c>.</summary>
public abstract class LocksSpread(string key) : Spread("Locks", key)
{
    protected static string Number(double value, string format = "0") => value.ToString(format, CultureInfo.CurrentCulture);
}

/// <summary>1. Pins at the shear line: how a key lets the plug turn.</summary>
public sealed partial class PinsSpread() : LocksSpread("Pins")
{
    /// <summary>The lock's cuts: the depths its key pins are made for.</summary>
    public static readonly int[] LockCuts = [3, 7, 2, 6, 4];

    /// <summary>Another key, alike but for two cuts.</summary>
    public static readonly int[] WrongCuts = [3, 5, 2, 6, 6];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InsertText), nameof(ReadoutText), nameof(Turns))]
    private double _insert = 0.35;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Turns), nameof(Cuts))]
    private bool _wrong;

    /// <summary>The key in the lock.</summary>
    public int[] Cuts => Wrong ? WrongCuts : LockCuts;

    public bool Turns => PinTumbler.Turns(LockCuts, Cuts, Insert);

    public string InsertText => Text.Format("Locks_Percent", Number(Insert * 100));

    public string ReadoutText
    {
        get
        {
            if (Turns) return Text.GetString("Locks_Pins_Open");
            var at = PinTumbler.Gaps(LockCuts, Cuts, Insert).Count(g => Math.Abs(g) <= PinTumbler.Tolerance);
            return Text.Format("Locks_Pins_Readout", at, PinTumbler.Pins);
        }
    }

    public override IReadOnlyList<string> Parts => ["plug", "shell", "keypins", "drivers", "springs"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "shell", () => (Insert, Wrong) = (0, false)),
        Say(2, "drivers"),
        Say(3, "keypins", () => Insert = 0.5),
        TryIt(4, () => Turns, () => Insert = 1, highlight: "plug"),
        Quiz(5, answer: 0, highlight: "keypins"),
    ];
}

/// <summary>2. How many keys: depths, cuts, and the rule about neighbouring cuts.</summary>
public sealed partial class KeysSpread() : LocksSpread("Keys")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CutsText), nameof(ReadoutText))]
    private int _cuts = 5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DepthsText), nameof(ReadoutText), nameof(MostSteep))]
    private int _depths = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SteepestText), nameof(ReadoutText))]
    private int _steepest = 9;

    /// <summary>The steepest step the depths allow: one less than their number.</summary>
    public int MostSteep => Depths - 1;

    public string CutsText => Number(Cuts);

    public string DepthsText => Number(Depths);

    public string SteepestText => Text.Format("Locks_Steps", Number(Steepest));

    public string ReadoutText => Text.Format("Locks_Keys_Readout",
        Number(KeyCount.All(Cuts, Depths), "N0"), Number(KeyCount.Allowed(Cuts, Depths, Steepest), "N0"));

    partial void OnDepthsChanged(int value)
    {
        if (Steepest > value - 1) Steepest = value - 1;
    }

    public override IReadOnlyList<string> Parts => ["key", "depths", "steep", "count"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "key", () => (Cuts, Depths, Steepest) = (5, 10, 9)),
        Say(2, "depths"),
        TryIt(3, () => Cuts >= 6, () => Cuts = 6, highlight: "count"),
        Say(4, "steep", () => Steepest = 3),
        Quiz(5, answer: 0, highlight: "count"),
    ];
}

/// <summary>3. One lock, two keys: master wafers.</summary>
public sealed partial class MasterSpread() : LocksSpread("Master")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Chosen), nameof(KeyText), nameof(ReadoutText), nameof(Opens))]
    private int _keyChoice;

    public int[] Chosen => MasterKeyed.Keys[Math.Clamp(KeyChoice, 0, MasterKeyed.Keys.Count - 1)];

    public bool Opens => MasterKeyed.Blocking(Chosen) < 0;

    public string KeyText => Text.GetString($"Locks_Master_Key{Math.Clamp(KeyChoice, 0, MasterKeyed.Keys.Count - 1) + 1}");

    public string ReadoutText => (Opens
        ? Text.GetString("Locks_Master_Open")
        : Text.Format("Locks_Master_Blocked", MasterKeyed.Blocking(Chosen) + 1))
        + " " + Text.Format("Locks_Master_Count", MasterKeyed.KeysThatOpen());

    public override IReadOnlyList<string> Parts => ["key", "keypins", "wafers", "drivers"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "keypins", () => KeyChoice = 0),
        Say(2, "wafers"),
        TryIt(3, () => KeyChoice == 1, () => KeyChoice = 1, highlight: "wafers"),
        Say(4, "key", () => KeyChoice = 3),
        Quiz(5, answer: 2, highlight: "wafers"),
    ];
}

/// <summary>4. One pin at a time: why no lock is perfect.</summary>
public sealed partial class BindingSpread() : LocksSpread("Binding")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SetText), nameof(ReadoutText), nameof(Holding))]
    private int _set;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Offsets), nameof(Holding), nameof(Order))]
    private bool _perfect;

    /// <summary>The holes' errors: the real lock's, or none.</summary>
    public double[] Offsets => Perfect ? new double[PinTumbler.Pins] : Binding.Offsets;

    /// <summary>The order the pins are set in (left to right when perfect).</summary>
    public int[] Order => Binding.Order(Offsets);

    /// <summary>The pins holding the plug now.</summary>
    public int[] Holding => Binding.Binds(Offsets, Set);

    public string SetText => Number(Set);

    public string ReadoutText
    {
        get
        {
            if (Set >= PinTumbler.Pins) return Text.GetString("Locks_Binding_Open");
            if (Holding.Length > 1) return Text.GetString("Locks_Binding_All");
            return Text.Format("Locks_Binding_Readout", Holding[0] + 1);
        }
    }

    public override IReadOnlyList<string> Parts => ["turn", "binding", "holes", "set"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "holes", () => (Set, Perfect) = (0, false)),
        Say(2, "turn"),
        Say(3, "binding"),
        TryIt(4, () => Set >= 3, () => Set = 3, highlight: "set"),
        Quiz(5, answer: 1, highlight: "holes"),
    ];
}

/// <summary>5. The lever lock: gates, a stump, and a bolt.</summary>
public sealed partial class LeverSpread() : LocksSpread("Lever")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AngleText), nameof(ReadoutText), nameof(Bolt))]
    private double _angle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Bolt), nameof(Steps))]
    private bool _wrong;

    public double[] Steps => Wrong ? LeverLock.WrongKey : LeverLock.Gates;

    /// <summary>How far the bolt is across: 0 locked, 1 open.</summary>
    public double Bolt => LeverLock.Bolt(Steps, Angle);

    public string AngleText => Text.Format("Locks_Degrees", Number(Angle));

    public string ReadoutText => Bolt >= 1
        ? Text.GetString("Locks_Lever_Open")
        : Angle >= LeverLock.PushFrom && Bolt <= 0
            ? Text.GetString("Locks_Lever_Stuck")
            : Text.Format("Locks_Lever_Readout", Number(Bolt * 100));

    public override IReadOnlyList<string> Parts => ["bit", "levers", "stump", "bolt"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "bolt", () => (Angle, Wrong) = (0, false)),
        Say(2, "levers"),
        Say(3, "stump", () => Angle = 50),
        TryIt(4, () => Bolt >= 1, () => Angle = 150, highlight: "bit"),
        Quiz(5, answer: 2, highlight: "levers"),
    ];
}

/// <summary>6. Numbered wheels: a combination lock.</summary>
public sealed partial class DialsSpread() : LocksSpread("Dials")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FirstText), nameof(ReadoutText), nameof(Opens), nameof(Shown))]
    private int _first;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SecondText), nameof(ReadoutText), nameof(Opens), nameof(Shown))]
    private int _second;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThirdText), nameof(ReadoutText), nameof(Opens), nameof(Shown))]
    private int _third;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Opens), nameof(Slop))]
    private bool _sloppy;

    public int[] Shown => [First, Second, Third];

    public int Slop => Sloppy ? 1 : 0;

    public bool Opens => Dials.Blocking(Shown, Slop) < 0;

    public string FirstText => Number(First);

    public string SecondText => Number(Second);

    public string ThirdText => Number(Third);

    public string ReadoutText => (Opens
        ? Text.GetString("Locks_Dials_Open")
        : Text.Format("Locks_Dials_Blocked", Dials.Blocking(Shown, Slop) + 1))
        + " " + Text.Format("Locks_Dials_Tries", Number(Dials.Tries(Slop), "N0"));

    public override IReadOnlyList<string> Parts => ["wheels", "notches", "bar", "shackle"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "wheels", () => (First, Second, Third, Sloppy) = (0, 0, 0, false)),
        Say(2, "notches"),
        Say(3, "bar", () => (First, Second) = (7, 2)),
        TryIt(4, () => Opens, () => Third = 5, highlight: "shackle"),
        Quiz(5, answer: 1, highlight: "notches"),
    ];
}
