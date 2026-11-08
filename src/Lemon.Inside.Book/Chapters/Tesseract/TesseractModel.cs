namespace Lemon.Inside.Book.Chapters.Tesseract;

/// <summary>A point in four directions: x, y, z and w, the fourth.</summary>
public readonly record struct Vec4(double X, double Y, double Z, double W)
{
    public static Vec4 operator +(Vec4 a, Vec4 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);

    public static Vec4 operator -(Vec4 a, Vec4 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);

    public static Vec4 operator *(Vec4 a, double k) => new(a.X * k, a.Y * k, a.Z * k, a.W * k);

    public double this[int axis] => axis switch { 0 => X, 1 => Y, 2 => Z, _ => W };

    public Vec4 With(int axis, double value) => axis switch
    {
        0 => this with { X = value },
        1 => this with { Y = value },
        2 => this with { Z = value },
        _ => this with { W = value },
    };

    public double Dot(Vec4 o) => (X * o.X) + (Y * o.Y) + (Z * o.Z) + (W * o.W);

    public override string ToString() => FormattableString.Invariant($"({X:0.###}, {Y:0.###}, {Z:0.###}, {W:0.###})");
}

/// <summary>A point in three directions.</summary>
public readonly record struct Vec3(double X, double Y, double Z)
{
    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vec3 operator *(Vec3 a, double k) => new(a.X * k, a.Y * k, a.Z * k);

    public override string ToString() => FormattableString.Invariant($"({X:0.###}, {Y:0.###}, {Z:0.###})");
}

/// <summary>
/// Cubes of any number of directions, 0 (a point) to 4 (a tesseract), one unit across and centred on the origin: each
/// corner is ±½ along each of the shape's directions. A corner's index says which: bit k set is +½ along direction k.
/// </summary>
public static class HyperCube
{
    public static int Corners(int n) => 1 << n;

    /// <summary>Two copies of the shape one direction down, each corner joined to its copy.</summary>
    public static int Edges(int n) => n == 0 ? 0 : (2 * Edges(n - 1)) + Corners(n - 1);

    public static int Squares(int n) => n < 2 ? 0 : Choose(n, 2) * (1 << (n - 2));

    public static int Cubes(int n) => n < 3 ? 0 : Choose(n, 3) * (1 << (n - 3));

    public static Vec4 Corner(int index, int n)
    {
        var p = new Vec4(0, 0, 0, 0);
        for (var k = 0; k < n; k++) p = p.With(k, (index & (1 << k)) != 0 ? 0.5 : -0.5);
        return p;
    }

    public static IReadOnlyList<Vec4> CornerList(int n) => Enumerable.Range(0, Corners(n)).Select(i => Corner(i, n)).ToList();

    /// <summary>Pairs of corners one edge apart: they differ along exactly one direction.</summary>
    public static IReadOnlyList<(int A, int B)> EdgeList(int n)
    {
        var edges = new List<(int, int)>();
        for (var a = 0; a < Corners(n); a++)
        {
            for (var k = 0; k < n; k++)
            {
                var b = a | (1 << k);
                if (b != a) edges.Add((a, b));
            }
        }

        return edges;
    }

    /// <summary>The square faces, each as its four corners in order round it.</summary>
    public static IReadOnlyList<int[]> SquareList(int n)
    {
        var squares = new List<int[]>();
        for (var i = 0; i < n; i++)
        {
            for (var j = i + 1; j < n; j++)
            {
                for (var rest = 0; rest < Corners(n); rest++)
                {
                    if ((rest & ((1 << i) | (1 << j))) != 0) continue;
                    squares.Add([rest, rest | (1 << i), rest | (1 << i) | (1 << j), rest | (1 << j)]);
                }
            }
        }

        return squares;
    }

    /// <summary>Turns a point in the plane of directions <paramref name="a"/> and <paramref name="b"/>; the others stay.</summary>
    public static Vec4 Turn(Vec4 p, int a, int b, double degrees)
    {
        var r = degrees * Math.PI / 180;
        var (c, s) = (Math.Cos(r), Math.Sin(r));
        return p.With(a, (p[a] * c) - (p[b] * s)).With(b, (p[a] * s) + (p[b] * c));
    }

    private static int Choose(int n, int k)
    {
        var result = 1;
        for (var i = 1; i <= k; i++) result = result * (n - k + i) / i;
        return result;
    }
}

/// <summary>Seeing four directions: a shadow cast along w, near things bigger (perspective), from an eye at w = distance.</summary>
public static class Look
{
    public static double Scale(double w, double distance) => distance / Math.Max(0.05, distance - w);

    public static Vec3 Shadow(Vec4 p, double distance)
    {
        var s = Scale(p.W, distance);
        return new Vec3(p.X * s, p.Y * s, p.Z * s);
    }

    /// <summary>The inner cube's size over the outer's: the far cube (w = −½) over the near one (w = +½).</summary>
    public static double InnerRatio(double distance) => Scale(-0.5, distance) / Scale(0.5, distance);
}

/// <summary>1. Building cubes by sliding: each new direction doubles the corners.</summary>
public static class Build
{
    public static string NameKey(int n) => $"Tesseract_Shape_{Math.Clamp(n, 0, 4)}";
}

/// <summary>
/// 2. A cube's shadow on the floor in light from straight above: the cube turned about the upright by
/// <c>turn</c> degrees, then tipped by <c>tilt</c>. A unit cube; the floor is z = 0 and up is z.
/// </summary>
public static class Shadow
{
    public static IReadOnlyList<Vec3> Corners(double turn, double tilt)
    {
        var list = new List<Vec3>();
        for (var i = 0; i < 8; i++)
        {
            var p = HyperCube.Corner(i, 3);
            p = HyperCube.Turn(p, 0, 1, turn);
            p = HyperCube.Turn(p, 1, 2, tilt);
            list.Add(new Vec3(p.X, p.Y, p.Z));
        }

        return list;
    }

    /// <summary>The shadow's outline: the corners' points on the floor that stand out, in order round it.</summary>
    public static IReadOnlyList<(double X, double Y)> Outline(double turn, double tilt) =>
        Hull(Corners(turn, tilt).Select(c => (c.X, c.Y)).ToList());

    public static double Area(double turn, double tilt)
    {
        var hull = Outline(turn, tilt);
        var area = 0.0;
        for (var i = 0; i < hull.Count; i++)
        {
            var (a, b) = (hull[i], hull[(i + 1) % hull.Count]);
            area += (a.X * b.Y) - (b.X * a.Y);
        }

        return Math.Abs(area) / 2;
    }

    /// <summary>The biggest shadow a unit cube can cast: standing on a corner, √3 faces' worth.</summary>
    public static double Biggest => Math.Sqrt(3);

    /// <summary>The tilt that stands the cube on a corner (turned 45°): a body diagonal upright.</summary>
    public static double CornerTilt => Math.Atan(Math.Sqrt(2)) * 180 / Math.PI;

    /// <summary>A convex outline of points (gift wrapping, collinear points dropped).</summary>
    public static List<(double X, double Y)> Hull(List<(double X, double Y)> points)
    {
        var p = points.Distinct(new Near()).OrderBy(q => q.X).ThenBy(q => q.Y).ToList();
        if (p.Count < 3) return p;

        static double Cross((double X, double Y) o, (double X, double Y) a, (double X, double Y) b) =>
            ((a.X - o.X) * (b.Y - o.Y)) - ((a.Y - o.Y) * (b.X - o.X));
        var hull = new List<(double, double)>();
        foreach (var pass in new[] { p, Enumerable.Reverse(p).ToList() })
        {
            var start = hull.Count;
            foreach (var q in pass)
            {
                while (hull.Count >= start + 2 && Cross(hull[^2], hull[^1], q) <= 1e-9) hull.RemoveAt(hull.Count - 1);
                hull.Add(q);
            }

            hull.RemoveAt(hull.Count - 1);
        }

        return hull;
    }

    private sealed class Near : IEqualityComparer<(double X, double Y)>
    {
        public bool Equals((double X, double Y) a, (double X, double Y) b) => Math.Abs(a.X - b.X) < 1e-6 && Math.Abs(a.Y - b.Y) < 1e-6;

        public int GetHashCode((double X, double Y) a) => HashCode.Combine(Math.Round(a.X, 4), Math.Round(a.Y, 4));
    }
}

/// <summary>4. Turning in four directions: in the x–w plane, and with <c>both</c> also in the y–z plane at once.</summary>
public static class Spin
{
    public static Vec4 Turn(Vec4 p, double degrees, bool both)
    {
        var q = HyperCube.Turn(p, 0, 3, degrees);
        return both ? HyperCube.Turn(q, 1, 2, degrees) : q;
    }

    /// <summary>How many of the four directions a turn leaves alone.</summary>
    public static int StillDirections(bool both) => both ? 0 : 2;
}

/// <summary>
/// 5. Slicing: a unit cube (corners 0 to 1) cut by a flat plane, and a unit tesseract cut by a flat 3D space, either
/// face first (z, or w, equals the depth) or corner first (the coordinates add up to the depth times the number of
/// directions). <c>depth</c> runs 0 to 1 through the shape.
/// </summary>
public static class Slice
{
    /// <summary>Where the cut crosses the shape's edges: the slice's corners, in the cut's own directions.</summary>
    public static IReadOnlyList<double[]> Points(int n, double depth, bool cornerFirst) =>
        Segments(n, depth, cornerFirst).SelectMany(s => new[] { s.A, s.B }).Distinct(new NearPoint()).ToList();

    /// <summary>The slice's edges: where the cut crosses each square face of the shape.</summary>
    public static IReadOnlyList<(double[] A, double[] B)> Segments(int n, double depth, bool cornerFirst)
    {
        var level = cornerFirst ? depth * n : depth;
        double Height(Vec4 p) => cornerFirst ? Enumerable.Range(0, n).Sum(k => p[k]) : p[n - 1];
        Vec4 At(int i) => HyperCube.Corner(i, n) + Half(n);
        var segments = new List<(double[], double[])>();
        foreach (var square in HyperCube.SquareList(n))
        {
            var crossings = new List<double[]>();
            for (var k = 0; k < 4; k++)
            {
                var (a, b) = (At(square[k]), At(square[(k + 1) % 4]));
                var (ha, hb) = (Height(a) - level, Height(b) - level);
                if (Math.Abs(ha) < 1e-9 && Math.Abs(hb) < 1e-9) continue; // the edge lies in the cut
                if (Math.Abs(ha) < 1e-9) crossings.Add(Within(a, n, cornerFirst));
                else if (ha * hb < 0) crossings.Add(Within(a + ((b - a) * (ha / (ha - hb))), n, cornerFirst));
            }

            var distinct = crossings.Distinct(new NearPoint()).ToList();
            if (distinct.Count == 2) segments.Add((distinct[0], distinct[1]));
        }

        return segments;
    }

    /// <summary>A point in the cut, in coordinates along it (n − 1 of them), centred on the shape's middle line.</summary>
    private static double[] Within(Vec4 p, int n, bool cornerFirst)
    {
        var c = p - Half(n); // centred on the origin
        if (!cornerFirst) return Enumerable.Range(0, n - 1).Select(k => c[k]).ToArray();

        return n == 3
            ? [(c.X - c.Y) / Math.Sqrt(2), (c.X + c.Y - (2 * c.Z)) / Math.Sqrt(6)]
            : [(c.X - c.Y) / Math.Sqrt(2), (c.X + c.Y - (2 * c.Z)) / Math.Sqrt(6), (c.X + c.Y + c.Z - (3 * c.W)) / Math.Sqrt(12)];
    }

    private static Vec4 Half(int n)
    {
        var h = new Vec4(0, 0, 0, 0);
        for (var k = 0; k < n; k++) h = h.With(k, 0.5);
        return h;
    }

    /// <summary>The shape a slice of a cube with this many corners is: a key <c>Tesseract_Flat_…</c>.</summary>
    public static string FlatKey(int corners) => corners switch
    {
        3 => "Tesseract_Flat_Triangle",
        4 => "Tesseract_Flat_Square",
        5 => "Tesseract_Flat_Pentagon",
        6 => "Tesseract_Flat_Hexagon",
        _ => "Tesseract_Flat_None",
    };

    /// <summary>The shape a slice of a tesseract with this many corners is: a key <c>Tesseract_Solid_…</c>.</summary>
    public static string SolidKey(int corners) => corners switch
    {
        4 => "Tesseract_Solid_Tetrahedron",
        6 => "Tesseract_Solid_Octahedron",
        8 => "Tesseract_Solid_Cube",
        12 => "Tesseract_Solid_Truncated",
        0 => "Tesseract_Solid_None",
        _ => "Tesseract_Solid_Other",
    };

    private sealed class NearPoint : IEqualityComparer<double[]>
    {
        public bool Equals(double[]? a, double[]? b) => a!.Zip(b!).All(t => Math.Abs(t.First - t.Second) < 1e-6);

        public int GetHashCode(double[] a) => a.Aggregate(17, (h, v) => HashCode.Combine(h, Math.Round(v, 4)));
    }
}

/// <summary>
/// 6. Unfolding. A cube unfolds into a cross of six squares; a tesseract into a cross of eight cubes, a column of four
/// with four more round the second. Cubes A to H; each has an opposite it never touches.
/// </summary>
public static class Net
{
    public const int Count = 8;

    /// <summary>Each cube's place in the unfolded cross (x, y, z in cubes), and the cube opposite it.</summary>
    public static readonly (int X, int Y, int Z)[] Places =
    [
        (0, 0, 1), (0, 0, 0), (0, 0, -1), (0, 0, -2), (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0),
    ];

    private static readonly int[] Opposites = [2, 3, 0, 1, 5, 4, 7, 6];

    public static string Letter(int cube) => ((char)('A' + Math.Clamp(cube, 0, Count - 1))).ToString();

    public static int Opposite(int cube) => Opposites[Math.Clamp(cube, 0, Count - 1)];

    /// <summary>Folded up in four directions, every cube shares a face with all the others but its opposite.</summary>
    public static IReadOnlyList<int> Touching(int cube) =>
        Enumerable.Range(0, Count).Where(c => c != cube && c != Opposite(cube)).ToList();

    /// <summary>The cubes it shares a face with in the unfolded cross.</summary>
    public static IReadOnlyList<int> TouchingInCross(int cube)
    {
        var p = Places[Math.Clamp(cube, 0, Count - 1)];
        return Enumerable.Range(0, Count).Where(c =>
        {
            var q = Places[c];
            return Math.Abs(p.X - q.X) + Math.Abs(p.Y - q.Y) + Math.Abs(p.Z - q.Z) == 1;
        }).ToList();
    }

    /// <summary>
    /// A point of the unfolded cube's cross (x, y on the floor) folded up by <paramref name="fold"/> (0 flat, 1 a closed
    /// cube). The middle square is 0 to 1 each way; the others hinge on its edges, and the lid on the far one.
    /// </summary>
    public static Vec3 FoldCube(double x, double y, double fold)
    {
        var a = Math.Clamp(fold, 0, 1) * Math.PI / 2;
        var (c, s) = (Math.Cos(a), Math.Sin(a));
        if (y > 2)
        {
            // The lid: hinged on the far square's edge, which is itself hinged on the middle square.
            var t = y - 2;
            var (along, up) = (1 + (t * c), t * s);
            return new Vec3(x, 1 + (along * c) - (up * s), (along * s) + (up * c));
        }

        if (y > 1) return new Vec3(x, 1 + ((y - 1) * c), (y - 1) * s);
        if (y < 0) return new Vec3(x, -y * -c, -y * s);
        if (x > 1) return new Vec3(1 + ((x - 1) * c), y, (x - 1) * s);
        if (x < 0) return new Vec3(-x * -c, y, -x * s);
        return new Vec3(x, y, 0);
    }

    /// <summary>The cross's six squares, each by its lower-left corner.</summary>
    public static readonly (double X, double Y)[] Squares = [(0, 0), (0, -1), (0, 1), (0, 2), (-1, 0), (1, 0)];
}
