using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Chapters.Gears;
using Lemon.Inside.Book.Chapters.LightShadow;
using Lemon.Inside.Book.Chapters.Watch;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Contents;

/// <summary>
/// Builds each chapter's pages, a (text, picture) pair per spread: a written chapter's own page types, and
/// placeholders for the rest. Writing a chapter means adding its case here.
/// </summary>
public static class ChapterRegistry
{
    /// <summary>Slugs of the chapters that are written (not placeholders).</summary>
    public static IReadOnlySet<string> Ported { get; } = new HashSet<string>(StringComparer.Ordinal) { "Light-Shadow", "Gears", "Watch" };

    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, BookServices services)
    {
        var pages = chapter.Slug switch
        {
            "Light-Shadow" => LightShadowChapter.CreatePages(chapter, number, textPage, navigator),
            "Gears" => GearsChapter.CreatePages(chapter, number, textPage, navigator),
            "Watch" => WatchChapter.CreatePages(chapter, number, textPage, navigator),
            _ => [(new ChapterTextPageViewModel(textPage, navigator, chapter, number),
                new ChapterDiagramPageViewModel(textPage + 1, navigator, chapter))],
        };

        if (pages.Count != chapter.Spreads)
        {
            throw new InvalidOperationException($"Chapter '{chapter.Slug}' says {chapter.Spreads} spreads but has {pages.Count}.");
        }

        return pages;
    }
}
