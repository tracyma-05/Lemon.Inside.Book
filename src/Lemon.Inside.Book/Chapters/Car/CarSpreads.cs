using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Car;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Car_</c>.</summary>
public abstract class CarSpread(string key) : Spread("Car", key)
{
    protected static string Number(double value, string format = "0.0") => value.ToString(format, CultureInfo.CurrentCulture);
}

/// <summary>1. From up-and-down to round and round: the crank and its rod.</summary>
public sealed partial class CrankSpread() : CarSpread("Crank")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RodText), nameof(ReadoutText))]
    private double _rod = 3.5;

    public string RodText => Text.Format("Car_Times", Number(Rod));

    public string ReadoutText => Text.Format("Car_Crank_Readout", Number(SliderCrank.FastestAt(Rod), "0"));

    public override IReadOnlyList<string> Parts => ["piston", "rod", "crank", "graph"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "crank", () => Rod = 3.5),
        Say(2, "piston"),
        Say(3, "graph"),
        TryIt(4, () => Rod <= 1.8, () => Rod = 1.6, highlight: "rod"),
        Quiz(5, answer: 0, highlight: "graph"),
    ];
}

/// <summary>2. Opening on time: a cam lifts the valve.</summary>
public sealed partial class CamSpread() : CarSpread("Cam")
{
    /// <summary>The engine speed the readout uses, turns a minute.</summary>
    public const double Rpm = 3000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LiftText))]
    private double _lift = 9;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurationText), nameof(ReadoutText))]
    private double _duration = 240;

    public string LiftText => Text.Format("Car_Millimetres", Number(Lift));

    public string DurationText => Text.Format("Car_Degrees", Number(Duration, "0"));

    public string ReadoutText => Text.Format("Car_Cam_Readout", Number(Duration, "0"), Number(Cam.OpenMilliseconds(Duration, Rpm), "0.0"));

    public override IReadOnlyList<string> Parts => ["cam", "valve", "spring", "graph"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "cam", () => (Lift, Duration) = (9, 240)),
        Say(2, "valve"),
        Say(3, "spring"),
        TryIt(4, () => Duration >= 280, () => Duration = 290, highlight: "graph"),
        Quiz(5, answer: 1, highlight: "cam"),
    ];
}

/// <summary>3. Smooth turning: more cylinders, and a flywheel.</summary>
public sealed partial class CylindersSpread() : CarSpread("Cylinders")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Count), nameof(CountText), nameof(ReadoutText))]
    private int _countStep;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FlywheelText), nameof(ReadoutText))]
    private double _flywheel = 1;

    public int Count => Cylinders.Choices[Math.Clamp(CountStep, 0, Cylinders.Choices.Count - 1)];

    public string CountText => Count.ToString(CultureInfo.CurrentCulture);

    public string FlywheelText => Text.Format("Car_Times", Number(Flywheel));

    public string ReadoutText => Text.Format("Car_Cylinders_Readout", Number(Cylinders.Wobble(Count, Flywheel) * 100, "0.0"));

    public override IReadOnlyList<string> Parts => ["graph", "flywheel", "gauge"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "graph", () => (CountStep, Flywheel) = (0, 1)),
        Say(2, "gauge"),
        TryIt(3, () => Count >= 4, () => CountStep = 3, highlight: "graph"),
        Say(4, "flywheel", () => Flywheel = 2.5),
        Quiz(5, answer: 0, highlight: "flywheel"),
    ];
}

/// <summary>4. Moving off: the clutch slips, then grips.</summary>
public sealed partial class ClutchSpread() : CarSpread("Clutch")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PedalText), nameof(ReadoutText))]
    private double _pedal = 1;

    public string PedalText => Text.Format("Car_Percent", Number(Pedal * 100, "0"));

    public string ReadoutText => Clutch.Grip(Pedal) <= 0
        ? Text.GetString("Car_Clutch_Apart")
        : Text.Format("Car_Clutch_Readout", Number(Clutch.Grip(Pedal), "0"));

    public override IReadOnlyList<string> Parts => ["flywheel", "plate", "springs", "speeds"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "flywheel", () => Pedal = 1),
        Say(2, "plate"),
        TryIt(3, () => Pedal <= 0.05, () => Pedal = 0, highlight: "springs"),
        Say(4, "speeds"),
        Quiz(5, answer: 2, highlight: "plate"),
    ];
}

/// <summary>5. Round a bend: the differential lets the wheels turn at different speeds.</summary>
public sealed partial class BendSpread() : CarSpread("Bend")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RadiusText), nameof(ReadoutText))]
    private double _radius = 12;

    public string RadiusText => Text.Format("Car_Metres", Number(Radius, "0"));

    public string ReadoutText
    {
        get
        {
            var (inner, outer) = Differential.OnBend(Radius);
            return Text.Format("Car_Bend_Readout", Number(Radius, "0"), Number(((outer / inner) - 1) * 100, "0"));
        }
    }

    public override IReadOnlyList<string> Parts => ["road", "wheels", "case", "spider"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "road", () => Radius = 40),
        Say(2, "wheels", () => Radius = 12),
        Say(3, "spider"),
        TryIt(4, () => Radius <= 6, () => Radius = 5, highlight: "spider"),
        Quiz(5, answer: 0, highlight: "case"),
    ];
}

/// <summary>6. One wheel on ice: the differential's weakness, and locking it.</summary>
public sealed partial class IceSpread() : CarSpread("Ice")
{
    /// <summary>How much the wheel on dry road can grip, as a share of the engine's turning force.</summary>
    public const double DryGrip = 0.8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GripText), nameof(ReadoutText), nameof(Push))]
    private double _leftGrip = DryGrip;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Push))]
    private bool _locked;

    /// <summary>The push forward, as a share of the most the engine can give.</summary>
    public double Push => Differential.Push(LeftGrip, DryGrip, Locked);

    public string GripText => Text.Format("Car_Percent", Number(LeftGrip / DryGrip * 100, "0"));

    public string ReadoutText => Text.Format(Differential.Spins(LeftGrip, DryGrip, Locked) ? "Car_Ice_Spins" : "Car_Ice_Readout", Number(Push * 100, "0"));

    public override IReadOnlyList<string> Parts => ["slippy", "firm", "diff", "push"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "firm", () => (LeftGrip, Locked) = (DryGrip, false)),
        Say(2, "slippy", () => LeftGrip = 0.1),
        Say(3, "push"),
        TryIt(4, () => Locked, () => Locked = true, highlight: "diff"),
        Quiz(5, answer: 1, highlight: "slippy"),
    ];
}
