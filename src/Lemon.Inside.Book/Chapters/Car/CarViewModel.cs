using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Car;

/// <summary>
/// The Car chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class CarViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public CarViewModel(INarrationService? narration = null) => Attach(narration);

    public CrankSpread Crank { get; } = new();

    public CamSpread Cam { get; } = new();

    public CylindersSpread Cylinders { get; } = new();

    public ClutchSpread Clutch { get; } = new();

    public BendSpread Bend { get; } = new();

    public IceSpread Ice { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<CarSpread> Spreads => [Crank, Cam, Cylinders, Clutch, Bend, Ice];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
