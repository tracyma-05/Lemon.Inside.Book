using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Locks;

/// <summary>
/// The Locks chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class LocksViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public LocksViewModel(INarrationService? narration = null) => Attach(narration);

    public PinsSpread Pins { get; } = new();

    public KeysSpread Keys { get; } = new();

    public MasterSpread Master { get; } = new();

    public BindingSpread Binding { get; } = new();

    public LeverSpread Lever { get; } = new();

    public DialsSpread Dials { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<LocksSpread> Spreads => [Pins, Keys, Master, Binding, Lever, Dials];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
