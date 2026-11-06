using System.Diagnostics;
using System.Numerics;
using Lemon.Inside.Book.Controls.Scene;
using Xunit;

namespace Lemon.Inside.Book.Tests;

/// <summary>
/// How long the scene renderer takes on this machine (the P0 spike's desktop numbers). Runs only when
/// <c>BOOK_BENCH</c> is set, in Release: <c>BOOK_BENCH=1 dotnet test -c Release --filter SceneBenchmark</c>.
/// </summary>
public sealed class SceneBenchmark(ITestOutputHelper output)
{
    [Fact]
    public void LightAndRasterTimes()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable("BOOK_BENCH") is { Length: > 0 }, "Set BOOK_BENCH to run.");

        var block = new Box(new Vector3(-0.4f, 0, -0.4f), new Vector3(0.4f, 0.8f, 0.4f));
        var mesh = new SceneMesh();
        mesh.AddQuad(new Vector3(-3, 0, 3), new Vector3(6, 0, 0), new Vector3(0, 0, -6), 64, new Vector3(0.78f));
        mesh.AddBox(block, 8, new Vector3(0.85f, 0.55f, 0.3f));

        foreach (var samples in new[] { 4, 8, 16 })
        {
            var lamp = new AreaLight { Center = new Vector3(0.6f, 2.5f, 0), Size = 1, Samples = samples };
            lamp.Irradiance(mesh, [block]);
            var watch = Stopwatch.StartNew();
            const int runs = 5;
            for (var i = 0; i < runs; i++)
            {
                lamp.Irradiance(mesh, [block]);
            }

            output.WriteLine($"light {samples}x{samples} samples, {mesh.VertexCount} vertices: {watch.Elapsed.TotalMilliseconds / runs:0.0} ms");
        }

        var radiance = Enumerable.Repeat(new Vector3(0.3f), mesh.VertexCount).ToArray();
        var camera = new OrbitCamera();
        foreach (var (w, h) in new[] { (320, 240), (640, 480), (1280, 960) })
        {
            var raster = new SoftwareRasterizer();
            raster.Resize(w, h);
            var view = camera.ViewProjection(w / (double)h);
            raster.Clear((0, 0, 0), (0, 0, 0));
            raster.Draw(mesh, radiance, view);
            var watch = Stopwatch.StartNew();
            const int runs = 20;
            for (var i = 0; i < runs; i++)
            {
                camera.Yaw += 3;
                raster.Clear((0, 0, 0), (0, 0, 0));
                raster.Draw(mesh, radiance, camera.ViewProjection(w / (double)h));
            }

            output.WriteLine($"raster {w}x{h}: {watch.Elapsed.TotalMilliseconds / runs:0.0} ms");
        }
    }
}
