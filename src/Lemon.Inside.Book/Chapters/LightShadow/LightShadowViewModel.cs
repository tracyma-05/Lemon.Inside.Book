using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Infrastructures.Localization;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// The Light and shadow spread (P0: the rendering spike): the lamp's height, size and place, and the renderer's
/// settings that the spike measures (samples on the lamp, picture resolution, turning by itself).
/// </summary>
public sealed partial class LightShadowViewModel : ObservableObject, IChapterContent
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LightHeightText))]
    private double _lightHeight = 2.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LightSizeText))]
    private double _lightSize = 0.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LightXText))]
    private double _lightX = 0.6;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SamplesText))]
    private int _samples = 8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RenderScaleText))]
    private double _renderScale = 0.5;

    [ObservableProperty]
    private bool _spin;

    private int _views;

    public LightShadowViewModel()
    {
        // The values are worded in the reader's language: say them again when it changes.
        LocalizationService.Instance.PropertyChanged += (_, _) => OnPropertyChanged(string.Empty);
    }

    public string LightHeightText => Metres(LightHeight);

    public string LightSizeText => Metres(LightSize);

    public string LightXText => Metres(LightX);

    public string SamplesText => string.Create(CultureInfo.InvariantCulture, $"{Samples} × {Samples}");

    public string RenderScaleText => string.Create(CultureInfo.InvariantCulture, $"{RenderScale * 100:0}%");

    /// <summary>A view of the chapter came on screen.</summary>
    public void ViewAttached() => _views++;

    /// <summary>A view of the chapter left the screen; the last one stops the turning camera.</summary>
    public void ViewDetached()
    {
        _views = Math.Max(0, _views - 1);
        if (_views == 0)
        {
            Spin = false;
        }
    }

    private static string Metres(double value) =>
        LocalizationService.Instance.Format("Light_Metres", value.ToString("0.00", CultureInfo.CurrentCulture));
}
