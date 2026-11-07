using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Weighing;

/// <summary>
/// The Weighing chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class WeighingViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public WeighingViewModel(INarrationService? narration = null) => Attach(narration);

    public BalanceSpread Balance { get; } = new();

    public SteelyardSpread Steelyard { get; } = new();

    public SpringSpread Spring { get; } = new();

    public RobervalSpread Roberval { get; } = new();

    public StrainSpread Strain { get; } = new();

    public MoonSpread Moon { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<WeighingSpread> Spreads => [Balance, Steelyard, Spring, Roberval, Strain, Moon];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
