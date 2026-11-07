using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Bicycle;

/// <summary>Builds the Bicycle chapter's pages: a (text, picture) pair for each spread.</summary>
public static class BicycleChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var bicycle = new BicycleViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < bicycle.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new BicycleTextPageViewModel(page, navigator, chapter, number, bicycle, i),
                new BicycleDiagramPageViewModel(page + 1, navigator, chapter, bicycle, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Bicycle chapter: the spread's words and its picture's settings.</summary>
public sealed class BicycleTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, BicycleViewModel bicycle, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public BicycleViewModel Bicycle { get; } = bicycle;

    public BicycleSpread Spread { get; } = bicycle.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Bicycle_SpreadOf", Title, SpreadIndex + 1, Bicycle.Spreads.Count);

    public IChapterContent Content => Bicycle;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Bicycle chapter: the spread's picture.</summary>
public sealed class BicycleDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, BicycleViewModel bicycle, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public BicycleViewModel Bicycle { get; } = bicycle;

    public BicycleSpread Spread { get; } = bicycle.Spreads[spread];

    public IChapterContent Content => Bicycle;
}

public partial class BicycleTextPageView : ChapterPageView
{
    public BicycleTextPageView()
    {
        InitializeComponent();
    }
}

public partial class BicycleDiagramPageView : ChapterPageView
{
    public BicycleDiagramPageView()
    {
        InitializeComponent();
    }
}
