using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Infrastructures.Localization;

namespace Lemon.Inside.Book.Chapters.Watch;

/// <summary>
/// One spread of the mechanical watch chapter: its title, two paragraphs, a closer look, and the settings its picture
/// uses. The strings are <c>Watch_&lt;Key&gt;_Title</c>, <c>_P1</c>, <c>_P2</c> and <c>_Deeper</c>.
/// </summary>
public abstract class WatchSpread(string key) : ObservableObject
{
    protected static ILocalizationService Text => LocalizationService.Instance;

    public string Key { get; } = key;

    public string Title => Text.GetString($"Watch_{Key}_Title");

    public IReadOnlyList<string> Paragraphs => [Text.GetString($"Watch_{Key}_P1"), Text.GetString($"Watch_{Key}_P2")];

    public string Deeper => Text.GetString($"Watch_{Key}_Deeper");

    /// <summary>Every key the spread's own words use (for the localization tests).</summary>
    public IEnumerable<string> TextKeys => new[] { "_Title", "_P1", "_P2", "_Deeper" }.Select(s => $"Watch_{Key}{s}");

    /// <summary>The reader's language changed: every worded value is said again.</summary>
    internal void OnLanguageChanged() => OnPropertyChanged(string.Empty);

    protected static string Number(double value, string format = "0") => value.ToString(format, CultureInfo.CurrentCulture);
}

/// <summary>1. Stored up in a spring: the mainspring in its barrel.</summary>
public sealed partial class SpringSpread() : WatchSpread("Spring")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WoundText), nameof(ReadoutText))]
    private double _wound = 0.6;

    public string WoundText => Text.Format("Watch_Percent", Number(Wound * 100));

    public string ReadoutText => Text.Format("Watch_Spring_Readout", Number(Wound * GoingTrain.ReserveHours), Number(Wound * GoingTrain.SpringTurns, "0.0"));
}

/// <summary>2. From one spring to three hands: the going train.</summary>
public sealed partial class HandsSpread() : WatchSpread("Hands")
{
    /// <summary>The speeds the slider steps through, times real time.</summary>
    public static IReadOnlyList<double> Speeds { get; } = [1, 60, 600, 3600];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Speed), nameof(SpeedText))]
    private int _speedStep = 1;

    public double Speed => Speeds[Math.Clamp(SpeedStep, 0, Speeds.Count - 1)];

    public string SpeedText => Text.Format("Watch_Times", Number(Speed));
}

/// <summary>3. Tick, tock: the lever escapement lets the spring out one tooth at a time.</summary>
public sealed partial class EscapementSpread() : WatchSpread("Escape")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SlowText))]
    private double _slowMotion = 0.1;

    public string SlowText => Text.Format("Watch_Escape_Slow", Number(1 / Math.Max(0.001, SlowMotion)));

    public string BeatsText => Text.Format("Watch_Escape_Readout", Number(GoingTrain.BeatsPerHour, "N0"), Number(GoingTrain.BeatsPerHour / 3600));
}

/// <summary>4. The heartbeat: the balance and hairspring set the rate.</summary>
public sealed partial class BalanceSpread() : WatchSpread("Balance")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StiffnessText), nameof(ReadoutText), nameof(ErrorText), nameof(Frequency))]
    private double _stiffness = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AmplitudeText))]
    private double _amplitude = 270;

    /// <summary>Swings a second.</summary>
    public double Frequency => Balance.Frequency(Stiffness);

    public string StiffnessText => Text.Format("Watch_Percent", Number(Stiffness * 100));

    public string AmplitudeText => Text.Format("Watch_Degrees", Number(Amplitude));

    public string ReadoutText => Text.Format("Watch_Balance_Readout", Number(Frequency, "0.000"), Number(Frequency * 7200, "N0"));

    public string ErrorText
    {
        get
        {
            var error = Balance.DailyError(Frequency);
            return Math.Abs(error) < 1
                ? Text.GetString("Watch_Balance_OnTime")
                : Text.Format(error > 0 ? "Watch_Balance_Gains" : "Watch_Balance_Loses", Number(Math.Abs(error) / 60, "0.0"));
        }
    }
}

/// <summary>5. Wound by the wrist: the rotor of an automatic watch.</summary>
public sealed partial class RotorSpread() : WatchSpread("Rotor")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActivityText))]
    private double _activity = 0.6;

    [ObservableProperty]
    private bool _reverser = true;

    public string ActivityText => Text.Format("Watch_Percent", Number(Activity * 100));
}

/// <summary>6. The whole movement, taken apart layer by layer.</summary>
public sealed partial class MovementSpread() : WatchSpread("Movement")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExplodeText))]
    private double _explode = 0.6;

    [ObservableProperty]
    private int _hidden;

    public string ExplodeText => Text.Format("Watch_Percent", Number(Explode * 100));

    public bool ShowPlate { get => IsShown(MovementLayer.Plate); set => Show(MovementLayer.Plate, value); }

    public bool ShowBarrel { get => IsShown(MovementLayer.Barrel); set => Show(MovementLayer.Barrel, value); }

    public bool ShowTrain { get => IsShown(MovementLayer.Train); set => Show(MovementLayer.Train, value); }

    public bool ShowEscapement { get => IsShown(MovementLayer.Escapement); set => Show(MovementLayer.Escapement, value); }

    public bool ShowMotionWorks { get => IsShown(MovementLayer.MotionWorks); set => Show(MovementLayer.MotionWorks, value); }

    public bool ShowDial { get => IsShown(MovementLayer.Dial); set => Show(MovementLayer.Dial, value); }

    public bool IsShown(MovementLayer layer) => (Hidden & (1 << (int)layer)) == 0;

    public void Show(MovementLayer layer, bool shown)
    {
        Hidden = shown ? Hidden & ~(1 << (int)layer) : Hidden | (1 << (int)layer);
        OnPropertyChanged($"Show{layer}");
    }
}
