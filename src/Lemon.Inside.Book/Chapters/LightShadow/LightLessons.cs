using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.LightShadow;

// The Light and shadow chapter's spoken walkthroughs, one per spread. Each sets the picture up as it speaks, then
// leaves one thing for the reader to try or one question to answer. Clips are Narr_Light_<Key>_NN.

public sealed partial class BrightnessSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "lamp", () => Power = 5),
        Say(2, "floor", () => Power = 10),
        Say(3, "chart", () => Power = 80),
        Quiz(4, answer: 1, highlight: "chart"),
        TryIt(5, () => Power >= 140, () => Power = MaxPower, highlight: "floor"),
    ];
}

public sealed partial class TiltSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "patch", () => Tilt = 0),
        Say(2, "patch", () => Tilt = 45),
        TryIt(3, () => Math.Abs(Tilt - 60) <= 3, () => Tilt = 60, highlight: "board"),
        Quiz(4, answer: 2, highlight: "patch"),
    ];
}

public sealed partial class DistanceSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "square", () => Distance = 1),
        Say(2, "frame", () => Distance = 2),
        Say(3, "frame", () => Distance = 3),
        Quiz(4, answer: 0, highlight: "frame"),
    ];
}

public sealed partial class AngleSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "car", () => Distance = 5),
        Say(2, "arc", () => Distance = 10),
        TryIt(3, () => Distance >= 19.5 && Distance <= 20.5, () => Distance = 20, highlight: "car"),
        Say(4, "view"),
    ];
}

public sealed partial class SkySpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "spot", () => (Elevation, Distance) = (90, 2.5)),
        Say(2, "patch"),
        Say(3, "patch", () => Elevation = 20),
        TryIt(4, () => Distance >= 4.5, () => Distance = 5, highlight: "lamp"),
        Quiz(5, answer: 1, highlight: "patch"),
    ];
}

public sealed partial class RadianceSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "panel", () => (Distance, Tilt) = (2, 0)),
        Say(2, "seen", () => Distance = 4),
        TryIt(3, () => Tilt >= 50, () => Tilt = 60, highlight: "panel"),
        Quiz(4, answer: 0, highlight: "readout"),
    ];
}

public sealed partial class ReflectionSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "mirror", () => (LightAngle, Roughness, EyeAngle) = (40, 0, 40)),
        Say(2, "eye", () => EyeAngle = -20),
        Say(3, "lobe", () => Roughness = 1),
        TryIt(4, () => Roughness is > 0.15 and < 0.5, () => Roughness = 0.3, highlight: "lobe"),
        Quiz(5, answer: 2, highlight: "lobe"),
    ];
}

public sealed partial class ShadowSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "edge", () => (LightHeight, LightSize, LightX) = (2.5, 0.05, 0.6)),
        Say(2, "edge", () => LightSize = 1.5),
        TryIt(3, () => LightHeight >= 3.8, () => LightHeight = 4, highlight: "lamp"),
        Quiz(4, answer: 1, highlight: "edge"),
    ];
}

public sealed partial class BounceSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "box", () => (Bounces, Wall) = (0, WallColor.Red)),
        Say(2, "floor", () => Bounces = 1),
        TryIt(3, () => Wall == WallColor.Blue, () => Wall = WallColor.Blue, highlight: "wall"),
        Say(4, "box", () => Bounces = 3),
        Quiz(5, answer: 0),
    ];
}
