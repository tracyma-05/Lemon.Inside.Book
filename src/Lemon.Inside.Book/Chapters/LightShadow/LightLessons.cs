using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.LightShadow;

// The Light and shadow chapter's spoken walkthroughs, one per spread. Each sets the picture up as it speaks, then
// leaves one thing for the reader to try or one question to answer. Clips are Narr_Light_<Key>_NN.

public sealed partial class BrightnessSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => Power = 5 },
        new(Clip(2)) { Enter = () => Power = 10 },
        new(Clip(3)) { Enter = () => Power = 80 },
        Quiz(4, answer: 1),
        TryIt(5, () => Power >= 140, () => Power = MaxPower),
    ];
}

public sealed partial class TiltSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => Tilt = 0 },
        new(Clip(2)) { Enter = () => Tilt = 45 },
        TryIt(3, () => Math.Abs(Tilt - 60) <= 3, () => Tilt = 60),
        Quiz(4, answer: 2),
    ];
}

public sealed partial class DistanceSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => Distance = 1 },
        new(Clip(2)) { Enter = () => Distance = 2 },
        new(Clip(3)) { Enter = () => Distance = 3 },
        Quiz(4, answer: 0),
    ];
}

public sealed partial class AngleSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => Distance = 5 },
        new(Clip(2)) { Enter = () => Distance = 10 },
        TryIt(3, () => Distance >= 19.5 && Distance <= 20.5, () => Distance = 20),
        new(Clip(4)),
    ];
}

public sealed partial class SkySpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => (Elevation, Distance) = (90, 2.5) },
        new(Clip(2)),
        new(Clip(3)) { Enter = () => Elevation = 20 },
        TryIt(4, () => Distance >= 4.5, () => Distance = 5),
        Quiz(5, answer: 1),
    ];
}

public sealed partial class RadianceSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => (Distance, Tilt) = (2, 0) },
        new(Clip(2)) { Enter = () => Distance = 4 },
        TryIt(3, () => Tilt >= 50, () => Tilt = 60),
        Quiz(4, answer: 0),
    ];
}

public sealed partial class ReflectionSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => (LightAngle, Roughness, EyeAngle) = (40, 0, 40) },
        new(Clip(2)) { Enter = () => EyeAngle = -20 },
        new(Clip(3)) { Enter = () => Roughness = 1 },
        TryIt(4, () => Roughness is > 0.15 and < 0.5, () => Roughness = 0.3),
        Quiz(5, answer: 2),
    ];
}

public sealed partial class ShadowSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => (LightHeight, LightSize, LightX) = (2.5, 0.05, 0.6) },
        new(Clip(2)) { Enter = () => LightSize = 1.5 },
        TryIt(3, () => LightHeight >= 3.8, () => LightHeight = 4),
        Quiz(4, answer: 1),
    ];
}

public sealed partial class BounceSpread
{
    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        new(Clip(1)) { Enter = () => (Bounces, Wall) = (0, WallColor.Red) },
        new(Clip(2)) { Enter = () => Bounces = 1 },
        TryIt(3, () => Wall == WallColor.Blue, () => Wall = WallColor.Blue),
        new(Clip(4)) { Enter = () => Bounces = 3 },
        Quiz(5, answer: 0),
    ];
}
