using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Car;
using Lemon.Inside.Book.Contents;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The Car chapter: the models behind its pictures, its words, and every spread drawn.</summary>
public sealed class CarTests
{
    [Fact]
    public void TheCrank_GoesFromTopToBottom_AndALongRodApproachesAWave()
    {
        foreach (var rod in new[] { 1.6, 3.5, 5.0 })
        {
            Assert.Equal(0, SliderCrank.Travel(0, rod), 9);
            Assert.Equal(2, SliderCrank.Travel(Math.PI, rod), 9);
            Assert.True(SliderCrank.Travel(Math.PI / 2, rod) > SliderCrank.Wave(Math.PI / 2)); // past halfway at a quarter turn
            Assert.InRange(SliderCrank.FastestAt(rod), 45, 89.9);
        }

        Assert.True(SliderCrank.FastestAt(1.6) < SliderCrank.FastestAt(5)); // shorter rod, more lopsided
        Assert.Equal(SliderCrank.Wave(1), SliderCrank.Travel(1, 1e4), 3);
    }

    [Fact]
    public void TheCam_OpensOnceInTwoTurns_ForItsDuration()
    {
        Assert.Equal(9, Cam.Lift(360, 9, 240), 9);
        Assert.Equal(0, Cam.Lift(360 + 120, 9, 240), 9);
        Assert.Equal(0, Cam.Lift(0, 9, 240), 9);
        Assert.True(Cam.Lift(360 + 100, 9, 240) > 0);
        Assert.Equal(Cam.BaseRadius + 9, Cam.Radius(0, 9, 240), 9);  // the nose
        Assert.Equal(Cam.BaseRadius, Cam.Radius(180, 9, 240), 9);    // the back of the cam
        Assert.Equal(240.0 / 360 / 50 * 1000, Cam.OpenMilliseconds(240, 3000), 9);
    }

    [Fact]
    public void MoreCylinders_AndAHeavierFlywheel_SmoothTheTurning()
    {
        var previous = double.MaxValue;
        foreach (var count in Cylinders.Choices)
        {
            var wobble = Cylinders.Wobble(count, 1);
            Assert.True(wobble < previous, $"{count} cylinders: {wobble}");
            previous = wobble;
            Assert.True(Cylinders.Mean(count) > 0);
        }

        Assert.InRange(Cylinders.Wobble(1, 1), 0.03, 0.15);
        Assert.Equal(Cylinders.Wobble(1, 1) / 2, Cylinders.Wobble(1, 2), 9);
        var speeds = Cylinders.SpeedThroughCycle(4, 1);
        Assert.Equal(Cylinders.Wobble(4, 1), speeds.Max(), 2);
    }

    [Fact]
    public void TheClutch_SlipsThenGrips_AndApartItPassesNothing()
    {
        var clutch = new Clutch();
        for (var i = 0; i < 100; i++) clutch.Step(0.05, 1);
        Assert.Equal(0, clutch.Gearbox, 9);
        Assert.False(clutch.Gripping);

        var slipped = false;
        for (var i = 0; i < 400; i++)
        {
            clutch.Step(0.05, Math.Max(0, 0.75 - (i * 0.005)));
            slipped |= clutch.Heat > 0;
        }

        Assert.True(slipped);
        Assert.True(clutch.Gripping);
        Assert.Equal(clutch.Engine, clutch.Gearbox, 9);
        Assert.Equal(0, clutch.Heat, 9);
        Assert.True(clutch.Engine > 5);
    }

    [Fact]
    public void TheDifferential_TurnsAtTheAverage_AndShares()
    {
        var (inner, outer) = Differential.OnBend(5);
        Assert.InRange((outer / inner) - 1, 0.37, 0.39);
        Assert.Equal(10, Differential.Case(8, 12), 9);
        Assert.Equal(0, Differential.Spider(7, 7), 9);
        Assert.Equal(1, Differential.Case(inner, outer), 9);

        Assert.Equal(1, Differential.Push(0.8, 0.8, locked: false), 9);
        Assert.Equal(0.2, Differential.Push(0.1, 0.8, locked: false), 9);
        Assert.Equal(0.9, Differential.Push(0.1, 0.8, locked: true), 9);
        Assert.True(Differential.Spins(0.1, 0.8, locked: false));
        Assert.False(Differential.Spins(0.3, 0.8, locked: true));
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var car = new CarViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Car").Spreads, car.Spreads.Count);
        car.Cylinders.CountStep = 3;
        Assert.Equal(4, car.Cylinders.Count);
        car.Bend.Radius = 5;
        Assert.Contains("38", car.Bend.ReadoutText);
        car.Ice.LeftGrip = 0.1;
        Assert.Equal(0.2, car.Ice.Push, 9);
        car.Ice.Locked = true;
        Assert.Equal(0.9, car.Ice.Push, 9);
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new CarViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }
        }
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Car");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Car");
        Type[] pictures =
        [
            typeof(CrankDiagram), typeof(CamDiagram), typeof(CylindersDiagram), typeof(ClutchDiagram), typeof(BendDiagram),
            typeof(IceDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"car-{s + 1}");
        }

        var car = Assert.IsType<CarTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Car;
        car.Crank.Rod = 1.6;
        car.Cam.Duration = 290;
        (car.Cylinders.CountStep, car.Cylinders.Flywheel) = (3, 2);
        car.Clutch.Pedal = 0.5;
        car.Bend.Radius = 5;
        (car.Ice.LeftGrip, car.Ice.Locked) = (0.1, false);
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(30);
            Snapshot(window, $"car-{s + 1}-changed");
        }

        window.Close();
    }
}
