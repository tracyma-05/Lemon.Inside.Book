using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Watch;

/// <summary>Builds the watch chapter's pages: a (text, picture) pair for each spread.</summary>
public static class WatchChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator)
    {
        var watch = new WatchViewModel();
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < watch.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new WatchTextPageViewModel(page, navigator, chapter, number, watch, i),
                new WatchDiagramPageViewModel(page + 1, navigator, chapter, watch, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the watch chapter: the spread's words and its picture's settings.</summary>
public sealed class WatchTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, WatchViewModel watch, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public WatchViewModel Watch { get; } = watch;

    public WatchSpread Spread { get; } = watch.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Watch_SpreadOf", Title, SpreadIndex + 1, Watch.Spreads.Count);

    public IChapterContent Content => Watch;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the watch chapter: the spread's picture.</summary>
public sealed class WatchDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, WatchViewModel watch, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public WatchViewModel Watch { get; } = watch;

    public WatchSpread Spread { get; } = watch.Spreads[spread];

    public IChapterContent Content => Watch;
}

public partial class WatchTextPageView : ChapterPageView
{
    public WatchTextPageView()
    {
        InitializeComponent();
    }
}

public partial class WatchDiagramPageView : ChapterPageView
{
    public WatchDiagramPageView()
    {
        InitializeComponent();
    }
}
