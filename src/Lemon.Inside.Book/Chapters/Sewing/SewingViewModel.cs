using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Sewing;

/// <summary>
/// The Sewing chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class SewingViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public SewingViewModel(INarrationService? narration = null) => Attach(narration);

    public StitchSpread Stitch { get; } = new();

    public NeedleSpread Needle { get; } = new();

    public HookSpread Hook { get; } = new();

    public TakeUpSpread TakeUp { get; } = new();

    public FeedSpread Feed { get; } = new();

    public TensionSpread Tension { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<SewingSpread> Spreads => [Stitch, Needle, Hook, TakeUp, Feed, Tension];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
