using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Lifting;

/// <summary>
/// The Lifting chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class LiftingViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public LiftingViewModel(INarrationService? narration = null) => Attach(narration);

    public SeesawSpread Seesaw { get; } = new();

    public PulleySpread Pulley { get; } = new();

    public TackleSpread Tackle { get; } = new();

    public WinchSpread Winch { get; } = new();

    public JackSpread Jack { get; } = new();

    public ElevatorSpread Elevator { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<LiftingSpread> Spreads => [Seesaw, Pulley, Tackle, Winch, Jack, Elevator];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
