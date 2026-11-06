using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Watch;

/// <summary>
/// The mechanical watch chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class WatchViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public WatchViewModel(INarrationService? narration = null) => Attach(narration);

    public SpringSpread Spring { get; } = new();

    public HandsSpread Hands { get; } = new();

    public EscapementSpread Escapement { get; } = new();

    public BalanceSpread Balance { get; } = new();

    public RotorSpread Rotor { get; } = new();

    public MovementSpread Movement { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<WatchSpread> Spreads => [Spring, Hands, Escapement, Balance, Rotor, Movement];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
