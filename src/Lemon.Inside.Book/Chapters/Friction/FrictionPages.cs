using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Friction;

/// <summary>Builds the Friction chapter's pages: a (text, picture) pair for each spread.</summary>
public static class FrictionChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var friction = new FrictionViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < friction.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new FrictionTextPageViewModel(page, navigator, chapter, number, friction, i),
                new FrictionDiagramPageViewModel(page + 1, navigator, chapter, friction, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Friction chapter: the spread's words and its picture's settings.</summary>
public sealed class FrictionTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, FrictionViewModel friction, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public FrictionViewModel Friction { get; } = friction;

    public FrictionSpread Spread { get; } = friction.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Friction_SpreadOf", Title, SpreadIndex + 1, Friction.Spreads.Count);

    public IChapterContent Content => Friction;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Friction chapter: the spread's picture.</summary>
public sealed class FrictionDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, FrictionViewModel friction, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public FrictionViewModel Friction { get; } = friction;

    public FrictionSpread Spread { get; } = friction.Spreads[spread];

    public IChapterContent Content => Friction;
}

public partial class FrictionTextPageView : ChapterPageView
{
    public FrictionTextPageView()
    {
        InitializeComponent();
    }
}

public partial class FrictionDiagramPageView : ChapterPageView
{
    public FrictionDiagramPageView()
    {
        InitializeComponent();
    }
}
