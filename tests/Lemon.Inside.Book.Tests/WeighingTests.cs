using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Weighing;
using Lemon.Book.Shell.Infrastructures.Timing;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.Controls;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The Weighing chapter: the models behind its pictures, its words, and every spread drawn.</summary>
public sealed class WeighingTests
{
    [Fact]
    public void TheBalance_SettlesAtATilt_ThatGrowsAsItsCentreOfMassRises()
    {
        Assert.Equal(0, Balance.Tilt(0, 2), 9);
        Assert.InRange(Balance.Tilt(10, 2), 19, 22); // "about twenty degrees"
        Assert.True(Balance.Tilt(10, 1) >= 35);
        Assert.True(Balance.Tilt(10, 1.2) < 35); // the walkthrough's step is not already done
        Assert.True(Balance.Tilt(10, 1) > Balance.Tilt(10, 3));
        Assert.True(Balance.Topples(0));
        Assert.True(Balance.Topples(-0.5));
        Assert.Equal(Balance.MostTilt, Balance.Tilt(0, -0.5));
    }

    [Fact]
    public void TheSteelyard_IsMarkedInKilograms()
    {
        Assert.Equal(40, Steelyard.BalanceAt(8), 9);
        Assert.Equal(75, Steelyard.BalanceAt(15), 9);
        Assert.Equal(8, Steelyard.Reading(40), 9);
        Assert.True(Steelyard.Balanced(8, 40));
        Assert.True(Steelyard.Tilt(8, 20) > 0); // the sack's side sinks
        Assert.True(Steelyard.Tilt(8, 60) < 0);
        Assert.True(Steelyard.BalanceAt(18) <= Steelyard.LongArm);
    }

    [Fact]
    public void TheSpring_StretchesInStep_UntilItIsOverloaded()
    {
        Assert.Equal(4, Spring.Stretch(2, 2), 9);
        Assert.Equal(2 * Spring.Stretch(2, 2), Spring.Stretch(4, 4), 9);
        Assert.Equal(0, Spring.Reading(0, Spring.Limit), 9); // up to its limit it comes back to zero
        Assert.True(Spring.Reading(0, 7) > 0); // overloaded, it does not
        Assert.True(Spring.Reading(2, 7) > 2);

        var spread = new SpringSpread { Mass = 7 };
        spread.Mass = 1;
        Assert.Equal(7, spread.Highest, 9);
        spread.Off = true;
        Assert.Equal(0, spread.Load);
    }

    [Fact]
    public void ParallelLinks_ReadTheSameAnywhereOnThePan()
    {
        Assert.Equal(Roberval.Load, Roberval.Reading(0, false), 9);
        Assert.Equal(750, Roberval.Reading(5, false), 9);
        Assert.True(Roberval.Reading(-5, false) < Roberval.Load);
        foreach (var offset in new[] { -6.0, -2, 0, 3, 6 }) Assert.Equal(Roberval.Load, Roberval.Reading(offset, true), 9);
    }

    [Fact]
    public void FourGauges_CancelTheWarmth()
    {
        Assert.Equal(200e-6, Gauge.Strain(2), 12);
        Assert.InRange(Gauge.Change(2), 0.1, 0.15); // "just over a tenth of an ohm"
        Assert.Equal(2, Gauge.Reading(2, false, false), 9);
        Assert.Equal(3, Gauge.Reading(2, true, false), 9); // "a kilogram more"
        Assert.Equal(2, Gauge.Reading(2, true, true), 9);
        Assert.Equal(4 * Gauge.Output(2, false), Gauge.Output(2, true), 9);
        Assert.Equal(5, Gauge.Output(Gauge.FullLoad, true), 9); // a few millivolts
    }

    [Fact]
    public void ASpringWeighsThePull_ABalanceTheStuff()
    {
        Assert.Equal(1, Worlds.SpringReads(1, 0), 9);
        Assert.InRange(Worlds.SpringReads(1, 1), 1 / 6.5, 1 / 5.5); // "about a sixth"
        Assert.InRange(Worlds.SpringReads(1, 2), 0.37, 0.39);
        foreach (var place in new[] { 0, 1, 2 }) Assert.Equal(2, Worlds.BalanceReads(2), 9);
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var weighing = new WeighingViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Weighing").Spreads, weighing.Spreads.Count);

        weighing.Steelyard.Position = 40;
        Assert.True(weighing.Steelyard.Balanced);

        weighing.Roberval.Parallel = true;
        weighing.Roberval.Offset = -5;
        Assert.Equal(Roberval.Load, weighing.Roberval.Reading, 9);

        foreach (var spread in weighing.Spreads)
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)spread.GetType().GetProperty("ReadoutText")!.GetValue(spread)));
        }
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new WeighingViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }

            foreach (var place in new[] { 0, 1, 2 }) Assert.Contains(MoonSpread.PlaceKey(place), keys);
        }
    }

    [Fact]
    public void Solve_FindsTheValueThatDrawsAPlace()
    {
        Assert.Equal(3, WeighingParts.Solve(x => x * x, 9, 0, 5), 6);
        Assert.Equal(0, WeighingParts.Solve(x => x * x, -4, 0, 5)); // beyond either end, the end
        Assert.Equal(5, WeighingParts.Solve(x => x * x, 400, 0, 5));
        Assert.Equal(1.5, WeighingParts.Solve(x => Math.Min(x, 2), 1.5, 0, 5), 6); // a drawing that stops moving (a beam on its stop) still works below the stop
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Weighing");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Weighing");
        Type[] pictures =
        [
            typeof(BalanceDiagram), typeof(SteelyardDiagram), typeof(SpringDiagram), typeof(RobervalDiagram), typeof(StrainDiagram),
            typeof(MoonDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"weighing-{s + 1}");
        }

        var weighing = Assert.IsType<WeighingTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Weighing;
        (weighing.Balance.Difference, weighing.Balance.Drop) = (10, 1);
        (weighing.Steelyard.Sack, weighing.Steelyard.Position) = (15, 75);
        (weighing.Spring.Mass, weighing.Spring.Off) = (7, false);
        weighing.Spring.Off = true;
        (weighing.Roberval.Offset, weighing.Roberval.Parallel) = (5, true);
        (weighing.Strain.Mass, weighing.Strain.Warm, weighing.Strain.Bridge) = (4, true, false);
        (weighing.Moon.Place, weighing.Moon.Mass) = (1, 3);
        BookClock.BeginStepping();
        try
        {
            for (var s = 0; s < pictures.Length; s++)
            {
                model.GoToPage(BookContents.SpreadPage(chapter, s));
                Frames(2);

                // Let the moving pictures settle: the beams stop swinging, the loads stop bobbing.
                for (var i = 0; i < 240; i++)
                {
                    BookClock.Advance(1.0 / 30);
                    Frames(1);
                }

                Snapshot(window, $"weighing-{s + 1}-changed");
            }
        }
        finally
        {
            BookClock.EndStepping();
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ThePictures_AreWorkedByTheirGrips()
    {
        var (window, model) = OpenAt("Weighing");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Weighing");
        var weighing = Assert.IsType<WeighingTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Weighing;
        Avalonia.Point Far(Picture picture, string grip, double dx, double dy) =>
            picture.GripCentre(grip)!.Value + new Avalonia.Vector(dx, dy);

        Picture Open(int spread)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, spread));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Picture>().First(p => p.IsEffectivelyVisible);
            Assert.NotEmpty(picture.MarkedGrips);
            return picture;
        }

        var balance = Open(0);
        balance.DragGrip("difference", Far(balance, "difference", 0, 2000));
        Assert.Equal(20, weighing.Balance.Difference, 9);
        balance.DragGrip("drop", Far(balance, "drop", 0, 2000));
        Assert.Equal(4, weighing.Balance.Drop, 9);
        Frames(2);
        balance.DragGrip("difference", Far(balance, "difference", 0, -2000));
        Assert.Equal(0, weighing.Balance.Difference, 9);

        var steelyard = Open(1);
        steelyard.DragGrip("poise", Far(steelyard, "poise", 2000, 0));
        Assert.Equal(Steelyard.LongArm, weighing.Steelyard.Position, 9);
        steelyard.DragGrip("sack", Far(steelyard, "sack", 0, 2000));
        Assert.Equal(18, weighing.Steelyard.Sack, 9);
        Frames(2);
        steelyard.DragGrip("sack", Far(steelyard, "sack", 0, -2000));
        Assert.Equal(2, weighing.Steelyard.Sack, 9);

        var spring = Open(2);
        spring.DragGrip("load", Far(spring, "load", 0, 2000));
        Assert.Equal(8, weighing.Spring.Mass, 9);
        spring.DragGrip("off", default);
        Assert.True(weighing.Spring.Off);
        Frames(2);
        Assert.DoesNotContain("load", spring.MarkedGrips); // nothing hangs to drag
        spring.DragGrip("off", default);
        Assert.False(weighing.Spring.Off);

        var roberval = Open(3);
        roberval.DragGrip("offset", Far(roberval, "offset", 2000, 0));
        Assert.Equal(6, weighing.Roberval.Offset, 9);
        roberval.DragGrip("parallel", default);
        Assert.True(weighing.Roberval.Parallel);

        var strain = Open(4);
        strain.DragGrip("mass", Far(strain, "mass", 0, 2000));
        Assert.Equal(Gauge.FullLoad, weighing.Strain.Mass, 9);
        strain.DragGrip("warm", default);
        Assert.True(weighing.Strain.Warm);
        strain.DragGrip("bridge", default);
        Assert.True(weighing.Strain.Bridge);

        var moon = Open(5);
        moon.DragGrip("mass", Far(moon, "mass", 0, 2000));
        Assert.Equal(5, weighing.Moon.Mass, 9);
        moon.DragGrip("place", default);
        Assert.Equal(1, weighing.Moon.Place);

        window.Close();
    }
}
