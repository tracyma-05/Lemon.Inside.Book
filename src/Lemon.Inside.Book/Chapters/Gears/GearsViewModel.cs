using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Gears;

/// <summary>
/// The Gears chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class GearsViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public GearsViewModel(INarrationService? narration = null) => Attach(narration);

    public RatioSpread Ratio { get; } = new();

    public TorqueSpread Torque { get; } = new();

    public InvoluteSpread Involute { get; } = new();

    public MeshSpread Mesh { get; } = new();

    public TrainSpread Train { get; } = new();

    public PlanetarySpread Planetary { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<GearSpread> Spreads => [Ratio, Torque, Involute, Mesh, Train, Planetary];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
