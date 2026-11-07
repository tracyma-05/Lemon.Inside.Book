using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Friction;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Friction_</c>.</summary>
public abstract class FrictionSpread(string key) : Spread("Friction", key)
{
    protected static string Number(double value, string format = "0") => value.ToString(format, CultureInfo.CurrentCulture);

    /// <summary>A force said as the weight it would hold up: "4 kg".</summary>
    protected static string Kg(double value) => Text.Format("Friction_Kg", Number(value, value < 10 ? "0.#" : "N0"));

    protected static string Metres(double value) => Text.Format("Friction_Metres", Number(value, "0.#"));
}

/// <summary>1. The ramp: steep enough, and the block slides.</summary>
public sealed partial class RampSpread() : FrictionSpread("Ramp")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AngleText), nameof(ReadoutText), nameof(Slides))]
    private double _angle = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SurfaceText), nameof(ReadoutText), nameof(Slides))]
    private int _surface = 1;

    public bool Slides => Ramp.Slides(Angle, Surface);

    public string AngleText => Text.Format("Friction_Degrees", Number(Angle));

    public string SurfaceText => Text.GetString(SurfaceKey(Surface));

    public static string SurfaceKey(int surface) => surface switch { 0 => "Friction_Ramp_Ice", 2 => "Friction_Ramp_Rubber", _ => "Friction_Ramp_Wood" };

    public string ReadoutText => Text.Format(Slides ? "Friction_Ramp_Slides" : "Friction_Ramp_Holds",
        Kg(Ramp.Downhill(Angle)), Kg(Ramp.MostGrip(Angle, Surface)), Text.Format("Friction_Degrees", Number(Ramp.Steepest(Surface))));

    public override IReadOnlyList<string> Parts => ["block", "ramp", "grip", "pull"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "block", () => (Angle, Surface) = (10, 1)),
        Say(2, "pull"),
        TryIt(3, () => Slides, () => Angle = 25, highlight: "ramp"),
        Say(4, "grip", () => Surface = 2),
        Quiz(5, answer: 1, highlight: "block"),
    ];
}

/// <summary>2. Starting and sliding: harder to get going than to keep going.</summary>
public sealed partial class StartSpread() : FrictionSpread("Start")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BoxText), nameof(ReadoutText), nameof(Moving))]
    private double _box = 40;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PullingText), nameof(ReadoutText), nameof(Moving))]
    private double _pulling;

    public bool Moving => Drag.Moving(Box, Pulling);

    public string BoxText => Kg(Box);

    public string PullingText => Kg(Pulling);

    public string ReadoutText => Moving
        ? Text.Format("Friction_Start_Moving", Kg(Drag.BreakAway(Box)), Kg(Drag.Friction(Box, Pulling)), Kg(Pulling - Drag.Friction(Box, Pulling)))
        : Text.Format("Friction_Start_Still", Kg(Drag.Friction(Box, Pulling)), Kg(Drag.BreakAway(Box)));

    public override IReadOnlyList<string> Parts => ["box", "hand", "floor", "chart"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "box", () => (Box, Pulling) = (40, 0)),
        Say(2, "hand", () => Pulling = 10),
        TryIt(3, () => Moving, () => Pulling = 22, highlight: "hand"),
        Say(4, "chart"),
        Quiz(5, answer: 0, highlight: "floor"),
    ];
}

/// <summary>3. The skid: locked wheels slide, and a sliding tyre grips less.</summary>
public sealed partial class SkidSpread() : FrictionSpread("Skid")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedText), nameof(ReadoutText), nameof(Distance))]
    private double _speed = 50;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Distance))]
    private bool _locked;

    public double Distance => Skid.Distance(Speed, Locked);

    public string SpeedText => Text.Format("Friction_Speed", Number(Speed));

    public string ReadoutText => Text.Format(Locked ? "Friction_Skid_Locked" : "Friction_Skid_Rolling", Metres(Distance),
        Metres(Skid.Distance(Speed, !Locked)));

    public override IReadOnlyList<string> Parts => ["car", "tyres", "road", "distance"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "car", () => (Speed, Locked) = (50, false)),
        Say(2, "tyres"),
        TryIt(3, () => Speed >= 100, () => Speed = 100, highlight: "distance"),
        Say(4, "road", () => Locked = true),
        Quiz(5, answer: 2, highlight: "tyres"),
    ];
}

/// <summary>4. A rope round a post: a few turns, and a hand can hold a boat.</summary>
public sealed partial class CapstanSpread() : FrictionSpread("Capstan")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TurnsText), nameof(ReadoutText), nameof(Hold))]
    private double _turns;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Hold))]
    private bool _wet;

    public double Hold => Capstan.Hold(Turns, Wet);

    public string TurnsText => Text.Format("Friction_Turns", Number(Turns, "0.##"));

    public string ReadoutText => Text.Format("Friction_Capstan_Readout", Kg(Hold), Kg(Capstan.Boat), Number(Capstan.Gain(Turns, Wet), "N0"));

    public override IReadOnlyList<string> Parts => ["boat", "rope", "post", "hand"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "boat", () => (Turns, Wet) = (0, false)),
        Say(2, "rope", () => Turns = 1),
        TryIt(3, () => Hold <= 25, () => Turns = 2, highlight: "post"),
        Say(4, "hand", () => Turns = 3),
        Quiz(5, answer: 1, highlight: "rope"),
    ];
}

/// <summary>5. Rolling instead of sliding: logs, then wheels on ball bearings.</summary>
public sealed partial class RollingSpread() : FrictionSpread("Rolling")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WayText), nameof(ReadoutText), nameof(Force))]
    private int _way;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CrateText), nameof(ReadoutText), nameof(Force))]
    private double _crate = 200;

    public double Force => Rolling.Force(Crate, Way);

    public string WayText => Text.GetString(WayKey(Way));

    public string CrateText => Kg(Crate);

    public static string WayKey(int way) => way switch { 1 => "Friction_Rolling_Logs", 2 => "Friction_Rolling_Wheels", _ => "Friction_Rolling_Dragged" };

    public string ReadoutText => Text.Format("Friction_Rolling_Readout", Kg(Force), Kg(Crate));

    public override IReadOnlyList<string> Parts => ["crate", "ground", "rollers", "hand"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "crate", () => (Way, Crate) = (0, 200)),
        Say(2, "ground"),
        TryIt(3, () => Way >= 1, () => Way = 1, highlight: "rollers"),
        Say(4, "rollers", () => Way = 2),
        Quiz(5, answer: 0, highlight: "rollers"),
    ];
}

/// <summary>6. Oil: a film that carries the load once it is moving fast enough.</summary>
public sealed partial class OilSpread() : FrictionSpread("Oil")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedText), nameof(ReadoutText), nameof(Touching))]
    private double _speed = 50;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Touching))]
    private bool _oiled;

    public bool Touching => Lubrication.Touching(Speed, Oiled);

    public string SpeedText => Text.Format("Friction_Percent", Number(Speed));

    public string ReadoutText => !Oiled
        ? Text.Format("Friction_Oil_Dry", Number(Lubrication.Dry * 100))
        : Text.Format(Touching ? "Friction_Oil_Thin" : "Friction_Oil_Thick", Number(Lubrication.Film(Speed), "0.0"),
            Number(Lubrication.Friction(Speed, true) * 100, "0.#"));

    public override IReadOnlyList<string> Parts => ["surfaces", "bumps", "oil", "chart"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "bumps", () => (Speed, Oiled) = (50, false)),
        Say(2, "oil", () => Oiled = true),
        TryIt(3, () => Touching, () => Speed = 5, highlight: "surfaces"),
        Say(4, "chart", () => Speed = 80),
        Quiz(5, answer: 2, highlight: "oil"),
    ];
}
