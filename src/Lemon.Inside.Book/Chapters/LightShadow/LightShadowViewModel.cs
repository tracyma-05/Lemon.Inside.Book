using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// The Light and shadow chapter: its spreads, in reading order, each with the settings of its own picture. One
/// instance serves every page of the chapter.
/// </summary>
public sealed class LightShadowViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public LightShadowViewModel(INarrationService? narration = null) => Attach(narration);

    public BrightnessSpread Brightness { get; } = new();

    public TiltSpread Tilt { get; } = new();

    public DistanceSpread Distance { get; } = new();

    public AngleSpread Angle { get; } = new();

    public SkySpread Sky { get; } = new();

    public RadianceSpread Radiance { get; } = new();

    public ReflectionSpread Reflection { get; } = new();

    public ShadowSpread Shadow { get; } = new();

    public BounceSpread Bounce { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<LightSpread> Spreads => [Brightness, Tilt, Distance, Angle, Sky, Radiance, Reflection, Shadow, Bounce];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
