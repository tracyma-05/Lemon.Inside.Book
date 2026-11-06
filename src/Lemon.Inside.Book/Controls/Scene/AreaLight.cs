using System.Numerics;

namespace Lemon.Inside.Book.Controls.Scene;

/// <summary>
/// A flat square lamp facing straight down, and the light it puts on every vertex of a <see cref="SceneMesh"/>.
/// The lamp is sampled at <see cref="Samples"/> × <see cref="Samples"/> points; each point's light falls off with the
/// square of the distance and with the cosine at both ends, and is stopped by the boxes in the way, which is what makes
/// a big lamp's shadow soft and a small one's sharp.
/// </summary>
public sealed class AreaLight
{
    /// <summary>Centre of the lamp.</summary>
    public Vector3 Center { get; set; } = new(0, 2.5f, 0);

    /// <summary>Length of the lamp's side.</summary>
    public float Size { get; set; } = 0.5f;

    /// <summary>
    /// The lamp's power, kept the same whatever its size: a bigger lamp of the same power is dimmer to look at
    /// (lower radiance) but lights the room as much.
    /// </summary>
    public float Power { get; set; } = 40f;

    /// <summary>Samples along each side of the lamp.</summary>
    public int Samples { get; set; } = 8;

    /// <summary>What the lamp looks like: its radiance (power per area per unit of projected solid angle).</summary>
    public float Radiance => Power / (MathF.PI * MathF.Max(Size * Size, 1e-4f));

    /// <summary>
    /// The irradiance (light arriving per area) at each vertex of <paramref name="mesh"/>, with the boxes in
    /// <paramref name="blockers"/> casting shadows.
    /// </summary>
    public float[] Irradiance(SceneMesh mesh, IReadOnlyList<Box> blockers)
    {
        var n = Math.Max(1, Samples);
        var points = new Vector3[n * n];
        var half = Size / 2;
        for (var j = 0; j < n; j++)
        {
            for (var i = 0; i < n; i++)
            {
                // Stratified, with a fixed jitter in each cell: no banding, and the same picture every time.
                var jx = Hash(i, j) - 0.5f;
                var jz = Hash(j + 31, i + 17) - 0.5f;
                points[(j * n) + i] = Center + new Vector3(
                    -half + (Size * (i + 0.5f + (jx * 0.8f)) / n),
                    0,
                    -half + (Size * (j + 0.5f + (jz * 0.8f)) / n));
            }
        }

        var lampNormal = new Vector3(0, -1, 0);
        var perSample = Radiance * MathF.Max(Size * Size, 1e-4f) / points.Length;
        var result = new float[mesh.VertexCount];
        for (var v = 0; v < result.Length; v++)
        {
            var p = mesh.Positions[v];
            var normal = mesh.Normals[v];
            if (mesh.Emission[v] != Vector3.Zero)
            {
                continue;
            }

            var start = p + (normal * 1e-3f);
            var sum = 0f;
            foreach (var q in points)
            {
                var d = q - start;
                var r2 = d.LengthSquared();
                if (r2 < 1e-8f)
                {
                    continue;
                }

                var dir = d / MathF.Sqrt(r2);
                var cosSurface = Vector3.Dot(normal, dir);
                var cosLamp = -Vector3.Dot(lampNormal, dir);
                if (cosSurface <= 0 || cosLamp <= 0)
                {
                    continue;
                }

                var blocked = false;
                for (var b = 0; b < blockers.Count && !blocked; b++)
                {
                    blocked = blockers[b].Blocks(start, q);
                }

                if (!blocked)
                {
                    sum += cosSurface * cosLamp / r2;
                }
            }

            result[v] = sum * perSample;
        }

        return result;
    }

    private static float Hash(int a, int b)
    {
        var h = (uint)((a * 374761393) + (b * 668265263));
        h = (h ^ (h >> 13)) * 1274126177u;
        return (h ^ (h >> 16)) / (float)uint.MaxValue;
    }
}
