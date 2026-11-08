using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Book.Shell.Infrastructures.Timing;
using Lemon.Inside.Book.Chapters.Watch;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.Controls;
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

    [Fact]
    public void TheGripsModels_FollowTheFinger()
    {
        // Five turns of the key from flat to full; a quarter turn on from half way is a twentieth more.
        var full = GoingTrain.SpringTurns * 2 * Math.PI;
        Assert.Equal(0.55, SpringDiagram.WoundAt(0.5, (0.5 * full) + (Math.PI / 2)), 9);
        Assert.Equal(0.55, SpringDiagram.WoundAt(0.5, Math.IEEERemainder((0.5 * full) + (Math.PI / 2), 2 * Math.PI)), 9);
        Assert.Equal(1, SpringDiagram.WoundAt(1, 0.3));

        // The minute hand a quarter turn on is a quarter of an hour later, past 12 too.
        var at = HandsDiagram.StartSeconds;
        Assert.Equal(at + 900, HandsDiagram.SetTo(at, (GoingTrain.Hands(at).Minute + 90) * Math.PI / 180), 6);
        Assert.Equal(11 * 3600.0 + 300, HandsDiagram.SetTo(10 * 3600 + (55 * 60), 30 * Math.PI / 180), 6);
        Assert.Equal([1, 2, 3, 0], new[] { 0, 1, 2, 3 }.Select(HandsDiagram.NextStep));

        // The balance put at an angle by hand: the moment it is there, in the same half swing.
        foreach (var t in new[] { 0.05, 0.31, 0.77 })
        {
            foreach (var degrees in new[] { -200.0, -20, 0, 90 })
            {
                var moment = EscapementDiagram.TimeAt(t, degrees);
                Assert.Equal(degrees, Escapement.At(moment).Balance, 6);
                Assert.True(Math.Abs(moment - t) <= 0.5 / GoingTrain.Frequency, $"{t}, {degrees}");
            }
        }

        Assert.Equal(1.25, BalanceDiagram.StiffnessAt(Math.PI / 2));
        Assert.Equal(0.8, BalanceDiagram.StiffnessAt(-Math.PI / 2));
        Assert.Equal(1.025, BalanceDiagram.StiffnessAt(0), 9);

        var page = new Avalonia.Rect(0, 0, 480, 600);
        Assert.Equal(0.37, MovementDiagram.ExplodeFor(page, MovementDiagram.DialY(page, 0.37)), 9);
        Assert.True(MovementDiagram.DialY(page, 1) < MovementDiagram.DialY(page, 0));
    }

    [AvaloniaFact]
    public void ThePictures_AreWorkedByTheirGrips()
    {
        var (window, model) = OpenAt("Watch");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Watch");
        var watch = Assert.IsType<WatchTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Watch;
        Avalonia.Point Far(Picture picture, string grip, double dx, double dy) =>
            picture.GripCentre(grip)!.Value + new Avalonia.Vector(dx, dy);

        // A quarter turn clockwise on screen about a part's middle.
        Avalonia.Point Quarter(Picture picture, string grip, string part)
        {
            var c = picture.PartCentre(part)!.Value;
            var d = picture.GripCentre(grip)!.Value - c;
            return c + new Avalonia.Vector(-d.Y, d.X);
        }

        Picture Open(int spread)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, spread));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Picture>().First(p => p.IsEffectivelyVisible);
            Assert.NotEmpty(picture.MarkedGrips);
            return picture;
        }

        var spring = Assert.IsType<SpringDiagram>(Open(0));
        spring.DragGrip("hours", Far(spring, "hours", 0, -2000));
        Assert.Equal(1, watch.Spring.Wound, 9);
        Frames(1);
        spring.DragGrip("hours", Far(spring, "hours", 0, 2000));
        Assert.Equal(0, watch.Spring.Wound, 9);
        watch.Spring.Wound = 0.5;
        Frames(2);
        var drawn = spring.Shown;
        spring.DragGrip("arbor", Quarter(spring, "arbor", "arbor"));
        Assert.Equal(drawn + 0.05, watch.Spring.Wound, 3); // a quarter turn of five: a twentieth on from the key as drawn

        var hands = Assert.IsType<HandsDiagram>(Open(1));
        hands.DragGrip("crown", default);
        Assert.Equal(2, watch.Hands.SpeedStep);
        Frames(1);
        var time = hands.Time;
        hands.DragGrip("minute", Quarter(hands, "minute", "dial"));
        Assert.InRange(hands.Time - time, 890, 910);

        var escapement = Assert.IsType<EscapementDiagram>(Open(2));
        escapement.DragGrip("speed", Far(escapement, "speed", 2000, 0));
        Assert.Equal(0.5, watch.Escapement.SlowMotion, 9);
        Frames(1);
        escapement.DragGrip("speed", Far(escapement, "speed", -2000, 0));
        Assert.Equal(0.02, watch.Escapement.SlowMotion, 9);
        Frames(1);
        var beat = escapement.Time;
        escapement.DragGrip("balance", Far(escapement, "balance", 30, 0));
        Assert.NotEqual(beat, escapement.Time);

        var balance = Open(3);
        balance.DragGrip("regulator", Far(balance, "regulator", 0, 2000));
        Assert.Equal(1.25, watch.Balance.Stiffness, 9);
        Frames(1);
        balance.DragGrip("regulator", Far(balance, "regulator", 0, -2000));
        Assert.Equal(0.8, watch.Balance.Stiffness, 9);
        watch.Balance.Amplitude = 270;
        Frames(1);
        var middle = balance.PartCentre("balance")!.Value;
        var rim = balance.GripCentre("swing")!.Value - middle;
        var reach = Math.Sqrt((rim.X * rim.X) + (rim.Y * rim.Y));
        var pulled = (90 + 150) * Math.PI / 180; // 150° round from where the pin rests, straight down
        balance.DragGrip("swing", middle + new Avalonia.Vector(reach * Math.Cos(pulled), reach * Math.Sin(pulled)));
        Assert.Contains(watch.Balance.Amplitude, new[] { 150.0, 210 });

        var rotor = Assert.IsType<RotorDiagram>(Open(4));
        rotor.DragGrip("reverser", default);
        Assert.False(watch.Rotor.Reverser);
        Frames(1);
        var wound = rotor.Winding.Wound;
        rotor.DragGrip("rotor", Quarter(rotor, "rotor", "rotor"));
        Assert.True(rotor.Winding.Wound > wound);

        var movement = Open(5);
        movement.DragGrip("explode", Far(movement, "explode", 0, -2000));
        Assert.Equal(1, watch.Movement.Explode, 9);
        Frames(1);
        movement.DragGrip("layerDial", default);
        Assert.False(watch.Movement.ShowDial);
        Frames(1);
        movement.DragGrip("layerDial", default);
        Assert.True(watch.Movement.ShowDial);

        window.Close();
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

            // The clock's elapsed time is a TimeSpan, so either end can round by one 100 ns tick.
            Assert.Equal(2 * escapement.SlowMotion, escapement.Time - start, 6);
            Snapshot(window, "watch-3-stepped");
        }
        finally
        {
            BookClock.EndStepping();
            window.Close();
        }
    }
}
