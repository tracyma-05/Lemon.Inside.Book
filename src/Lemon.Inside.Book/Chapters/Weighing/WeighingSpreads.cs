using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Weighing;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Weighing_</c>.</summary>
public abstract class WeighingSpread(string key) : Spread("Weighing", key)
{
    protected static string Number(double value, string format = "0") => value.ToString(format, CultureInfo.CurrentCulture);

    protected static string Kg(double value) => Text.Format("Weighing_Kg", Number(value, "0.##"));

    protected static string Grams(double value) => Text.Format("Weighing_Grams", Number(value, "0"));

    protected static string Centimetres(double value) => Text.Format("Weighing_Centimetres", Number(value, "0.#"));
}

/// <summary>1. The balance: how far it tips, and why its centre of mass hangs below the knife edge.</summary>
public sealed partial class BalanceSpread() : WeighingSpread("Balance")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DifferenceText), nameof(ReadoutText), nameof(Tilt))]
    private double _difference;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DropText), nameof(ReadoutText), nameof(Tilt))]
    private double _drop = 2;

    public double Tilt => Balance.Tilt(Difference, Drop);

    public string DifferenceText => Grams(Difference);

    public string DropText => Centimetres(Drop);

    public string ReadoutText => Balance.Topples(Drop)
        ? Text.GetString("Weighing_Balance_Topples")
        : Text.Format("Weighing_Balance_Readout", Number(Tilt, "0.#"));

    public override IReadOnlyList<string> Parts => ["pans", "pointer", "edge", "beam"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "pans", () => (Difference, Drop) = (0, 2)),
        Say(2, "pointer", () => Difference = 10),
        TryIt(3, () => Tilt >= 35, () => Drop = 1, highlight: "edge"),
        Say(4, "beam", () => Drop = -0.5),
        Quiz(5, answer: 2, highlight: "edge"),
    ];
}

/// <summary>2. The steelyard: one small weight slid along a long arm.</summary>
public sealed partial class SteelyardSpread() : WeighingSpread("Steelyard")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SackText), nameof(ReadoutText), nameof(Balanced))]
    private double _sack = 8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PositionText), nameof(ReadoutText), nameof(Balanced))]
    private double _position = 20;

    public bool Balanced => Steelyard.Balanced(Sack, Position);

    public string SackText => Kg(Sack);

    public string PositionText => Centimetres(Position);

    public string ReadoutText => Text.Format("Weighing_Steelyard_Readout", Kg(Steelyard.Reading(Position)))
        + " " + Text.GetString(Balanced ? "Weighing_Steelyard_Level"
            : Steelyard.Tilt(Sack, Position) > 0 ? "Weighing_Steelyard_SackDown" : "Weighing_Steelyard_PoiseDown");

    public override IReadOnlyList<string> Parts => ["hook", "poise", "arm", "pivot"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "hook", () => (Sack, Position) = (8, 20)),
        Say(2, "poise"),
        TryIt(3, () => Balanced, () => Position = Steelyard.BalanceAt(Sack), highlight: "poise"),
        Say(4, "arm", () => (Sack, Position) = (15, Steelyard.BalanceAt(15))),
        Quiz(5, answer: 0, highlight: "pivot"),
    ];
}

/// <summary>3. The spring balance: stretch in step with the load, until it is stretched for good.</summary>
public sealed partial class SpringSpread() : WeighingSpread("Spring")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MassText), nameof(ReadoutText), nameof(Load))]
    private double _mass = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Load))]
    private bool _off;

    /// <summary>The heaviest load the spring has carried, kg; what it keeps of an overload.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private double _highest = 2;

    /// <summary>What hangs on the spring now, kg.</summary>
    public double Load => Off ? 0 : Mass;

    public string MassText => Kg(Mass);

    public string ReadoutText
    {
        get
        {
            var text = Text.Format("Weighing_Spring_Readout", Kg(Spring.Reading(Load, Highest)), Centimetres(Spring.Stretch(Load, Highest)));
            var set = Spring.Set(Highest);
            return set > 0 ? text + " " + Text.Format("Weighing_Spring_Set", Centimetres(set)) : text;
        }
    }

    public override IReadOnlyList<string> Parts => ["spring", "pointer", "hook", "scale"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "spring", () => (Mass, Off, Highest) = (2, false, 2)),
        Say(2, "pointer", () => Mass = 4),
        TryIt(3, () => Mass >= 7, () => Mass = 7, highlight: "spring"),
        Say(4, "pointer", () => Off = true),
        Quiz(5, answer: 1, highlight: "scale"),
    ];

    partial void OnMassChanged(double value) => Remember();

    partial void OnOffChanged(bool value) => Remember();

    private void Remember()
    {
        if (Load > Highest) Highest = Load;
    }
}

/// <summary>4. The kitchen scale: two parallel links keep the pan level and the reading honest.</summary>
public sealed partial class RobervalSpread() : WeighingSpread("Roberval")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OffsetText), nameof(ReadoutText), nameof(Reading))]
    private double _offset;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Reading))]
    private bool _parallel;

    public double Reading => Roberval.Reading(Offset, Parallel);

    public string OffsetText => Centimetres(Offset);

    public string ReadoutText => Text.Format("Weighing_Roberval_Readout", Grams(Reading), Grams(Roberval.Load));

    public override IReadOnlyList<string> Parts => ["pan", "load", "links", "post"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "pan", () => (Offset, Parallel) = (0, false)),
        Say(2, "load", () => Offset = 5),
        TryIt(3, () => Parallel, () => Parallel = true, highlight: "links"),
        Say(4, "links", () => Offset = -5),
        Quiz(5, answer: 2, highlight: "pan"),
    ];
}

/// <summary>5. The load cell: a bending bar, gauges that stretch with it, and a bridge that ignores warmth.</summary>
public sealed partial class StrainSpread() : WeighingSpread("Strain")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MassText), nameof(ReadoutText), nameof(Reading))]
    private double _mass = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Reading))]
    private bool _warm;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Reading))]
    private bool _bridge;

    public double Reading => Gauge.Reading(Mass, Warm, Bridge);

    public string MassText => Kg(Mass);

    public string ReadoutText => Text.Format("Weighing_Strain_Readout", Number(Gauge.Strain(Mass) * 1e6),
        Number(Gauge.Change(Mass), "0.###"), Kg(Reading));

    public override IReadOnlyList<string> Parts => ["bar", "gauge", "bridge", "display"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "bar", () => (Mass, Warm, Bridge) = (2, false, false)),
        Say(2, "gauge"),
        TryIt(3, () => Warm, () => Warm = true, highlight: "display"),
        Say(4, "bridge", () => Bridge = true),
        Quiz(5, answer: 0, highlight: "gauge"),
    ];
}

/// <summary>6. Weight and mass: what a spring and a balance read on the Moon.</summary>
public sealed partial class MoonSpread() : WeighingSpread("Moon")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MassText), nameof(ReadoutText))]
    private double _mass = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlaceText), nameof(ReadoutText))]
    private int _place;

    public string MassText => Kg(Mass);

    public string PlaceText => Text.GetString(PlaceKey(Place));

    public string ReadoutText => Text.Format("Weighing_Moon_Readout", Text.GetString(PlaceKey(Place)),
        Kg(Worlds.SpringReads(Mass, Place)), Kg(Worlds.BalanceReads(Mass)));

    public override IReadOnlyList<string> Parts => ["bag", "spring", "balance", "ground"];

    public static string PlaceKey(int place) => place switch { 1 => "Weighing_Moon_TheMoon", 2 => "Weighing_Moon_Mars", _ => "Weighing_Moon_Earth" };

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "bag", () => (Mass, Place) = (1, 0)),
        Say(2, "spring"),
        TryIt(3, () => Place == 1, () => Place = 1, highlight: "ground"),
        Say(4, "balance"),
        Quiz(5, answer: 1, highlight: "spring"),
    ];
}
