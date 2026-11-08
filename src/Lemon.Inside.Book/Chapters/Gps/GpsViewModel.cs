using Lemon.Book.Shell.Explore.Narration;

namespace Lemon.Inside.Book.Chapters.Gps;

/// <summary>
/// The Gps chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class GpsViewModel : SpreadChapter
{
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    public GpsViewModel(INarrationService? narration = null) => Attach(narration);

    public EchoSpread Echo { get; } = new();

    public FixSpread Fix { get; } = new();

    public ClockSpread Clock { get; } = new();

    public DriftSpread Drift { get; } = new();

    public OrbitSpread Orbit { get; } = new();

    public AngleSpread Angle { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<GpsSpread> Spreads => [Echo, Fix, Clock, Drift, Orbit, Angle];

    public override IReadOnlyList<Spread> AllSpreads => Spreads;
}
