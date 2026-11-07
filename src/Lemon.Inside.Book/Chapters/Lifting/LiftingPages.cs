using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Lifting;

/// <summary>Builds the Lifting chapter's pages: a (text, picture) pair for each spread.</summary>
public static class LiftingChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var lifting = new LiftingViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < lifting.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new LiftingTextPageViewModel(page, navigator, chapter, number, lifting, i),
                new LiftingDiagramPageViewModel(page + 1, navigator, chapter, lifting, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Lifting chapter: the spread's words and its picture's settings.</summary>
public sealed class LiftingTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, LiftingViewModel lifting, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public LiftingViewModel Lifting { get; } = lifting;

    public LiftingSpread Spread { get; } = lifting.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Lifting_SpreadOf", Title, SpreadIndex + 1, Lifting.Spreads.Count);

    public IChapterContent Content => Lifting;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Lifting chapter: the spread's picture.</summary>
public sealed class LiftingDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, LiftingViewModel lifting, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public LiftingViewModel Lifting { get; } = lifting;

    public LiftingSpread Spread { get; } = lifting.Spreads[spread];

    public IChapterContent Content => Lifting;
}

public partial class LiftingTextPageView : ChapterPageView
{
    public LiftingTextPageView()
    {
        InitializeComponent();
    }
}

public partial class LiftingDiagramPageView : ChapterPageView
{
    public LiftingDiagramPageView()
    {
        InitializeComponent();
    }
}
