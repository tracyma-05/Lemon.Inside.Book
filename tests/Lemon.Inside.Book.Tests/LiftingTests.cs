using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Lifting;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.Controls;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The Lifting chapter: the models behind its pictures, its words, and every spread drawn.</summary>
public sealed class LiftingTests
{
    [Fact]
    public void TheLever_TradesForceForDistance()
    {
        Assert.Equal(Seesaw.Stone, Seesaw.Effort(Seesaw.Stone, 2), 9); // pivot in the middle: no gain
        Assert.Equal(50, Seesaw.Effort(Seesaw.Stone, 0.8), 9);
        Assert.Equal(4, Seesaw.Travel(0.8), 9);

        foreach (var pivot in new[] { 0.6, 0.8, 1.3, 2.0 })
        {
            // Both ends start and finish on the ground; in between, the hand moves Travel times as far as the stone.
            Assert.Equal(0, Seesaw.Rise(pivot, 0), 9);
            Assert.Equal(0, Seesaw.PivotHeight + (pivot * Math.Sin(Seesaw.Tilt(pivot, 0))), 9);
            Assert.Equal(0, Seesaw.PivotHeight - ((Seesaw.Length - pivot) * Math.Sin(Seesaw.Tilt(pivot, 1))), 9);
            Assert.Equal(Seesaw.Drop(pivot, 0.6), Seesaw.Rise(pivot, 0.6) * Seesaw.Travel(pivot), 9);

            // Work in equals work out: push × hand's travel = stone × its rise.
            Assert.Equal(Seesaw.Stone * Seesaw.Rise(pivot, 1), Seesaw.Effort(Seesaw.Stone, pivot) * Seesaw.Drop(pivot, 1), 6);
        }
    }

    [Fact]
    public void AMovingPulley_HalvesThePull_AndDoublesTheRope()
    {
        Assert.Equal(Pulleys.Bucket, Pulleys.Effort(Pulleys.Bucket, false), 9);
        Assert.Equal(Pulleys.Bucket / 2, Pulleys.Effort(Pulleys.Bucket, true), 9);
        Assert.Equal(1, Pulleys.Rise(1, false), 9);
        Assert.Equal(0.5, Pulleys.Rise(1, true), 9);
    }

    [Fact]
    public void ABlockAndTackle_DividesByItsStrands_LessALittleForFriction()
    {
        for (var n = 1; n <= Tackle.MostStrands; n++)
        {
            Assert.Equal(n, Tackle.Advantage(n, false), 9);
            Assert.True(Tackle.Advantage(n, true) < n);
            Assert.Equal(n, Tackle.RopePerMetre(n), 9);
            var (top, bottom) = Tackle.Wheels(n);
            Assert.Equal(n, top + bottom); // every strand passes one wheel after the hand's
            Assert.InRange(top - bottom, 0, 1);
            if (n > 1) Assert.True(Tackle.Advantage(n, true) > Tackle.Advantage(n - 1, true));
        }

        Assert.Equal(75, Tackle.Effort(Tackle.Load, 4, false), 9);
        Assert.InRange(Tackle.Advantage(4, true), 3.4, 3.6); // "about three and a half times"
        Assert.InRange(Tackle.Effort(Tackle.Load, 1, true), Tackle.Load, Tackle.Load * 1.06);
    }

    [Fact]
    public void TheWorm_MultipliesTheForce_AndHoldsTheLoad()
    {
        Assert.InRange(Winch.Effort(Winch.Load, 0.3, false), 16.6, 16.7); // a sixth of the load
        Assert.True(Winch.WormEfficiency < 0.5); // "more than half of your work turns into heat"
        Assert.InRange(Winch.Effort(Winch.Load, 0.3, true), 1.9, 2.1);
        Assert.True(Winch.SelfLocking(Winch.Lead, Winch.Friction));
        Assert.False(Winch.SelfLocking(20, Winch.Friction)); // a steep thread runs back
        Assert.True(Winch.Holds(true));
        Assert.False(Winch.Holds(false));
        Assert.Equal(Winch.TurnsPerMetre(false) * Winch.WormRatio, Winch.TurnsPerMetre(true), 9);
        Assert.Equal(3, Math.Round(Winch.TurnsPerMetre(false)));
    }

    [Fact]
    public void TheJack_MultipliesByTheAreas_AndLiftsLittlePerStroke()
    {
        Assert.Equal(16, Jack.AreaRatio(4), 9);
        Assert.InRange(Jack.Effort(Jack.Load, 4), 10, 11); // "about ten kilograms"
        Assert.Equal(1.25, Jack.RisePerStroke(4), 9);
        Assert.Equal(10, Jack.Rise(4, 8), 9); // the walkthrough's centimetre

        // The oil pushed out of the pump equals the oil under the ram: pump area × stroke = ram area × rise.
        foreach (var d in new[] { 2.0, 4.0, 7.5, 10.0 })
        {
            Assert.Equal(Jack.Stroke, Jack.AreaRatio(d) * Jack.RisePerStroke(d), 9);
        }
    }

    [Fact]
    public void TheCounterweight_LeavesTheMotorOnlyTheDifference()
    {
        Assert.Equal(Elevator.Car, Elevator.Motor(0, false), 9);
        Assert.Equal(Elevator.Counterweight - Elevator.Car, Elevator.Motor(0, true), 9);
        Assert.Equal(0, Elevator.Motor(6, true), 9); // six riders: balanced
        Assert.Equal(0, Elevator.Heavier(6, true));
        Assert.Equal(-1, Elevator.Heavier(0, true));
        Assert.Equal(1, Elevator.Heavier(Elevator.MostRiders, true));

        for (var riders = 0; riders <= Elevator.MostRiders; riders++)
        {
            Assert.True(Elevator.Motor(riders, true) <= (0.55 * Elevator.Rated)); // never much more than half a load
            Assert.True(Elevator.Motor(riders, true) < Elevator.Motor(riders, false));
            Assert.False(Elevator.Slips(riders, true));
        }

        Assert.InRange(Elevator.Grip, 2.5, 2.6); // "about two and a half times"
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var lifting = new LiftingViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Lifting").Spreads, lifting.Spreads.Count);

        lifting.Seesaw.Pivot = 0.8;
        Assert.Equal(50, lifting.Seesaw.Effort, 9);

        lifting.Pulley.Moving = true;
        Assert.Equal(20, lifting.Pulley.Effort, 9);

        lifting.Tackle.Strands = 4;
        Assert.Equal(75, lifting.Tackle.Effort, 9);

        lifting.Winch.Worm = true;
        Assert.True(lifting.Winch.Holds);

        (lifting.Jack.Ram, lifting.Jack.Strokes) = (4, 8);
        Assert.Equal(10, lifting.Jack.Rise, 9);

        (lifting.Elevator.Riders, lifting.Elevator.Counterweight) = (6, true);
        Assert.Equal(0, lifting.Elevator.Motor, 9);
        Assert.False(string.IsNullOrWhiteSpace(lifting.Elevator.ReadoutText));
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new LiftingViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }
        }
    }

    [Fact]
    public void TheGrips_WorkBackFromWhereTheHandIs()
    {
        // The seesaw's hand: dragged right down, the push is full; held where it starts, none.
        Assert.Equal(1, SeesawDiagram.PushFor(0.8, -5), 9);
        Assert.Equal(0, SeesawDiagram.PushFor(0.8, 5), 9);
        Assert.InRange(SeesawDiagram.PushFor(2, Seesaw.PivotHeight + 0.09 + 0.13), 0.4, 0.6); // the bar level: halfway

        // The pulley's hand goes down past a fixed wheel and up from a moving one.
        Assert.Equal(1, PulleyDiagram.PullFor(false, 2.2), 9);
        Assert.Equal(1, PulleyDiagram.PullFor(true, 2.9), 9);
        Assert.Equal(PulleySpread.MostPull, PulleyDiagram.PullFor(false, -9), 9);

        // The tackle's last strand sits n half-gaps right of the middle; its first wheel runs with the hand's rope,
        // the last with only a strand's share.
        Assert.Equal(4, TackleDiagram.StrandsAt(4 * 0.13 * 100, 100));
        Assert.Equal(1, TackleDiagram.StrandsAt(-500, 100));
        Assert.Equal(Tackle.MostStrands, TackleDiagram.StrandsAt(5000, 100));
        Assert.Equal(1.2, TackleDiagram.WheelTurn(4, 0, 1.2), 9);
        Assert.Equal(0.3, TackleDiagram.WheelTurn(4, 3, 1.2), 9);
    }

    [Fact]
    public void AStroke_PushesOilToTheRam_ThenRefillsFromTheTank()
    {
        var down = JackDiagram.Stroke(0.25);
        Assert.True(down.Outlet && !down.Inlet);
        Assert.Equal(0.5, down.Depth, 9);
        Assert.Equal(0.5, down.RamShare, 9); // the ram rises while the piston goes down
        var up = JackDiagram.Stroke(0.75);
        Assert.True(up.Inlet && !up.Outlet);
        Assert.Equal(1, up.RamShare, 9); // and holds while it comes back
        Assert.Equal(0, JackDiagram.Stroke(0).Depth, 9);
    }

    [Fact]
    public void TheLift_RunsDownAndBack_PausingAtEachEnd()
    {
        Assert.Equal(0, ElevatorDiagram.Travel(0), 9);
        Assert.Equal(1, ElevatorDiagram.Travel(7), 9);
        Assert.Equal(0, ElevatorDiagram.Travel(14), 9);
        Assert.Equal(0, ElevatorDiagram.Travel(0.5), 9); // still waiting at the top
        Assert.InRange(ElevatorDiagram.Travel(3.5), 0.4, 0.6);
    }

    /// <summary>Every picture has grips, and dragging or tapping them moves the spread's own values (the sliders follow).</summary>
    [AvaloniaFact]
    public void ThePictures_AreWorkedByTheirGrips()
    {
        var (window, model) = OpenAt("Lifting");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Lifting");
        var lifting = Assert.IsType<LiftingTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Lifting;
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

        var seesaw = Open(0);
        seesaw.DragGrip("pivot", Far(seesaw, "pivot", -2000, 0));
        Assert.Equal(0.6, lifting.Seesaw.Pivot, 9);
        seesaw.DragGrip("pivot", Far(seesaw, "pivot", 2000, 0));
        Assert.Equal(2, lifting.Seesaw.Pivot, 9);
        seesaw.DragGrip("push", Far(seesaw, "push", 0, 2000));
        Assert.Equal(1, lifting.Seesaw.Push, 9);

        var pulley = Open(1);
        pulley.DragGrip("pull", Far(pulley, "pull", 0, 2000));
        Assert.Equal(PulleySpread.MostPull, lifting.Pulley.Pull, 9);
        pulley.DragGrip("wheel", default);
        Assert.True(lifting.Pulley.Moving);

        var tackle = Open(2);
        tackle.DragGrip("strands", Far(tackle, "strands", 2000, 0));
        Assert.Equal(Tackle.MostStrands, lifting.Tackle.Strands);
        tackle.DragGrip("friction", default);
        Assert.True(lifting.Tackle.Friction);
        tackle.DragGrip("pull", Far(tackle, "pull", 0, 2000));
        Assert.Equal(TackleSpread.MostPull, lifting.Tackle.Pull, 9);

        var winch = Open(3);
        winch.DragGrip("handle", Far(winch, "handle", -2000, 2000));
        Assert.Equal(0.5, lifting.Winch.Handle, 9);
        winch.DragGrip("letgo", default);
        Assert.True(lifting.Winch.LetGo);
        winch.DragGrip("worm", default);
        Assert.True(lifting.Winch.Worm);

        var jack = Open(4);
        jack.DragGrip("pump", default);
        jack.DragGrip("pump", default);
        Assert.Equal(2, lifting.Jack.Strokes);
        jack.DragGrip("ram", Far(jack, "ram", 2000, 0));
        Assert.Equal(10, lifting.Jack.Ram, 9);
        jack.DragGrip("release", default);
        Assert.Equal(0, lifting.Jack.Strokes);

        var elevator = Open(5);
        elevator.DragGrip("riders", Far(elevator, "riders", 2000, 0));
        Assert.Equal(Elevator.MostRiders, lifting.Elevator.Riders);
        elevator.DragGrip("counterweight", default);
        Assert.True(lifting.Elevator.Counterweight);

        window.Close();
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Lifting");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Lifting");
        Type[] pictures =
        [
            typeof(SeesawDiagram), typeof(PulleyDiagram), typeof(TackleDiagram), typeof(WinchDiagram), typeof(JackDiagram),
            typeof(ElevatorDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"lifting-{s + 1}");
        }

        var lifting = Assert.IsType<LiftingTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Lifting;
        (lifting.Seesaw.Pivot, lifting.Seesaw.Push) = (0.8, 1);
        (lifting.Pulley.Moving, lifting.Pulley.Pull) = (true, 2);
        (lifting.Tackle.Strands, lifting.Tackle.Friction, lifting.Tackle.Pull) = (5, true, 1.5);
        (lifting.Winch.Worm, lifting.Winch.Handle) = (true, 0.45);
        (lifting.Jack.Ram, lifting.Jack.Strokes) = (2, 30);
        (lifting.Elevator.Riders, lifting.Elevator.Counterweight) = (13, true);
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(30);
            Snapshot(window, $"lifting-{s + 1}-changed");
        }

        window.Close();
    }
}
