using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Infrastructures.Localization;

namespace Lemon.Inside.Book.Chapters.Gears;

/// <summary>
/// The Gears chapter: its spreads, in reading order, each with the settings of its own picture. One instance serves
/// every page of the chapter.
/// </summary>
public sealed class GearsViewModel : IChapterContent
{
    private int _views;

    public GearsViewModel()
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

    public RatioSpread Ratio { get; } = new();

    public TorqueSpread Torque { get; } = new();

    public InvoluteSpread Involute { get; } = new();

    public MeshSpread Mesh { get; } = new();

    public TrainSpread Train { get; } = new();

    public PlanetarySpread Planetary { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<GearSpread> Spreads => [Ratio, Torque, Involute, Mesh, Train, Planetary];

    /// <summary>A page of the chapter came on screen.</summary>
    public void ViewAttached() => _views++;

    /// <summary>A page of the chapter left the screen.</summary>
    public void ViewDetached() => _views = Math.Max(0, _views - 1);
}
