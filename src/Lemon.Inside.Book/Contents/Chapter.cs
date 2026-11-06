namespace Lemon.Inside.Book.Contents;

/// <summary>
/// One chapter of the book: a topic told over one or more spreads, each a text page (left) and an interactive
/// picture (right).
/// </summary>
/// <param name="Slug">Stable id, also used for file names and resource keys (e.g. <c>Light-Shadow</c>).</param>
/// <param name="Volume">The part of the book the chapter belongs to.</param>
/// <param name="Spreads">How many two-page spreads the chapter takes.</param>
public sealed record Chapter(string Slug, Volume Volume, int Spreads = 1)
{
    /// <summary>Resource key of the chapter title: the slug with every non-alphanumeric character as <c>_</c>.</summary>
    public string TitleKey => "Chapter_" + ToKeySegment(Slug);

    internal static string ToKeySegment(string value) =>
        string.Concat(value.Select(c => char.IsLetterOrDigit(c) ? c : '_'));
}
