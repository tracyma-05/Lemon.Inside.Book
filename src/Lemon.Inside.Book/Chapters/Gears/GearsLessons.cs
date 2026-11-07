using Lemon.Book.Shell.Explore.Lessons;
using Lemon.Inside.Book.Controls.Mechanics;

namespace Lemon.Inside.Book.Chapters.Gears;

// The Gears chapter's spoken walkthroughs, one per spread, and the parts of each picture
// that can be clicked. Clips are Narr_Gears_<Key>_NN; parts are explained by Narr_Gears_<Key>_Part_<Id>.

public sealed partial class RatioSpread
{
    public override IReadOnlyList<string> Parts => ["driver", "driven"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "driver", () => (DriverTeeth, DrivenTeeth) = (20, 20)),
        Say(2, "driven", () => DrivenTeeth = 40),
        TryIt(3, () => DrivenTeeth == 3 * DriverTeeth, () => (DriverTeeth, DrivenTeeth) = (12, 36), highlight: "driven"),
        Quiz(4, answer: 2),
    ];
}

public sealed partial class TorqueSpread
{
    public override IReadOnlyList<string> Parts => ["crank", "drum", "load", "bars"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "crank", () => DrivenTeeth = 24),
        Say(2, "drum", () => DrivenTeeth = 48),
        Quiz(3, answer: 0, highlight: "bars"),
        Say(4, "load"),
    ];
}

public sealed partial class InvoluteSpread
{
    public override IReadOnlyList<string> Parts => ["can", "string", "pencil", "curve"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "can", () => Unwound = 0),
        TryIt(2, () => Unwound >= 180, () => Unwound = 200, highlight: "pencil"),
        Say(3, "string"),
        Say(4, "curve"),
    ];
}

public sealed partial class MeshSpread
{
    public override IReadOnlyList<string> Parts => ["contact", "line"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "contact", () => (PressureAngle, Speed) = (20, 0.15)),
        Say(2, "line"),
        Say(3, "contact", () => Speed = 0.5),
        TryIt(4, () => PressureAngle >= 27, () => PressureAngle = 28, highlight: "line"),
        Quiz(5, answer: 1),
    ];
}

public sealed partial class TrainSpread
{
    public override IReadOnlyList<string> Parts => ["input", "output"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "input", () => Steps = 1),
        Say(2, "output", () => Steps = 2),
        Quiz(3, answer: 1, highlight: "output"),
        TryIt(4, () => Steps == 4, () => Steps = 4, highlight: "output"),
    ];
}

public sealed partial class PlanetarySpread
{
    public override IReadOnlyList<string> Parts => ["sun", "ring", "carrier"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "sun", () => (Held, SunStep) = (PlanetHeld.Ring, 2)),
        Say(2, "ring"),
        TryIt(3, () => Held == PlanetHeld.Carrier, () => Held = PlanetHeld.Carrier, highlight: "carrier"),
        Say(4, "sun", () => Held = PlanetHeld.Sun),
        Quiz(5, answer: 0),
    ];
}
