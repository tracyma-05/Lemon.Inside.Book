using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Friction;
using Lemon.Inside.Book.Contents;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The Friction chapter: the models behind its pictures, its words, and every spread drawn.</summary>
public sealed class FrictionTests
{
    [Fact]
    public void ABlock_SlidesOnceTheSlopeBeatsItsGrip()
    {
        Assert.InRange(Ramp.Steepest(0), 2, 4); // ice: "a few degrees"
        Assert.InRange(Ramp.Steepest(1), 20, 23); // wood: "about twenty"
        Assert.InRange(Ramp.Steepest(2), 37, 40); // rubber: "nearly forty"
        Assert.False(Ramp.Slides(10, 1));
        Assert.True(Ramp.Slides(25, 1));
        Assert.False(Ramp.Slides(25, 2));
        Assert.True(Ramp.Downhill(10) < Ramp.MostGrip(10, 1));
        Assert.True(Ramp.Downhill(25) > Ramp.MostGrip(25, 1));
    }

    [Fact]
    public void FrictionMatchesThePull_ThenDropsOnceSliding()
    {
        Assert.Equal(10, Drag.Friction(40, 10), 9);
        Assert.False(Drag.Moving(40, 20));
        Assert.True(Drag.Moving(40, 22));
        Assert.Equal(14, Drag.Friction(40, 22), 9);
        Assert.True(Drag.Sliding < Drag.AtRest);
    }

    [Fact]
    public void Skidding_StopsLonger_AndTwiceTheSpeedIsFourTimesTheDistance()
    {
        Assert.Equal(4, Skid.Distance(100, false) / Skid.Distance(50, false), 9);
        Assert.True(Skid.Distance(50, true) > Skid.Distance(50, false));
        Assert.InRange(Skid.Distance(50, false), 10, 12);
        Assert.True(Skid.Distance(120, true) < 85); // the picture's longest bar fits
    }

    [Fact]
    public void EachTurn_DividesThePullBySixAndAHalf()
    {
        Assert.Equal(Capstan.Boat, Capstan.Hold(0, false), 9);
        Assert.InRange(Capstan.Gain(1, false), 6.4, 6.7);
        Assert.InRange(Capstan.Hold(1, false), 145, 160); // "about a hundred and fifty"
        Assert.InRange(Capstan.Hold(2, false), 22, 25); // "about twenty-three"
        Assert.InRange(Capstan.Hold(3, false), 3, 4); // "a few kilograms"
        Assert.True(Capstan.Hold(1.9, false) > 25); // the walkthrough's step is not already done
        Assert.True(Capstan.Hold(3, true) > Capstan.Hold(3, false));
    }

    [Fact]
    public void Rolling_TakesAFractionOfTheDrag()
    {
        Assert.Equal(80, Rolling.Force(200, 0), 9);
        Assert.Equal(8, Rolling.Force(200, 1), 9);
        Assert.Equal(1, Rolling.Force(200, 2), 9);
    }

    [Fact]
    public void Oil_CarriesTheLoad_OnceTheFilmIsThickerThanTheBumps()
    {
        Assert.True(Lubrication.Touching(50, false));
        Assert.False(Lubrication.Touching(50, true));
        Assert.True(Lubrication.Touching(5, true));
        Assert.True(Lubrication.Friction(5, true) > Lubrication.Friction(50, true));
        Assert.True(Lubrication.Friction(50, true) < 0.02);
        Assert.True(Lubrication.Friction(100, true) > Lubrication.Friction(70, true) - 0.01); // a low point, then a slow rise
        Assert.Equal(Lubrication.Dry, Lubrication.Friction(50, false));
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var friction = new FrictionViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Friction").Spreads, friction.Spreads.Count);

        friction.Ramp.Angle = 25;
        Assert.True(friction.Ramp.Slides);
        friction.Capstan.Turns = 2;
        Assert.True(friction.Capstan.Hold <= 25);

        foreach (var spread in friction.Spreads)
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)spread.GetType().GetProperty("ReadoutText")!.GetValue(spread)));
        }
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new FrictionViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }

            foreach (var i in new[] { 0, 1, 2 })
            {
                Assert.Contains(RampSpread.SurfaceKey(i), keys);
                Assert.Contains(RollingSpread.WayKey(i), keys);
            }
        }
    }

    [Fact]
    public void TheGrips_TurnWhereTheyAreDraggedIntoTheSpreadsValues()
    {
        // The board's far end under the pointer: level, forty-five degrees, and no steeper than the slider.
        Assert.Equal(0, RampDiagram.AngleFor(5, 0), 9);
        Assert.Equal(45, RampDiagram.AngleFor(1, 1), 9);
        Assert.Equal(45, RampDiagram.AngleFor(0.1, 5), 9);
        Assert.Equal(26.5, RampDiagram.AngleFor(2, 1), 9);

        Assert.Equal(40, StartDiagram.BoxForSide(50 + (40 * 0.9)), 9);
        Assert.Equal(20, StartDiagram.BoxForSide(0), 9);
        Assert.Equal(40, StartDiagram.PullForLean(70), 9);
        Assert.Equal(0, StartDiagram.PullForLean(-10), 9);

        // Dragging the end of the stop gives back the speed that stops there.
        foreach (var locked in new[] { false, true })
        {
            Assert.Equal(50, SkidDiagram.SpeedFor(Skid.Distance(50, locked), locked), 9);
            Assert.Equal(120, SkidDiagram.SpeedFor(1000, locked), 9);
        }

        // Round the post: a quarter turn ends on its left; a drag winds on from the wrap there is, not from nothing.
        Assert.Equal(0, CapstanDiagram.TurnsAt(-Math.PI / 2, 0), 9);
        Assert.Equal(0.25, CapstanDiagram.TurnsAt(Math.PI, 0), 9);
        Assert.Equal(2.25, CapstanDiagram.TurnsAt(Math.PI, 2.1), 9);
        Assert.Equal(3, CapstanDiagram.TurnsAt(-Math.PI / 2, 2.9), 9);

        Assert.Equal(200, RollingDiagram.CrateForSide(90 + (200 * 0.12)), 9);
        Assert.Equal(500, RollingDiagram.CrateForSide(1000), 9);
        Assert.Equal(50, OilDiagram.SpeedForArrow(80), 9);
        Assert.Equal(0, OilDiagram.SpeedForArrow(-5), 9);
    }

    [Fact]
    public void TheMotion_FollowsTheModels()
    {
        // A braking car slows steadily and stops where the model says, a locked one farther on.
        foreach (var locked in new[] { false, true })
        {
            Assert.Equal((0.0, 50 / 3.6), SkidDiagram.Stop(50, locked, 0));
            var (travelled, speed) = SkidDiagram.Stop(50, locked, 100);
            Assert.Equal(Skid.Distance(50, locked), travelled, 9);
            Assert.Equal(0, speed, 9);
            Assert.True(SkidDiagram.Stop(50, locked, 0.5).Travelled < 2 * SkidDiagram.Stop(50, locked, 0.25).Travelled); // slowing
        }

        // A sliding block speeds up: twice the time, four times the distance; steeper, faster.
        Assert.Equal(0, RampDiagram.SlidDown(30, 1, 0), 9);
        Assert.Equal(4 * RampDiagram.SlidDown(30, 1, 1), RampDiagram.SlidDown(30, 1, 2), 9);
        Assert.True(RampDiagram.SlidDown(40, 1, 2) > RampDiagram.SlidDown(25, 1, 2));
    }

    [AvaloniaFact]
    public void ThePictures_AreWorkedByTheirGrips()
    {
        var (window, model) = OpenAt("Friction");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Friction");
        var friction = Assert.IsType<FrictionTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Friction;
        Avalonia.Point Far(Lemon.Inside.Book.Controls.Picture picture, string grip, double dx, double dy) =>
            picture.GripCentre(grip)!.Value + new Avalonia.Vector(dx, dy);

        Lemon.Inside.Book.Controls.Picture Open(int spread)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, spread));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Lemon.Inside.Book.Controls.Picture>().First(p => p.IsEffectivelyVisible);
            Assert.NotEmpty(picture.MarkedGrips);
            return picture;
        }

        var ramp = Open(0);
        ramp.DragGrip("angle", Far(ramp, "angle", 0, -2000));
        Assert.Equal(45, friction.Ramp.Angle, 9);
        ramp.DragGrip("surface", default);
        Assert.Equal(2, friction.Ramp.Surface);

        var start = Open(1);
        start.DragGrip("pull", Far(start, "pull", 2000, 0));
        Assert.Equal(40, friction.Start.Pulling, 9);
        start.DragGrip("box", Far(start, "box", 0, -2000));
        Assert.Equal(80, friction.Start.Box, 9);

        var skid = Open(2);
        skid.DragGrip("speed", Far(skid, "speed", 2000, 0));
        Assert.Equal(120, friction.Skid.Speed, 9);
        skid.DragGrip("lock", default);
        Assert.True(friction.Skid.Locked);

        var capstan = Open(3);
        capstan.DragGrip("turns", capstan.PartCentre("post")!.Value + new Avalonia.Vector(-100, 0));
        Assert.Equal(0.25, friction.Capstan.Turns, 9);
        capstan.DragGrip("wet", default);
        Assert.True(friction.Capstan.Wet);

        var rolling = Open(4);
        rolling.DragGrip("way", default);
        Assert.Equal(1, friction.Rolling.Way);
        rolling.DragGrip("crate", Far(rolling, "crate", 0, -2000));
        Assert.Equal(500, friction.Rolling.Crate, 9);

        var oil = Open(5);
        oil.DragGrip("speed", Far(oil, "speed", -2000, 0));
        Assert.Equal(0, friction.Oil.Speed, 9);
        oil.DragGrip("speed", Far(oil, "speed", 2000, 0));
        Assert.Equal(100, friction.Oil.Speed, 9);
        oil.DragGrip("oil", default);
        Assert.True(friction.Oil.Oiled);

        Frames(5);
        Snapshot(window, "friction-6-gripped");
        window.Close();
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Friction");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Friction");
        Type[] pictures =
        [
            typeof(RampDiagram), typeof(StartDiagram), typeof(SkidDiagram), typeof(CapstanDiagram), typeof(RollingDiagram),
            typeof(OilDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"friction-{s + 1}");
        }

        var friction = Assert.IsType<FrictionTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Friction;
        (friction.Ramp.Angle, friction.Ramp.Surface) = (30, 1);
        (friction.Start.Box, friction.Start.Pulling) = (40, 25);
        (friction.Skid.Speed, friction.Skid.Locked) = (120, true);
        (friction.Capstan.Turns, friction.Capstan.Wet) = (2.6, false);
        (friction.Rolling.Way, friction.Rolling.Crate) = (2, 500);
        (friction.Oil.Oiled, friction.Oil.Speed) = (true, 5);
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(10);
            Snapshot(window, $"friction-{s + 1}-changed");
        }

        window.Close();
    }
}
