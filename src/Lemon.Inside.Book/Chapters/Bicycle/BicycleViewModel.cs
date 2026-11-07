using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Bicycle;

/// <summary>
/// The Bicycle chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class BicycleViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public BicycleViewModel(INarrationService? narration = null) => Attach(narration);

    public WheelSpread Wheel { get; } = new();

    public ChainSpread Chain { get; } = new();

    public HillSpread Hill { get; } = new();

    public FreewheelSpread Freewheel { get; } = new();

    public BrakeSpread Brakes { get; } = new();

    public SteerSpread Steer { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<BicycleSpread> Spreads => [Wheel, Chain, Hill, Freewheel, Brakes, Steer];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
