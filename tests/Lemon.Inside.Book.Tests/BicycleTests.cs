using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Bicycle;
using Lemon.Inside.Book.Contents;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The Bicycle chapter: the models behind its pictures, its words, and every spread drawn.</summary>
public sealed class BicycleTests
{
    [Fact]
    public void ALoadedWheel_IsHeldUpByTheSpokes_AndOnlyTheOnesBelowLosePull()
    {
        const double Built = 1000;
        const double Kilograms = 80;
        foreach (var turned in new[] { 0.0, 0.1, 0.3 })
        {
            var unloaded = SpokedWheel.Tensions(Built, 0, turned);
            Assert.Equal(0, SpokedWheel.UpwardPull(unloaded, turned), 6); // balanced with no one on it

            var loaded = SpokedWheel.Tensions(Built, Kilograms, turned);
            Assert.Equal(Kilograms * Bike.Gravity, SpokedWheel.UpwardPull(loaded, turned), 6); // the difference holds the load
            for (var i = 0; i < loaded.Length; i++)
            {
                var below = Math.Cos(turned + (i * 2 * Math.PI / loaded.Length)) > 1e-9;
                if (below) Assert.True(loaded[i] < Built, $"spoke {i}");
                else Assert.Equal(Built, loaded[i], 9);
            }
        }

        // The spoke straight down loses about a fifth of the load.
        Assert.InRange(SpokedWheel.BottomLoss(100) / (100 * Bike.Gravity), 0.15, 0.25);
    }

    [Fact]
    public void LooseSpokes_GoSlackUnderAHeavyLoad()
    {
        var wheel = new WheelSpread { Tension = 1000, Load = 120 };
        Assert.False(wheel.IsSlack);
        wheel.Tension = 150;
        Assert.True(wheel.IsSlack);
        Assert.Contains(SpokedWheel.Tensions(150, 120), t => t == 0);
        Assert.All(SpokedWheel.Tensions(150, 120), t => Assert.True(t >= 0));
    }

    [Fact]
    public void TheChain_TurnsTheWheelByTheRatioOfTheTeeth()
    {
        Assert.Equal(2, Drive.Ratio(44, 22), 9);
        Assert.Equal(3 * Bike.WheelCircumference, Drive.Development(48, 16), 9);

        // Real sizes: a 53-tooth chainring is about 21 cm across, an 11-tooth sprocket about 4.5 cm.
        Assert.InRange(2 * Drive.PitchRadius(53), 0.20, 0.22);
        Assert.InRange(2 * Drive.PitchRadius(11), 0.04, 0.05);
    }

    [Fact]
    public void Climbing_ABiggerSprocket_IsSlowerButLighter_AndTheWorkStaysAlike()
    {
        const double Slope = 8;
        var cogs = Climb.Cassette;
        for (var i = 1; i < cogs.Count; i++)
        {
            Assert.True(Climb.Speed(cogs[i]) < Climb.Speed(cogs[i - 1]));
            Assert.True(Climb.PedalForce(Slope, cogs[i]) < Climb.PedalForce(Slope, cogs[i - 1]));
        }

        // Push × pedal travel per metre is the road's pull: the same work, whatever the gear, but for the air.
        foreach (var cog in cogs)
        {
            var pedalPath = 2 * Math.PI * Bike.CrankLength / Drive.Development(Climb.Chainring, cog);
            Assert.Equal(Climb.Resistance(Slope, Climb.Speed(cog)), Climb.PedalForce(Slope, cog) * pedalPath, 6);
        }

        // On the flat a middle gear is easy; up 8% only the low gears are.
        Assert.True(Climb.PedalKilograms(0, 15) <= Climb.ComfortableKilograms);
        Assert.True(Climb.PedalKilograms(Slope, 15) > Climb.ComfortableKilograms);
        Assert.True(Climb.PedalKilograms(Slope, cogs[^1]) <= Climb.ComfortableKilograms);
    }

    [Fact]
    public void TheFreewheel_DrivesForwards_AndClicksWhileTheWheelRunsAhead()
    {
        var hub = new Freewheel();
        for (var i = 0; i < 100; i++) hub.Step(0.01, 0.6);
        Assert.True(hub.Engaged);
        Assert.Equal(0.6, hub.WheelSpeed, 9);
        Assert.Equal(hub.Driver, hub.Wheel, 9);
        Assert.Equal(0, hub.Clicks);

        // Stop pedalling: the wheel rolls on, slowing a little, and the pawls tick over the teeth.
        for (var i = 0; i < 200; i++) hub.Step(0.01, 0);
        Assert.False(hub.Engaged);
        Assert.Equal(0.6 - (2 * Freewheel.Slowing), hub.WheelSpeed, 6);
        Assert.InRange(hub.Lift, 0, 1);
        Assert.Equal((int)Math.Floor(hub.Overrun * Freewheel.Teeth), hub.Clicks);
        Assert.True(hub.Clicks > 20);

        // Pedalling backwards makes the wheel no slower.
        var speed = hub.WheelSpeed;
        hub.Step(0.01, -0.4);
        Assert.Equal(speed - (0.01 * Freewheel.Slowing), hub.WheelSpeed, 9);

        // Pedal hard again: the driver catches up with a tooth and drives once more, never pulling the wheel back.
        var wheel = hub.Wheel;
        for (var i = 0; i < 300; i++)
        {
            hub.Step(0.01, 1);
            Assert.True(hub.Wheel >= wheel);
            wheel = hub.Wheel;
        }

        Assert.True(hub.Engaged);
        Assert.Equal(1, hub.WheelSpeed, 9);
        Assert.Equal(0, hub.Lift, 6);
    }

    [Fact]
    public void Braking_ShiftsWeightForwards_SkidsTheBackWheel_OrTipsOver()
    {
        Assert.Equal(Braking.FrontShare, Braking.FrontLoad(0), 9);
        Assert.Equal(1, Braking.FrontLoad(Braking.TipOver), 9);

        Assert.Equal((BrakeOutcome.Stops, 0.3), Braking.Brake(BrakeUse.Both, 0.3));
        Assert.Equal(BrakeOutcome.RearSkids, Braking.Brake(BrakeUse.Rear, 0.3).Outcome);
        Assert.Equal(BrakeOutcome.Flips, Braking.Brake(BrakeUse.Front, 0.7).Outcome);

        // At the back brake's limit the back tyre grips with all it has.
        var g = Braking.RearLimit;
        Assert.Equal(Braking.Grip * (1 - Braking.FrontLoad(g)), g, 9);

        // Twice the speed, four times the distance; the front brake stops shortest.
        Assert.Equal(4 * Braking.StoppingDistance(20, 0.4), Braking.StoppingDistance(40, 0.4), 9);
        var front = Braking.StoppingDistance(25, Braking.Brake(BrakeUse.Front, 0.55).Slowing);
        var rear = Braking.StoppingDistance(25, Braking.Brake(BrakeUse.Rear, 0.55).Slowing);
        Assert.True(front < rear / 2);
    }

    [Fact]
    public void Trail_StraightensTheWheel_AndNegativeTrailThrowsItAside()
    {
        // A road bike's numbers give a road bike's trail.
        Assert.InRange(Steering.Trail(73, 0.045) * 1000, 50, 70);
        Assert.True(Steering.Trail(80, 0.08) < 0);

        var steady = new Steering();
        steady.Nudge();
        steady.Step(4, Steering.Trail(72, 0.045));
        Assert.True(Math.Abs(steady.Angle) < 0.01, $"{steady.Angle}");

        var wild = new Steering();
        wild.Nudge();
        wild.Step(4, Steering.Trail(80, 0.08));
        Assert.Equal(Steering.Lock, Math.Abs(wild.Angle), 9);
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var bicycle = new BicycleViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Bicycle").Spreads, bicycle.Spreads.Count);
        bicycle.Chain.Chainring = 48;
        bicycle.Chain.Sprocket = 16;
        Assert.Contains("3.00", bicycle.Chain.ReadoutText);
        bicycle.Hill.Gear = 9;
        Assert.Equal(36, bicycle.Hill.Sprocket);
        bicycle.Brakes.Use = BrakeUse.Rear;
        Assert.True(bicycle.Brakes.IsRear);
        Assert.Equal(BrakeOutcome.RearSkids, bicycle.Brakes.Outcome);
        bicycle.Steer.HeadAngle = 80;
        bicycle.Steer.Offset = 80;
        Assert.True(bicycle.Steer.Trail < 0);
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new BicycleViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }
        }
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Bicycle");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Bicycle");
        Type[] pictures =
        [
            typeof(WheelDiagram), typeof(ChainDiagram), typeof(HillDiagram), typeof(FreewheelDiagram), typeof(BrakeDiagram),
            typeof(SteerDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"bicycle-{s + 1}");
        }

        // Slack spokes, a steep climb, the back brake skidding and negative trail.
        var page = Assert.IsType<BicycleTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]);
        var bicycle = page.Bicycle;
        (bicycle.Wheel.Tension, bicycle.Wheel.Load) = (150, 120);
        (bicycle.Hill.Slope, bicycle.Hill.Gear) = (10, 4);
        (bicycle.Brakes.Use, bicycle.Brakes.Strength) = (BrakeUse.Front, 0.75);
        (bicycle.Steer.HeadAngle, bicycle.Steer.Offset) = (80, 80);
        foreach (var (spread, name) in new[] { (0, "slack"), (2, "steep"), (4, "over"), (5, "negative-trail") })
        {
            model.GoToPage(BookContents.SpreadPage(chapter, spread));
            Frames(40);
            if (name == "over")
            {
                // Late in the stop, once the back wheel has lifted.
                Thread.Sleep(1700);
                Frames(3);
            }

            Snapshot(window, $"bicycle-{spread + 1}-{name}");
        }

        window.Close();
    }
}
