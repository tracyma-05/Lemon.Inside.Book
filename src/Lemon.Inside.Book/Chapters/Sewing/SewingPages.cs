using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Sewing;

/// <summary>Builds the Sewing chapter's pages: a (text, picture) pair for each spread.</summary>
public static class SewingChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var sewing = new SewingViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < sewing.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new SewingTextPageViewModel(page, navigator, chapter, number, sewing, i),
                new SewingDiagramPageViewModel(page + 1, navigator, chapter, sewing, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Sewing chapter: the spread's words and its picture's settings.</summary>
public sealed class SewingTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, SewingViewModel sewing, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public SewingViewModel Sewing { get; } = sewing;

    public SewingSpread Spread { get; } = sewing.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Sewing_SpreadOf", Title, SpreadIndex + 1, Sewing.Spreads.Count);

    public IChapterContent Content => Sewing;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Sewing chapter: the spread's picture.</summary>
public sealed class SewingDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, SewingViewModel sewing, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public SewingViewModel Sewing { get; } = sewing;

    public SewingSpread Spread { get; } = sewing.Spreads[spread];

    public IChapterContent Content => Sewing;
}

public partial class SewingTextPageView : ChapterPageView
{
    public SewingTextPageView()
    {
        InitializeComponent();
    }
}

public partial class SewingDiagramPageView : ChapterPageView
{
    public SewingDiagramPageView()
    {
        InitializeComponent();
    }
}
