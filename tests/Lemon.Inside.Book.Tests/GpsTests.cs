using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Gps;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.Controls;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The GPS chapter: the models behind its pictures, its words, and every spread drawn.</summary>
public sealed class GpsTests
{
    [Fact]
    public void ADelay_IsADistance_AtTheSpeedOfLight()
    {
        Assert.InRange(Radio.MetresPerMicrosecond, 299, 300); // "a millionth of a second is 300 metres"
        Assert.InRange(Radio.Km(1), 299, 300); // "a thousandth of a second is 300 kilometres"
        Assert.InRange(Radio.Milliseconds(Orbit.Gps), 67, 68); // "about 67 thousandths" overhead
        Assert.InRange(Radio.Milliseconds(Orbit.HorizonDistance(Orbit.Gps)), 85.5, 86.5); // "about 86" at the horizon
        Assert.Equal(0, Orbit.GroundAngle(Orbit.Gps, Orbit.Gps)!.Value, 6); // straight overhead: one place
        Assert.Null(Orbit.GroundAngle(Orbit.Gps, 15_000)); // nearer than its height: nowhere on the ground
        Assert.InRange(Orbit.GroundAngle(Orbit.Gps, Radio.Km(75))!.Value, 10, 60);
    }

    [Fact]
    public void TwoCircles_CrossAtTwoPoints_AndThreeAgreeOnOne()
    {
        var you = new MapPoint(30, 10);
        Assert.Null(Map.Places(you, 1));
        var two = Map.Places(you, 2)!;
        Assert.Equal(2, two.Count);
        Assert.Contains(two, p => p.DistanceTo(you) < 1e-6);
        Assert.Equal([you], Map.Places(you, 3));
        Assert.Single(Circles.Cross(new MapPoint(0, 0), 1, new MapPoint(2, 0), 1)); // just touching
        Assert.Empty(Circles.Cross(new MapPoint(0, 0), 1, new MapPoint(5, 0), 1));
    }

    [Fact]
    public void AWrongClock_StopsTheCirclesMeeting()
    {
        Assert.Equal(0, Map.Miss(0), 6);
        Assert.True(Map.Miss(120) > 5);
        Assert.True(Map.Miss(-120) > 5);
        Assert.True(Map.Miss(200) > Map.Miss(50));
        Assert.InRange(Map.Measured(100)[0] - Map.Measured(0)[0], 29.9, 30.1); // 100 µs: 30 km on every distance
    }

    [Fact]
    public void SatelliteClocks_GainAbout38MicrosecondsADay_ElevenKilometres()
    {
        Assert.Equal(38.5, Clocks.PerDay, 9);
        Assert.InRange(Clocks.DriftKm(1, false), 11, 12); // "more than 11 kilometres a day"
        Assert.Equal(0, Clocks.DriftKm(7, true), 9);
        Assert.InRange(Orbit.Speed(Orbit.Gps), 3.8, 3.95); // "nearly 4 kilometres a second"
        var slowed = 10.22999999543 / 10.23;
        Assert.InRange((1 - slowed) * 86_400 * 1e6, 37, 40); // the factory setting takes about that much off a day
    }

    [Fact]
    public void TheHigherTheOrbit_TheSlowerTheRound_AndTheMoreItSees()
    {
        Assert.InRange(Orbit.Hours(400), 1.5, 1.6); // "about an hour and a half"
        Assert.InRange(Orbit.Hours(Orbit.Gps), 11.9, 12); // "almost exactly twelve hours"
        Assert.InRange(Orbit.Hours(35_786), 23.9, 24); // "a whole day"
        Assert.InRange(Orbit.Seen(Orbit.Gps), 0.34, 0.4); // "more than a third"
        Assert.True(Orbit.Seen(400) < 0.05);
        Assert.Equal("11 h 58 min", OrbitSpread.Period(Orbit.Gps).Replace(" ", " "));
    }

    [Fact]
    public void BandsCrossingAtAShallowAngle_LeaveALongPatch()
    {
        Assert.Equal(10 * Math.Sqrt(2), Patch.Best(5), 9);
        Assert.InRange(Patch.Longest(5, 10), 110, 120);
        Assert.Equal(Patch.Longest(5, 30), Patch.Longest(5, 150), 9);
        Assert.Equal(100, Patch.Area(5, 90), 9);
        Assert.True(Patch.Longest(5, 25) > 1.25 * Patch.Best(5)); // the walkthrough starts with a poor layout
        Assert.True(Patch.Longest(5, 90) <= 1.25 * Patch.Best(5));
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var gps = new GpsViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Gps").Spreads, gps.Spreads.Count);
        foreach (var spread in gps.Spreads)
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)spread.GetType().GetProperty("ReadoutText")!.GetValue(spread)));
        }

        gps.Echo.Delay = 62;
        Assert.False(string.IsNullOrWhiteSpace(gps.Echo.ReadoutText));
        gps.Clock.Offset = -80;
        Assert.False(string.IsNullOrWhiteSpace(gps.Clock.ReadoutText));
        gps.Clock.Offset = 0;
        Assert.True(gps.Clock.Miss < 0.01);
        gps.Drift.Corrected = true;
        Assert.False(string.IsNullOrWhiteSpace(gps.Drift.ReadoutText));
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new GpsViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }
        }
    }

    /// <summary>Every picture has grips, and dragging or tapping them moves the spread's own values (the sliders follow).</summary>
    [AvaloniaFact]
    public void ThePictures_AreWorkedByTheirGrips()
    {
        var (window, model) = OpenAt("Gps");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Gps");
        var gps = Assert.IsType<GpsTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Gps;
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

        var echo = Open(0);
        echo.DragGrip("delay", Far(echo, "delay", 2000, 0));
        Assert.Equal(90, gps.Echo.Delay, 9);

        var fix = Open(1);
        fix.DragGrip("count", default);
        Assert.Equal(3, gps.Fix.Used);
        Frames(2);
        fix.DragGrip("you", Far(fix, "you", 2000, 2000));
        Assert.Equal(200, gps.Fix.East, 9);
        Assert.Equal(-200, gps.Fix.North, 9);

        var clock = Open(2);
        clock.DragGrip("offset", Far(clock, "offset", -2000, 0));
        Assert.Equal(-200, gps.Clock.Offset, 9);

        var drift = Open(3);
        drift.DragGrip("corrected", default);
        Assert.True(gps.Drift.Corrected);
        drift.DragGrip("days", Far(drift, "days", 2000, 0));
        Assert.Equal(7, gps.Drift.Days, 9);

        var orbit = Open(4);
        orbit.DragGrip("height", Far(orbit, "height", 2000, 0));
        Assert.Equal(36_000, gps.Orbit.Altitude, 9);

        var angle = Open(5);
        angle.DragGrip("error", Far(angle, "error", -2000, 0));
        Assert.Equal(1, gps.Angle.Error, 9);
        Frames(2);
        angle.DragGrip("satellite", Far(angle, "satellite", 2000, 0));
        Assert.InRange(gps.Angle.Angle, 160, 175);

        window.Close();
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Gps");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Gps");
        Type[] pictures =
        [
            typeof(EchoDiagram), typeof(FixDiagram), typeof(ClockDiagram), typeof(DriftDiagram), typeof(OrbitDiagram), typeof(AngleDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"gps-{s + 1}");
        }

        var gps = Assert.IsType<GpsTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Gps;
        gps.Echo.Delay = EchoSpread.Overhead;
        gps.Fix.Count = 3;
        gps.Clock.Offset = 0;
        (gps.Drift.Days, gps.Drift.Corrected) = (7, true);
        gps.Orbit.Altitude = 400;
        gps.Angle.Angle = 90;
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(10);
            Snapshot(window, $"gps-{s + 1}-changed");
        }

        window.Close();
    }
}
