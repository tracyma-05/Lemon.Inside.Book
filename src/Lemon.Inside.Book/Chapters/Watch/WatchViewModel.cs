using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Infrastructures.Localization;

namespace Lemon.Inside.Book.Chapters.Watch;

/// <summary>
/// The mechanical watch chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class WatchViewModel : IChapterContent
{
    private int _views;

    public WatchViewModel()
    {
        // Values are worded in the reader's language: say them again when it changes.
        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            foreach (var spread in Spreads)
            {
                spread.OnLanguageChanged();
            }
        };
    }

    public SpringSpread Spring { get; } = new();

    public HandsSpread Hands { get; } = new();

    public EscapementSpread Escapement { get; } = new();

    public BalanceSpread Balance { get; } = new();

    public RotorSpread Rotor { get; } = new();

    public MovementSpread Movement { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<WatchSpread> Spreads => [Spring, Hands, Escapement, Balance, Rotor, Movement];

    /// <summary>A page of the chapter came on screen.</summary>
    public void ViewAttached() => _views++;

    /// <summary>A page of the chapter left the screen.</summary>
    public void ViewDetached() => _views = Math.Max(0, _views - 1);
}
