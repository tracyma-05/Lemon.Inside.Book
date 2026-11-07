using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Bridges;

/// <summary>
/// The Bridges chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class BridgesViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public BridgesViewModel(INarrationService? narration = null) => Attach(narration);

    public BeamSpread Beam { get; } = new();

    public FrameSpread Frame { get; } = new();

    public TrussSpread Truss { get; } = new();

    public ArchSpread Arch { get; } = new();

    public CableSpread Cable { get; } = new();

    public ExpansionSpread Expansion { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<BridgesSpread> Spreads => [Beam, Frame, Truss, Arch, Cable, Expansion];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
