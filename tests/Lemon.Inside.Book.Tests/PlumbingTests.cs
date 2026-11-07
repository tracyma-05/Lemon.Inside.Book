using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Plumbing;
using Lemon.Inside.Book.Contents;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The Plumbing chapter: the models behind its pictures, its words, and every spread drawn.</summary>
public sealed class PlumbingTests
{
    [Fact]
    public void Height_IsPressure()
    {
        Assert.InRange(Water.Pressure(10), 0.95, 1.0); // "about one bar for every ten metres"
        Assert.InRange(Water.Pressure(Water.Atmosphere), 1.0, 1.02); // the atmosphere holds up 10.3 m of water
        Assert.Equal(0, Water.Pressure(-3));

        Assert.Equal(29, Tower.Head(30, 1), 9);
        Assert.True(Tower.Flows(30, 24));
        Assert.InRange(Water.Speed(Tower.Head(30, 24)), 10.5, 11); // six metres of head
        Assert.False(Tower.Flows(30, 30)); // level with the tower's water: nothing
        Assert.False(Tower.Flows(30, 33));
    }

    [Fact]
    public void TheTap_IsFullyOpenOnceTheGapIsAsBigAsTheHole()
    {
        Assert.Equal(0, Tap.Flow(0, false));
        Assert.Equal(2, Tap.FullTurns, 9);
        Assert.InRange(Tap.Lift(0.5), 0.5, 0.99); // "less than a millimetre"
        Assert.True(Tap.Flow(0.5, false) > 0);
        Assert.Equal(1, Tap.Opening(2), 9);
        Assert.Equal(Tap.Flow(2, false), Tap.Flow(3, false), 9); // the third turn gives nothing more
        Assert.True(Tap.Flow(1, false) < Tap.Flow(2, false));
        Assert.True(Tap.Flow(0, true) > 0); // a worn washer drips
        Assert.True(Tap.Flow(0, true) < 0.1);
    }

    [Fact]
    public void TheSiphon_RunsWhileItsEndIsBelowTheTanksWater()
    {
        Assert.False(Siphon.Flows(false, 0.5)); // full of air
        Assert.True(Siphon.Flows(true, 0.5));
        Assert.False(Siphon.Flows(true, 2.5)); // end above the tank's water
        Assert.True(Siphon.Speed(true, 0) > Siphon.Speed(true, 1));
        Assert.True(Siphon.Crest > 3); // above the slider's highest end
        Assert.InRange(Siphon.HighestCrest, 10, 10.5);
    }

    [Fact]
    public void TheFloat_ShutsTheValveAtItsLevel_OrSpillsAboveTheOverflow()
    {
        Assert.Equal(1, Cistern.Opening(0, 28), 9);
        Assert.Equal(0, Cistern.Opening(28, 28), 9);
        Assert.InRange(Cistern.Opening(26, 28), 0.01, 0.99); // slows as it arrives
        Assert.False(Cistern.Spills(28));
        Assert.True(Cistern.Spills(37));

        // Filling from empty settles at the set level; flushing empties it; set too high, it rises to the overflow.
        var level = 0.0;
        for (var i = 0; i < 4000; i++) level = Cistern.Step(level, 28, false, 0.02);
        Assert.InRange(level, 27.5, 28);
        for (var i = 0; i < 200; i++) level = Cistern.Step(level, 28, true, 0.02);
        Assert.InRange(level, 0, 0.5);
        for (var i = 0; i < 4000; i++) level = Cistern.Step(level, 37, false, 0.02);
        Assert.Equal(Cistern.Overflow, level, 6);
    }

    [Fact]
    public void TheTrap_HoldsUntilTheSuctionIsAsDeepAsItsSeal()
    {
        Assert.Equal(5, Trap.Seal);
        Assert.Equal(3, Trap.Drop(3, false), 9);
        Assert.Equal(2, Trap.Left(3, false), 9);
        Assert.False(Trap.Broken(3, false));
        Assert.True(Trap.Broken(6, false));
        Assert.False(Trap.Broken(6, true)); // the vent lets air in
        Assert.False(Trap.Broken(12, true));
        Assert.Equal(0, Trap.Left(8, false), 9);
    }

    [Fact]
    public void APumpAtTheTop_CannotLiftWaterMoreThanTenMetres()
    {
        Assert.True(Pump.Works(4, false));
        Assert.False(Pump.Works(11, false));
        Assert.Equal(Water.Atmosphere, Pump.Rise(12, false), 9);
        Assert.True(Pump.Works(14, true));
        Assert.Equal(12, Pump.Rise(12, true), 9);
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var plumbing = new PlumbingViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Plumbing").Spreads, plumbing.Spreads.Count);

        plumbing.Tower.TapHeight = 24;
        Assert.Equal(6, plumbing.Tower.Head, 9);

        plumbing.Tap.Turns = 2;
        Assert.Equal(Tap.FullFlow, plumbing.Tap.Flow, 9);

        plumbing.Siphon.Primed = true;
        Assert.True(plumbing.Siphon.Flows);

        plumbing.Trap.Suction = 6;
        Assert.True(plumbing.Trap.Broken);
        plumbing.Trap.Vent = true;
        Assert.False(plumbing.Trap.Broken);

        plumbing.Pump.Depth = 12;
        Assert.False(plumbing.Pump.Works);
        plumbing.Pump.Down = true;
        Assert.True(plumbing.Pump.Works);

        foreach (var spread in plumbing.Spreads)
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)spread.GetType().GetProperty("ReadoutText")!.GetValue(spread)));
        }
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new PlumbingViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }
        }
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Plumbing");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Plumbing");
        Type[] pictures =
        [
            typeof(TowerDiagram), typeof(TapDiagram), typeof(SiphonDiagram), typeof(CisternDiagram), typeof(TrapDiagram),
            typeof(PumpDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"plumbing-{s + 1}");
        }

        var plumbing = Assert.IsType<PlumbingTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Plumbing;
        (plumbing.Tower.Surface, plumbing.Tower.TapHeight) = (25, 30);
        (plumbing.Tap.Turns, plumbing.Tap.Worn) = (0, true);
        (plumbing.Siphon.Primed, plumbing.Siphon.Outlet) = (true, 0.2);
        (plumbing.Cistern.Set, plumbing.Cistern.Flushing) = (37, true);
        (plumbing.Trap.Suction, plumbing.Trap.Vent) = (8, false);
        (plumbing.Pump.Depth, plumbing.Pump.Down) = (13, false);
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(30);
            Snapshot(window, $"plumbing-{s + 1}-changed");
        }

        window.Close();
    }
}
