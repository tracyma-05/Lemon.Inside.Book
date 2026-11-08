using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Tesseract;

/// <summary>Builds the Tesseract chapter's pages: a (text, picture) pair for each spread.</summary>
public static class TesseractChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var tesseract = new TesseractViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < tesseract.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new TesseractTextPageViewModel(page, navigator, chapter, number, tesseract, i),
                new TesseractDiagramPageViewModel(page + 1, navigator, chapter, tesseract, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Tesseract chapter: the spread's words and its picture's settings.</summary>
public sealed class TesseractTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, TesseractViewModel tesseract, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public TesseractViewModel Tesseract { get; } = tesseract;

    public TesseractSpread Spread { get; } = tesseract.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Tesseract_SpreadOf", Title, SpreadIndex + 1, Tesseract.Spreads.Count);

    public IChapterContent Content => Tesseract;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Tesseract chapter: the spread's picture.</summary>
public sealed class TesseractDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, TesseractViewModel tesseract, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public TesseractViewModel Tesseract { get; } = tesseract;

    public TesseractSpread Spread { get; } = tesseract.Spreads[spread];

    public IChapterContent Content => Tesseract;
}

public partial class TesseractTextPageView : ChapterPageView
{
    public TesseractTextPageView()
    {
        InitializeComponent();
    }
}

public partial class TesseractDiagramPageView : ChapterPageView
{
    public TesseractDiagramPageView()
    {
        InitializeComponent();
    }
}
