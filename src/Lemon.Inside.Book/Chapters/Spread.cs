using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Controls.Explore;
using Lemon.Book.Shell.Explore.Lessons;
using Lemon.Book.Shell.Explore.Narration;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.Infrastructures.Settings;
using Lemon.Inside.Book.Contents;

namespace Lemon.Inside.Book.Chapters;

/// <summary>
/// One spread of a chapter: its title, two paragraphs, a closer look, the settings its picture uses, and its spoken
/// walkthrough. The strings are <c>&lt;Area&gt;_&lt;Key&gt;_Title</c>, <c>_P1</c>, <c>_P2</c> and <c>_Deeper</c>; the
/// walkthrough's clips are <c>Narr_&lt;Area&gt;_&lt;Key&gt;_NN</c>.
/// </summary>
public abstract class Spread(string area, string key) : ObservableObject
{
    private readonly Dictionary<string, Action> _solutions = new(StringComparer.Ordinal);

    protected static ILocalizationService Text => LocalizationService.Instance;

    public string Area { get; } = area;

    public string Key { get; } = key;

    public string Title => Text.GetString($"{Area}_{Key}_Title");

    public IReadOnlyList<string> Paragraphs => [Text.GetString($"{Area}_{Key}_P1"), Text.GetString($"{Area}_{Key}_P2")];

    public string Deeper => Text.GetString($"{Area}_{Key}_Deeper");

    /// <summary>Every key the spread's own words use (for the localization tests).</summary>
    public IEnumerable<string> TextKeys => new[] { "_Title", "_P1", "_P2", "_Deeper" }.Select(s => $"{Area}_{Key}{s}");

    /// <summary>The spoken walkthrough of this spread; set once the chapter is given the narration channel.</summary>
    public LessonPlayer Lesson { get; private set; } = null!;

    /// <summary>The steps of the walkthrough, built afresh on every start.</summary>
    public abstract IReadOnlyList<LessonStep> Walkthrough();

    /// <summary>The reader's language changed: every worded value is said again.</summary>
    internal void OnLanguageChanged() => OnPropertyChanged(string.Empty);

    internal void UseNarration(INarrationService narration)
    {
        Lesson = new LessonPlayer(narration, Walkthrough);
        PropertyChanged += OnSettingChanged;
    }

    /// <summary>Does what a "your turn" step asks, as a reader would (for the tests).</summary>
    internal void Solve(string step)
    {
        if (_solutions.TryGetValue(step, out var solve)) solve();
    }

    /// <summary>The clip key of step <paramref name="number"/> (from 1).</summary>
    protected string Clip(int number) => $"Narr_{Area}_{Key}_{number:00}";

    /// <summary>A "your turn" step: waits until <paramref name="done"/>; <paramref name="solve"/> is what a reader would do.</summary>
    protected LessonStep TryIt(int number, Func<bool> done, Action solve)
    {
        var key = Clip(number);
        _solutions[key] = solve;
        return new LessonStep(key) { TryIt = done };
    }

    /// <summary>A question with three answers, <c>&lt;Area&gt;_&lt;Key&gt;_Quiz1..3</c>; <paramref name="answer"/> from 0.</summary>
    protected LessonStep Quiz(int number, int answer) => new(Clip(number))
    {
        Choices = [$"{Area}_{Key}_Quiz1", $"{Area}_{Key}_Quiz2", $"{Area}_{Key}_Quiz3"],
        Answer = answer,
    };

    /// <summary>The reader moved one of the spread's controls: a walkthrough being spoken makes way.</summary>
    private void OnSettingChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.PropertyName) && Lesson.IsActive && !Lesson.IsApplying)
        {
            Lesson.UserInteracted();
        }
    }
}

/// <summary>
/// A chapter told over several spreads: one instance serves every page of the chapter. It says worded values again
/// when the language changes, and stops any walkthrough once the reader turns away from the chapter.
/// </summary>
public abstract class SpreadChapter : IChapterContent
{
    private int _views;

    /// <summary>The spreads in reading order.</summary>
    public abstract IReadOnlyList<Spread> AllSpreads { get; }

    /// <summary>A page of the chapter came on screen.</summary>
    public void ViewAttached() => _views++;

    /// <summary>A page of the chapter left the screen; once none is left, no walkthrough talks over the next one.</summary>
    public void ViewDetached()
    {
        _views = Math.Max(0, _views - 1);
        if (_views > 0) return;

        foreach (var spread in AllSpreads)
        {
            spread.Lesson.Stop();
        }
    }

    /// <summary>Call once from the chapter's constructor, after its spreads exist.</summary>
    /// <param name="narration">The book's narration channel; null (tests) gives a silent one.</param>
    protected void Attach(INarrationService? narration)
    {
        narration ??= BookServices.Create(new MemorySettingsStore()).Narration;
        foreach (var spread in AllSpreads)
        {
            spread.UseNarration(narration);
        }

        LocalizationService.Instance.PropertyChanged += (_, _) =>
        {
            foreach (var spread in AllSpreads)
            {
                spread.OnLanguageChanged();
            }
        };
    }
}
