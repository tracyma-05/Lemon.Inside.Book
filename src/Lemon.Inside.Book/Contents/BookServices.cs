using Lemon.Book.Shell.Contents;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Settings;
using Lemon.Inside.Book.Infrastructures.Settings;

namespace Lemon.Inside.Book.Contents;

/// <summary>
/// What the book shares: the reader's settings, the one narration channel, the bookmarks,
/// the notes and the reading progress.
/// </summary>
public sealed record BookServices(
    ISettingsStore Settings, INarrationService Narration, Bookmarks Bookmarks, Notes Notes, ReadingProgress Progress)
{
    /// <summary>
    /// Services over <paramref name="settings"/>. Without audio (tests, or a platform with no player) narration runs
    /// as timed subtitles.
    /// </summary>
    public static BookServices Create(ISettingsStore settings, NarrationManifest? manifest = null, INarrationAudio? audio = null) =>
        new(settings,
            new NarrationService(settings, manifest ?? NarrationManifest.Empty, audio ?? SilentNarrationAudio.Instance),
            new Bookmarks(settings),
            new Notes(settings),
            new ReadingProgress(settings));
}
