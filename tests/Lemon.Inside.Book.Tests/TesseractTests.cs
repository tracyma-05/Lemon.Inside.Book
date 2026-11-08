using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Tesseract;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.Controls;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The Tesseract chapter: the geometry behind its pictures, its words, and every spread drawn.</summary>
public sealed class TesseractTests
{
    [Fact]
    public void EachSlide_DoublesTheCorners_AndATesseractHas16Corners32Edges24Squares8Cubes()
    {
        Assert.Equal([1, 2, 4, 8, 16], Enumerable.Range(0, 5).Select(HyperCube.Corners));
        Assert.Equal([0, 1, 4, 12, 32], Enumerable.Range(0, 5).Select(HyperCube.Edges));
        Assert.Equal(24, HyperCube.Squares(4));
        Assert.Equal(8, HyperCube.Cubes(4));
        Assert.Equal(6, HyperCube.Squares(3));
        Assert.Equal(32, HyperCube.EdgeList(4).Count);
        Assert.Equal(24, HyperCube.SquareList(4).Count);
        Assert.All(HyperCube.EdgeList(4), e => Assert.Equal(1, Distance(HyperCube.Corner(e.A, 4), HyperCube.Corner(e.B, 4)), 9));
    }

    [Fact]
    public void ACubesShadow_IsASquareFaceOn_AndAHexagonOfRoot3StoodOnACorner()
    {
        Assert.Equal(4, Shadow.Outline(0, 0).Count);
        Assert.Equal(1, Shadow.Area(0, 0), 9);
        Assert.Equal(6, Shadow.Outline(45, Shadow.CornerTilt).Count);
        Assert.Equal(Shadow.Biggest, Shadow.Area(45, Shadow.CornerTilt), 6);
        Assert.InRange(Shadow.Area(45, 55), 1.7, Shadow.Biggest + 1e-9); // what the walkthrough sets
        for (var turn = 0; turn <= 90; turn += 15)
        {
            for (var tilt = 0; tilt <= 90; tilt += 15) Assert.InRange(Shadow.Area(turn, tilt), 1 - 1e-9, Shadow.Biggest + 1e-9);
        }
    }

    [Fact]
    public void TheFarCube_LooksSmaller_TheNearerTheEye()
    {
        Assert.Equal(0.6, Look.InnerRatio(2), 9);
        Assert.Equal(2.0 / 3, Look.InnerRatio(2.5), 9);
        Assert.True(Look.InnerRatio(1.2) < 0.5);
        Assert.True(Look.InnerRatio(5) > Look.InnerRatio(2));
    }

    [Fact]
    public void AHalfTurnInTheXWPlane_SwapsTheNearAndFarCubes_AndLeavesYAndZAlone()
    {
        var far = HyperCube.Corner(0, 4); // w = −½
        var turned = Spin.Turn(far, 180, false);
        Assert.Equal(0.5, turned.W, 9);
        Assert.Equal(far.Y, turned.Y, 9);
        Assert.Equal(far.Z, turned.Z, 9);
        var both = Spin.Turn(far, 90, true);
        Assert.NotEqual(far.Y, both.Y, 6);
        Assert.Equal(2, Spin.StillDirections(false));
        Assert.Equal(0, Spin.StillDirections(true));
    }

    [Fact]
    public void Slices_AreTrianglesAndHexagons_TetrahedraAndOctahedra()
    {
        Assert.Equal(4, Slice.Points(3, 0.5, false).Count); // a square
        Assert.Equal(3, Slice.Points(3, 0.1, true).Count); // a triangle
        Assert.Equal(6, Slice.Points(3, 0.5, true).Count); // a hexagon
        Assert.Equal(8, Slice.Points(4, 0.5, false).Count); // a cube
        Assert.Equal(4, Slice.Points(4, 0.15, true).Count); // a tetrahedron
        Assert.Equal(12, Slice.Points(4, 0.375, true).Count); // corners cut off
        Assert.Equal(6, Slice.Points(4, 0.5, true).Count); // an octahedron
        Assert.Equal(12, Slice.Segments(4, 0.5, true).Count); // an octahedron's twelve edges
        Assert.Equal(12, Slice.Segments(4, 0.5, false).Count); // a cube's twelve edges
        Assert.Equal("Tesseract_Solid_Octahedron", Slice.SolidKey(6));
    }

    [Fact]
    public void FoldedUp_EveryCubeTouchesSixOthers_AndNeverItsOpposite()
    {
        for (var cube = 0; cube < Net.Count; cube++)
        {
            Assert.Equal(6, Net.Touching(cube).Count);
            Assert.DoesNotContain(Net.Opposite(cube), Net.Touching(cube));
            Assert.Equal(cube, Net.Opposite(Net.Opposite(cube)));
            Assert.All(Net.TouchingInCross(cube), c => Assert.Contains(c, Net.Touching(cube))); // what touches flat still touches folded
        }

        Assert.Equal(6, Net.TouchingInCross(1).Count); // B, in the middle of the cross
        Assert.Single(Net.TouchingInCross(3)); // D, at the foot of the column
        Assert.Equal(1, Net.Opposite(3));

        var lid = Net.FoldCube(0.5, 2.5, 1);
        Assert.Equal(new Vec3(0.5, 0.5, 1), Round(lid));
        Assert.Equal(new Vec3(0.5, 0, 0.5), Round(Net.FoldCube(0.5, -0.5, 1)));
        Assert.Equal(new Vec3(0.5, 2.5, 0), Round(Net.FoldCube(0.5, 2.5, 0)));
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var tesseract = new TesseractViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Tesseract").Spreads, tesseract.Spreads.Count);
        foreach (var spread in tesseract.Spreads)
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)spread.GetType().GetProperty("ReadoutText")!.GetValue(spread)));
        }

        tesseract.Build.Slide = 0.4;
        Assert.False(string.IsNullOrWhiteSpace(tesseract.Build.ReadoutText));
        (tesseract.Slice.CornerFirst, tesseract.Slice.Depth) = (true, 0.5);
        Assert.Equal(6, tesseract.Slice.SolidCorners);
        tesseract.Spin.Both = true;
        Assert.False(string.IsNullOrWhiteSpace(tesseract.Spin.ReadoutText));
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        string[] names =
        [
            .. Enumerable.Range(0, 5).Select(Build.NameKey), .. new[] { 3, 4, 5, 6, 0 }.Select(Slice.FlatKey),
            .. new[] { 4, 6, 8, 12, 0, 7 }.Select(Slice.SolidKey),
        ];
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new TesseractViewModel().Spreads.SelectMany(s => s.TextKeys).Concat(names))
            {
                Assert.Contains(key, keys);
            }
        }
    }

    /// <summary>Every picture has grips, and dragging or tapping them moves the spread's own values (the sliders follow).</summary>
    [AvaloniaFact]
    public void ThePictures_AreWorkedByTheirGrips()
    {
        var (window, model) = OpenAt("Tesseract");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Tesseract");
        var tesseract = Assert.IsType<TesseractTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Tesseract;
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

        var build = Open(0);
        build.DragGrip("next", default);
        Assert.Equal(1, tesseract.Build.Directions);
        Assert.Equal(0, tesseract.Build.Slide, 9);
        Frames(2);
        build.DragGrip("slide", Far(build, "slide", 2000, 0));
        Assert.Equal(1, tesseract.Build.Slide, 9);

        var shadow = Open(1);
        shadow.DragGrip("turn", Far(shadow, "turn", 2000, 0));
        Assert.Equal(90, tesseract.Shadow.Turn, 9);

        var nested = Open(2);
        nested.DragGrip("eye", Far(nested, "eye", -2000, 0));
        Assert.Equal(1.2, tesseract.Nested.Distance, 9);

        var spin = Open(3);
        spin.DragGrip("dial", new Avalonia.Point(spin.PartCentre("dial")!.Value.X, spin.PartCentre("dial")!.Value.Y + 2000));
        Assert.InRange(tesseract.Spin.Angle, 170, 190);

        var slice = Open(4);
        slice.DragGrip("first", default);
        Assert.True(tesseract.Slice.CornerFirst);
        slice.DragGrip("depth", Far(slice, "depth", 2000, 0));
        Assert.Equal(1, tesseract.Slice.Depth, 9);

        var net = Open(5);
        net.DragGrip("choose", default);
        Assert.Equal(2, tesseract.Net.Cube);
        Frames(2);
        net.DragGrip("fold", Far(net, "fold", 2000, 0));
        Assert.Equal(1, tesseract.Net.Fold, 9);

        window.Close();
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Tesseract");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Tesseract");
        Type[] pictures =
        [
            typeof(BuildDiagram), typeof(ShadowDiagram), typeof(NestedDiagram), typeof(SpinDiagram), typeof(SliceDiagram), typeof(NetDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"tesseract-{s + 1}");
        }

        var tesseract = Assert.IsType<TesseractTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Tesseract;
        (tesseract.Build.Dimension, tesseract.Build.Slide) = (3, 0.5);
        (tesseract.Shadow.Turn, tesseract.Shadow.Tilt) = (45, 55);
        tesseract.Nested.Distance = 1.2;
        (tesseract.Spin.Angle, tesseract.Spin.Both) = (120, true);
        (tesseract.Slice.CornerFirst, tesseract.Slice.Depth) = (true, 0.375);
        (tesseract.Net.Fold, tesseract.Net.Chosen) = (1, 3);
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(10);
            Snapshot(window, $"tesseract-{s + 1}-changed");
        }

        window.Close();
    }

    private static double Distance(Vec4 a, Vec4 b)
    {
        var d = a - b;
        return Math.Sqrt(d.Dot(d));
    }

    private static Vec3 Round(Vec3 v) => new(Math.Round(v.X, 6) + 0, Math.Round(v.Y, 6) + 0, Math.Round(v.Z, 6) + 0);
}
