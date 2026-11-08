using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Canals;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.Controls;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The Canals chapter: the models behind its pictures, its words, and every spread drawn.</summary>
public sealed class CanalsTests
{
    [Fact]
    public void ALock_FillsFastThenSlowly()
    {
        Assert.InRange(Chamber.FillTime(1) / 60, 7, 9); // about eight minutes
        Assert.InRange(Chamber.FillTime(4) / 60, 1.8, 2.2); // "about two minutes"
        Assert.Equal(4, Chamber.FillTime(1) / Chamber.FillTime(4), 9);
        Assert.InRange(Chamber.MetreTime(1, true) / Chamber.MetreTime(1, false), 2.9, 3.3); // "three times as long"
        Assert.True(Chamber.RiseRate(0, 1) > Chamber.RiseRate(2.5, 1));
        Assert.Equal(0, Chamber.RiseRate(Chamber.Rise, 1), 9);
        Assert.InRange(Math.Sqrt(2 * Chamber.G * Chamber.Rise), 7.4, 7.8); // "about seven and a half metres a second"
    }

    [Fact]
    public void TheWater_PressesMitreGatesShut()
    {
        Assert.InRange(Gates.Push(3), 15, 17); // "about sixteen tonnes"
        Assert.Equal(4, Gates.Push(4) / Gates.Push(2), 9); // twice the difference, four times the push
        Assert.True(Gates.Hold(3, false));
        Assert.False(Gates.Hold(3, true));
        Assert.True(Gates.Hold(0, true)); // level: nothing pushes
    }

    [Fact]
    public void SidePonds_SaveTheUpperSlices()
    {
        Assert.Equal(540, Ponds.Chamberful, 9);
        Assert.Equal(1, Ponds.Share(0), 9);
        Assert.Equal(0.5, Ponds.Share(2), 9);
        Assert.Equal(0.4, Ponds.Share(3), 9); // "two-fifths"
    }

    [Fact]
    public void AFlight_UsesLessWater_ButTakesLonger()
    {
        Assert.Equal(4, Flight.Step(6), 9);
        Assert.Equal(Flight.Water(1) / 6, Flight.Water(6), 9);
        Assert.True(Flight.Minutes(6) > 60); // "over an hour"
        Assert.Equal(8, Flight.Hill / 3, 9); // "an eight-storey building"
    }

    [Fact]
    public void AFloatingBoat_AddsNoWeight()
    {
        Assert.Equal(540, BoatLift.Water, 9);
        Assert.Equal(0, BoatLift.Imbalance(true, 0), 9);
        Assert.Equal(18, BoatLift.Imbalance(false, 10), 9);
    }

    [Fact]
    public void TheDrag_GrowsWithTheSquareOfTheSpeed()
    {
        Assert.InRange(Towing.Drag(30, 4), 55, 65); // "about sixty"
        Assert.Equal(4, Towing.Drag(30, 8) / Towing.Drag(30, 4), 9);
        Assert.True(Towing.Manages(30, 4));
        Assert.False(Towing.Manages(30, 8));
        Assert.InRange(Towing.OnRoad, 1.4, 1.7); // "a tonne and a half"
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var canals = new CanalsViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Canals").Spreads, canals.Spreads.Count);
        canals.Gates.Downstream = true;
        Assert.False(canals.Gates.Holds);
        canals.Lift.Boat = true;
        Assert.Equal(0, canals.Lift.Imbalance, 9);
        foreach (var spread in canals.Spreads)
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)spread.GetType().GetProperty("ReadoutText")!.GetValue(spread)));
        }
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new CanalsViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }
        }
    }

    [Fact]
    public void TheWaterCycle_EmptiesIntoThePonds_AndFillsBackFromThem()
    {
        // Two ponds, four slabs: the top two go into ponds 1 and 2, the last two downhill.
        var start = WaterDiagram.Cycle(2, 0.6);
        Assert.True(start.Emptying);
        Assert.Equal(0, start.Pond);
        Assert.InRange(start.Level, 3.4, 3.6);
        var down = WaterDiagram.Cycle(2, 1.2 * 3.5);
        Assert.True(down.ToLower);
        Assert.Equal([1.0, 1.0, 0.0], down.Ponds);
        var bottom = WaterDiagram.Cycle(2, (1.2 * 4) + 0.5);
        Assert.Equal(0, bottom.Level, 9);

        // Coming up, the lower pond gives back first and the summit makes up the top two.
        var half = (4 * 1.2) + 0.9;
        var up = WaterDiagram.Cycle(2, half + 0.6);
        Assert.False(up.Emptying);
        Assert.Equal(1, up.Pond);
        Assert.True(WaterDiagram.Cycle(2, half + (1.2 * 3.5)).FromSummit);
        Assert.Equal([0.0, 0.0, 0.0], WaterDiagram.Cycle(2, half + (1.2 * 3.5)).Ponds);
    }

    [Fact]
    public void TheBoat_ClimbsTheFlight_OneLockAtATime()
    {
        Assert.Equal(-1, FlightDiagram.Trip(3, 0.5).Chamber); // waiting at the bottom
        var lifting = FlightDiagram.Trip(3, 1.2 + 0.9 + 0.65);
        Assert.Equal(0, lifting.Chamber);
        Assert.True(lifting.Rising);
        Assert.InRange(lifting.Level, 0.1, Flight.Step(3) - 0.1);
        var top = FlightDiagram.Trip(3, 1.2 + (3 * 2.2) + 0.9 + 0.5);
        Assert.Equal(Flight.Hill, top.Level, 9);
        Assert.Equal(6, FlightDiagram.Steps(4));
        Assert.Equal(1, FlightDiagram.Steps(30));
        Assert.Equal(8, FlightDiagram.Steps(-1));
        Assert.True(LiftDiagram.Pace(18) < LiftDiagram.Pace(0));
    }

    /// <summary>Every picture has grips, and dragging or tapping them moves the spread's own values (the sliders follow).</summary>
    [AvaloniaFact]
    public void ThePictures_AreWorkedByTheirGrips()
    {
        var (window, model) = OpenAt("Canals");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Canals");
        var canals = Assert.IsType<CanalsTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Canals;
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

        var lockPicture = Open(0);
        lockPicture.DragGrip("fill", default);
        Assert.True(canals.Lock.Filling);
        lockPicture.DragGrip("paddles", Far(lockPicture, "paddles", 0, -500));
        Assert.Equal(4, canals.Lock.Paddles);
        lockPicture.DragGrip("empty", default);
        Assert.False(canals.Lock.Filling);

        var gates = Open(1);
        gates.DragGrip("head", Far(gates, "head", 0, 500));
        Assert.Equal(0, canals.Gates.Head, 9);
        gates.DragGrip("flip", default);
        Assert.True(canals.Gates.Downstream);

        var water = Open(2);
        water.DragGrip("pond1", default);
        Assert.Equal(1, canals.Water.SidePonds);
        water.DragGrip("boats", Far(water, "boats", 2000, 0));
        Assert.Equal(100, canals.Water.Boats, 9);

        var flight = Open(3);
        flight.DragGrip("step", Far(flight, "step", 0, 2000));
        Assert.Equal(8, canals.Flight.Locks);

        var lift = Open(4);
        lift.DragGrip("boat", default);
        Assert.True(canals.Lift.Boat);
        lift.DragGrip("extra", Far(lift, "extra", 0, -2000));
        Assert.Equal(20, canals.Lift.Extra, 9);

        var towing = Open(5);
        towing.DragGrip("speed", Far(towing, "speed", 2000, 0));
        Assert.Equal(8, canals.Towing.Speed, 9);
        towing.DragGrip("load", Far(towing, "load", 0, -2000));
        Assert.Equal(60, canals.Towing.Load, 9);

        window.Close();
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Canals");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Canals");
        Type[] pictures =
        [
            typeof(LockDiagram), typeof(GatesDiagram), typeof(WaterDiagram), typeof(FlightDiagram), typeof(LiftDiagram), typeof(TowingDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"canals-{s + 1}");
        }

        var canals = Assert.IsType<CanalsTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Canals;
        (canals.Lock.Paddles, canals.Lock.Filling) = (4, true);
        (canals.Gates.Head, canals.Gates.Downstream) = (3, true);
        (canals.Water.SidePonds, canals.Water.Boats) = (3, 80);
        canals.Flight.Locks = 6;
        (canals.Lift.Boat, canals.Lift.Extra) = (true, 10);
        (canals.Towing.Load, canals.Towing.Speed) = (50, 7);
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(30);
            Snapshot(window, $"canals-{s + 1}-changed");
        }

        window.Close();
    }
}
