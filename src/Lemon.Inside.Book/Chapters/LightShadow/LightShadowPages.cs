using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>The Light and shadow spread's left page: what the picture shows and the lamp's settings.</summary>
public sealed class LightShadowTextPageViewModel(int index, IBookNavigator navigator, Chapter chapter, int number, LightShadowViewModel light)
    : ChapterTextPageViewModel(index, navigator, chapter, number), IChapterPage
{
    public LightShadowViewModel Light { get; } = light;

    public IChapterContent Content => Light;
}

/// <summary>The Light and shadow spread's right page: the lamp, the floor and the box, to turn round.</summary>
public sealed class LightShadowDiagramPageViewModel(int index, IBookNavigator navigator, Chapter chapter, LightShadowViewModel light)
    : ChapterDiagramPageViewModel(index, navigator, chapter), IChapterPage
{
    public LightShadowViewModel Light { get; } = light;

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
