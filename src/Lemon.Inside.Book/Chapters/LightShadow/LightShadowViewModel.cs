using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Infrastructures.Localization;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// The Light and shadow chapter: its spreads, in reading order, each with the settings of its own picture. One
/// instance serves every page of the chapter.
/// </summary>
public sealed class LightShadowViewModel : IChapterContent
{
    private int _views;

    public LightShadowViewModel()
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

    public BrightnessSpread Brightness { get; } = new();

    public TiltSpread Tilt { get; } = new();

    public DistanceSpread Distance { get; } = new();

    public AngleSpread Angle { get; } = new();

    public SkySpread Sky { get; } = new();

    public RadianceSpread Radiance { get; } = new();

    public ReflectionSpread Reflection { get; } = new();

    public ShadowSpread Shadow { get; } = new();

    public BounceSpread Bounce { get; } = new();

    /// <summary>The spreads in reading order.</summary>
    public IReadOnlyList<LightSpread> Spreads => [Brightness, Tilt, Distance, Angle, Sky, Radiance, Reflection, Shadow, Bounce];

    /// <summary>A page of the chapter came on screen.</summary>
    public void ViewAttached() => _views++;

    /// <summary>A page of the chapter left the screen.</summary>
    public void ViewDetached() => _views = Math.Max(0, _views - 1);
}
