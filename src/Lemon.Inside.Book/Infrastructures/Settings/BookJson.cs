using System.Text.Json.Serialization;
using Lemon.Book.Shell.Contents;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Inside.Book.Contents;

namespace Lemon.Inside.Book.Infrastructures.Settings;

/// <summary>
/// Source-generated JSON for everything the book saves, so saving works without reflection (trimmed and
/// native AOT builds turn reflection-based serialization off).
/// </summary>
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(NarrationSettings))]
[JsonSerializable(typeof(List<BookmarkRecord>))]
[JsonSerializable(typeof(List<NoteRecord>))]
[JsonSerializable(typeof(Dictionary<string, ChapterProgressRecord>))]
[JsonSerializable(typeof(Views.WindowPlacement))]
internal sealed partial class BookJson : JsonSerializerContext;
