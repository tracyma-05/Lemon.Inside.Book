using Lemon.Book.Shell.Explore.Lessons;
using Lemon.Inside.Book.Controls.Mechanics;

namespace Lemon.Inside.Book.Chapters.Gears;

// The Gears chapter's spoken walkthroughs, one per spread. Clips are Narr_Gears_<Key>_NN.

public sealed partial class RatioSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => (DriverTeeth, DrivenTeeth) = (20, 20) },
        new(Clip(2)) { Enter = () => DrivenTeeth = 40 },
        TryIt(3, () => DrivenTeeth == 3 * DriverTeeth, () => (DriverTeeth, DrivenTeeth) = (12, 36)),
        Quiz(4, answer: 2),
    ];
}

public sealed partial class TorqueSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => DrivenTeeth = 24 },
        new(Clip(2)) { Enter = () => DrivenTeeth = 48 },
        Quiz(3, answer: 0),
        new(Clip(4)),
    ];
}

public sealed partial class InvoluteSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => Unwound = 0 },
        TryIt(2, () => Unwound >= 180, () => Unwound = 200),
        new(Clip(3)),
        new(Clip(4)),
    ];
}

public sealed partial class MeshSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => (PressureAngle, Speed) = (20, 0.15) },
        new(Clip(2)),
        new(Clip(3)) { Enter = () => Speed = 0.5 },
        TryIt(4, () => PressureAngle >= 27, () => PressureAngle = 28),
        Quiz(5, answer: 1),
    ];
}

public sealed partial class TrainSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => Steps = 1 },
        new(Clip(2)) { Enter = () => Steps = 2 },
        Quiz(3, answer: 1),
        TryIt(4, () => Steps == 4, () => Steps = 4),
    ];
}

public sealed partial class PlanetarySpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => (Held, SunStep) = (PlanetHeld.Ring, 2) },
        new(Clip(2)),
        TryIt(3, () => Held == PlanetHeld.Carrier, () => Held = PlanetHeld.Carrier),
        new(Clip(4)) { Enter = () => Held = PlanetHeld.Sun },
        Quiz(5, answer: 0),
    ];
}
