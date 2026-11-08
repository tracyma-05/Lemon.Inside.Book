using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Colour;

/// <summary>Builds the Colour chapter's pages: a (text, picture) pair for each spread.</summary>
public static class ColourChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var colour = new ColourViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < colour.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new ColourTextPageViewModel(page, navigator, chapter, number, colour, i),
                new ColourDiagramPageViewModel(page + 1, navigator, chapter, colour, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Colour chapter: the spread's words and its picture's settings.</summary>
public sealed class ColourTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, ColourViewModel colour, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public ColourViewModel Colour { get; } = colour;

    public ColourSpread Spread { get; } = colour.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Colour_SpreadOf", Title, SpreadIndex + 1, Colour.Spreads.Count);

    public IChapterContent Content => Colour;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Colour chapter: the spread's picture.</summary>
public sealed class ColourDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, ColourViewModel colour, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public ColourViewModel Colour { get; } = colour;

    public ColourSpread Spread { get; } = colour.Spreads[spread];

    public IChapterContent Content => Colour;
}

public partial class ColourTextPageView : ChapterPageView
{
    public ColourTextPageView()
    {
        InitializeComponent();
    }
}

public partial class ColourDiagramPageView : ChapterPageView
{
    public ColourDiagramPageView()
    {
        InitializeComponent();
    }
}
