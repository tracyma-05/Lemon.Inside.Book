using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Canals;

/// <summary>
/// The Canals chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class CanalsViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public CanalsViewModel(INarrationService? narration = null) => Attach(narration);

    public LockSpread Lock { get; } = new();

    public GatesSpread Gates { get; } = new();

    public WaterSpread Water { get; } = new();

    public FlightSpread Flight { get; } = new();

    public LiftSpread Lift { get; } = new();

    public TowingSpread Towing { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<CanalsSpread> Spreads => [Lock, Gates, Water, Flight, Lift, Towing];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
