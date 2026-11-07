using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Bicycle;

// The Bicycle chapter's spoken walkthroughs, one per spread, and the parts of each picture
// that can be clicked. Clips are Narr_Bicycle_<Key>_NN; parts are explained by Narr_Bicycle_<Key>_Part_<Id>.

public sealed partial class WheelSpread
{
    public override IReadOnlyList<string> Parts => ["hub", "rim", "bottom", "top", "load"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "hub", () => (Tension, Load) = (1000, 0)),
        Say(2, "load", () => Load = 80),
        Say(3, "bottom"),
        TryIt(4, () => IsSlack, () => (Tension, Load) = (150, 120), highlight: "bottom"),
        Quiz(5, answer: 0, highlight: "top"),
    ];
}

public sealed partial class ChainSpread
{
    public override IReadOnlyList<string> Parts => ["pedal", "chainring", "chain", "sprocket", "wheel"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "pedal", () => (Chainring, Sprocket) = (44, 22)),
        Say(2, "chain"),
        Say(3, "sprocket", () => Sprocket = 11),
        TryIt(4, () => Math.Abs(Ratio - 3) < 1e-9, () => (Chainring, Sprocket) = (48, 16), highlight: "wheel"),
        Quiz(5, answer: 1, highlight: "chainring"),
    ];
}

public sealed partial class HillSpread
{
    public override IReadOnlyList<string> Parts => ["cassette", "rider", "slope", "push"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "rider", () => (Slope, Gear) = (0, 2)),
        Say(2, "push", () => Slope = 8),
        TryIt(3, () => IsComfortable, () => Gear = Climb.Cassette.Count - 1, highlight: "cassette"),
        Say(4, "slope"),
        Quiz(5, answer: 0, highlight: "cassette"),
    ];
}

public sealed partial class FreewheelSpread
{
    public override IReadOnlyList<string> Parts => ["driver", "pawl", "ratchet", "spring"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "driver", () => PedalSpeed = 0.6),
        Say(2, "pawl"),
        TryIt(3, () => PedalSpeed <= 0.05, () => PedalSpeed = 0, highlight: "pawl"),
        Say(4, "ratchet"),
        Quiz(5, answer: 0, highlight: "pawl"),
    ];
}

public sealed partial class BrakeSpread
{
    public override IReadOnlyList<string> Parts => ["pads", "front", "rear", "centre", "distance"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "pads", () => (Use, Speed, Strength) = (BrakeUse.Both, 20, 0.3)),
        TryIt(2, () => Speed >= 39.5, () => Speed = 40, highlight: "distance"),
        Say(3, "centre", () => Strength = 0.5),
        Say(4, "rear", () => Use = BrakeUse.Rear),
        Quiz(5, answer: 0, highlight: "front"),
    ];
}

public sealed partial class SteerSpread
{
    public override IReadOnlyList<string> Parts => ["axis", "contact", "trail", "fork", "top"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "axis", () => (HeadAngle, Offset) = (72, 45)),
        Say(2, "trail"),
        Say(3, "top"),
        TryIt(4, () => Trail < 0, () => (HeadAngle, Offset) = (80, 80), highlight: "fork"),
        Quiz(5, answer: 0, highlight: "contact"),
    ];
}
