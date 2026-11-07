using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Watch;

// The mechanical watch chapter's spoken walkthroughs, one per spread. Clips are Narr_Watch_<Key>_NN.

public sealed partial class SpringSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "barrel", () => Wound = 0.05),
        TryIt(2, () => Wound >= 0.95, () => Wound = 1, highlight: "arbor"),
        Say(3, "gauges"),
        Quiz(4, answer: 1),
    ];
}

public sealed partial class HandsSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "dial", () => SpeedStep = 0),
        Say(2, "dial", () => SpeedStep = 1),
        TryIt(3, () => SpeedStep == Speeds.Count - 1, () => SpeedStep = Speeds.Count - 1, highlight: "dial"),
        Quiz(4, answer: 2, highlight: "train"),
    ];
}

public sealed partial class EscapementSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "stones", () => SlowMotion = 0.05),
        Say(2, "balance"),
        Say(3, "lever"),
        TryIt(4, () => SlowMotion >= 0.4, () => SlowMotion = 0.5, highlight: "wheel"),
        Quiz(5, answer: 0),
    ];
}

public sealed partial class BalanceSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "balance", () => (Stiffness, Amplitude) = (1, 270)),
        Say(2, "trace", () => Amplitude = 150),
        Quiz(3, answer: 1, highlight: "trace"),
        TryIt(4, () => Stiffness >= 1.01, () => Stiffness = 1.02, highlight: "hairspring"),
        Say(5, null, () => Stiffness = 1),
    ];
}

public sealed partial class RotorSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "rotor", () => (Activity, Reverser) = (0.6, true)),
        Say(2, "wheel"),
        TryIt(3, () => !Reverser, () => Reverser = false, highlight: "wheel"),
        Quiz(4, answer: 2, highlight: "reserve"),
        Say(5, "barrel", () => Reverser = true),
    ];
}

public sealed partial class MovementSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "movement", () => (Explode, Hidden) = (0, 0)),
        Say(2, "movement", () => Explode = 0.8),
        Say(3, "train", () => Hidden = 1 << (int)MovementLayer.Dial),
        TryIt(4, () => !IsShown(MovementLayer.MotionWorks), () => Show(MovementLayer.MotionWorks, false), highlight: "motionworks"),
        Say(5, "barrel", () => Hidden = 0),
    ];
}
