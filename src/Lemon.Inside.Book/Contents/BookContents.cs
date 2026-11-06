namespace Lemon.Inside.Book.Contents;

/// <summary>
/// The fixed running order of the book: front matter, one table-of-contents page per volume, the chapters,
/// then the back matter.
/// </summary>
public static class BookContents
{
    public static IReadOnlyList<Chapter> Chapters { get; } =
    [
        new("Light-Shadow", Volume.Light, Spreads: 9),

        new("Gears", Volume.Machines, Spreads: 6),
        new("Watch", Volume.Machines),
    ];

    public static IReadOnlyList<Volume> Volumes { get; } = Enum.GetValues<Volume>();

    /// <summary>Pages before the table of contents: cover, how-to-use, title page.</summary>
    internal const int FrontMatterPages = 3;

    /// <summary>Index of the first table-of-contents page.</summary>
    public static int FirstContentsPage => FrontMatterPages;

    public static int ContentsPage(Volume volume) => FrontMatterPages + (int)volume;

    /// <summary>
    /// Index of a chapter's first text page; its picture page follows, then the chapter's next spreads. The front
    /// matter plus one contents page per volume is an odd count (3 + 2) and every chapter takes whole spreads, so
    /// every chapter opens on a left page.
    /// </summary>
    public static int ChapterPage(int chapterIndex) =>
        FrontMatterPages + Volumes.Count + (2 * Chapters.Take(chapterIndex).Sum(c => c.Spreads));

    /// <summary>Index of the text page of a chapter's spread (0 is the chapter's first).</summary>
    public static int SpreadPage(int chapterIndex, int spread) => ChapterPage(chapterIndex) + (2 * spread);

    /// <summary>Index of the text page of the chapter with this slug, e.g. "Light-Shadow".</summary>
    public static int ChapterPageOf(string slug)
    {
        var index = Chapters.ToList().FindIndex(c => c.Slug == slug);
        return index >= 0 ? ChapterPage(index) : throw new ArgumentException($"No chapter '{slug}'.", nameof(slug));
    }

    /// <summary>Total pages: front matter, contents, two per spread of every chapter, credits and the back cover.</summary>
    public static int PageCount => ChapterPage(Chapters.Count) + 2;
}
