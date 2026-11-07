using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Weighing;

/// <summary>Builds the Weighing chapter's pages: a (text, picture) pair for each spread.</summary>
public static class WeighingChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var weighing = new WeighingViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < weighing.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new WeighingTextPageViewModel(page, navigator, chapter, number, weighing, i),
                new WeighingDiagramPageViewModel(page + 1, navigator, chapter, weighing, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Weighing chapter: the spread's words and its picture's settings.</summary>
public sealed class WeighingTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, WeighingViewModel weighing, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public WeighingViewModel Weighing { get; } = weighing;

    public WeighingSpread Spread { get; } = weighing.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Weighing_SpreadOf", Title, SpreadIndex + 1, Weighing.Spreads.Count);

    public IChapterContent Content => Weighing;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Weighing chapter: the spread's picture.</summary>
public sealed class WeighingDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, WeighingViewModel weighing, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public WeighingViewModel Weighing { get; } = weighing;

    public WeighingSpread Spread { get; } = weighing.Spreads[spread];

    public IChapterContent Content => Weighing;
}

public partial class WeighingTextPageView : ChapterPageView
{
    public WeighingTextPageView()
    {
        InitializeComponent();
    }
}

public partial class WeighingDiagramPageView : ChapterPageView
{
    public WeighingDiagramPageView()
    {
        InitializeComponent();
    }
}
