using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Inside.Book.Controls.Mechanics;

namespace Lemon.Inside.Book.Chapters.Gears;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Gears_</c>.</summary>
public abstract class GearSpread(string key) : Spread("Gears", key)
{
    protected static string Teeth(int teeth) => Text.Format("Gears_TeethCount", teeth);

    protected static string Number(double value, string format = "0.00") => value.ToString(format, CultureInfo.CurrentCulture);
}

/// <summary>1. Wheels that turn each other: the driven gear turns driver teeth ÷ driven teeth times per turn.</summary>
public sealed partial class RatioSpread() : GearSpread("Ratio")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DriverText), nameof(ReadoutText), nameof(Ratio))]
    private int _driverTeeth = 12;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DrivenText), nameof(ReadoutText), nameof(Ratio))]
    private int _drivenTeeth = 30;

    /// <summary>Turns of the driven gear per turn of the driving one.</summary>
    public double Ratio => (double)DriverTeeth / DrivenTeeth;

    public string DriverText => Teeth(DriverTeeth);

    public string DrivenText => Teeth(DrivenTeeth);

    public string ReadoutText => Text.Format("Gears_Ratio_Readout", Number(Ratio));
}

/// <summary>2. Slower, but stronger: a crank on a small gear turns a drum on a big one.</summary>
public sealed partial class TorqueSpread() : GearSpread("Torque")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DrivenText), nameof(ReadoutText), nameof(Ratio))]
    private int _drivenTeeth = 36;

    /// <summary>How many times slower, and how many times harder, the drum turns than the crank.</summary>
    public double Ratio => (double)DrivenTeeth / TorqueDiagram.CrankTeeth;

    public string DrivenText => Teeth(DrivenTeeth);

    public string ReadoutText => Text.Format("Gears_Torque_Readout", Number(1 / Ratio), Number(Ratio));
}

/// <summary>3. The shape of a tooth: the curve a string's end draws as it unwinds from a can.</summary>
public sealed partial class InvoluteSpread() : GearSpread("Involute")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnwoundText))]
    private double _unwound = 70;

    public string UnwoundText => Text.Format("Gears_Degrees", Number(Unwound, "0"));
}

/// <summary>4. Where the teeth touch: the line of action and the pressure angle.</summary>
public sealed partial class MeshSpread() : GearSpread("Mesh")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PressureText), nameof(PairsText))]
    private double _pressureAngle = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedText))]
    private double _speed = 0.5;

    public string PressureText => Text.Format("Gears_Degrees", Number(Math.Round(PressureAngle * 2) / 2, "0.#"));

    public string SpeedText => Text.Format("Gears_Percent", Number(Speed * 100, "0"));

    /// <summary>How many pairs of teeth touch on average.</summary>
    public double ContactRatio => GearGeometry.ContactRatio(
        MeshDiagram.DriverTeeth, MeshDiagram.DrivenTeeth, Math.Round(PressureAngle * 2) / 2 * Math.PI / 180);

    public string PairsText => Text.Format("Gears_Mesh_Readout", Number(ContactRatio, "0.0"));
}

/// <summary>5. Gears in a row: each step of a train multiplies the slowing.</summary>
public sealed partial class TrainSpread() : GearSpread("Train")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepsText), nameof(ReadoutText), nameof(Ratio))]
    private int _steps = 2;

    /// <summary>Turns of the first gear per turn of the last.</summary>
    public double Ratio => Math.Pow(TrainDiagram.StepRatio, Steps);

    public string StepsText => Steps.ToString(CultureInfo.CurrentCulture);

    public string ReadoutText => Text.Format("Gears_Train_Readout", Number(Ratio, "0"));
}

/// <summary>6. Gears inside a gear: hold one part of a planetary set and the other two trade speeds.</summary>
public sealed partial class PlanetarySpread() : GearSpread("Planet")
{
    /// <summary>The sun's teeth the slider steps through: each fits three planets evenly in a 60-tooth ring.</summary>
    public static IReadOnlyList<int> SunChoices { get; } = [12, 18, 24, 30, 36];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRingHeld), nameof(IsSunHeld), nameof(IsCarrierHeld), nameof(ReadoutText), nameof(Ratio))]
    private PlanetHeld _held = PlanetHeld.Ring;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SunTeeth), nameof(SunText), nameof(ReadoutText), nameof(Ratio))]
    private int _sunStep = 2;

    public int SunTeeth => SunChoices[Math.Clamp(SunStep, 0, SunChoices.Count - 1)];

    public string SunText => Teeth(SunTeeth);

    /// <summary>Turns of the output part for one turn of the input.</summary>
    public double Ratio => Planetary.Ratio(SunTeeth, Held);

    public string ReadoutText
    {
        get
        {
            var (input, output) = Planetary.Parts(Held);
            return Text.Format(Ratio < 0 ? "Gears_Planet_ReadoutBack" : "Gears_Planet_Readout",
                Text.GetString($"Gears_Planet_{input}Name"), Text.GetString($"Gears_Planet_{output}Name"), Number(Math.Abs(Ratio)));
        }
    }

    public bool IsRingHeld
    {
        get => Held == PlanetHeld.Ring;
        set { if (value) Held = PlanetHeld.Ring; }
    }

    public bool IsSunHeld
    {
        get => Held == PlanetHeld.Sun;
        set { if (value) Held = PlanetHeld.Sun; }
    }

    public bool IsCarrierHeld
    {
        get => Held == PlanetHeld.Carrier;
        set { if (value) Held = PlanetHeld.Carrier; }
    }
}
