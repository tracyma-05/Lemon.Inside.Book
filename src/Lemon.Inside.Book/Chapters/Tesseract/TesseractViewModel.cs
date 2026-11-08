using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Tesseract;

/// <summary>
/// The Tesseract chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class TesseractViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public TesseractViewModel(INarrationService? narration = null) => Attach(narration);

    public BuildSpread Build { get; } = new();

    public ShadowSpread Shadow { get; } = new();

    public NestedSpread Nested { get; } = new();

    public SpinSpread Spin { get; } = new();

    public SliceSpread Slice { get; } = new();

    public NetSpread Net { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<TesseractSpread> Spreads => [Build, Shadow, Nested, Spin, Slice, Net];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
