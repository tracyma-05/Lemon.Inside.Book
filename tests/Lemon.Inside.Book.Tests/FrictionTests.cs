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
