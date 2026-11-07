using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Boats;

/// <summary>Builds the Boats chapter's pages: a (text, picture) pair for each spread.</summary>
public static class BoatsChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var boats = new BoatsViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < boats.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new BoatsTextPageViewModel(page, navigator, chapter, number, boats, i),
                new BoatsDiagramPageViewModel(page + 1, navigator, chapter, boats, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Boats chapter: the spread's words and its picture's settings.</summary>
public sealed class BoatsTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, BoatsViewModel boats, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public BoatsViewModel Boats { get; } = boats;

    public BoatsSpread Spread { get; } = boats.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Boats_SpreadOf", Title, SpreadIndex + 1, Boats.Spreads.Count);

    public IChapterContent Content => Boats;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Boats chapter: the spread's picture.</summary>
public sealed class BoatsDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, BoatsViewModel boats, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public BoatsViewModel Boats { get; } = boats;

    public BoatsSpread Spread { get; } = boats.Spreads[spread];

    public IChapterContent Content => Boats;
}

public partial class BoatsTextPageView : ChapterPageView
{
    public BoatsTextPageView()
    {
        InitializeComponent();
    }
}

public partial class BoatsDiagramPageView : ChapterPageView
{
    public BoatsDiagramPageView()
    {
        InitializeComponent();
    }
}
