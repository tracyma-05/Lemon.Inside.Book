namespace Lemon.Inside.Book.Chapters.Sewing;

/// <summary>
/// 1. A seam ten centimetres long through two layers of cloth: one thread looped into a chain, or two threads locked
/// together in the middle of the cloth. Lengths in millimetres.
/// </summary>
public static class Seam
{
    /// <summary>The two layers' thickness, mm; the seam's length, mm.</summary>
    public const double Cloth = 1;
    public const double Length = 100;

    public static double Clamp(double stitch) => Math.Clamp(stitch, 1, 5);

    public static int Stitches(double stitch) => (int)Math.Floor(Length / Clamp(stitch));

    /// <summary>
    /// The upper thread one stitch takes, mm. In a lockstitch it runs along the top and dips half-way into the cloth at
    /// each hole and back; in a chain stitch it also goes right through and loops along the underside to the next hole.
    /// </summary>
    public static double Upper(double stitch, bool chain) => chain ? (3 * Clamp(stitch)) + (2 * Cloth) : Clamp(stitch) + Cloth;

    /// <summary>The lower (bobbin) thread one stitch takes, mm: none in a chain stitch.</summary>
    public static double Lower(double stitch, bool chain) => chain ? 0 : Clamp(stitch) + Cloth;

    /// <summary>All the thread in the seam, mm.</summary>
    public static double Thread(double stitch, bool chain) => Stitches(stitch) * (Upper(stitch, chain) + Lower(stitch, chain));

    /// <summary>Pull the last loop of a chain and every stitch comes undone; each lockstitch is a knot of its own.</summary>
    public static bool Unravels(bool chain) => chain;
}

/// <summary>
/// 2. The needle, driven up and down by a crank on the handwheel's shaft. The handwheel's angle is in degrees: 0 with
/// the needle at the top, 180 at the bottom, rising again from 180 to 360.
/// </summary>
public static class Needle
{
    /// <summary>The crank's radius and the connecting rod's length, mm: a 30 mm stroke.</summary>
    public const double Crank = 15;
    public const double Rod = 45;

    /// <summary>How high the needle's tip is above its lowest point, mm.</summary>
    public static double Rise(double angle)
    {
        var a = angle * Math.PI / 180;
        var s = Crank * Math.Sin(a);
        return (Crank * Math.Cos(a)) + Math.Sqrt((Rod * Rod) - (s * s)) - (Rod - Crank);
    }

    /// <summary>Is the needle on its way up (past the bottom, before the top)?</summary>
    public static bool Rising(double angle) => Wrap(angle) > 180;

    /// <summary>The cloth's top, above the needle's lowest point, mm: the needle is out of the cloth above it.</summary>
    public const double ClothHeight = 12;

    /// <summary>The loop's widest, mm, reached when the needle has come up <see cref="Peak"/> mm.</summary>
    public const double Widest = 3;
    public const double Peak = 2.5;

    /// <summary>
    /// The loop of thread beside the needle, mm wide. On the way up the cloth grips the thread on the needle's short
    /// side, so the thread there stays behind and bulges out; once the needle has come up twice <see cref="Peak"/> the
    /// thread is drawn up straight again. A needle with its eye at the top (a hand-sewing needle) makes no loop.
    /// </summary>
    public static double Loop(double angle, bool eyeAtPoint = true)
    {
        if (!eyeAtPoint || !Rising(angle)) return 0;

        var r = Rise(angle) / Peak;
        return r >= 2 ? 0 : Widest * r * (2 - r);
    }

    public static double Wrap(double angle) => ((angle % 360) + 360) % 360;
}

/// <summary>3. The hook: its point must pass the needle just as the loop opens.</summary>
public static class Hook
{
    /// <summary>The handwheel angle at which a well-set hook's point passes the needle, degrees.</summary>
    public const double Right = 220;

    /// <summary>The narrowest loop the point can slip into, mm.</summary>
    public const double Narrowest = 2;

    /// <summary>The loop's width as the point passes, mm, with the hook set <paramref name="timing"/> degrees late.</summary>
    public static double LoopAt(double timing) => Needle.Loop(Right + timing);

    public static bool Catches(double timing) => LoopAt(timing) >= Narrowest;

    /// <summary>How far the needle has come up as the point passes, mm.</summary>
    public static double RiseAt(double timing) => Needle.Rise(Right + timing);

    /// <summary>The hook turns twice for each stitch: its angle, degrees, with 0 the point at the needle.</summary>
    public static double Turn(double angle, double timing) => Needle.Wrap(2 * (angle - Right - timing));
}

/// <summary>
/// 4. The take-up lever: it lets down enough upper thread for the loop to go round the bobbin case, then pulls it all
/// back up and sets the stitch.
/// </summary>
public static class TakeUp
{
    /// <summary>The bobbin case's width, mm.</summary>
    public const double Bobbin = 22;

    /// <summary>The thread let down for the loop to go round the bobbin case, mm.</summary>
    public static double Loop => Math.PI * Bobbin;

    /// <summary>The thread that stays in the seam each stitch, mm.</summary>
    public static double PerStitch(double stitch) => Seam.Upper(stitch, false);

    /// <summary>
    /// How many times a bit of thread rubs through the needle's eye before it is sewn in: each stitch it goes down a
    /// loop's length and back, moving on only a stitch's worth.
    /// </summary>
    public static double Passes(double stitch) => 2 * Loop / PerStitch(stitch);

    /// <summary>
    /// The share of the loop let down below the cloth, 0 to 1, at a handwheel angle: it opens as the hook takes the loop,
    /// is widest as it goes round the bobbin, and is all drawn back up just after the needle has passed the top.
    /// </summary>
    public static double Down(double angle)
    {
        var p = Needle.Wrap(angle - Hook.Right);
        if (p < 100) return Smooth(p / 100);
        if (p < 120) return 1;
        return p < 190 ? 1 - Smooth((p - 120) / 70) : 0;
    }

    /// <summary>The lever's height, 0 (lowest) to 1 (highest): it falls to let the loop down and rises to take it back.</summary>
    public static double Lever(double angle) => 1 - Down(angle);

    private static double Smooth(double t) => t * t * (3 - (2 * t));
}

/// <summary>5. The feed dogs: teeth under the cloth that move it on a stitch while the needle is out of it.</summary>
public static class Feed
{
    /// <summary>A home machine's pace, stitches a minute; how far the teeth rise through the plate, mm.</summary>
    public const double Pace = 800;
    public const double Lift = 1;

    public static double MetresPerMinute(double stitch) => Pace * Math.Max(0, stitch) / 1000;

    /// <summary>
    /// Where the teeth are at a handwheel angle, mm: forward along the seam, and up above the plate. They rise while the
    /// needle is above the cloth, carry it a stitch on, sink and come back underneath.
    /// </summary>
    public static (double X, double Y) Dog(double angle, double stitch, bool reverse)
    {
        var a = angle * Math.PI / 180;
        var x = Math.Max(0, stitch) / 2 * Math.Sin(a);
        return (reverse ? -x : x, Lift * Math.Cos(a));
    }
}

/// <summary>
/// 6. Tension: the upper thread's pull, set on the dial, against the bobbin thread's, set by a screw on the bobbin case.
/// Where they lock depends on which pulls harder. Pulls in grams-force at the stitch, as this picture's model has them.
/// </summary>
public static class Tension
{
    /// <summary>The upper pull per dial number, gf; the difference that drags the knot right through, gf.</summary>
    public const double PerNumber = 7.5;
    public const double Span = 20;

    public static double Upper(double dial) => PerNumber * dial;

    /// <summary>Where the threads lock, as a share of the cloth's thickness from the top: 0 on top, 1 underneath.</summary>
    public static double Knot(double dial, double bobbin) => Math.Clamp(0.5 + ((bobbin - Upper(dial)) / (2 * Span)), 0, 1);

    /// <summary>-1 the upper thread is too tight (bobbin thread shows on top), 1 too loose (loops underneath), 0 even.</summary>
    public static int Verdict(double dial, double bobbin)
    {
        var knot = Knot(dial, bobbin);
        return knot < 0.3 ? -1 : knot > 0.7 ? 1 : 0;
    }
}
