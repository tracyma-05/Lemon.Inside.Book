using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Boats;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Boats_</c>.</summary>
public abstract class BoatsSpread(string key) : Spread("Boats", key)
{
    protected static string Number(double value, string format = "0.0") => value.ToString(format, CultureInfo.CurrentCulture);
}

/// <summary>1. What floats: a block settles until the water it pushes aside weighs as much as it does.</summary>
public sealed partial class FloatSpread() : BoatsSpread("Float")
{
    /// <summary>Things the slider passes, by density (kg/m³), and their names' keys.</summary>
    public static IReadOnlyList<(double Density, string Key)> Materials { get; } =
    [
        (240, "Boats_Material_Cork"), (500, "Boats_Material_Pine"), (750, "Boats_Material_Oak"), (920, "Boats_Material_Ice"),
        (1200, "Boats_Material_Rubber"), (1900, "Boats_Material_Brick"),
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DensityText), nameof(ReadoutText), nameof(Sinks))]
    private double _density = 500;

    public bool Sinks => FloatingBlock.Sinks(Density);

    /// <summary>The material this density is close to, if any.</summary>
    public string? MaterialKey => Materials.Where(m => Math.Abs(m.Density - Density) <= 60).Select(m => m.Key).FirstOrDefault();

    public string DensityText => MaterialKey is { } key
        ? Text.Format("Boats_Float_DensityLike", Number(Density, "0"), Text.GetString(key))
        : Text.Format("Boats_Density", Number(Density, "0"));

    public string ReadoutText => Sinks
        ? Text.GetString("Boats_Float_Sinks")
        : Text.Format("Boats_Float_Readout", Number(FloatingBlock.SubmergedShare(Density) * 100, "0"));

    public override IReadOnlyList<string> Parts => ["block", "waterline", "weight", "lift"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "block", () => Density = 500),
        Say(2, "lift"),
        TryIt(3, () => Sinks, () => Density = 1900, highlight: "block"),
        Say(4, "waterline", () => Density = 920),
        Quiz(5, answer: 0, highlight: "weight"),
    ];
}

/// <summary>2. Where the push comes from: water presses harder the deeper it is.</summary>
public sealed partial class PressureSpread() : BoatsSpread("Pressure")
{
    /// <summary>The cube's side, metres.</summary>
    public const double Side = 1.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DepthText), nameof(ReadoutText))]
    private double _depth = 2;

    public string DepthText => Text.Format("Boats_Metres", Number(Depth));

    public string ReadoutText => Text.Format("Boats_Pressure_Readout",
        Number(Water.Pressure(Depth) / 1000, "0"), Number(Water.Pressure(Depth + Side) / 1000, "0"),
        Number((Water.Pressure(Depth + Side) - Water.Pressure(Depth)) * Side * Side / 1000, "0.0"));

    public override IReadOnlyList<string> Parts => ["cube", "top", "bottom", "sides"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "cube", () => Depth = 2),
        Say(2, "bottom"),
        TryIt(3, () => Depth >= 8, () => Depth = 9, highlight: "top"),
        Say(4, "sides"),
        Quiz(5, answer: 1, highlight: "cube"),
    ];
}

/// <summary>3. A steel boat: the hull's shape pushes aside far more water than its steel would.</summary>
public sealed partial class HullSpread() : BoatsSpread("Hull")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CargoText), nameof(ReadoutText), nameof(Sinks))]
    private double _cargo = 20;

    public bool Sinks => Hull.Sinks(Cargo);

    public string CargoText => Text.Format("Boats_Tonnes", Number(Cargo, "0"));

    public string ReadoutText => Sinks
        ? Text.Format("Boats_Hull_Sinks", Number(Hull.MostCargo, "0"))
        : Text.Format("Boats_Hull_Readout", Number(Hull.Draft(Cargo), "0.00"), Number(Hull.Depth - Hull.Draft(Cargo), "0.00"));

    public override IReadOnlyList<string> Parts => ["hull", "cargo", "lump", "draft"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "lump", () => Cargo = 0),
        Say(2, "hull"),
        Say(3, "draft", () => Cargo = 40),
        TryIt(4, () => Sinks, () => Cargo = 80, highlight: "cargo"),
        Quiz(5, answer: 0, highlight: "hull"),
    ];
}

/// <summary>4. Tipped over: the water's push moves to the low side and turns the boat back.</summary>
public sealed partial class HeelSpread() : BoatsSpread("Heel")
{
    /// <summary>The boat in this spread: 6 m wide, 3 m deep, floating 1.5 m deep, its weight centred 1.6 m up.</summary>
    public static Section Boat { get; } = new(6, 3, 1.5, 1.6);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeelText), nameof(ReadoutText))]
    private double _heelAngle = 15;

    public string HeelText => Text.Format("Boats_Degrees", Number(HeelAngle, "0"));

    public string ReadoutText
    {
        get
        {
            var lever = Boat.RightingLever(HeelAngle * Math.PI / 180);
            return HeelAngle < 0.5
                ? Text.GetString("Boats_Heel_Upright")
                : Text.Format(lever >= 0 ? "Boats_Heel_Readout" : "Boats_Heel_Tips", Number(Math.Abs(lever), "0.00"));
        }
    }

    public override IReadOnlyList<string> Parts => ["hull", "weight", "lift", "lever"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "weight", () => HeelAngle = 0),
        Say(2, "lift"),
        TryIt(3, () => HeelAngle >= 20, () => HeelAngle = 25, highlight: "lift"),
        Say(4, "lever"),
        Quiz(5, answer: 0, highlight: "hull"),
    ];
}

/// <summary>5. Steady or top-heavy: where the weight sits and how wide the boat is decide whether it rights itself.</summary>
public sealed partial class StabilitySpread() : BoatsSpread("Stability")
{
    public const double HullDepth = 3;
    public const double Draft = 1.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeightText), nameof(ReadoutText), nameof(Boat), nameof(MetacentricHeight))]
    private double _centreHeight = 1.6;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BeamText), nameof(ReadoutText), nameof(Boat), nameof(MetacentricHeight))]
    private double _beam = 6;

    public Section Boat => new(Beam, HullDepth, Draft, CentreHeight);

    public double MetacentricHeight => Boat.MetacentricHeight;

    public string HeightText => Text.Format("Boats_Metres", Number(CentreHeight));

    public string BeamText => Text.Format("Boats_Metres", Number(Beam));

    public string ReadoutText => Text.Format(MetacentricHeight >= 0 ? "Boats_Stability_Readout" : "Boats_Stability_TopHeavy",
        Number(MetacentricHeight, "0.00"));

    public override IReadOnlyList<string> Parts => ["boat", "weight", "curve"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "boat", () => (CentreHeight, Beam) = (1.6, 6)),
        Say(2, "curve"),
        TryIt(3, () => MetacentricHeight < 0, () => (CentreHeight, Beam) = (3.5, 5), highlight: "weight"),
        Say(4, "boat", () => Beam = 8),
        Quiz(5, answer: 0, highlight: "weight"),
    ];
}

/// <summary>6. Diving and surfacing: ballast tanks change the submarine's weight, not its size.</summary>
public sealed partial class SubmarineSpread() : BoatsSpread("Submarine")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BallastText), nameof(ReadoutText), nameof(IsNeutral))]
    private double _ballast;

    /// <summary>Whether the submarine weighs (almost) just what its water does.</summary>
    public bool IsNeutral => Math.Abs(Ballast - Submarine.Neutral) <= 0.02;

    public string BallastText => Text.Format("Boats_Percent", Number(Ballast * 100, "0"));

    public string ReadoutText
    {
        get
        {
            var weight = Number(Submarine.Weight(Ballast) * 100, "0");
            return Text.Format(IsNeutral ? "Boats_Submarine_Hangs" : Submarine.Weight(Ballast) < 1 ? "Boats_Submarine_Floats" : "Boats_Submarine_Sinks", weight);
        }
    }

    public override IReadOnlyList<string> Parts => ["sub", "tanks", "gauge"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "sub", () => Ballast = 0),
        Say(2, "tanks", () => Ballast = 1),
        TryIt(3, () => IsNeutral, () => Ballast = Submarine.Neutral, highlight: "tanks"),
        Say(4, "gauge"),
        Quiz(5, answer: 1, highlight: "tanks"),
    ];
}
