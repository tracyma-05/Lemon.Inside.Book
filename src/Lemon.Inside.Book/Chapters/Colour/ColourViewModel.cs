using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Colour;

/// <summary>
/// The Colour chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class ColourViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public ColourViewModel(INarrationService? narration = null) => Attach(narration);

    public LampsSpread Lamps { get; } = new();

    public InksSpread Inks { get; } = new();

    public StepsSpread Steps { get; } = new();

    public BlendSpread Blend { get; } = new();

    public PanesSpread Panes { get; } = new();

    public HaloSpread Halo { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<ColourSpread> Spreads => [Lamps, Inks, Steps, Blend, Panes, Halo];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
