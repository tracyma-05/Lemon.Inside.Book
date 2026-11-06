using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Watch;

// The mechanical watch chapter's spoken walkthroughs, one per spread. Clips are Narr_Watch_<Key>_NN.

public sealed partial class SpringSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => Wound = 0.05 },
        TryIt(2, () => Wound >= 0.95, () => Wound = 1),
        new(Clip(3)),
        Quiz(4, answer: 1),
    ];
}

public sealed partial class HandsSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => SpeedStep = 0 },
        new(Clip(2)) { Enter = () => SpeedStep = 1 },
        TryIt(3, () => SpeedStep == Speeds.Count - 1, () => SpeedStep = Speeds.Count - 1),
        Quiz(4, answer: 2),
    ];
}

public sealed partial class EscapementSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => SlowMotion = 0.05 },
        new(Clip(2)),
        new(Clip(3)),
        TryIt(4, () => SlowMotion >= 0.4, () => SlowMotion = 0.5),
        Quiz(5, answer: 0),
    ];
}

public sealed partial class BalanceSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => (Stiffness, Amplitude) = (1, 270) },
        new(Clip(2)) { Enter = () => Amplitude = 150 },
        Quiz(3, answer: 1),
        TryIt(4, () => Stiffness >= 1.01, () => Stiffness = 1.02),
        new(Clip(5)) { Enter = () => Stiffness = 1 },
    ];
}

public sealed partial class RotorSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => (Activity, Reverser) = (0.6, true) },
        new(Clip(2)),
        TryIt(3, () => !Reverser, () => Reverser = false),
        Quiz(4, answer: 2),
        new(Clip(5)) { Enter = () => Reverser = true },
    ];
}

public sealed partial class MovementSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => (Explode, Hidden) = (0, 0) },
        new(Clip(2)) { Enter = () => Explode = 0.8 },
        new(Clip(3)) { Enter = () => Hidden = 1 << (int)MovementLayer.Dial },
        TryIt(4, () => !IsShown(MovementLayer.MotionWorks), () => Show(MovementLayer.MotionWorks, false)),
        new(Clip(5)) { Enter = () => Hidden = 0 },
    ];
}
