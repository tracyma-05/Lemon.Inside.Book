using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Lemon.Inside.Book.Chapters.Bicycle;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Bicycle_</c>.</summary>
public abstract class BicycleSpread(string key) : Spread("Bicycle", key)
{
    protected static string Number(double value, string format = "0.0") => value.ToString(format, CultureInfo.CurrentCulture);
}

/// <summary>1. Standing on wires: a load takes some pull off the spokes under the hub.</summary>
public sealed partial class WheelSpread() : BicycleSpread("Wheel")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TensionText), nameof(ReadoutText), nameof(BottomTension), nameof(IsSlack))]
    private double _tension = 1000;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LoadText), nameof(ReadoutText), nameof(BottomTension), nameof(IsSlack))]
    private double _load = 60;

    /// <summary>The pull of the spoke pointing straight down, in newtons.</summary>
    public double BottomTension => Math.Max(0, Tension - SpokedWheel.BottomLoss(Load));

    /// <summary>Whether the spokes under the hub have stopped pulling.</summary>
    public bool IsSlack => BottomTension <= 0;

    public string TensionText => Text.Format("Bicycle_Newtons", Number(Math.Round(Tension / 10) * 10, "0"));

    public string LoadText => Text.Format("Bicycle_Kilograms", Number(Load, "0"));

    public string ReadoutText => IsSlack
        ? Text.GetString("Bicycle_Wheel_Slack")
        : Text.Format("Bicycle_Wheel_Readout", Number(BottomTension, "0"), Number(Math.Round(Tension / 10) * 10, "0"));
}

/// <summary>2. From the pedals to the wheel: the chain moves as many teeth past each sprocket.</summary>
public sealed partial class ChainSpread() : BicycleSpread("Chain")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChainringText), nameof(ReadoutText), nameof(Ratio))]
    private int _chainring = 44;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SprocketText), nameof(ReadoutText), nameof(Ratio))]
    private int _sprocket = 22;

    /// <summary>Turns of the back wheel per turn of the pedals.</summary>
    public double Ratio => Drive.Ratio(Chainring, Sprocket);

    public string ChainringText => Text.Format("Bicycle_TeethCount", Chainring);

    public string SprocketText => Text.Format("Bicycle_TeethCount", Sprocket);

    public string ReadoutText => Text.Format("Bicycle_Chain_Readout", Number(Ratio, "0.00"), Number(Drive.Development(Chainring, Sprocket), "0.0"));
}

/// <summary>3. Gears for hills: the same rhythm, a lighter push, a slower ride.</summary>
public sealed partial class HillSpread() : BicycleSpread("Hill")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SlopeText), nameof(ReadoutText), nameof(PushKilograms), nameof(IsComfortable))]
    private double _slope;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Sprocket), nameof(GearText), nameof(ReadoutText), nameof(PushKilograms), nameof(IsComfortable))]
    private int _gear = 2;

    /// <summary>The sprocket the chain is on.</summary>
    public int Sprocket => Climb.Cassette[Math.Clamp(Gear, 0, Climb.Cassette.Count - 1)];

    /// <summary>The push on each pedal, as kilograms standing on it.</summary>
    public double PushKilograms => Climb.PedalKilograms(Slope, Sprocket);

    /// <summary>Whether that push could be kept up all the way up a long hill.</summary>
    public bool IsComfortable => PushKilograms <= Climb.ComfortableKilograms;

    public string SlopeText => Text.Format("Bicycle_Percent", Number(Slope, "0.#"));

    public string GearText => Text.Format("Bicycle_Hill_GearValue", Gear + 1, Sprocket);

    public string ReadoutText => Text.Format("Bicycle_Hill_Readout",
        Number(Climb.Cadence, "0"), Number(Climb.Speed(Sprocket) * 3.6, "0"), Number(PushKilograms, "0"),
        Number(Climb.Power(Slope, Sprocket), "0"));
}

/// <summary>4. Coasting: the pawls in the back hub let the wheel run ahead of the pedals.</summary>
public sealed partial class FreewheelSpread() : BicycleSpread("Freewheel")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PedalText))]
    private double _pedalSpeed = 0.6;

    public string PedalText => Math.Abs(PedalSpeed) < 0.02
        ? Text.GetString("Bicycle_Freewheel_Still")
        : Text.Format(PedalSpeed < 0 ? "Bicycle_Freewheel_Back" : "Bicycle_Freewheel_Forward", Number(Math.Abs(PedalSpeed), "0.0"));
}

/// <summary>5. Stopping: braking tips the weight forwards.</summary>
public sealed partial class BrakeSpread() : BicycleSpread("Brakes")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedText), nameof(ReadoutText), nameof(Distance))]
    private double _speed = 25;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StrengthText), nameof(ReadoutText), nameof(Distance), nameof(Outcome))]
    private double _strength = 0.3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBoth), nameof(IsFront), nameof(IsRear), nameof(ReadoutText), nameof(Distance), nameof(Outcome))]
    private BrakeUse _use = BrakeUse.Both;

    public BrakeOutcome Outcome => Braking.Brake(Use, Strength).Outcome;

    /// <summary>Metres to stop.</summary>
    public double Distance => Braking.StoppingDistance(Speed, Braking.Brake(Use, Strength).Slowing);

    public string SpeedText => Text.Format("Bicycle_Kmh", Number(Speed, "0"));

    public string StrengthText => Text.Format("Bicycle_Brakes_G", Number(Strength, "0.00"));

    public string ReadoutText
    {
        get
        {
            var (outcome, slowing) = Braking.Brake(Use, Strength);
            var front = Number(Braking.FrontLoad(slowing) * 100, "0");
            return outcome switch
            {
                BrakeOutcome.RearSkids => Text.Format("Bicycle_Brakes_Skid", Number(slowing, "0.00"), Number(Distance, "0.0")),
                BrakeOutcome.Flips => Text.GetString("Bicycle_Brakes_Flip"),
                _ => Text.Format("Bicycle_Brakes_Readout", Number(Distance, "0.0"), front),
            };
        }
    }

    public bool IsBoth
    {
        get => Use == BrakeUse.Both;
        set { if (value) Use = BrakeUse.Both; }
    }

    public bool IsFront
    {
        get => Use == BrakeUse.Front;
        set { if (value) Use = BrakeUse.Front; }
    }

    public bool IsRear
    {
        get => Use == BrakeUse.Rear;
        set { if (value) Use = BrakeUse.Rear; }
    }
}

/// <summary>6. Why it steers itself straight: the trail behind the steering axis.</summary>
public sealed partial class SteerSpread() : BicycleSpread("Steer")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeadAngleText), nameof(ReadoutText), nameof(Trail))]
    private double _headAngle = 72;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OffsetText), nameof(ReadoutText), nameof(Trail))]
    private double _offset = 45;

    /// <summary>Trail in millimetres (negative: the tyre touches the ground ahead of the steering axis).</summary>
    public double Trail => Steering.Trail(HeadAngle, Offset / 1000) * 1000;

    public string HeadAngleText => Text.Format("Bicycle_Degrees", Number(HeadAngle, "0"));

    public string OffsetText => Text.Format("Bicycle_Millimetres", Number(Offset, "0"));

    public string ReadoutText => Text.Format("Bicycle_Steer_Readout", Number(Trail, "0"), Text.GetString(Trail switch
    {
        < 0 => "Bicycle_Steer_Unstable",
        < 35 => "Bicycle_Steer_Light",
        <= 90 => "Bicycle_Steer_Steady",
        _ => "Bicycle_Steer_Heavy",
    }));
}
