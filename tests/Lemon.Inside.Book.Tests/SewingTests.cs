using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Sewing;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.Controls;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The Sewing chapter: the models behind its pictures, its words, and every spread drawn.</summary>
public sealed class SewingTests
{
    [Fact]
    public void ALockstitch_HoldsAndTakesLessThread()
    {
        Assert.Equal(40, Seam.Stitches(2.5));
        Assert.InRange(Seam.Thread(2.5, false) / Seam.Length, 2.7, 2.9); // "a little under three times"
        Assert.InRange(Seam.Thread(2.5, true), 370, 390); // "about 38 centimetres"
        Assert.Equal(0, Seam.Lower(2.5, true), 9);
        Assert.Equal(Seam.Upper(2.5, false), Seam.Lower(2.5, false), 9);
        Assert.True(Seam.Unravels(true));
        Assert.False(Seam.Unravels(false));
    }

    [Fact]
    public void TheNeedle_LingersAtTheBottom_AndTheLoopOpensOnTheWayUp()
    {
        Assert.Equal(30, Needle.Rise(0), 9);
        Assert.Equal(0, Needle.Rise(180), 9);
        Assert.InRange(Needle.Rise(220), 2.2, 2.7); // "forty degrees past the bottom, about two and a half millimetres"
        Assert.Equal(0, Needle.Loop(150), 9); // going down: no loop
        Assert.True(Needle.Loop(220) > Needle.Loop(205));
        Assert.True(Needle.Loop(220) > Needle.Loop(240));
        Assert.Equal(0, Needle.Loop(270), 9); // drawn flat again
        Assert.Equal(0, Needle.Loop(220, eyeAtPoint: false), 9);
        Assert.InRange(Needle.Rise(90), Needle.ClothHeight, 13); // the needle is just out of the cloth a quarter turn from the top
    }

    [Fact]
    public void TheHook_CatchesOnlyInItsWindow()
    {
        Assert.True(Hook.Catches(0));
        Assert.False(Hook.Catches(-25));
        Assert.False(Hook.Catches(20));
        var (from, to) = HookDiagram.Window();
        Assert.InRange(to - from, 20, 30); // "about twenty-five degrees"
        Assert.Equal(0, Hook.Turn(Hook.Right, 0), 9);
        Assert.Equal(0, Hook.Turn(Hook.Right + 180, 0), 9); // twice round in half a turn of the handwheel
    }

    [Fact]
    public void TheTakeUpLever_LendsSevenCentimetres_AndTakesThemBack()
    {
        Assert.InRange(TakeUp.Loop, 68, 70); // "about seven centimetres"
        Assert.InRange(TakeUp.Loop / TakeUp.PerStitch(2.5), 19, 21); // "twenty times what one stitch keeps"
        Assert.InRange(TakeUp.Passes(2.5), 38, 42); // "about forty times"
        Assert.Equal(0, TakeUp.Down(Hook.Right), 9);
        Assert.Equal(1, TakeUp.Down(Hook.Right + 110), 9);
        Assert.Equal(0, TakeUp.Down(60), 9); // all taken back just after the needle passes the top
        Assert.Equal(1, TakeUp.Lever(90), 9);
    }

    [Fact]
    public void TheFeedDogs_MoveTheClothOnlyWhileTheNeedleIsOut()
    {
        Assert.Equal(2, Feed.MetresPerMinute(2.5), 9); // "two metres a minute"
        for (var angle = 0.0; angle < 360; angle += 5)
        {
            if (Feed.Dog(angle, 2.5, false).Y > 0) Assert.True(Needle.Rise(angle) > Needle.ClothHeight, $"{angle}");
        }

        Assert.Equal(2.5, Feed.Dog(90, 2.5, false).X - Feed.Dog(270, 2.5, false).X, 9); // a stitch along while up
        Assert.Equal(-Feed.Dog(45, 2.5, false).X, Feed.Dog(45, 2.5, true).X, 9);
        Assert.Equal(0, Feed.Dog(45, 0, false).X, 9);
    }

    [Fact]
    public void TheKnot_GoesToTheThreadThatPullsHarder()
    {
        Assert.Equal(0.5, Tension.Knot(4, 30), 9);
        Assert.Equal(0, Tension.Verdict(4, 30));
        Assert.Equal(-1, Tension.Verdict(8, 30)); // upper too tight: blue on top
        Assert.Equal(1, Tension.Verdict(1, 30)); // upper too loose: loops underneath
        Assert.Equal(1, Tension.Verdict(1, 45)); // tightening the bobbin makes loops worse, not better
        Assert.Equal(0, Tension.Knot(9, 15), 9);
        Assert.Equal(1, Tension.Knot(0, 45), 9);
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var sewing = new SewingViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Sewing").Spreads, sewing.Spreads.Count);
        sewing.Hook.Timing = 25;
        Assert.False(sewing.Hook.Catches);
        sewing.Tension.Dial = 8;
        Assert.True(sewing.Tension.Knot < 0.3);
        foreach (var spread in sewing.Spreads)
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)spread.GetType().GetProperty("ReadoutText")!.GetValue(spread)));
        }

        sewing.Feed.Length = 0;
        Assert.False(string.IsNullOrWhiteSpace(sewing.Feed.ReadoutText));
        (sewing.Stitch.Chain, sewing.Stitch.Pulled) = (true, true);
        Assert.False(string.IsNullOrWhiteSpace(sewing.Stitch.ReadoutText));
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new SewingViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }
        }
    }

    /// <summary>Every picture has grips, and dragging or tapping them moves the spread's own values (the sliders follow).</summary>
    [AvaloniaFact]
    public void ThePictures_AreWorkedByTheirGrips()
    {
        var (window, model) = OpenAt("Sewing");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Sewing");
        var sewing = Assert.IsType<SewingTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Sewing;
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

        var stitch = Open(0);
        stitch.DragGrip("length", Far(stitch, "length", 2000, 0));
        Assert.Equal(5, sewing.Stitch.Length, 9);
        stitch.DragGrip("kind", default);
        Assert.True(sewing.Stitch.Chain);
        Frames(2);
        stitch.DragGrip("pull", default);
        Assert.True(sewing.Stitch.Pulled);

        var needle = Open(1);
        needle.DragGrip("swap", default);
        Assert.True(sewing.Needle.HandNeedle);
        needle.DragGrip("wheel", Far(needle, "wheel", 0, 2000));
        Assert.InRange(sewing.Needle.Angle, 90, 270);

        var hook = Open(2);
        hook.DragGrip("timing", Far(hook, "timing", 2000, 0));
        Assert.Equal(30, sewing.Hook.Timing, 9);

        var takeUp = Open(3);
        takeUp.DragGrip("hold", default);
        Assert.True(sewing.TakeUp.Held);
        takeUp.DragGrip("length", Far(takeUp, "length", 0, -2000));
        Assert.Equal(5, sewing.TakeUp.Length, 9);

        var feed = Open(4);
        feed.DragGrip("length", Far(feed, "length", -2000, 0));
        Assert.Equal(0, sewing.Feed.Length, 9);
        feed.DragGrip("reverse", default);
        Assert.True(sewing.Feed.Reverse);

        var tension = Open(5);
        tension.DragGrip("dial", Far(tension, "dial", 0, -2000));
        Assert.Equal(9, sewing.Tension.Dial, 9);
        tension.DragGrip("screw", Far(tension, "screw", -2000, 0));
        Assert.Equal(15, sewing.Tension.Bobbin, 9);

        window.Close();
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Sewing");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Sewing");
        Type[] pictures =
        [
            typeof(StitchDiagram), typeof(NeedleDiagram), typeof(HookDiagram), typeof(TakeUpDiagram), typeof(FeedDiagram), typeof(TensionDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"sewing-{s + 1}");
        }

        var sewing = Assert.IsType<SewingTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Sewing;
        (sewing.Stitch.Chain, sewing.Stitch.Pulled) = (true, true);
        sewing.Needle.Angle = Hook.Right;
        sewing.Hook.Timing = 25;
        sewing.TakeUp.Held = true;
        (sewing.Feed.Length, sewing.Feed.Reverse) = (5, true);
        sewing.Tension.Dial = 1;
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(30);
            Snapshot(window, $"sewing-{s + 1}-changed");
        }

        window.Close();
    }
}
