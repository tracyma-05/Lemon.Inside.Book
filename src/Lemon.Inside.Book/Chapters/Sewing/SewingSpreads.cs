using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Sewing;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Sewing_</c>.</summary>
public abstract class SewingSpread(string key) : Spread("Sewing", key)
{
    protected static string Number(double value, string format = "0") => value.ToString(format, CultureInfo.CurrentCulture);

    protected static string Mm(double value) => Text.Format("Sewing_Mm", Number(value, "0.#"));

    protected static string Cm(double mm) => Text.Format("Sewing_Cm", Number(mm / 10, "0.#"));
}

/// <summary>1. Two kinds of stitch: one thread chained to itself, or two threads locked together.</summary>
public sealed partial class StitchSpread() : SewingSpread("Stitch")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _chain;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LengthText), nameof(ReadoutText))]
    private double _length = 2.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _pulled;

    public string LengthText => Mm(Length);

    public string ReadoutText
    {
        get
        {
            var count = Seam.Stitches(Length);
            var sewn = Chain
                ? Text.Format("Sewing_Stitch_ReadoutChain", Number(count), Cm(Seam.Thread(Length, true)))
                : Text.Format("Sewing_Stitch_Readout", Number(count), Cm(count * Seam.Upper(Length, false)), Cm(count * Seam.Lower(Length, false)));
            return Pulled ? $"{sewn} {Text.GetString(Seam.Unravels(Chain) ? "Sewing_Stitch_Undone" : "Sewing_Stitch_Holds")}" : sewn;
        }
    }

    public override IReadOnlyList<string> Parts => ["holes", "upper", "lower", "knots"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "holes", () => (Chain, Length, Pulled) = (false, 2.5, false)),
        Say(2, "knots"),
        TryIt(3, () => Chain, () => Chain = true, highlight: "upper"),
        Say(4, "upper", () => Pulled = true),
        Quiz(5, answer: 1, highlight: "holes"),
    ];
}

/// <summary>2. The needle: its eye is at the point, and the loop opens as it starts back up.</summary>
public sealed partial class NeedleSpread() : SewingSpread("Needle")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AngleText), nameof(ReadoutText))]
    private double _angle = 150;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _handNeedle;

    public string AngleText => Text.Format("Sewing_Degrees", Number(Angle));

    public string ReadoutText => HandNeedle
        ? Text.GetString("Sewing_Needle_ReadoutHand")
        : Text.Format(Needle.Rising(Angle) ? "Sewing_Needle_ReadoutUp" : "Sewing_Needle_ReadoutDown", Mm(Needle.Rise(Angle)),
            Mm(Needle.Loop(Angle)));

    public override IReadOnlyList<string> Parts => ["needle", "eye", "loop", "cloth"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "needle", () => (Angle, HandNeedle) = (150, false)),
        Say(2, "eye"),
        TryIt(3, () => !HandNeedle && Needle.Loop(Angle) >= Hook.Narrowest, () => Angle = Hook.Right, highlight: "loop"),
        Say(4, "cloth"),
        Quiz(5, answer: 0, highlight: "eye"),
    ];
}

/// <summary>3. The hook: its point has to meet the loop at just the right moment.</summary>
public sealed partial class HookSpread() : SewingSpread("Hook")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TimingText), nameof(ReadoutText), nameof(Catches))]
    private double _timing;

    public bool Catches => Hook.Catches(Timing);

    public string TimingText => Timing switch
    {
        > 0.5 => Text.Format("Sewing_Hook_Late", Number(Timing)),
        < -0.5 => Text.Format("Sewing_Hook_Early", Number(-Timing)),
        _ => Text.GetString("Sewing_Hook_OnTime"),
    };

    public string ReadoutText => Text.Format(Catches ? "Sewing_Hook_Readout" : "Sewing_Hook_Missed", Mm(Hook.RiseAt(Timing)),
        Mm(Hook.LoopAt(Timing)));

    public override IReadOnlyList<string> Parts => ["hook", "bobbin", "loop", "seam"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "hook", () => Timing = 0),
        Say(2, "bobbin"),
        TryIt(3, () => !Catches, () => Timing = 20, highlight: "seam"),
        Say(4, "loop", () => Timing = 0),
        Quiz(5, answer: 2, highlight: "hook"),
    ];
}

/// <summary>4. The take-up lever: thread let down round the bobbin, then pulled back to set the stitch.</summary>
public sealed partial class TakeUpSpread() : SewingSpread("TakeUp")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LengthText), nameof(ReadoutText))]
    private double _length = 2.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _held;

    public string LengthText => Mm(Length);

    public string ReadoutText => Held
        ? Text.GetString("Sewing_TakeUp_Tangle")
        : Text.Format("Sewing_TakeUp_Readout", Cm(TakeUp.Loop), Mm(TakeUp.PerStitch(Length)), Number(TakeUp.Passes(Length)));

    public override IReadOnlyList<string> Parts => ["lever", "spool", "needle", "bobbin"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "lever", () => (Length, Held) = (2.5, false)),
        Say(2, "bobbin"),
        TryIt(3, () => Held, () => Held = true, highlight: "lever"),
        Say(4, "needle", () => Held = false),
        Quiz(5, answer: 1, highlight: "spool"),
    ];
}

/// <summary>5. The feed dogs: teeth that carry the cloth a stitch on while the needle is out of it.</summary>
public sealed partial class FeedSpread() : SewingSpread("Feed")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LengthText), nameof(ReadoutText))]
    private double _length = 2.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _reverse;

    public string LengthText => Mm(Length);

    public string ReadoutText
    {
        get
        {
            if (Length < 0.1) return Text.GetString("Sewing_Feed_OnTheSpot");

            var moving = Text.Format("Sewing_Feed_Readout", Number(100 / Length), Number(Feed.Pace),
                Text.Format("Sewing_Metres", Number(Feed.MetresPerMinute(Length), "0.#")));
            return Reverse ? $"{moving} {Text.GetString("Sewing_Feed_Backwards")}" : moving;
        }
    }

    public override IReadOnlyList<string> Parts => ["dogs", "foot", "plate", "cloth"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "dogs", () => (Length, Reverse) = (2.5, false)),
        Say(2, "foot"),
        TryIt(3, () => Reverse, () => Reverse = true, highlight: "dogs"),
        Say(4, "cloth", () => (Reverse, Length) = (false, 5)),
        Quiz(5, answer: 0, highlight: "plate"),
    ];
}

/// <summary>6. Tension: the two threads pull against each other, and the knot goes to the stronger side.</summary>
public sealed partial class TensionSpread() : SewingSpread("Tension")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DialText), nameof(ReadoutText), nameof(Knot))]
    private double _dial = 4;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BobbinText), nameof(ReadoutText), nameof(Knot))]
    private double _bobbin = 30;

    public double Knot => Tension.Knot(Dial, Bobbin);

    public string DialText => Number(Dial, "0.#");

    public string BobbinText => Text.Format("Sewing_Grams", Number(Bobbin));

    public string ReadoutText => Text.Format(Tension.Verdict(Dial, Bobbin) switch
    {
        -1 => "Sewing_Tension_Tight",
        1 => "Sewing_Tension_Loose",
        _ => "Sewing_Tension_Even",
    }, Text.Format("Sewing_Grams", Number(Tension.Upper(Dial))), Text.Format("Sewing_Grams", Number(Bobbin)));

    public override IReadOnlyList<string> Parts => ["knots", "dial", "screw", "cloth"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "knots", () => (Dial, Bobbin) = (4, 30)),
        Say(2, "dial"),
        TryIt(3, () => Tension.Verdict(Dial, Bobbin) == -1, () => Dial = 8, highlight: "knots"),
        Say(4, "screw", () => Dial = 4),
        Quiz(5, answer: 2, highlight: "dial"),
    ];
}
