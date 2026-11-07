using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Locks;

/// <summary>Builds the Locks chapter's pages: a (text, picture) pair for each spread.</summary>
public static class LocksChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var locks = new LocksViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < locks.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new LocksTextPageViewModel(page, navigator, chapter, number, locks, i),
                new LocksDiagramPageViewModel(page + 1, navigator, chapter, locks, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Locks chapter: the spread's words and its picture's settings.</summary>
public sealed class LocksTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, LocksViewModel locks, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public LocksViewModel Locks { get; } = locks;

    public LocksSpread Spread { get; } = locks.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Locks_SpreadOf", Title, SpreadIndex + 1, Locks.Spreads.Count);

    public IChapterContent Content => Locks;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Locks chapter: the spread's picture.</summary>
public sealed class LocksDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, LocksViewModel locks, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public LocksViewModel Locks { get; } = locks;

    public LocksSpread Spread { get; } = locks.Spreads[spread];

    public IChapterContent Content => Locks;
}

public partial class LocksTextPageView : ChapterPageView
{
    public LocksTextPageView()
    {
        InitializeComponent();
    }
}

public partial class LocksDiagramPageView : ChapterPageView
{
    public LocksDiagramPageView()
    {
        InitializeComponent();
    }
}
