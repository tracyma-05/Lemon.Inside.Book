using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Canals;

/// <summary>Builds the Canals chapter's pages: a (text, picture) pair for each spread.</summary>
public static class CanalsChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var canals = new CanalsViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < canals.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new CanalsTextPageViewModel(page, navigator, chapter, number, canals, i),
                new CanalsDiagramPageViewModel(page + 1, navigator, chapter, canals, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Canals chapter: the spread's words and its picture's settings.</summary>
public sealed class CanalsTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, CanalsViewModel canals, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public CanalsViewModel Canals { get; } = canals;

    public CanalsSpread Spread { get; } = canals.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Canals_SpreadOf", Title, SpreadIndex + 1, Canals.Spreads.Count);

    public IChapterContent Content => Canals;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Canals chapter: the spread's picture.</summary>
public sealed class CanalsDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, CanalsViewModel canals, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public CanalsViewModel Canals { get; } = canals;

    public CanalsSpread Spread { get; } = canals.Spreads[spread];

    public IChapterContent Content => Canals;
}

public partial class CanalsTextPageView : ChapterPageView
{
    public CanalsTextPageView()
    {
        InitializeComponent();
    }
}

public partial class CanalsDiagramPageView : ChapterPageView
{
    public CanalsDiagramPageView()
    {
        InitializeComponent();
    }
}
