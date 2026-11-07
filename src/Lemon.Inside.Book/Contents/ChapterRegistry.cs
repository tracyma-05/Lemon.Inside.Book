using Lemon.Book.Shell.ViewModels.Pages;
using Lemon.Inside.Book.Chapters.Bicycle;
using Lemon.Inside.Book.Chapters.Bridges;
using Lemon.Inside.Book.Chapters.Boats;
using Lemon.Inside.Book.Chapters.Car;
using Lemon.Inside.Book.Chapters.Friction;
using Lemon.Inside.Book.Chapters.Gears;
using Lemon.Inside.Book.Chapters.Lifting;
using Lemon.Inside.Book.Chapters.Plumbing;
using Lemon.Inside.Book.Chapters.LightShadow;
using Lemon.Inside.Book.Chapters.Locks;
using Lemon.Inside.Book.Chapters.Watch;
using Lemon.Inside.Book.Chapters.Weighing;
using Lemon.Inside.Book.ViewModels.Pages;

namespace Lemon.Inside.Book.Contents;

/// <summary>
/// Builds each chapter's pages, a (text, picture) pair per spread: a written chapter's own page types, and
/// placeholders for the rest. Writing a chapter means adding its case here.
/// </summary>
public static class ChapterRegistry
{
    /// <summary>Slugs of the chapters that are written (not placeholders).</summary>
    public static IReadOnlySet<string> Ported { get; } = new HashSet<string>(StringComparer.Ordinal) { "Light-Shadow", "Gears", "Watch", "Bicycle", "Boats", "Car", "Locks", "Lifting", "Plumbing", "Bridges", "Weighing", "Friction" };

    public static IReadOnlyList<(PageViewModel Text, PageViewModel Diagram)> CreatePages(
        Chapter chapter, int number, int textPage, IBookNavigator navigator, BookServices services)
    {
        var pages = chapter.Slug switch
        {
            "Light-Shadow" => LightShadowChapter.CreatePages(chapter, number, textPage, navigator, services.Narration),
            "Gears" => GearsChapter.CreatePages(chapter, number, textPage, navigator, services.Narration),
            "Lifting" => LiftingChapter.CreatePages(chapter, number, textPage, navigator, services.Narration),
            "Plumbing" => PlumbingChapter.CreatePages(chapter, number, textPage, navigator, services.Narration),
            "Bridges" => BridgesChapter.CreatePages(chapter, number, textPage, navigator, services.Narration),
            "Weighing" => WeighingChapter.CreatePages(chapter, number, textPage, navigator, services.Narration),
            "Friction" => FrictionChapter.CreatePages(chapter, number, textPage, navigator, services.Narration),
            "Locks" => LocksChapter.CreatePages(chapter, number, textPage, navigator, services.Narration),
            "Car" => CarChapter.CreatePages(chapter, number, textPage, navigator, services.Narration),
            "Boats" => BoatsChapter.CreatePages(chapter, number, textPage, navigator, services.Narration),
            "Bicycle" => BicycleChapter.CreatePages(chapter, number, textPage, navigator, services.Narration),
            "Watch" => WatchChapter.CreatePages(chapter, number, textPage, navigator, services.Narration),
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
