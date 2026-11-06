using Avalonia.Headless.XUnit;
using Lemon.Book.Shell.Explore.Lessons;
using Lemon.Inside.Book.Chapters;
using Lemon.Inside.Book.Chapters.Gears;
using Lemon.Inside.Book.Chapters.LightShadow;
using Lemon.Inside.Book.Chapters.Watch;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>Every spread's spoken walkthrough: its words exist, and a reader can follow it to the end.</summary>
public sealed class WalkthroughTests
{
    private static IEnumerable<Spread> AllSpreads(FakeNarration narration) =>
    [
        .. new LightShadowViewModel(narration).AllSpreads,
        .. new GearsViewModel(narration).AllSpreads,
        .. new WatchViewModel(narration).AllSpreads,
    ];

    [Fact]
    public void EveryClipAndAnswer_IsWrittenInBothLanguages()
    {
        var used = WalkthroughKeys(AllSpreads(new FakeNarration()).Select(s => s.Walkthrough()));
        foreach (var keys in BothLanguages())
        {
            var missing = used.Where(k => !keys.Contains(k)).ToList();
            Assert.True(missing.Count == 0, string.Join(", ", missing));
        }
    }

    [Fact]
    public void EverySpread_HasAWalkthrough_WithSomethingToDoOrAnswer()
    {
        foreach (var spread in AllSpreads(new FakeNarration()))
        {
            var steps = spread.Walkthrough();
            Assert.True(steps.Count >= 4, spread.Key);
            Assert.Contains(steps, s => s.TryIt is not null || s.Choices is not null);
            Assert.All(steps, s => Assert.StartsWith($"Narr_{spread.Area}_{spread.Key}_", s.Key, StringComparison.Ordinal));
            Assert.All(steps.Where(s => s.Choices is not null), s => Assert.InRange(s.Answer, 0, s.Choices!.Count - 1));
        }
    }

    [AvaloniaFact]
    public void EveryWalkthrough_RunsToTheEnd_AndEachTaskCanBeDone()
    {
        var narration = new FakeNarration();
        foreach (var spread in AllSpreads(narration))
        {
            var lesson = spread.Lesson;
            var steps = spread.Walkthrough();
            lesson.GapSeconds = 0;
            lesson.Start();
            for (var guard = 0; guard < 200 && lesson.State != LessonState.Done; guard++)
            {
                if (lesson.State == LessonState.Quiz)
                {
                    lesson.AnswerCommand.Execute(lesson.Choices[steps[lesson.Index].Answer]);
                }
                else if (lesson.State == LessonState.TryIt)
                {
                    // The step's scene is set and not yet done; doing what it asks finishes it.
                    var step = steps[lesson.Index];
                    Assert.False(step.TryIt!(), step.Key);
                    spread.Solve(step.Key);
                    Assert.True(step.TryIt(), step.Key);
                }

                narration.Finish();
                lesson.Tick(0.1);
            }

            Assert.Equal(LessonState.Done, lesson.State);
            lesson.Stop();
        }
    }

    [AvaloniaFact]
    public void MovingAControl_PausesTheWalkthrough_ButItsOwnStepsDoNot()
    {
        var narration = new FakeNarration();
        var tilt = new LightShadowViewModel(narration).Tilt;
        tilt.Lesson.Start();
        Assert.Equal(LessonState.Speaking, tilt.Lesson.State);

        tilt.Tilt = 33;
        Assert.Equal(LessonState.Paused, tilt.Lesson.State);
        tilt.Lesson.Stop();
    }

    [Fact]
    public void TurningAwayFromTheChapter_StopsItsWalkthrough()
    {
        var watch = new WatchViewModel(new FakeNarration());
        watch.ViewAttached();
        watch.Balance.Lesson.Start();
        Assert.True(watch.Balance.Lesson.IsActive);

        watch.ViewDetached();
        Assert.False(watch.Balance.Lesson.IsActive);
    }
}
