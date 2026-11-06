using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Infrastructures.Localization;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// One spread of the Light and shadow chapter: its title, two paragraphs, a closer look, and the settings its picture
/// uses. The strings are <c>Light_&lt;Key&gt;_Title</c>, <c>_P1</c>, <c>_P2</c> and <c>_Deeper</c>.
/// </summary>
public abstract class LightSpread(string key) : ObservableObject
{
    private static ILocalizationService Text => LocalizationService.Instance;

    public string Key { get; } = key;

    public string Title => Text.GetString($"Light_{Key}_Title");

    public IReadOnlyList<string> Paragraphs => [Text.GetString($"Light_{Key}_P1"), Text.GetString($"Light_{Key}_P2")];

    public string Deeper => Text.GetString($"Light_{Key}_Deeper");

    /// <summary>Every key the spread's own words use (for the localization tests).</summary>
    public IEnumerable<string> TextKeys => new[] { "_Title", "_P1", "_P2", "_Deeper" }.Select(s => $"Light_{Key}{s}");

    /// <summary>The reader's language changed: every worded value is said again.</summary>
    internal void OnLanguageChanged() => OnPropertyChanged(string.Empty);

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

/// <summary>5. Sharp and soft shadows: a square lamp over a box.</summary>
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
