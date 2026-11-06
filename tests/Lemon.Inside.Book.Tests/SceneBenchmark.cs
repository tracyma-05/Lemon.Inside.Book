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

        // The bounce spread's scene: floor, wall and box, about 1,100 vertices.
        var room = new SceneMesh();
        room.AddQuad(new Vector3(-1.5f, 0, 2), new Vector3(3.5f, 0, 0), new Vector3(0, 0, -4), 22, new Vector3(0.8f));
        room.AddQuad(new Vector3(-1.5f, 0, -2), new Vector3(0, 2.4f, 0), new Vector3(0, 0, 4), 18, new Vector3(0.75f, 0.1f, 0.08f));
        room.AddBox(new Box(new Vector3(0.1f, 0, -0.35f), new Vector3(0.8f, 0.7f, 0.35f)), 6, new Vector3(0.8f));
        var lit = Enumerable.Repeat(new Vector3(1), room.VertexCount).ToArray();
        foreach (var (bounces, stride) in new[] { (1, 1), (2, 1), (3, 1), (3, 4) })
        {
            var watch = Stopwatch.StartNew();
            Radiosity.Bounce(room, lit, bounces, stride);
            output.WriteLine($"radiosity {bounces} bounces, stride {stride}, {room.VertexCount} vertices: {watch.Elapsed.TotalMilliseconds:0.0} ms");
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

            // The smooth picture: the same frame shrunk by half each way.
            var small = new int[w / 2 * (h / 2)];
            watch.Restart();
            for (var i = 0; i < runs; i++)
            {
                raster.Downsample2x(small);
            }

            output.WriteLine($"  shrink to {w / 2}x{h / 2}: {watch.Elapsed.TotalMilliseconds / runs:0.0} ms");
        }
    }
}
