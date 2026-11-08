using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Bridges;
using Lemon.Book.Shell.Infrastructures.Timing;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.Controls;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The Bridges chapter: the models behind its pictures, its words, and every spread drawn.</summary>
public sealed class BridgesTests
{
    [Fact]
    public void ABeam_SagsWithTheCubeOfItsSpan_AndStiffensWithTheCubeOfItsDepth()
    {
        Assert.Equal(8, Beam.Sag(12, 0.4) / Beam.Sag(6, 0.4), 9);
        Assert.Equal(Beam.Sag(6, 0.4), Beam.Sag(12, 0.8), 12); // twice as deep undoes twice as long
        Assert.InRange(Beam.Sag(6, 0.4), 0.004, 0.007); // about half a centimetre
        Assert.True(Beam.Sag(12, 0.2) * BeamDiagram.Exaggeration > 1.5); // the picture clamps the worst case
    }

    [Fact]
    public void ABracedFrame_HoldsItsShape_AndItsBraceIsStretched()
    {
        Assert.Equal(0, Frame.Lean(Frame.MostPush, true));
        Assert.Equal(Frame.MostLean, Frame.Lean(Frame.MostPush, false), 9);
        Assert.Equal(0, Frame.Lean(0, false));
        Assert.InRange(Frame.BraceForce(Frame.MostPush), 2.8, 2.9); // "nearly three tonnes"
    }

    [Fact]
    public void TheTruss_BalancesAtEveryJoint()
    {
        Assert.Equal(15, Truss.Members.Count); // 2 × 9 joints = 15 members + 3 reactions: just determinate

        // A lorry in the middle: by sections, the top chord's middle carries W·a/h, the bottom chord's middle 3W·a/4h.
        var (squeeze, stretch) = Truss.Chords(2, 0.5);
        Assert.Equal(Truss.Lorry * Truss.PanelLength / 2, squeeze, 6);
        Assert.Equal(0.75 * Truss.Lorry * Truss.PanelLength / 2, stretch, 6);
        Assert.Equal(squeeze / 2, Truss.Chords(4, 0.5).Squeeze, 6); // twice as tall, half the force

        foreach (var position in new[] { 0.1, 0.33, 0.5, 0.8 })
        {
            var forces = Truss.Forces(2.5, position);
            for (var m = 0; m < Truss.Members.Count; m++)
            {
                var kind = Truss.Members[m].Kind;
                if (kind == Truss.Kind.Top) Assert.True(forces[m] <= 1e-9, "the top chord is never stretched");
                if (kind == Truss.Kind.Bottom) Assert.True(forces[m] >= -1e-9, "the bottom chord is never squeezed");
            }
        }

        // Moving the lorry can turn a diagonal from squeezed to stretched.
        var diagonal = Enumerable.Range(0, Truss.Members.Count).First(m => Truss.Members[m].Kind == Truss.Kind.Diagonal && Truss.Members[m].A == 1);
        Assert.NotEqual(Math.Sign(Truss.Forces(2, 0.1)[diagonal]), Math.Sign(Truss.Forces(2, 0.9)[diagonal]));
    }

    [Fact]
    public void AFlatterArch_PushesHarderOnItsBanks()
    {
        Assert.InRange(Arch.Thrust(3), 41, 42);
        Assert.True(Arch.Thrust(1.5) > 80);
        Assert.Equal(2 * Arch.Thrust(4), Arch.Thrust(2), 9);
    }

    [Fact]
    public void HalfTheSag_TwiceThePull()
    {
        Assert.Equal(2500, Cable.Pull(20), 6);
        Assert.Equal(2 * Cable.Pull(20), Cable.Pull(10), 6);
        Assert.True(Cable.AtTower(20) > Cable.Pull(20));
        Assert.Equal(20, Cable.Height(Cable.Span / 2, 20), 9);
        Assert.Equal(0, Cable.Height(0, 20), 9);
    }

    [Fact]
    public void SteelGrowsInTheSun()
    {
        Assert.Equal(12, Deck.Change(40), 6); // "twelve centimetres longer"
        Assert.Equal(-12, Deck.Change(-10), 6);
        Assert.InRange(Deck.Change(Deck.Mild + 30), 14, 14.5); // the quiz's "about fourteen"
        Assert.InRange(Deck.Change(45) - Deck.Change(-5), 23, 25); // "about a quarter of a metre" over fifty degrees
        Assert.InRange(Deck.Push(40), 2900, 3200); // "about three thousand tonnes"
        Assert.Equal(8, ExpansionSpread.MildGap - Deck.Change(40), 6);
        Assert.True(ExpansionSpread.MildGap - Deck.Change(-20) > 0);
    }

    [Fact]
    public void ABounce_OvershootsThenSettlesOnItsTarget()
    {
        var bounce = Bounce.At(0);
        var most = 0.0;
        for (var i = 0; i < 300; i++)
        {
            bounce = bounce.Step(1, 1.0 / 60, 6, 0.15);
            most = Math.Max(most, bounce.Value);
        }

        Assert.True(most > 1.2, "a lightly damped spring overshoots");
        Assert.Equal(1, bounce.Value, 2); // five seconds on, it has settled
        Assert.Equal(1, Bounce.At(0).Step(1, 0.1, 6, 0.15).Step(1, 4.9, 6, 0.15).Value, 2); // a slow frame changes nothing
    }

    [Fact]
    public void TheFramesHand_IsWhereItsPushPutsIt()
    {
        foreach (var brace in new[] { false, true })
        {
            foreach (var push in new[] { 0, 0.5, 1.25, 2 })
            {
                Assert.Equal(push, FrameDiagram.PushFor(FrameDiagram.HandAt(push, brace), brace), 6);
            }
        }

        Assert.True(FrameDiagram.HandAt(2, false) > FrameDiagram.HandAt(2, true)); // unbraced, the frame goes over too
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var bridges = new BridgesViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Bridges").Spreads, bridges.Spreads.Count);

        bridges.Beam.Span = 12;
        Assert.InRange(bridges.Beam.Sag, 4, 5);

        bridges.Truss.Height = 4;
        Assert.True(bridges.Truss.Squeeze <= 10.01);

        bridges.Arch.Rise = 1.5;
        Assert.True(bridges.Arch.Thrust >= 80);

        foreach (var spread in bridges.Spreads)
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)spread.GetType().GetProperty("ReadoutText")!.GetValue(spread)));
        }
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new BridgesViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }
        }
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Bridges");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Bridges");
        Type[] pictures =
        [
            typeof(BeamDiagram), typeof(FrameDiagram), typeof(TrussDiagram), typeof(ArchDiagram), typeof(CableDiagram),
            typeof(ExpansionDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"bridges-{s + 1}");
        }

        var bridges = Assert.IsType<BridgesTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Bridges;
        (bridges.Beam.Span, bridges.Beam.Depth) = (12, 0.2);
        (bridges.Frame.Push, bridges.Frame.Brace) = (1.5, false);
        (bridges.Truss.Height, bridges.Truss.Position) = (1.2, 0.3);
        (bridges.Arch.Rise, bridges.Arch.Abutments) = (2, false);
        (bridges.Cable.Sag, bridges.Cable.Anchored) = (8, false);
        (bridges.Expansion.Temperature, bridges.Expansion.Joint) = (40, false);
        BookClock.BeginStepping();
        try
        {
            for (var s = 0; s < pictures.Length; s++)
            {
                model.GoToPage(BookContents.SpreadPage(chapter, s));
                Frames(2);

                // Let the moving pictures settle: the plank stop bouncing, the arch land, the lorry arrive.
                for (var i = 0; i < 120; i++)
                {
                    BookClock.Advance(1.0 / 30);
                    Frames(1);
                }

                Snapshot(window, $"bridges-{s + 1}-changed");
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
        var (window, model) = OpenAt("Bridges");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Bridges");
        var bridges = Assert.IsType<BridgesTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Bridges;
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

        var beam = Open(0);
        beam.DragGrip("span", Far(beam, "span", 2000, 0));
        Assert.Equal(12, bridges.Beam.Span, 9);
        Frames(2);
        beam.DragGrip("depth", Far(beam, "depth", 0, 2000));
        Assert.Equal(0.8, bridges.Beam.Depth, 9);
        Frames(2);
        beam.DragGrip("span", Far(beam, "span", -2000, 0));
        Assert.Equal(4, bridges.Beam.Span, 9);

        var frame = Open(1);
        frame.DragGrip("push", Far(frame, "push", 2000, 0));
        Assert.Equal(Frame.MostPush, bridges.Frame.Push, 9);
        frame.DragGrip("brace", default);
        Assert.True(bridges.Frame.Brace);
        Frames(2);
        frame.DragGrip("push", Far(frame, "push", -2000, 0));
        Assert.Equal(0, bridges.Frame.Push, 9);

        var truss = Open(2);
        truss.DragGrip("lorry", Far(truss, "lorry", -2000, 0));
        Assert.Equal(0, bridges.Truss.Position, 9);
        truss.DragGrip("height", Far(truss, "height", 0, -2000));
        Assert.Equal(4, bridges.Truss.Height, 9);

        var arch = Open(3);
        arch.DragGrip("rise", Far(arch, "rise", 0, 2000));
        Assert.Equal(1, bridges.Arch.Rise, 9);
        arch.DragGrip("abutments", default);
        Assert.False(bridges.Arch.Abutments);

        var cable = Open(4);
        cable.DragGrip("sag", Far(cable, "sag", 0, -2000));
        Assert.Equal(5, bridges.Cable.Sag, 9);
        cable.DragGrip("anchor", default);
        Assert.False(bridges.Cable.Anchored);

        var expansion = Open(5);
        expansion.DragGrip("temperature", Far(expansion, "temperature", 0, -2000));
        Assert.Equal(45, bridges.Expansion.Temperature, 9);
        expansion.DragGrip("joint", default);
        Assert.False(bridges.Expansion.Joint);

        window.Close();
    }
}
