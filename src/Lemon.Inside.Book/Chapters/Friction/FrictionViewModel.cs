using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Friction;

/// <summary>
/// The Friction chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class FrictionViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public FrictionViewModel(INarrationService? narration = null) => Attach(narration);

    public RampSpread Ramp { get; } = new();

    public StartSpread Start { get; } = new();

    public SkidSpread Skid { get; } = new();

    public CapstanSpread Capstan { get; } = new();

    public RollingSpread Rolling { get; } = new();

    public OilSpread Oil { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<FrictionSpread> Spreads => [Ramp, Start, Skid, Capstan, Rolling, Oil];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
