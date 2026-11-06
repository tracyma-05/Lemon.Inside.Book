using System.Numerics;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.LightShadow;
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

    [AvaloniaFact]
    public void TheSpread_DrawsThePicture_AndDraggingTurnsTheCamera()
    {
        var (window, model) = OpenAt("Light-Shadow");
        var diagram = window.GetVisualDescendants().OfType<LightShadowDiagram>().Single();
        Assert.True(diagram.PictureSize.Width > 10);

        var yaw = diagram.Camera.Yaw;
        diagram.Camera.Drag(40, 0);
        Assert.NotEqual(yaw, diagram.Camera.Yaw);

        var light = Assert.IsType<LightShadowTextPageViewModel>(model.Pages[TextPageOf("Light-Shadow")]).Light;
        light.LightSize = 1.5;
        Frames(5);
        Snapshot(window, "light-shadow");
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
