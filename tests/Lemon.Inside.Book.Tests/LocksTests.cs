using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Locks;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.Controls;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The Locks chapter: the models behind its pictures, its words, and every spread drawn.</summary>
public sealed class LocksTests
{
    private static readonly int[] Lock = PinsSpread.LockCuts;

    [Fact]
    public void TheRightKey_LiftsEveryPinToTheShearLine_OnlyWhenFullyIn()
    {
        Assert.True(PinTumbler.Turns(Lock, Lock, 1));
        Assert.All(PinTumbler.Gaps(Lock, Lock, 1), g => Assert.Equal(0, g, 9));
        Assert.False(PinTumbler.Turns(Lock, Lock, 0.9));
        Assert.False(PinTumbler.Turns(Lock, Lock, 0));

        // With no key, every key pin drops and its driver pin crosses the line.
        Assert.All(PinTumbler.Gaps(Lock, Lock, 0), g => Assert.True(g < -PinTumbler.Tolerance));
    }

    [Fact]
    public void AWrongCut_LeavesItsPinOffTheLine_ByHalfAMillimetreAStep()
    {
        var gaps = PinTumbler.Gaps(Lock, PinsSpread.WrongCuts, 1);
        Assert.False(PinTumbler.Turns(Lock, PinsSpread.WrongCuts, 1));
        for (var i = 0; i < Lock.Length; i++)
        {
            // A shallower cut lifts its pin too high; a deeper one leaves it too low.
            Assert.Equal((Lock[i] - PinsSpread.WrongCuts[i]) * PinTumbler.Step, gaps[i], 9);
        }
    }

    [Fact]
    public void KeyCount_MultipliesByTheDepthsForEveryCut_AndTheNeighbourRuleCutsItDown()
    {
        Assert.Equal(100_000, KeyCount.All(5, 10), 6);
        Assert.Equal(100_000, KeyCount.Allowed(5, 10, 9), 6);
        Assert.Equal(7, KeyCount.Allowed(2, 3, 1), 9); // 00 01 10 11 12 21 22
        Assert.Equal(KeyCount.All(6, 10), KeyCount.All(5, 10) * 10, 6);
        Assert.True(KeyCount.Allowed(5, 10, 3) < KeyCount.Allowed(5, 10, 5));
        Assert.True(KeyCount.Allowed(5, 10, 5) < KeyCount.All(5, 10));

        foreach (var (cuts, depths, steepest) in new[] { (5, 10, 9), (7, 10, 2), (3, 4, 1) })
        {
            var key = KeyCount.Sample(cuts, depths, steepest);
            Assert.Equal(cuts, key.Length);
            Assert.All(key, d => Assert.InRange(d, 0, depths - 1));
            for (var i = 1; i < cuts; i++) Assert.InRange(Math.Abs(key[i] - key[i - 1]), 0, steepest);
        }
    }

    [Fact]
    public void AMasterKeyedLock_OpensForBothKeys_AndForAMixOfThem()
    {
        Assert.Equal(-1, MasterKeyed.Blocking(MasterKeyed.Own));
        Assert.Equal(-1, MasterKeyed.Blocking(MasterKeyed.Master));
        Assert.Equal(-1, MasterKeyed.Blocking(MasterKeyed.Mixed));
        Assert.NotEqual(-1, MasterKeyed.Blocking(MasterKeyed.Neighbour));
        Assert.Equal(8, MasterKeyed.KeysThatOpen()); // three pins with wafers

        for (var i = 0; i < PinTumbler.Pins; i++)
        {
            // The shallow key puts the key pin's top on the line; the deep one puts the wafer's top there.
            var shallow = Math.Min(MasterKeyed.Own[i], MasterKeyed.Master[i]);
            var deep = Math.Max(MasterKeyed.Own[i], MasterKeyed.Master[i]);
            int[] withShallow = [.. MasterKeyed.Own];
            withShallow[i] = shallow;
            int[] withDeep = [.. MasterKeyed.Own];
            withDeep[i] = deep;
            Assert.Equal(PinTumbler.ShearLine, MasterKeyed.KeyPinTop(withShallow, i), 9);
            Assert.Equal(PinTumbler.ShearLine, MasterKeyed.KeyPinTop(withDeep, i) + MasterKeyed.Wafer(i), 9);
        }
    }

    [Fact]
    public void PinsBindOneAtATime_UnlessTheHolesArePerfect()
    {
        var order = Binding.Order(Binding.Offsets);
        Assert.Equal([1, 3, 4, 0, 2], order);
        for (var set = 0; set < order.Length; set++)
        {
            Assert.Equal([order[set]], Binding.Binds(Binding.Offsets, set));
        }

        Assert.Empty(Binding.Binds(Binding.Offsets, 5));
        Assert.Equal(5, Binding.Binds(new double[5], 0).Length);
    }

    [Fact]
    public void TheLeverLock_ThrowsItsBolt_OnlyWhileEveryGateIsInLine()
    {
        Assert.Equal(0, LeverLock.Bolt(LeverLock.Gates, 60), 9);
        Assert.Equal(1, LeverLock.Bolt(LeverLock.Gates, 120), 9);
        Assert.Equal(1, LeverLock.Bolt(LeverLock.Gates, 180), 9);
        Assert.InRange(LeverLock.Bolt(LeverLock.Gates, 90), 0.45, 0.55);
        Assert.True(LeverLock.Aligned(LeverLock.Gates, 90));
        Assert.False(LeverLock.Aligned(LeverLock.Gates, 30));

        for (var a = 0.0; a <= 180; a += 1) Assert.False(LeverLock.Aligned(LeverLock.WrongKey, a), $"{a}°");
        Assert.Equal(0, LeverLock.Bolt(LeverLock.WrongKey, 180), 9);
    }

    [Fact]
    public void TheDials_OpenOnTheirCode_AndSloppyNotchesNeedFarFewerTries()
    {
        Assert.Equal(-1, Dials.Blocking(Dials.Code, 0));
        Assert.Equal(1, Dials.Blocking([7, 3, 5], 0));
        Assert.Equal(-1, Dials.Blocking([7, 3, 5], 1));
        Assert.Equal(-1, Dials.Blocking([8, 2, 4], 1));
        Assert.Equal(3, Dials.Off(0, 0)); // 0 is three from 7, going round past 9
        Assert.Equal(1000, Dials.Tries(0), 9);
        Assert.Equal(64, Dials.Tries(1), 9);
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var locks = new LocksViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Locks").Spreads, locks.Spreads.Count);

        locks.Pins.Insert = 1;
        Assert.True(locks.Pins.Turns);
        locks.Pins.Wrong = true;
        Assert.False(locks.Pins.Turns);

        locks.Keys.Depths = 4;
        Assert.Equal(3, locks.Keys.Steepest); // never more than the depths allow
        Assert.Contains(1024.ToString("N0", System.Globalization.CultureInfo.CurrentCulture), locks.Keys.ReadoutText);

        locks.Master.KeyChoice = 3;
        Assert.True(locks.Master.Opens);

        locks.Lever.Angle = 180;
        Assert.Equal(1, locks.Lever.Bolt, 9);
        locks.Lever.Wrong = true;
        Assert.Equal(0, locks.Lever.Bolt, 9);

        (locks.Dials.First, locks.Dials.Second, locks.Dials.Third) = (7, 2, 5);
        Assert.True(locks.Dials.Opens);
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new LocksViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }
        }
    }

    [Fact]
    public void TheGrips_WorkBackFromWhereTheyAreDragged()
    {
        // The key's bow: its middle 6.5 mm before the shoulder, the shoulder a key's length out when the key is out.
        Assert.Equal(1, PinsDiagram.InsertFor(-6.5), 9);
        Assert.Equal(0, PinsDiagram.InsertFor(-6.5 - PinTumbler.KeyLength), 9);
        Assert.Equal(0, PinsDiagram.InsertFor(-500), 9);

        // The lever key's bit: left at 0°, up at 90°, right at 180°, and below it back at either end.
        var c = new Avalonia.Point(100, 100);
        Assert.Equal(0, LeverDiagram.AngleAt(c, new Avalonia.Point(50, 100)), 9);
        Assert.Equal(90, LeverDiagram.AngleAt(c, new Avalonia.Point(100, 50)), 9);
        Assert.Equal(180, LeverDiagram.AngleAt(c, new Avalonia.Point(150, 100)), 9);
        Assert.Equal(180, LeverDiagram.AngleAt(c, new Avalonia.Point(130, 150)), 9);
        Assert.Equal(0, LeverDiagram.AngleAt(c, new Avalonia.Point(70, 150)), 9);

        // A wheel's tab, opposite its notch: at the bottom (the notch at the top) it shows the code, each tenth of a turn
        // clockwise one number more.
        for (var w = 0; w < 3; w++)
        {
            Assert.Equal(Dials.Code[w], DialsDiagram.NumberAt(w, c, new Avalonia.Point(100, 160)));
            Assert.Equal((Dials.Code[w] + 3) % Dials.Numbers, DialsDiagram.NumberAt(w, c, c - (new Avalonia.Vector(Math.Sin(0.6 * Math.PI), -Math.Cos(0.6 * Math.PI)) * 50)));
        }

        // A key sliding into the master-keyed lock: its pins ride the blade, and once it is home they sit where the
        // lock's model puts them.
        var master = MasterKeyed.Keys[1];
        for (var i = 0; i < PinTumbler.Pins; i++)
        {
            Assert.Equal(MasterKeyed.KeyPinTop(master, i), MasterDiagram.KeyPinTop(master, i, 1), 9);
            Assert.Equal(PinTumbler.Floor + MasterKeyed.KeyPin(i), MasterDiagram.KeyPinTop(master, i, 0), 9);
        }
    }

    /// <summary>Every picture has grips, and dragging or tapping them moves the spread's own values (the sliders follow).</summary>
    [AvaloniaFact]
    public void ThePictures_AreWorkedByTheirGrips()
    {
        var (window, model) = OpenAt("Locks");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Locks");
        var locks = Assert.IsType<LocksTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Locks;
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

        var pins = Open(0);
        pins.DragGrip("insert", Far(pins, "insert", 2000, 0));
        Assert.Equal(1, locks.Pins.Insert, 9);
        Assert.True(locks.Pins.Turns);
        pins.DragGrip("swap", default);
        Assert.True(locks.Pins.Wrong);

        var keys = Open(1);
        keys.DragGrip("cuts", Far(keys, "cuts", 2000, 0));
        Assert.Equal(7, locks.Keys.Cuts);
        keys.DragGrip("depths", Far(keys, "depths", 0, -2000));
        Assert.Equal(4, locks.Keys.Depths);
        Assert.Equal(3, locks.Keys.Steepest);
        Frames(2);
        keys.DragGrip("steepest", Far(keys, "steepest", 0, 2000));
        Assert.Equal(3, locks.Keys.Steepest);
        locks.Keys.Depths = 10;
        locks.Keys.Steepest = 2;
        Frames(2);
        keys.DragGrip("steepest", Far(keys, "steepest", 0, 2000));
        Assert.Equal(9, locks.Keys.Steepest);

        var master = Open(2);
        master.DragGrip("key", default);
        Assert.Equal(1, locks.Master.KeyChoice);

        var binding = Open(3);
        binding.DragGrip("pick", default);
        binding.DragGrip("pick", default);
        Assert.Equal(2, locks.Binding.Set);
        binding.DragGrip("tension", default);
        Assert.Equal(0, locks.Binding.Set);
        binding.DragGrip("perfect", default);
        Assert.True(locks.Binding.Perfect);

        var lever = Open(4);
        lever.DragGrip("turn", Far(lever, "turn", 2000, 0));
        Assert.Equal(180, locks.Lever.Angle, 9);
        lever.DragGrip("swap", default);
        Assert.True(locks.Lever.Wrong);

        var dials = Open(5);
        dials.DragGrip("first", Far(dials, "first", 0, 2000));
        dials.DragGrip("second", Far(dials, "second", 0, 2000));
        dials.DragGrip("third", Far(dials, "third", 0, 2000));
        Assert.True(locks.Dials.Opens);
        dials.DragGrip("sloppy", default);
        Assert.True(locks.Dials.Sloppy);

        window.Close();
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Locks");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Locks");
        Type[] pictures =
        [
            typeof(PinsDiagram), typeof(KeysDiagram), typeof(MasterDiagram), typeof(BindingDiagram), typeof(LeverDiagram),
            typeof(DialsDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"locks-{s + 1}");
        }

        var locks = Assert.IsType<LocksTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Locks;
        (locks.Pins.Insert, locks.Pins.Wrong) = (1, false);
        (locks.Keys.Cuts, locks.Keys.Steepest) = (7, 2);
        locks.Master.KeyChoice = 2;
        (locks.Binding.Set, locks.Binding.Perfect) = (2, false);
        locks.Lever.Angle = 100;
        (locks.Dials.First, locks.Dials.Second, locks.Dials.Third, locks.Dials.Sloppy) = (7, 3, 5, true);
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(30);
            Snapshot(window, $"locks-{s + 1}-changed");
        }

        window.Close();
    }
}
