using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Bridges;

/// <summary>Builds the Bridges chapter's pages: a (text, picture) pair for each spread.</summary>
public static class BridgesChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var bridges = new BridgesViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < bridges.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new BridgesTextPageViewModel(page, navigator, chapter, number, bridges, i),
                new BridgesDiagramPageViewModel(page + 1, navigator, chapter, bridges, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Bridges chapter: the spread's words and its picture's settings.</summary>
public sealed class BridgesTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, BridgesViewModel bridges, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public BridgesViewModel Bridges { get; } = bridges;

    public BridgesSpread Spread { get; } = bridges.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Bridges_SpreadOf", Title, SpreadIndex + 1, Bridges.Spreads.Count);

    public IChapterContent Content => Bridges;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Bridges chapter: the spread's picture.</summary>
public sealed class BridgesDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, BridgesViewModel bridges, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public BridgesViewModel Bridges { get; } = bridges;

    public BridgesSpread Spread { get; } = bridges.Spreads[spread];

    public IChapterContent Content => Bridges;
}

public partial class BridgesTextPageView : ChapterPageView
{
    public BridgesTextPageView()
    {
        InitializeComponent();
    }
}

public partial class BridgesDiagramPageView : ChapterPageView
{
    public BridgesDiagramPageView()
    {
        InitializeComponent();
    }
}
