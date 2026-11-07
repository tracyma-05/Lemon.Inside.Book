namespace Lemon.Inside.Book.Chapters.Locks;

/// <summary>
/// Spreads 1, 3 and 4: a pin-tumbler lock seen from the side, all in millimetres. The key's blade lies along x, from its
/// shoulder (x = 0, at the lock's face when the key is fully in) to its tip; heights are measured up from the blade's
/// bottom edge. Above each cut stands a stack: a key pin resting on the key, a driver pin, a spring. The plug can turn
/// only when every key pin's top is exactly at the shear line, the gap between the plug and the shell.
/// </summary>
public static class PinTumbler
{
    /// <summary>Pin stacks in the lock.</summary>
    public const int Pins = 5;

    /// <summary>The first pin's distance from the lock's face (and from the key's shoulder, fully in).</summary>
    public const double FirstPin = 4;

    /// <summary>From one pin to the next.</summary>
    public const double Spacing = 4;

    /// <summary>The blade's height, uncut.</summary>
    public const double Blade = 8;

    /// <summary>How much deeper each step of cut goes.</summary>
    public const double Step = 0.5;

    /// <summary>The shallowest cut (depth 0) still takes this much off the blade.</summary>
    public const double FirstDepth = 1;

    /// <summary>Cut depths, 0 (shallow) to <see cref="Depths"/> − 1 (deep).</summary>
    public const int Depths = 10;

    /// <summary>The shear line's height: the top of the plug, a little above the keyway.</summary>
    public const double ShearLine = Blade + 2.5;

    /// <summary>How close a key pin's top must come to the shear line for the plug to pass, mm.</summary>
    public const double Tolerance = 0.05;

    /// <summary>With no key under it, a key pin drops until it rests this high (on the keyway's floor).</summary>
    public const double Floor = 2;

    /// <summary>The key, shoulder to tip.</summary>
    public const double KeyLength = FirstPin + ((Pins - 1) * Spacing) + 3;

    /// <summary>Where pin <paramref name="i"/> stands, from the lock's face.</summary>
    public static double PinAt(int i) => FirstPin + (i * Spacing);

    /// <summary>How deep a cut of <paramref name="depth"/> (0–9) goes into the blade.</summary>
    public static double CutDepth(double depth) => FirstDepth + (depth * Step);

    /// <summary>
    /// The top of the key's blade at <paramref name="x"/> along it: each cut a V with a small flat at the bottom and
    /// 45° sides, the tip sloping down, nothing outside the blade.
    /// </summary>
    public static double KeyTop(IReadOnlyList<int> cuts, double x)
    {
        if (x < 0 || x > KeyLength) return double.NaN;

        var top = Blade;
        for (var i = 0; i < cuts.Count; i++)
        {
            top = Math.Min(top, Blade - CutDepth(cuts[i]) + Math.Max(0, Math.Abs(x - PinAt(i)) - 0.4));
        }

        var tipStart = KeyLength - 2.5;
        if (x > tipStart) top = Math.Min(top, Blade - ((x - tipStart) * 2));
        return Math.Max(Floor, top);
    }

    /// <summary>The key pin that sits a key cut to <paramref name="depth"/> exactly at the shear line.</summary>
    public static double KeyPinLength(int depth) => ShearLine - (Blade - CutDepth(depth));

    /// <summary>
    /// The top of key pin <paramref name="i"/> (of length <paramref name="pinLength"/>) with <paramref name="key"/>
    /// pushed <paramref name="insert"/> of the way in (0 out, 1 fully in). Pushed partway, the pin rides on whatever part
    /// of the blade is under it; with no blade under it, it rests on the floor.
    /// </summary>
    public static double KeyPinTop(IReadOnlyList<int> key, int i, double pinLength, double insert)
    {
        var underPin = PinAt(i) + ((1 - Math.Clamp(insert, 0, 1)) * KeyLength);
        var top = KeyTop(key, underPin);
        return (double.IsNaN(top) ? Floor : top) + pinLength;
    }

    /// <summary>How far each key pin's top is from the shear line (mm, + above), for a lock pinned to <paramref name="lockCuts"/>.</summary>
    public static double[] Gaps(IReadOnlyList<int> lockCuts, IReadOnlyList<int> key, double insert) =>
        Enumerable.Range(0, lockCuts.Count).Select(i => KeyPinTop(key, i, KeyPinLength(lockCuts[i]), insert) - ShearLine).ToArray();

    /// <summary>Whether the plug turns: every pin at the shear line.</summary>
    public static bool Turns(IReadOnlyList<int> lockCuts, IReadOnlyList<int> key, double insert) =>
        Gaps(lockCuts, key, insert).All(g => Math.Abs(g) <= Tolerance);
}

/// <summary>
/// Spread 2: how many different keys a lock design allows. A cut can take any of <c>depths</c> depths, but two
/// neighbouring cuts may differ by at most <c>steepest</c> steps, or the slope between them would be too steep for
/// the pins to ride over (the "maximum adjacent cut").
/// </summary>
public static class KeyCount
{
    /// <summary>Every combination, ignoring the rule about neighbours.</summary>
    public static double All(int cuts, int depths) => Math.Pow(depths, cuts);

    /// <summary>The keys that keep every pair of neighbouring cuts within <paramref name="steepest"/> steps.</summary>
    public static double Allowed(int cuts, int depths, int steepest)
    {
        // Count, cut by cut, the keys ending in each depth.
        var ways = Enumerable.Repeat(1.0, depths).ToArray();
        for (var c = 1; c < cuts; c++)
        {
            var next = new double[depths];
            for (var d = 0; d < depths; d++)
            {
                for (var e = Math.Max(0, d - steepest); e <= Math.Min(depths - 1, d + steepest); e++)
                {
                    next[d] += ways[e];
                }
            }

            ways = next;
        }

        return ways.Sum();
    }

    /// <summary>
    /// A sample key for the picture: a fixed pattern fitted to <paramref name="depths"/> and kept within
    /// <paramref name="steepest"/> of each neighbour.
    /// </summary>
    public static int[] Sample(int cuts, int depths, int steepest)
    {
        int[] pattern = [3, 8, 1, 6, 9, 2, 7];
        var key = new int[cuts];
        for (var i = 0; i < cuts; i++)
        {
            var want = (int)Math.Round(pattern[i % pattern.Length] * (depths - 1) / 9.0);
            key[i] = i == 0 ? want : Math.Clamp(want, key[i - 1] - steepest, key[i - 1] + steepest);
        }

        return key;
    }
}

/// <summary>
/// Spread 3: a lock with a master key. Where the lock's own key and the master key differ, the stack has a thin extra
/// pin, a master wafer, so it has two places it can split: one for each key. Any key that takes each cut from either
/// of the two opens it.
/// </summary>
public static class MasterKeyed
{
    /// <summary>The lock's own key, one cut a pin.</summary>
    public static readonly int[] Own = [2, 6, 3, 7, 4];

    /// <summary>The master key, which opens every door in the building.</summary>
    public static readonly int[] Master = [5, 6, 7, 2, 4];

    /// <summary>A neighbour's key: their own lock, the same master.</summary>
    public static readonly int[] Neighbour = [3, 6, 1, 7, 8];

    /// <summary>A key cut from both, never handed out to anyone: it opens this lock too.</summary>
    public static readonly int[] Mixed = [5, 6, 3, 2, 4];

    /// <summary>The keys the picture can try, in the slider's order.</summary>
    public static readonly IReadOnlyList<int[]> Keys = [Own, Master, Neighbour, Mixed];

    /// <summary>
    /// The key pin's length at pin <paramref name="i"/>: set for the shallower of the two cuts. The deeper cut lets it
    /// drop by the wafer's thickness, and the wafer's top then sits at the shear line instead.
    /// </summary>
    public static double KeyPin(int i) => PinTumbler.KeyPinLength(Math.Min(Own[i], Master[i]));

    /// <summary>The top of key pin <paramref name="i"/> with <paramref name="key"/> fully in.</summary>
    public static double KeyPinTop(IReadOnlyList<int> key, int i) => PinTumbler.Blade - PinTumbler.CutDepth(key[i]) + KeyPin(i);

    /// <summary>The master wafer's thickness at pin <paramref name="i"/> (0 where both keys agree).</summary>
    public static double Wafer(int i) => Math.Abs(Own[i] - Master[i]) * PinTumbler.Step;

    /// <summary>Whether pin <paramref name="i"/> lets <paramref name="key"/> through: one of its splits lands on the shear line.</summary>
    public static bool PinAllows(IReadOnlyList<int> key, int i) => key[i] == Own[i] || key[i] == Master[i];

    /// <summary>The first pin that blocks <paramref name="key"/>, or −1 when it opens the lock.</summary>
    public static int Blocking(IReadOnlyList<int> key)
    {
        for (var i = 0; i < PinTumbler.Pins; i++)
        {
            if (!PinAllows(key, i)) return i;
        }

        return -1;
    }

    /// <summary>How many different keys open this lock: two choices at every pin with a wafer.</summary>
    public static int KeysThatOpen() => 1 << Enumerable.Range(0, PinTumbler.Pins).Count(i => Own[i] != Master[i]);
}

/// <summary>
/// Spread 4: why the pins of a real lock bind one at a time. No two holes are drilled in exactly the same place; turn
/// the plug gently and it moves until the hole furthest out of line pinches its pin against the shell. Only that pin is
/// held; once it is lifted to the shear line, the plug turns a hair more and the next one binds.
/// </summary>
public static class Binding
{
    /// <summary>Each hole's error, mm across the plug: tiny, but never the same twice.</summary>
    public static readonly double[] Offsets = [0.012, 0.031, 0.004, 0.022, 0.017];

    /// <summary>The order the pins bind in, the furthest out of line first.</summary>
    public static int[] Order(IReadOnlyList<double> offsets) =>
        Enumerable.Range(0, offsets.Count).OrderByDescending(i => offsets[i]).ToArray();

    /// <summary>
    /// The pins holding the plug once <paramref name="set"/> pins are set: one pin when the holes differ, all the unset
    /// pins when they are perfectly in line.
    /// </summary>
    public static int[] Binds(IReadOnlyList<double> offsets, int set)
    {
        var order = Order(offsets);
        if (set >= order.Length) return [];

        var rest = order.Skip(set).ToArray();
        var most = offsets[rest[0]];
        return rest.Where(i => Math.Abs(offsets[i] - most) < 1e-6).ToArray();
    }
}

/// <summary>
/// Spread 5: a lever lock. Flat levers lie over the bolt, each with a slot (its gate) cut at its own height; a stump
/// on the bolt can slide only along the gates. Turning the key swings its bit up under the levers, lifting each by the
/// height of its step. Only while every gate is in line with the stump can the bit push the bolt across.
/// </summary>
public static class LeverLock
{
    /// <summary>Each lever's gate height, mm: how far the right key lifts it.</summary>
    public static readonly double[] Gates = [4.5, 2.0, 6.5];

    /// <summary>A key whose middle step is a little too short.</summary>
    public static readonly double[] WrongKey = [4.5, 3.0, 6.5];

    /// <summary>How close a gate must come to the stump for it to slip in, mm.</summary>
    public const double Tolerance = 0.4;

    /// <summary>The key's turn (degrees) where the bit starts pushing the bolt, and where it lets go.</summary>
    public const double PushFrom = 75;

    public const double PushTo = 105;

    /// <summary>How far a step lifts its lever at <paramref name="angle"/> degrees of turn: up and back down over half a turn.</summary>
    public static double Lift(double step, double angle) => step * Math.Sin(Math.Clamp(angle, 0, 180) * Math.PI / 180);

    /// <summary>Whether every gate is in line with the stump at <paramref name="angle"/>.</summary>
    public static bool Aligned(IReadOnlyList<double> key, double angle) =>
        key.Select((step, i) => Math.Abs(Lift(step, angle) - Gates[i])).All(d => d <= Tolerance);

    /// <summary>
    /// How far the bolt has gone across (0 locked, 1 open) after turning to <paramref name="angle"/>: it moves while
    /// the bit pushes it, but only as far as the gates have stayed in line.
    /// </summary>
    public static double Bolt(IReadOnlyList<double> key, double angle)
    {
        var travel = 0.0;
        for (var a = PushFrom; a <= Math.Min(angle, PushTo) + 1e-9; a += 0.5)
        {
            if (!Aligned(key, a)) break;
            travel = (a - PushFrom) / (PushTo - PushFrom);
        }

        return travel;
    }
}

/// <summary>
/// Spread 6: a combination lock of numbered wheels. Inside each wheel is a sleeve with a notch; a bar can drop into
/// the notches only when every wheel shows its number. Cheap wheels have wide notches, so neighbours of the right
/// number work too.
/// </summary>
public static class Dials
{
    /// <summary>Numbers on each wheel.</summary>
    public const int Numbers = 10;

    /// <summary>The wheels' code.</summary>
    public static readonly int[] Code = [7, 2, 5];

    /// <summary>How far wheel <paramref name="wheel"/> showing <paramref name="shown"/> is from its number, either way round.</summary>
    public static int Off(int shown, int wheel)
    {
        var d = Math.Abs(shown - Code[wheel]) % Numbers;
        return Math.Min(d, Numbers - d);
    }

    /// <summary>Whether wheel <paramref name="wheel"/> lets the bar in; a sloppy notch takes <paramref name="slop"/> either side.</summary>
    public static bool Lets(int shown, int wheel, int slop) => Off(shown, wheel) <= slop;

    /// <summary>The first wheel that blocks, or −1 when the bar drops and the lock opens.</summary>
    public static int Blocking(IReadOnlyList<int> shown, int slop)
    {
        for (var i = 0; i < Code.Length; i++)
        {
            if (!Lets(shown[i], i, slop)) return i;
        }

        return -1;
    }

    /// <summary>Settings a thief must try to be sure of opening it: every combination, or fewer when each try covers several.</summary>
    public static double Tries(int slop) => Math.Pow(Math.Ceiling(Numbers / (double)((2 * slop) + 1)), Code.Length);
}
