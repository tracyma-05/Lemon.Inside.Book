using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Infrastructures.Localization;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Light_</c>.</summary>
public abstract class LightSpread(string key) : Spread("Light", key)
{
    protected static string Metres(double value) => Text.Format("Light_Metres", value.ToString("0.0", CultureInfo.CurrentCulture));

    protected static string Percent(double share) => Text.Format("Light_Percent", (share * 100).ToString("0", CultureInfo.CurrentCulture));

    protected static string Times(double value) => Text.Format("Light_Times", value.ToString("0.00", CultureInfo.CurrentCulture));
}

/// <summary>1. How bright is a lamp: twice the light does not look twice as bright, and too much is just white.</summary>
public sealed partial class BrightnessSpread() : LightSpread("Brightness")
{
    public const double MinPower = 2;
    public const double MaxPower = 150;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PowerText))]
    private double _power = 30;

    public string PowerText => LocalizationService.Instance.Format("Light_Watts", Power.ToString("0", CultureInfo.CurrentCulture));
}

/// <summary>2. Light at a slant: the same beam spread over a longer patch.</summary>
public sealed partial class TiltSpread() : LightSpread("Tilt")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TiltText), nameof(PatchText), nameof(BrightnessText))]
    private double _tilt = 30;

    public string TiltText => LocalizationService.Instance.Format("Light_Degrees", Tilt.ToString("0", CultureInfo.CurrentCulture));

    /// <summary>How much longer the lit patch is than the beam is wide (1 / cos).</summary>
    public double PatchLength => 1 / Math.Cos(Tilt * Math.PI / 180);

    public string PatchText => Times(PatchLength);

    public string BrightnessText => Percent(1 / PatchLength);
}

/// <summary>3. Further away, dimmer: the light through one square at 1 m covers d² squares at d metres.</summary>
public sealed partial class DistanceSpread() : LightSpread("Distance")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DistanceText), nameof(SquaresText), nameof(BrightnessText))]
    private double _distance = 2;

    public string DistanceText => Metres(Distance);

    public string SquaresText => (Distance * Distance).ToString("0.0", CultureInfo.CurrentCulture);

    public string BrightnessText => Percent(1 / (Distance * Distance));
}

/// <summary>4. How big does it look: the angle a car takes up, in degrees and radians.</summary>
public sealed partial class AngleSpread() : LightSpread("Angle")
{
    /// <summary>The car's length, side on, in metres.</summary>
    public const double CarLength = 4.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DistanceText), nameof(AngleText))]
    private double _distance = 10;

    public string DistanceText => Metres(Distance);

    /// <summary>The angle between the car's two ends, seen from the eye, in radians.</summary>
    public double Radians => 2 * Math.Atan(CarLength / 2 / Distance);

    public string AngleText => LocalizationService.Instance.Format(
        "Light_Angle_Value",
        (Radians * 180 / Math.PI).ToString("0.0", CultureInfo.CurrentCulture),
        Radians.ToString("0.000", CultureInfo.CurrentCulture));
}

/// <summary>8. Sharp and soft shadows: a square lamp over a box.</summary>
public sealed partial class ShadowSpread() : LightSpread("Shadow")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LightHeightText))]
    private double _lightHeight = 2.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LightSizeText))]
    private double _lightSize = 0.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LightXText))]
    private double _lightX = 0.6;

    public string LightHeightText => Metres(LightHeight);

    public string LightSizeText => Metres(LightSize);

    public string LightXText => Metres(LightX);
}

/// <summary>5. What a spot on the floor sees: a half-sky, and the lamp as a patch of it.</summary>
public sealed partial class SkySpread() : LightSpread("Sky")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ElevationText), nameof(ShareText))]
    private double _elevation = 50;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DistanceText), nameof(ShareText))]
    private double _distance = 2.5;

    public string ElevationText => LocalizationService.Instance.Format("Light_Degrees", Elevation.ToString("0", CultureInfo.CurrentCulture));

    public string DistanceText => Metres(Distance);

    public string ShareText => LocalizationService.Instance.Format(
        "Light_Sky_Share", (SkyScene.FloorShare(Elevation, Distance) * 100).ToString("0.0", CultureInfo.CurrentCulture));
}

/// <summary>6. Why a glowing panel looks as bright near or far, straight on or slanted (radiance).</summary>
public sealed partial class RadianceSpread() : LightSpread("Radiance")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DistanceText))]
    private double _distance = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TiltText))]
    private double _tilt;

    public string DistanceText => Metres(Distance);

    public string TiltText => LocalizationService.Instance.Format("Light_Degrees", Tilt.ToString("0", CultureInfo.CurrentCulture));
}

/// <summary>7. Mirror, glossy and matte: where the light goes after it lands.</summary>
public sealed partial class ReflectionSpread() : LightSpread("Reflection")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LightAngleText))]
    private double _lightAngle = 40;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RoughnessText))]
    private double _roughness = 0.3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EyeAngleText))]
    private double _eyeAngle = 40;

    public string LightAngleText => LocalizationService.Instance.Format("Light_Degrees", LightAngle.ToString("0", CultureInfo.CurrentCulture));

    public string RoughnessText => Percent(Roughness);

    public string EyeAngleText => LocalizationService.Instance.Format("Light_Degrees", EyeAngle.ToString("0", CultureInfo.CurrentCulture));
}

/// <summary>9. Colour and bounces: a coloured wall tints the floor once light bounces.</summary>
public sealed partial class BounceSpread() : LightSpread("Bounce")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BouncesText))]
    private int _bounces;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRed), nameof(IsGreen), nameof(IsBlue))]
    private WallColor _wall = WallColor.Red;

    public string BouncesText => Bounces.ToString(CultureInfo.CurrentCulture);

    public bool IsRed
    {
        get => Wall == WallColor.Red;
        set { if (value) Wall = WallColor.Red; }
    }

    public bool IsGreen
    {
        get => Wall == WallColor.Green;
        set { if (value) Wall = WallColor.Green; }
    }

    public bool IsBlue
    {
        get => Wall == WallColor.Blue;
        set { if (value) Wall = WallColor.Blue; }
    }
}
