using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Gps;

/// <summary>Builds the Gps chapter's pages: a (text, picture) pair for each spread.</summary>
public static class GpsChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var gps = new GpsViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < gps.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new GpsTextPageViewModel(page, navigator, chapter, number, gps, i),
                new GpsDiagramPageViewModel(page + 1, navigator, chapter, gps, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Gps chapter: the spread's words and its picture's settings.</summary>
public sealed class GpsTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, GpsViewModel gps, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public GpsViewModel Gps { get; } = gps;

    public GpsSpread Spread { get; } = gps.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Gps_SpreadOf", Title, SpreadIndex + 1, Gps.Spreads.Count);

    public IChapterContent Content => Gps;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Gps chapter: the spread's picture.</summary>
public sealed class GpsDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, GpsViewModel gps, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public GpsViewModel Gps { get; } = gps;

    public GpsSpread Spread { get; } = gps.Spreads[spread];

    public IChapterContent Content => Gps;
}

public partial class GpsTextPageView : ChapterPageView
{
    public GpsTextPageView()
    {
        InitializeComponent();
    }
}

public partial class GpsDiagramPageView : ChapterPageView
{
    public GpsDiagramPageView()
    {
        InitializeComponent();
    }
}
