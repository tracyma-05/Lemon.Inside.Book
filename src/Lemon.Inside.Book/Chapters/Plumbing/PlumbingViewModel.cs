using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Plumbing;

/// <summary>
/// The Plumbing chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class PlumbingViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public PlumbingViewModel(INarrationService? narration = null) => Attach(narration);

    public TowerSpread Tower { get; } = new();

    public TapSpread Tap { get; } = new();

    public SiphonSpread Siphon { get; } = new();

    public CisternSpread Cistern { get; } = new();

    public TrapSpread Trap { get; } = new();

    public PumpSpread Pump { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<PlumbingSpread> Spreads => [Tower, Tap, Siphon, Cistern, Trap, Pump];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
