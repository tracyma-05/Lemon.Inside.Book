using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Book.Shell.Infrastructures.Timing;
using Lemon.Inside.Book.Chapters.Watch;
using Lemon.Inside.Book.Contents;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The mechanical watch: the going train, the escapement's beats, the balance's rate, winding, and every spread drawn.</summary>
public sealed class WatchTests
{
    [Fact]
    public void TheTrain_TurnsTheMinuteHandOnceAnHour_AndTheSecondsHandOnceAMinute()
    {
        var turns = GoingTrain.TurnsPerHour();
        Assert.Equal(1.0 / 8, turns[0], 9);  // barrel
        Assert.Equal(1, turns[1], 9);        // centre wheel: minute hand
        Assert.Equal(60, turns[3], 9);       // fourth wheel: seconds hand
        Assert.Equal(GoingTrain.BeatsPerHour, turns[4] * GoingTrain.EscapeTeeth * 2, 6); // two beats per escape tooth
        Assert.Equal(5, GoingTrain.SpringTurns, 9);

        var (hour, minute, second) = GoingTrain.Hands(HandsDiagram.StartSeconds);
        Assert.Equal((10 * 30) + 4.25, hour, 6);
        Assert.Equal((8 * 6) + 3, minute, 6);
        Assert.Equal(180, second, 6);
    }

    [Fact]
    public void TheEscapeWheel_MovesOnlyAtTheTick_AndOneToothPerSwing()
    {
        var f = GoingTrain.Frequency;
        Assert.Equal(2.5, f, 9);

        // One full swing to and fro: two beats, one tooth (two half teeth).
        var (_, _, start, _) = Escapement.At(0.1);
        var (_, _, end, _) = Escapement.At(0.1 + (1 / f));
        Assert.Equal(2, end - start, 9);

        // Between beats (balance far from the middle) the wheel is locked and the lever rests on one side.
        var quarter = 1 / (4 * f);
        var a = Escapement.At(quarter - 0.01);
        var b = Escapement.At(quarter + 0.01);
        Assert.False(a.Ticking);
        Assert.Equal(a.HalfTeeth, b.HalfTeeth);
        Assert.Equal(1, Math.Abs(a.Lever), 9);

        // Across a beat, the lever changes sides.
        var before = Escapement.At((1 / (2 * f)) - 0.03);
        var after = Escapement.At((1 / (2 * f)) + 0.03);
        Assert.True(Escapement.At(1 / (2 * f)).Ticking);
        Assert.Equal(-before.Lever, after.Lever, 9);
        Assert.Equal(1, after.HalfTeeth - before.HalfTeeth, 9);

        // An hour of beats.
        Assert.Equal(GoingTrain.BeatsPerHour, Escapement.At(3600.1).HalfTeeth - Escapement.At(0.1).HalfTeeth, 6);
    }

    [Fact]
    public void TheBalance_SwingsFasterWithAStifferSpring_ButNotWithAWiderSwing()
    {
        Assert.Equal(GoingTrain.Frequency, Balance.Frequency(), 9);
        Assert.Equal(2 * GoingTrain.Frequency, Balance.Frequency(4), 9);
        Assert.Equal(GoingTrain.Frequency, Balance.Frequency(2, 2), 9);

        // The swing's width does not change when it comes back to the middle.
        var f = Balance.Frequency();
        Assert.Equal(0, Balance.Angle(1 / f, 120, f), 6);
        Assert.Equal(0, Balance.Angle(1 / f, 300, f), 6);

        // 1% stiffer: about half a percent faster, some 7 minutes a day.
        var spread = new WatchViewModel().Balance;
        spread.Stiffness = 1.01;
        Assert.InRange(Balance.DailyError(spread.Frequency) / 60, 7, 7.3);
        Assert.Contains("7.2", spread.ErrorText);
    }

    [Fact]
    public void TheRotor_WindsBothWaysThroughTheReverser_AndStopsAtFull()
    {
        var both = new AutoWinding();
        both.Step(0.5, 0);
        both.Step(-0.5, 0);
        Assert.Equal(1, both.WheelTurned, 9);

        var one = new AutoWinding { Reverser = false };
        one.Step(0.5, 0);
        one.Step(-0.5, 0);
        Assert.Equal(0.5, one.WheelTurned, 9);

        for (var i = 0; i < 2000; i++)
        {
            both.Step(1, 0);
        }

        Assert.Equal(1, both.Wound); // the bridle slips instead of over-winding

        var still = new AutoWinding();
        still.Step(0, 10);
        Assert.True(still.Wound < 0.35); // the watch keeps using its spring
    }

    [Fact]
    public void TheMovementsLayers_RiseAsTheyArePulledApart_AndCanBeHidden()
    {
        Assert.Equal(0, MovementDiagram.LayerHeight(MovementLayer.Plate, 1));
        Assert.True(MovementDiagram.LayerHeight(MovementLayer.Dial, 1) > MovementDiagram.LayerHeight(MovementLayer.Dial, 0));
        var movement = new WatchViewModel().Movement;
        movement.ShowTrain = false;
        Assert.False(movement.IsShown(MovementLayer.Train));
        Assert.True(movement.IsShown(MovementLayer.Dial));
        movement.ShowTrain = true;
        Assert.Equal(0, movement.Hidden);
    }

    [Fact]
    public void TheSpring_CoilsTightRoundTheArbor_WhenWound()
    {
        Assert.True(SpringDiagram.CoilRadius(0.5, 1) < SpringDiagram.CoilRadius(0.5, 0));
        Assert.True(SpringDiagram.CoilRadius(1, 0) < 1);
        Assert.True(SpringDiagram.CoilRadius(0, 1) > 0.15);
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new WatchViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }
        }
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Watch");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Watch");
        Assert.Equal(BookContents.Chapters[chapter].Spreads, new WatchViewModel().Spreads.Count);
        Type[] pictures =
        [
            typeof(SpringDiagram), typeof(HandsDiagram), typeof(EscapementDiagram), typeof(BalanceDiagram), typeof(RotorDiagram),
            typeof(MovementDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"watch-{s + 1}");
        }

        window.Close();
    }

    [AvaloniaFact]
    public void OnAHeldClock_TheEscapementMovesExactlyAsFarAsTheClockIsStepped()
    {
        // A video export holds the book's clock and steps it a frame at a time: the escapement must follow it exactly,
        // however long each frame takes to draw, so a recording is the same every time.
        var (window, model) = OpenAt("Watch");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Watch");
        model.GoToPage(BookContents.SpreadPage(chapter, 2));
        Frames(5);
        var escapement = window.GetVisualDescendants().OfType<EscapementDiagram>().First(c => c.IsEffectivelyVisible);
        BookClock.BeginStepping();
        try
        {
            Frames(2);
            var start = escapement.Time;
            for (var i = 0; i < 60; i++)
            {
                BookClock.Advance(1.0 / 30);
                Frames(1);
            }

            Assert.Equal(2 * escapement.SlowMotion, escapement.Time - start, 9);
            Snapshot(window, "watch-3-stepped");
        }
        finally
        {
            BookClock.EndStepping();
            window.Close();
        }
    }
}
