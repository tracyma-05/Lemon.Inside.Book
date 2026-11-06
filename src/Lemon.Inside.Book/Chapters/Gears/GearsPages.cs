using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Gears;

/// <summary>Builds the Gears chapter's pages: a (text, picture) pair for each spread.</summary>
public static class GearsChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var gears = new GearsViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < gears.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new GearsTextPageViewModel(page, navigator, chapter, number, gears, i),
                new GearsDiagramPageViewModel(page + 1, navigator, chapter, gears, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Gears chapter: the spread's words and its picture's settings.</summary>
public sealed class GearsTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, GearsViewModel gears, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public GearsViewModel Gears { get; } = gears;

    public GearSpread Spread { get; } = gears.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Gears_SpreadOf", Title, SpreadIndex + 1, Gears.Spreads.Count);

    public IChapterContent Content => Gears;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Gears chapter: the spread's picture.</summary>
public sealed class GearsDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, GearsViewModel gears, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public GearsViewModel Gears { get; } = gears;

    public GearSpread Spread { get; } = gears.Spreads[spread];

    public IChapterContent Content => Gears;
}

public partial class GearsTextPageView : ChapterPageView
{
    public GearsTextPageView()
    {
        InitializeComponent();
    }
}

public partial class GearsDiagramPageView : ChapterPageView
{
    public GearsDiagramPageView()
    {
        InitializeComponent();
    }
}
