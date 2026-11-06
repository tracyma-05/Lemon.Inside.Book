using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>Builds the Light and shadow chapter's pages: a (text, picture) pair for each spread.</summary>
public static class LightShadowChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var light = new LightShadowViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < light.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new LightShadowTextPageViewModel(page, navigator, chapter, number, light, i),
                new LightShadowDiagramPageViewModel(page + 1, navigator, chapter, light, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Light and shadow chapter: the spread's words and its picture's settings.</summary>
public sealed class LightShadowTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, LightShadowViewModel light, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public LightShadowViewModel Light { get; } = light;

    public LightSpread Spread { get; } = light.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Light_SpreadOf", Title, SpreadIndex + 1, Light.Spreads.Count);

    public IChapterContent Content => Light;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Light and shadow chapter: the spread's picture.</summary>
public sealed class LightShadowDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, LightShadowViewModel light, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public LightShadowViewModel Light { get; } = light;

    public LightSpread Spread { get; } = light.Spreads[spread];

    public IChapterContent Content => Light;
}

public partial class LightShadowTextPageView : ChapterPageView
{
    public LightShadowTextPageView()
    {
        InitializeComponent();
    }
}

public partial class LightShadowDiagramPageView : ChapterPageView
{
    public LightShadowDiagramPageView()
    {
        InitializeComponent();
    }
}
