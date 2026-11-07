using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Lifting;
using Lemon.Inside.Book.Contents;
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
