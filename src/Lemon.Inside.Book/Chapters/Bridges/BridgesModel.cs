namespace Lemon.Inside.Book.Chapters.Bridges;

/// <summary>1. A plank bridge: a beam resting on two banks with a load in the middle.</summary>
public static class Beam
{
    /// <summary>The load, tonnes; the plank's width, m; its wood's stiffness (Young's modulus), Pa.</summary>
    public const double Load = 2;
    public const double Width = 0.3;
    public const double Stiffness = 10e9;

    /// <summary>How far the middle sags, m: load × span³ / (48 × stiffness × the section's second moment).</summary>
    public static double Sag(double span, double depth) =>
        Load * 1000 * 9.81 * Math.Pow(span, 3) / (48 * Stiffness * (Width * Math.Pow(depth, 3) / 12));
}

/// <summary>2. A frame of four bars pinned at the corners, with or without a diagonal brace.</summary>
public static class Frame
{
    /// <summary>The hardest sideways push, tonnes; how far the unbraced square leans under it, degrees (in truth it would go flat).</summary>
    public const double MostPush = 2;
    public const double MostLean = 35;

    /// <summary>The lean under a sideways push of <paramref name="push"/> tonnes, degrees: braced, none at all.</summary>
    public static double Lean(double push, bool brace) => brace ? 0 : Math.Clamp(push / MostPush, 0, 1) * MostLean;

    /// <summary>What the brace carries for a sideways push at the top corner: the push over cos 45°.</summary>
    public static double BraceForce(double push) => push * Math.Sqrt(2);
}

/// <summary>
/// 3. A Warren truss of four panels: five joints along the deck, four along the top, joined by straight members
/// pinned at their ends. The forces come from the balance at every joint (a statically determinate truss).
/// </summary>
public static class Truss
{
    /// <summary>The span, m; the lorry's weight, tonnes.</summary>
    public const double Span = 16;
    public const int Panels = 4;
    public const double Lorry = 10;

    public enum Kind { Bottom, Top, Diagonal }

    /// <summary>Joint indices: 0..4 along the deck, 5..8 along the top.</summary>
    public static readonly IReadOnlyList<(int A, int B, Kind Kind)> Members = BuildMembers();

    public static double PanelLength => Span / Panels;

    /// <summary>Where joint <paramref name="j"/> is, m (x along the deck, y up), for a truss <paramref name="height"/> tall.</summary>
    public static (double X, double Y) Joint(int j, double height) =>
        j <= Panels ? (j * PanelLength, 0) : ((j - Panels - 0.5) * PanelLength, height);

    /// <summary>
    /// Each member's force, tonnes: positive when it is stretched, negative when squeezed, with the lorry at
    /// <paramref name="position"/> (0..1 of the span) shared between the deck joints either side of it.
    /// </summary>
    public static double[] Forces(double height, double position, double load = Lorry)
    {
        var joints = (2 * Panels) + 1;
        var unknowns = Members.Count + 3; // the members, then the pin's two reactions and the roller's one
        var a = new double[2 * joints, unknowns + 1];
        for (var m = 0; m < Members.Count; m++)
        {
            var (j0, j1, _) = Members[m];
            var (x0, y0) = Joint(j0, height);
            var (x1, y1) = Joint(j1, height);
            var length = Math.Sqrt(((x1 - x0) * (x1 - x0)) + ((y1 - y0) * (y1 - y0)));
            var (ux, uy) = ((x1 - x0) / length, (y1 - y0) / length);

            // A stretched member pulls each of its joints toward the other.
            a[2 * j0, m] += ux;
            a[(2 * j0) + 1, m] += uy;
            a[2 * j1, m] -= ux;
            a[(2 * j1) + 1, m] -= uy;
        }

        a[0, Members.Count] = 1;
        a[1, Members.Count + 1] = 1;
        a[(2 * Panels) + 1, Members.Count + 2] = 1;

        // The lorry, shared between the two deck joints round it; the balance puts the loads on the right-hand side.
        var at = Math.Clamp(position, 0, 1) * Panels;
        var left = Math.Min((int)Math.Floor(at), Panels - 1);
        var share = at - left;
        a[(2 * left) + 1, unknowns] += load * (1 - share);
        a[(2 * (left + 1)) + 1, unknowns] += load * share;

        var solution = Solve(a, unknowns);
        return solution[..Members.Count];
    }

    /// <summary>The biggest squeeze in the top chord and stretch in the bottom chord, tonnes.</summary>
    public static (double Squeeze, double Stretch) Chords(double height, double position)
    {
        var forces = Forces(height, position);
        double squeeze = 0, stretch = 0;
        for (var m = 0; m < Members.Count; m++)
        {
            if (Members[m].Kind == Kind.Top) squeeze = Math.Max(squeeze, -forces[m]);
            if (Members[m].Kind == Kind.Bottom) stretch = Math.Max(stretch, forces[m]);
        }

        return (squeeze, stretch);
    }

    private static List<(int, int, Kind)> BuildMembers()
    {
        var members = new List<(int, int, Kind)>();
        for (var i = 0; i < Panels; i++) members.Add((i, i + 1, Kind.Bottom));
        for (var i = 0; i < Panels - 1; i++) members.Add((Panels + 1 + i, Panels + 2 + i, Kind.Top));
        for (var i = 0; i < Panels; i++)
        {
            members.Add((i, Panels + 1 + i, Kind.Diagonal));
            members.Add((Panels + 1 + i, i + 1, Kind.Diagonal));
        }

        return members;
    }

    /// <summary>Gaussian elimination with partial pivoting on a square system (rows = unknowns).</summary>
    private static double[] Solve(double[,] a, int n)
    {
        for (var c = 0; c < n; c++)
        {
            var pivot = c;
            for (var r = c + 1; r < n; r++) if (Math.Abs(a[r, c]) > Math.Abs(a[pivot, c])) pivot = r;
            for (var k = 0; k <= n; k++) (a[c, k], a[pivot, k]) = (a[pivot, k], a[c, k]);
            for (var r = 0; r < n; r++)
            {
                if (r == c || a[r, c] == 0) continue;
                var f = a[r, c] / a[c, c];
                for (var k = c; k <= n; k++) a[r, k] -= f * a[c, k];
            }
        }

        var x = new double[n];
        for (var c = 0; c < n; c++) x[c] = a[c, n] / a[c, c];
        return x;
    }
}

/// <summary>4. A stone arch: its weight pushes out on the banks as well as down.</summary>
public static class Arch
{
    /// <summary>The span, m; the weight of the arch and the road on it, tonnes.</summary>
    public const double Span = 10;
    public const double Weight = 100;

    /// <summary>The outward push on each bank, tonnes: weight × span / (8 × rise), for weight spread evenly.</summary>
    public static double Thrust(double rise) => Weight * Span / (8 * rise);
}

/// <summary>5. A suspension bridge's cable, hanging between two towers with the deck hung from it.</summary>
public static class Cable
{
    /// <summary>The span between the towers, m; the deck's weight, tonnes.</summary>
    public const double Span = 200;
    public const double Deck = 2000;

    /// <summary>The cable's sideways pull, the same all along it, tonnes: deck × span / (8 × sag).</summary>
    public static double Pull(double sag) => Deck * Span / (8 * sag);

    /// <summary>The pull at the top of a tower, where the cable also holds up half the deck, tonnes.</summary>
    public static double AtTower(double sag) => Math.Sqrt((Pull(sag) * Pull(sag)) + (Deck / 2 * Deck / 2));

    /// <summary>The cable's height above its lowest point at <paramref name="x"/> m from the middle: a parabola.</summary>
    public static double Height(double x, double sag) => sag * 4 * x * x / (Span * Span);
}

/// <summary>6. A steel deck that grows in the sun and shrinks in the cold.</summary>
public static class Deck
{
    /// <summary>The deck's length, m; steel's growth per metre per degree; the day it was built for, °C.</summary>
    public const double Length = 400;
    public const double Growth = 12e-6;
    public const double Mild = 15;

    /// <summary>Steel's stiffness, Pa; the deck's steel cross-section, m².</summary>
    public const double Stiffness = 200e9;
    public const double Section = 0.5;

    /// <summary>How much longer than on a mild day, cm (negative: shorter).</summary>
    public static double Change(double temperature) => Length * Growth * (temperature - Mild) * 100;

    /// <summary>Held fast at both ends instead, the push on each abutment, tonnes.</summary>
    public static double Push(double temperature) =>
        Math.Abs(Stiffness * Growth * (temperature - Mild) * Section) / 9810;
}
