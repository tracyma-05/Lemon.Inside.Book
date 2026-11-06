using System.Numerics;

namespace Lemon.Inside.Book.Controls.Scene;

/// <summary>
/// Light that bounces: every surface lit by a lamp gives some of that light back, tinted by its colour, and lights
/// the others. Each bounce passes the light once more between every pair of vertices of a <see cref="SceneMesh"/>
/// (what each vertex sends, times how squarely the two face each other, over the distance squared). What stands
/// in between is not checked: fine for open scenes like a floor, a wall and a box.
/// </summary>
public static class Radiosity
{
    /// <summary>
    /// The light arriving at each vertex after <paramref name="bounces"/> bounces, starting from the lamp's own
    /// (<paramref name="direct"/>, coloured). <paramref name="stride"/> above 1 passes light from only every
    /// stride-th vertex, scaled up: quicker and rougher.
    /// </summary>
    public static Vector3[] Bounce(SceneMesh mesh, Vector3[] direct, int bounces, int stride = 1)
    {
        var count = mesh.VertexCount;
        var positions = mesh.Positions.ToArray();
        var normals = mesh.Normals.ToArray();
        var albedo = mesh.Albedo.ToArray();
        var areas = mesh.Areas.ToArray();
        stride = Math.Max(1, stride);

        var total = (Vector3[])direct.Clone();
        var arriving = direct;
        for (var bounce = 0; bounce < bounces; bounce++)
        {
            // What each vertex sends out (its radiosity), times the area it stands for.
            var sent = new Vector3[count];
            for (var j = 0; j < count; j++)
            {
                sent[j] = albedo[j] * arriving[j] * areas[j] * stride;
            }

            var next = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                if (mesh.Emission[i] != Vector3.Zero)
                {
                    continue;
                }

                var p = positions[i];
                var n = normals[i];
                var sum = Vector3.Zero;
                for (var j = i % stride; j < count; j += stride)
                {
                    if (sent[j] == Vector3.Zero)
                    {
                        continue;
                    }

                    var d = positions[j] - p;
                    var r2 = d.LengthSquared();
                    if (r2 < 1e-6f)
                    {
                        continue;
                    }

                    var r = MathF.Sqrt(r2);
                    var cosHere = Vector3.Dot(n, d) / r;
                    var cosThere = -Vector3.Dot(normals[j], d) / r;
                    if (cosHere <= 0 || cosThere <= 0)
                    {
                        continue;
                    }

                    // Kept from blowing up where two surfaces meet (a wall and the floor).
                    sum += sent[j] * (cosHere * cosThere / (MathF.PI * MathF.Max(r2, 0.04f)));
                }

                next[i] = sum;
            }

            for (var i = 0; i < count; i++)
            {
                total[i] += next[i];
            }

            arriving = next;
        }

        return total;
    }
}
