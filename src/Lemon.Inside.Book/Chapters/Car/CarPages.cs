using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.Car;

/// <summary>Builds the Car chapter's pages: a (text, picture) pair for each spread.</summary>
public static class CarChapter
{
    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, INarrationService narration)
    {
        var car = new CarViewModel(narration);
        var pages = new List<(PageViewModel, PageViewModel)>();
        for (var i = 0; i < car.Spreads.Count; i++)
        {
            var page = textPage + (2 * i);
            pages.Add((
                new CarTextPageViewModel(page, navigator, chapter, number, car, i),
                new CarDiagramPageViewModel(page + 1, navigator, chapter, car, i)));
        }

        return pages;
    }
}

/// <summary>A left page of the Car chapter: the spread's words and its picture's settings.</summary>
public sealed class CarTextPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, int number, CarViewModel car, int spread)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public CarViewModel Car { get; } = car;

    public CarSpread Spread { get; } = car.Spreads[spread];

    /// <summary>Which spread of the chapter this is, from 0.</summary>
    public int SpreadIndex { get; } = spread;

    /// <summary>The chapter's first spread shows the chapter's number and title above the spread's own.</summary>
    public bool IsFirstSpread => SpreadIndex == 0;

    public string SpreadLabel => LocalizationService.Instance.Format("Car_SpreadOf", Title, SpreadIndex + 1, Car.Spreads.Count);

    public IChapterContent Content => Car;

    public override string Caption => LocalizationService.Instance.Format("Page_Caption_Chapter", Number, $"{Title} · {Spread.Title}");

    protected override void OnCultureChanged()
    {
        base.OnCultureChanged();
        OnPropertyChanged(nameof(SpreadLabel));
    }
}

/// <summary>A right page of the Car chapter: the spread's picture.</summary>
public sealed class CarDiagramPageViewModel(
    int index, IBookNavigator navigator, Chapter chapter, CarViewModel car, int spread)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public CarViewModel Car { get; } = car;

    public CarSpread Spread { get; } = car.Spreads[spread];

    public IChapterContent Content => Car;
}

public partial class CarTextPageView : ChapterPageView
{
    public CarTextPageView()
    {
        InitializeComponent();
    }
}

public partial class CarDiagramPageView : ChapterPageView
{
    public CarDiagramPageView()
    {
        InitializeComponent();
    }
}
