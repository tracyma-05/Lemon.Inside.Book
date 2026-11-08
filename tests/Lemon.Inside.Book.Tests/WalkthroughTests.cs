using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Book.Shell.Explore.Lessons;
using Lemon.Inside.Book.Chapters;
using Lemon.Inside.Book.Chapters.Bicycle;
using Lemon.Inside.Book.Chapters.Bridges;
using Lemon.Inside.Book.Chapters.Boats;
using Lemon.Inside.Book.Chapters.Canals;
using Lemon.Inside.Book.Chapters.Sewing;
using Lemon.Inside.Book.Chapters.Colour;
using Lemon.Inside.Book.Chapters.Car;
using Lemon.Inside.Book.Chapters.Friction;
using Lemon.Inside.Book.Chapters.Lifting;
using Lemon.Inside.Book.Chapters.Plumbing;
using Lemon.Inside.Book.Chapters.Locks;
using Lemon.Inside.Book.Chapters.Gears;
using Lemon.Inside.Book.Chapters.LightShadow;
using Lemon.Inside.Book.Chapters.Watch;
using Lemon.Inside.Book.Chapters.Weighing;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.Controls;
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
        .. new BicycleViewModel(narration).AllSpreads,
        .. new BoatsViewModel(narration).AllSpreads,
        .. new CarViewModel(narration).AllSpreads,
        .. new LocksViewModel(narration).AllSpreads,
        .. new LiftingViewModel(narration).AllSpreads,
        .. new PlumbingViewModel(narration).AllSpreads,
        .. new BridgesViewModel(narration).AllSpreads,
        .. new WeighingViewModel(narration).AllSpreads,
        .. new FrictionViewModel(narration).AllSpreads,
        .. new CanalsViewModel(narration).AllSpreads,
        .. new SewingViewModel(narration).AllSpreads,
        .. new ColourViewModel(narration).AllSpreads,
    ];

    [Fact]
    public void EveryClipAndAnswer_IsWrittenInBothLanguages()
    {
        var spreads = AllSpreads(new FakeNarration()).ToList();
        var used = WalkthroughKeys(spreads.Select(s => s.Walkthrough()));
        foreach (var spread in spreads)
        {
            Assert.True(spread.Parts.Count >= 2, spread.Key);
            used.UnionWith(spread.Parts.Select(spread.PartTitleKey)!);
            used.UnionWith(spread.Parts.Select(spread.PartClipKey)!);
        }

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

    [AvaloniaTheory]
    [InlineData("Light-Shadow")]
    [InlineData("Gears")]
    [InlineData("Watch")]
    [InlineData("Bicycle")]
    [InlineData("Boats")]
    [InlineData("Car")]
    [InlineData("Locks")]
    [InlineData("Lifting")]
    [InlineData("Plumbing")]
    [InlineData("Bridges")]
    [InlineData("Weighing")]
    [InlineData("Friction")]
    [InlineData("Canals")]
    [InlineData("Sewing")]
    [InlineData("Colour")]
    public void EveryRing_SitsOnAPartThePictureDrew(string slug)
    {
        var (window, model) = OpenAt(slug);
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == slug);
        for (var s = 0; s < BookContents.Chapters[chapter].Spreads; s++)
        {
            var page = BookContents.SpreadPage(chapter, s);
            model.GoToPage(page);
            Frames(3);
            var diagram = model.Pages[page + 1];
            var spread = (Spread)diagram.GetType().GetProperty("Spread")!.GetValue(diagram)!;
            var picture = window.GetVisualDescendants().OfType<Picture>().First(p => p.IsEffectivelyVisible);
            var lesson = spread.Lesson;
            lesson.Start();
            var rung = 0;
            for (var guard = 0; guard < 50 && lesson.State != LessonState.Done; guard++)
            {
                Frames(2);
                Assert.Equal(lesson.Highlight, picture.Highlight);
                if (lesson.Highlight is { } id)
                {
                    Assert.True(picture.MarkedParts.Contains(id), $"{picture.GetType().Name} has no part '{id}' ({string.Join(", ", picture.MarkedParts)})");
                    rung++;
                    if (rung == 1) Snapshot(window, $"ring-{slug}-{s + 1}");
                }

                lesson.NextCommand.Execute(null);
            }

            Assert.True(rung > 0, spread.Key);
            lesson.Stop();
            Frames(2);
            Assert.Null(picture.Highlight);
        }

        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("Light-Shadow")]
    [InlineData("Gears")]
    [InlineData("Watch")]
    [InlineData("Bicycle")]
    [InlineData("Boats")]
    [InlineData("Car")]
    [InlineData("Locks")]
    [InlineData("Lifting")]
    [InlineData("Plumbing")]
    [InlineData("Bridges")]
    [InlineData("Weighing")]
    [InlineData("Friction")]
    [InlineData("Canals")]
    [InlineData("Sewing")]
    [InlineData("Colour")]
    public void EveryPart_CanBeClicked_AndItsCardShowsOnTheLeftPage(string slug)
    {
        var (window, model) = OpenAt(slug);
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == slug);
        for (var s = 0; s < BookContents.Chapters[chapter].Spreads; s++)
        {
            var page = BookContents.SpreadPage(chapter, s);
            model.GoToPage(page);
            Frames(3);
            var diagram = model.Pages[page + 1];
            var spread = (Spread)diagram.GetType().GetProperty("Spread")!.GetValue(diagram)!;
            var picture = window.GetVisualDescendants().OfType<Picture>().First(p => p.IsEffectivelyVisible);

            foreach (var id in spread.Parts)
            {
                // Somewhere inside its ring a click picks this part (a smaller one may sit over its middle).
                Assert.True(picture.PartCentre(id) is not null, $"{picture.GetType().Name} does not draw '{id}'");
                var spot = SpotFor(picture, id);
                Assert.True(spot is not null, $"{picture.GetType().Name}: '{id}' is covered by smaller parts");

                picture.ClickAt(spot.Value);
                Frames(1);
                Assert.Equal(id, spread.Card.Id);
                Assert.False(string.IsNullOrWhiteSpace(spread.Card.Text));
                var card = window.GetVisualDescendants().OfType<Controls.Explore.ObjectCardView>().Single(c => c.IsEffectivelyVisible);
                Assert.Same(spread.Card, card.DataContext);
                if (id == spread.Parts[0]) Snapshot(window, $"card-{slug}-{s + 1}");

                // The card's close button clears the picture's choice too (the parts may have moved on by now).
                spread.Card.CloseCommand.Execute(null);
                Frames(1);
                Assert.Null(picture.SelectedPart);
            }
        }

        window.Close();
    }

    [AvaloniaFact]
    public void FromTheKeyboard_ArrowsPickParts_EnterOpensTheCard_EscapeClosesIt_AndThePageStays()
    {
        var (window, model) = OpenAt("Watch");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Watch");
        var page = BookContents.SpreadPage(chapter, 2);
        model.GoToPage(page);
        Frames(3);
        var spread = (Spread)model.Pages[page + 1].GetType().GetProperty("Spread")!.GetValue(model.Pages[page + 1])!;
        var picture = window.GetVisualDescendants().OfType<Picture>().First(p => p.IsEffectivelyVisible);
        Assert.True(picture.Focusable);
        picture.Focus(Avalonia.Input.NavigationMethod.Tab);
        Frames(2);
        Snapshot(window, "keyboard-focus");

        void Press(Avalonia.Input.Key key)
        {
            window.KeyPress(key, Avalonia.Input.RawInputModifiers.None, Avalonia.Input.PhysicalKey.None, null);
            window.KeyRelease(key, Avalonia.Input.RawInputModifiers.None, Avalonia.Input.PhysicalKey.None, null);
            Frames(2);
        }

        Press(Avalonia.Input.Key.Right);
        Assert.Equal(spread.Parts[0], picture.HoveredPart);
        Press(Avalonia.Input.Key.Right);
        Assert.Equal(spread.Parts[1], picture.HoveredPart);
        Press(Avalonia.Input.Key.Left);
        Press(Avalonia.Input.Key.Left);
        Assert.Equal(spread.Parts[^1], picture.HoveredPart);
        Assert.Equal(page, model.PageIndex);

        Press(Avalonia.Input.Key.Enter);
        Assert.Equal(spread.Parts[^1], spread.Card.Id);
        Snapshot(window, "keyboard-card");
        Press(Avalonia.Input.Key.Escape);
        Assert.Null(spread.Card.Id);
        Assert.Null(picture.HoveredPart);
        window.Close();
    }

    /// <summary>A point in the part's ring where a click picks it, trying its middle first.</summary>
    private static Avalonia.Point? SpotFor(Picture picture, string id)
    {
        var centre = picture.PartCentre(id)!.Value;
        for (var r = 0.0; r <= 200; r += 4)
        {
            for (var a = 0; a < 16; a++)
            {
                var p = centre + new Avalonia.Point(r * Math.Cos(a * Math.PI / 8), r * Math.Sin(a * Math.PI / 8));
                if (picture.PartAt(p) == id) return p;
                if (r == 0) break;
            }
        }

        return null;
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
