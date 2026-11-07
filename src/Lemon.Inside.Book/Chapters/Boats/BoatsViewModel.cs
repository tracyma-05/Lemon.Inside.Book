using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Boats;

/// <summary>
/// The Boats chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class BoatsViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public BoatsViewModel(INarrationService? narration = null) => Attach(narration);

    public FloatSpread Float { get; } = new();

    public PressureSpread Pressure { get; } = new();

    public HullSpread Hull { get; } = new();

    public HeelSpread Heel { get; } = new();

    public StabilitySpread Stability { get; } = new();

    public SubmarineSpread Submarine { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<BoatsSpread> Spreads => [Float, Pressure, Hull, Heel, Stability, Submarine];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
