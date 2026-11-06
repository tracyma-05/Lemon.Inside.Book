using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Chapters.LightShadow;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Contents;

/// <summary>
/// Builds each chapter's two pages: a written chapter's own page types, and placeholders for the rest. Writing a
/// chapter means adding its case here.
/// </summary>
public static class ChapterRegistry
{
    /// <summary>Slugs of the chapters that are written (not placeholders).</summary>
    public static IReadOnlySet<string> Ported { get; } = new HashSet<string>(StringComparer.Ordinal) { "Light-Shadow" };

    public static (PageViewModel Text, PageViewModel Diagram) CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, BookServices services)
    {
        switch (chapter.Slug)
        {
            case "Light-Shadow":
            {
                var light = new LightShadowViewModel();
                return (new LightShadowTextPageViewModel(textPage, navigator, chapter, number, light),
                    new LightShadowDiagramPageViewModel(textPage + 1, navigator, chapter, light));
            }

            default:
                return (new ChapterTextPageViewModel(textPage, navigator, chapter, number),
                    new ChapterDiagramPageViewModel(textPage + 1, navigator, chapter));
        }
    }
}
