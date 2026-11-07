using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Plumbing;

/// <summary>Builds the Plumbing chapter's pages: a (text, picture) pair for each spread.</summary>
public static class PlumbingChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var plumbing = new PlumbingViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < plumbing.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new PlumbingTextPageViewModel(page, navigator, chapter, number, plumbing, i),
                new PlumbingDiagramPageViewModel(page + 1, navigator, chapter, plumbing, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Plumbing chapter: the spread's words and its picture's settings.</summary>
public sealed class PlumbingTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, PlumbingViewModel plumbing, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public PlumbingViewModel Plumbing { get; } = plumbing;

    public PlumbingSpread Spread { get; } = plumbing.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Plumbing_SpreadOf", Title, SpreadIndex + 1, Plumbing.Spreads.Count);

    public IChapterContent Content => Plumbing;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Plumbing chapter: the spread's picture.</summary>
public sealed class PlumbingDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, PlumbingViewModel plumbing, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public PlumbingViewModel Plumbing { get; } = plumbing;

    public PlumbingSpread Spread { get; } = plumbing.Spreads[spread];

    public IChapterContent Content => Plumbing;
}

public partial class PlumbingTextPageView : ChapterPageView
{
    public PlumbingTextPageView()
    {
        InitializeComponent();
    }
}

public partial class PlumbingDiagramPageView : ChapterPageView
{
    public PlumbingDiagramPageView()
    {
        InitializeComponent();
    }
}
