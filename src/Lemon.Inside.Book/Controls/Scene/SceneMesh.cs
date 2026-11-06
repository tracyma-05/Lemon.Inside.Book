using System.Numerics;

namespace Lemon.Inside.Book.Controls.Scene;

/// <summary>
/// Triangles for the software renderer: every vertex has a position, a normal, a surface colour (albedo) and the light
/// it gives off itself (emission). Flat surfaces are added as subdivided quads, so light computed per vertex
/// (<see cref="AreaLight"/>) shades them smoothly, soft shadows included.
/// </summary>
public sealed class SceneMesh
{
    private readonly List<Vector3> _positions = [];
    private readonly List<Vector3> _normals = [];
    private readonly List<Vector3> _albedo = [];
    private readonly List<Vector3> _emission = [];
    private readonly List<int> _indices = [];

    public int VertexCount => _positions.Count;

    public IReadOnlyList<Vector3> Positions => _positions;

    public IReadOnlyList<Vector3> Normals => _normals;

    public IReadOnlyList<Vector3> Albedo => _albedo;

    public IReadOnlyList<Vector3> Emission => _emission;

    /// <summary>Three vertex indices per triangle.</summary>
    public IReadOnlyList<int> Indices => _indices;

    /// <summary>Moves one vertex (a part that moves, such as a lamp, keeps its triangles).</summary>
    public void SetPosition(int vertex, Vector3 position) => _positions[vertex] = position;

    /// <summary>
    /// A parallelogram from <paramref name="origin"/> spanned by <paramref name="u"/> and <paramref name="v"/>, cut
    /// into <paramref name="divisions"/> × <paramref name="divisions"/> cells. Its normal is u × v.
    /// </summary>
    public void AddQuad(Vector3 origin, Vector3 u, Vector3 v, int divisions, Vector3 albedo, Vector3 emission = default)
    {
        var normal = Vector3.Normalize(Vector3.Cross(u, v));
        var first = _positions.Count;
        var n = Math.Max(1, divisions);
        for (var j = 0; j <= n; j++)
        {
            for (var i = 0; i <= n; i++)
            {
                _positions.Add(origin + (u * i / n) + (v * j / n));
                _normals.Add(normal);
                _albedo.Add(albedo);
                _emission.Add(emission);
            }
        }

        for (var j = 0; j < n; j++)
        {
            for (var i = 0; i < n; i++)
            {
                var a = first + (j * (n + 1)) + i;
                var b = a + 1;
                var c = a + n + 1;
                var d = c + 1;
                _indices.AddRange([a, b, d, a, d, c]);
            }
        }
    }

    /// <summary>An axis-aligned box, its five visible faces (no bottom) each cut into a grid, normals facing out.</summary>
    public void AddBox(Box box, int divisions, Vector3 albedo)
    {
        var (lo, hi) = (box.Min, box.Max);
        var size = hi - lo;
        var x = new Vector3(size.X, 0, 0);
        var y = new Vector3(0, size.Y, 0);
        var z = new Vector3(0, 0, size.Z);
        AddQuad(new Vector3(lo.X, hi.Y, lo.Z), z, x, divisions, albedo);                // top (+y)
        AddQuad(new Vector3(lo.X, lo.Y, hi.Z), x, y, divisions, albedo);                // front (+z)
        AddQuad(new Vector3(hi.X, lo.Y, lo.Z), -x, y, divisions, albedo);               // back (-z)
        AddQuad(new Vector3(hi.X, lo.Y, hi.Z), -z, y, divisions, albedo);               // right (+x)
        AddQuad(new Vector3(lo.X, lo.Y, lo.Z), z, y, divisions, albedo);                // left (-x)
    }
}

/// <summary>An axis-aligned box that blocks light.</summary>
public readonly record struct Box(Vector3 Min, Vector3 Max)
{
    /// <summary>Whether the segment from <paramref name="from"/> to <paramref name="to"/> passes through the box.</summary>
    public bool Blocks(Vector3 from, Vector3 to)
    {
        var d = to - from;
        double t0 = 1e-4, t1 = 1 - 1e-4;
        for (var axis = 0; axis < 3; axis++)
        {
            double o = from[axis], dir = d[axis], lo = Min[axis], hi = Max[axis];
            if (Math.Abs(dir) < 1e-12)
            {
                if (o < lo || o > hi)
                {
                    return false;
                }

                continue;
            }

            var a = (lo - o) / dir;
            var b = (hi - o) / dir;
            if (a > b)
            {
                (a, b) = (b, a);
            }

            t0 = Math.Max(t0, a);
            t1 = Math.Min(t1, b);
            if (t0 > t1)
            {
                return false;
            }
        }

        return true;
    }
}
