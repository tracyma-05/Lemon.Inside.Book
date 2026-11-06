using System.Numerics;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.LightShadow;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.Controls.Scene;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The scene renderer (P0 spike): shadows from a square lamp, and the Light and shadow spread.</summary>
public sealed class LightShadowTests
{
    private static readonly Box Block = new(new Vector3(-0.4f, 0, -0.4f), new Vector3(0.4f, 0.8f, 0.4f));

    [Fact]
    public void ABox_BlocksASegmentThroughIt_AndNotOnePassingBy()
    {
        Assert.True(Block.Blocks(new Vector3(0, 2, 0), new Vector3(0, -1, 0)));
        Assert.False(Block.Blocks(new Vector3(1, 2, 0), new Vector3(1, -1, 0)));
        Assert.False(Block.Blocks(new Vector3(0, 2, 0), new Vector3(0, 1, 0)));
    }

    [Fact]
    public void LightFallsOff_WithTheSquareOfTheDistance()
    {
        var near = FloorLight(new AreaLight { Center = new Vector3(0, 1, 0), Size = 0.01f, Samples = 1 }, [], 0);
        var far = FloorLight(new AreaLight { Center = new Vector3(0, 2, 0), Size = 0.01f, Samples = 1 }, [], 0);
        Assert.Equal(4, near / far, 2);
    }

    [Fact]
    public void UnderTheBox_ItIsDark_AndABiggerLampSoftensTheShadowsEdge()
    {
        // A lamp straight above the box: the floor just past the box's edge is in shadow for a small lamp,
        // partly lit for a big one (the penumbra: some of the lamp is hidden, some is not).
        var small = new AreaLight { Center = new Vector3(0, 2.5f, 0), Size = 0.05f, Samples = 8 };
        var big = new AreaLight { Center = new Vector3(0, 2.5f, 0), Size = 2f, Samples = 16 };

        var smallEdge = FloorLight(small, [Block], 0.5f);
        var bigEdge = FloorLight(big, [Block], 0.5f);
        var bigOpen = FloorLight(big, [], 0.5f);

        Assert.Equal(0, smallEdge, 6);
        Assert.InRange(bigEdge, bigOpen * 0.05, bigOpen * 0.95);
    }

    [Fact]
    public void TheRasterizer_DrawsAQuadInFrontOfTheCamera()
    {
        var mesh = new SceneMesh();
        mesh.AddQuad(new Vector3(-1, 0, 1), new Vector3(2, 0, 0), new Vector3(0, 0, -2), 1, Vector3.One);
        var raster = new SoftwareRasterizer();
        raster.Resize(64, 48);
        raster.Clear((0, 0, 0), (0, 0, 0));
        var camera = new OrbitCamera { Target = Vector3.Zero, Pitch = 60, Distance = 5 };
        raster.Draw(mesh, Enumerable.Repeat(new Vector3(0.5f), mesh.VertexCount).ToArray(), camera.ViewProjection(64 / 48.0));

        var centre = raster.Pixels[(24 * 64) + 32] & 0xFF;
        var corner = raster.Pixels[0] & 0xFF;
        Assert.InRange(centre, 150, 220); // 0.5 in linear light is about 188 after the sRGB curve
        Assert.Equal(0, corner);
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var light = new LightShadowViewModel();
        Assert.Equal(5, light.Spreads.Count);

        light.Tilt.Tilt = 60;
        Assert.Equal(2, light.Tilt.PatchLength, 6);
        light.Distance.Distance = 2;
        Assert.Contains("25", light.Distance.BrightnessText);
        light.Angle.Distance = 10;
        var near = light.Angle.Radians;
        light.Angle.Distance = 20;
        Assert.InRange(near / light.Angle.Radians, 1.9, 2.0); // twice as far, about half the angle
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new LightShadowViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }
        }
    }

    [Fact]
    public void TwiceTheLight_LooksLessThanTwiceAsBright_AndTooMuchIsWhite()
    {
        Assert.True(BrightnessScene.Looks(0.2) / BrightnessScene.Looks(0.1) < 1.5);
        Assert.Equal(1, BrightnessScene.Looks(3), 6);
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Light-Shadow");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Light-Shadow");
        Type[] pictures = [typeof(BrightnessScene), typeof(TiltDiagram), typeof(DistanceScene), typeof(AngleDiagram), typeof(ShadowScene)];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"light-shadow-{s + 1}");
        }

        window.Close();
    }

    [AvaloniaFact]
    public void MovingASlider_DrawsCoarseLightFirst_AndDraggingTurnsTheCamera()
    {
        var (window, model) = OpenAt("Light-Shadow");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Light-Shadow");
        model.GoToPage(BookContents.SpreadPage(chapter, 4));
        Frames(5);
        var scene = window.GetVisualDescendants().OfType<ShadowScene>().First(c => c.IsEffectivelyVisible);
        Assert.True(scene.PictureSize.Width > 10);

        var yaw = scene.Camera.Yaw;
        scene.Camera.Drag(40, 0);
        Assert.NotEqual(yaw, scene.Camera.Yaw);

        var page = Assert.IsType<LightShadowTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 4)]);
        page.Light.Shadow.LightSize = 1.5;
        Frames(2);
        Assert.True(scene.IsCoarse);
        scene.Settle();
        Frames(2);
        Assert.False(scene.IsCoarse);
        Snapshot(window, "light-shadow-5-big-lamp");
        window.Close();
    }

    /// <summary>The light on the floor at (<paramref name="x"/>, 0, 0).</summary>
    private static double FloorLight(AreaLight lamp, Box[] blockers, float x)
    {
        var floor = new SceneMesh();
        floor.AddQuad(new Vector3(x, 0, 0), new Vector3(0.001f, 0, 0), new Vector3(0, 0, -0.001f), 1, Vector3.One);
        return lamp.Irradiance(floor, blockers)[0];
    }
}
