namespace Lemon.Inside.Book.Chapters.Colour;

/// <summary>
/// A colour as three amounts, red, green and blue, each 0 to 1. Whether they are amounts of light or the numbers a file
/// stores (as a share of 255) is said where each is used.
/// </summary>
public readonly record struct Rgb(double R, double G, double B)
{
    public static readonly Rgb Black = new(0, 0, 0);
    public static readonly Rgb White = new(1, 1, 1);

    public static Rgb Grey(double value) => new(value, value, value);

    public static Rgb operator *(Rgb c, double k) => new(c.R * k, c.G * k, c.B * k);

    public static Rgb operator +(Rgb a, Rgb b) => new(a.R + b.R, a.G + b.G, a.B + b.B);

    /// <summary>Part-way from <paramref name="a"/> (t = 0) to <paramref name="b"/> (t = 1).</summary>
    public static Rgb Lerp(Rgb a, Rgb b, double t) => (a * (1 - t)) + (b * t);

    public Rgb Clamped => new(Math.Clamp(R, 0, 1), Math.Clamp(G, 0, 1), Math.Clamp(B, 0, 1));

    /// <summary>
    /// Only the three amounts: the generated one would also print <see cref="Clamped"/>, itself an <see cref="Rgb"/>,
    /// and so on without end.
    /// </summary>
    public override string ToString() => FormattableString.Invariant($"({R:0.###}, {G:0.###}, {B:0.###})");

    public double Max => Math.Max(R, Math.Max(G, B));

    public double Min => Math.Min(R, Math.Min(G, B));

    /// <summary>The biggest difference in any of the three, 0 to 1.</summary>
    public double Distance(Rgb other) => Math.Max(Math.Abs(R - other.R), Math.Max(Math.Abs(G - other.G), Math.Abs(B - other.B)));

    /// <summary>The colour as a screen shows these numbers (each a share of 255).</summary>
    public Avalonia.Media.Color ToColor(double opacity = 1) => Avalonia.Media.Color.FromArgb(
        (byte)Math.Round(255 * Math.Clamp(opacity, 0, 1)), Byte(R), Byte(G), Byte(B));

    private static byte Byte(double v) => (byte)Math.Round(255 * Math.Clamp(v, 0, 1));
}

/// <summary>Plain names for colours, for the readouts: a key <c>Colour_Name_…</c>.</summary>
public static class ColourNames
{
    public static string Key(Rgb colour)
    {
        var c = colour.Clamped;
        var max = c.Max;
        var span = max - c.Min;
        if (max < 0.1) return "Colour_Name_Black";
        if (span < 0.15 * max) return max > 0.8 ? "Colour_Name_White" : "Colour_Name_Grey";

        // The hue, degrees round the colour wheel: 0 red, 120 green, 240 blue.
        double hue;
        if (max == c.R) hue = 60 * ((c.G - c.B) / span);
        else if (max == c.G) hue = 60 * (2 + ((c.B - c.R) / span));
        else hue = 60 * (4 + ((c.R - c.G) / span));
        hue = (hue + 360) % 360;

        var dark = max < 0.6;
        return hue switch
        {
            < 15 => "Colour_Name_Red",
            < 45 => dark ? "Colour_Name_Brown" : "Colour_Name_Orange",
            < 70 => dark ? "Colour_Name_Olive" : "Colour_Name_Yellow",
            < 160 => "Colour_Name_Green",
            < 200 => "Colour_Name_Cyan",
            < 255 => "Colour_Name_Blue",
            < 290 => "Colour_Name_Purple",
            < 340 => "Colour_Name_Magenta",
            _ => "Colour_Name_Red",
        };
    }

    public static readonly string[] All =
    [
        "Colour_Name_Black", "Colour_Name_White", "Colour_Name_Grey", "Colour_Name_Red", "Colour_Name_Brown", "Colour_Name_Orange",
        "Colour_Name_Olive", "Colour_Name_Yellow", "Colour_Name_Green", "Colour_Name_Cyan", "Colour_Name_Blue", "Colour_Name_Purple",
        "Colour_Name_Magenta",
    ];
}

/// <summary>1. Three lamps, red, green and blue, each turned up from 0 to 1: their light adds up.</summary>
public static class Lamps
{
    /// <summary>How many brightnesses a screen stores for each lamp (0 to 255).</summary>
    public const int Steps = 256;

    /// <summary>Every mix of the three lamps a screen can store.</summary>
    public static double Colours => Math.Pow(Steps, 3);

    public static Rgb Mix(double red, double green, double blue) => new Rgb(red, green, blue).Clamped;

    /// <summary>A brightness as the number a file stores, 0 to 255.</summary>
    public static int Number(double share) => (int)Math.Round(255 * Math.Clamp(share, 0, 1));
}

/// <summary>
/// 2. Inks on white paper, each 0 (none) to 1 (a full layer). Each takes away one of the three lights: cyan red, magenta
/// green, yellow blue. Real inks let a little of the light they should take slip through.
/// </summary>
public static class Inks
{
    /// <summary>The share of red, green and blue that full real cyan, magenta and yellow still let through.</summary>
    public static readonly Rgb Leak = new(0.18, 0.12, 0.06);

    /// <summary>The light the paper sends back, 0 to 1 of each.</summary>
    public static Rgb Back(double cyan, double magenta, double yellow, bool real)
    {
        var leak = real ? Leak : Rgb.Black;
        return new Rgb(
            1 - (Math.Clamp(cyan, 0, 1) * (1 - leak.R)),
            1 - (Math.Clamp(magenta, 0, 1) * (1 - leak.G)),
            1 - (Math.Clamp(yellow, 0, 1) * (1 - leak.B)));
    }

    /// <summary>The share of all the light that comes back.</summary>
    public static double Brightness(Rgb light) => (light.R + light.G + light.B) / 3;
}

/// <summary>
/// 3. Numbers and light: the agreed rule (sRGB) a screen uses to turn a stored number, 0 to 255, into light, 0 (none) to
/// 1 (white), and how bright that light looks (lightness, 0 to 1, close to the eye's own scale).
/// </summary>
public static class Numbers
{
    public static double Light(double number)
    {
        var v = Math.Clamp(number, 0, 255) / 255;
        return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
    }

    public static double Number(double light)
    {
        var l = Math.Clamp(light, 0, 1);
        var v = l <= 0.0031308 ? l * 12.92 : (1.055 * Math.Pow(l, 1 / 2.4)) - 0.055;
        return 255 * v;
    }

    /// <summary>How bright light looks, 0 black to 1 white (CIE lightness over 100).</summary>
    public static double Looks(double light)
    {
        var l = Math.Clamp(light, 0, 1);
        return l <= 216.0 / 24389 ? l * 24389 / 27 / 100 : ((116 * Math.Cbrt(l)) - 16) / 100;
    }

    /// <summary>
    /// How many of the 256 numbers are left for the darkest tenth of the way from black to white (as it looks): with the
    /// numbers spaced for the eye, as files are, or spaced evenly in light.
    /// </summary>
    public static int DarkNumbers(bool spacedForEye) =>
        Enumerable.Range(0, 256).Count(n => Looks(spacedForEye ? Light(n) : n / 255.0) < 0.1);
}

/// <summary>4. Mixing two stored numbers, 0 to 255: by averaging the numbers, or as the light they stand for.</summary>
public static class Blend
{
    public static double Mix(double a, double b, double t, bool inLight) => inLight
        ? Numbers.Number(Numbers.Light(a) + ((Numbers.Light(b) - Numbers.Light(a)) * t))
        : a + ((b - a) * t);

    /// <summary>Two colours of stored numbers (shares of 255) mixed channel by channel.</summary>
    public static Rgb Mix(Rgb a, Rgb b, double t, bool inLight) => new(
        Mix(a.R * 255, b.R * 255, t, inLight) / 255, Mix(a.G * 255, b.G * 255, t, inLight) / 255, Mix(a.B * 255, b.B * 255, t, inLight) / 255);

    /// <summary>The grey that matches fine black and white stripes: half of white's light.</summary>
    public static double StripesMatch => Numbers.Number(0.5);

    public static readonly Rgb From = new(1, 0, 0);
    public static readonly Rgb To = new(0, 1, 0);
}

/// <summary>
/// 5. See-through panes: each lets 1 − opacity of what is behind through and adds opacity of its own colour. Colours as
/// stored numbers, as programs layer them.
/// </summary>
public static class Panes
{
    public static readonly Rgb Red = new(0.9, 0.2, 0.2);
    public static readonly Rgb Blue = new(0.2, 0.4, 0.95);

    public static Rgb Over(Rgb front, double opacity, Rgb behind) => Rgb.Lerp(behind, front, Math.Clamp(opacity, 0, 1));

    /// <summary>The share of the background two stacked panes hide (the same whichever is in front).</summary>
    public static double Hidden(double first, double second) => 1 - ((1 - Math.Clamp(first, 0, 1)) * (1 - Math.Clamp(second, 0, 1)));

    /// <summary>What the overlap shows over <paramref name="behind"/>, with the blue pane in front or the red.</summary>
    public static Rgb Overlap(Rgb behind, double red, double blue, bool blueOnTop) => blueOnTop
        ? Over(Blue, blue, Over(Red, red, behind))
        : Over(Red, red, Over(Blue, blue, behind));
}

/// <summary>
/// 6. A red sticker cut out of a picture: its pixels are either fully red or fully see-through, and a see-through pixel
/// still stores a colour (<c>hidden</c>). Made bigger, neighbouring pixels are mixed, colour and opacity alike. Kept
/// "premultiplied", each colour is stored already multiplied by its opacity, so a see-through pixel holds none.
/// </summary>
public static class Halo
{
    public static readonly Rgb Sticker = new(0.9, 0.15, 0.1);

    /// <summary>A stored pixel: its colour (premultiplied or not) and opacity.</summary>
    public readonly record struct Pixel(Rgb Colour, double Opacity)
    {
        public static Pixel Lerp(Pixel a, Pixel b, double t) => new(Rgb.Lerp(a.Colour, b.Colour, t), a.Opacity + ((b.Opacity - a.Opacity) * t));
    }

    public static Pixel Stored(bool inside, Rgb hidden, bool premultiplied) => inside
        ? new Pixel(Sticker, 1)
        : new Pixel(premultiplied ? Rgb.Black : hidden, 0);

    /// <summary>A (mixed) pixel laid on the background.</summary>
    public static Rgb Shown(Pixel pixel, Rgb background, bool premultiplied) => premultiplied
        ? pixel.Colour + (background * (1 - pixel.Opacity))
        : Panes.Over(pixel.Colour, pixel.Opacity, background);

    /// <summary>A pixel <paramref name="t"/> of the way from inside the sticker to outside it, as shown.</summary>
    public static Rgb Edge(double t, Rgb hidden, Rgb background, bool premultiplied) => Shown(
        Pixel.Lerp(Stored(true, hidden, premultiplied), Stored(false, hidden, premultiplied), t), background, premultiplied);

    /// <summary>What that pixel should look like: the sticker fading straight into the background.</summary>
    public static Rgb Clean(double t, Rgb background) => Rgb.Lerp(Sticker, background, t);

    /// <summary>How far the half-way edge pixel is off, 0 to 1: the ring round the cut-out.</summary>
    public static double Fringe(Rgb hidden, Rgb background, bool premultiplied) =>
        Edge(0.5, hidden, background, premultiplied).Distance(Clean(0.5, background));

    /// <summary>-1 a dark ring, 1 a light ring, 0 none to speak of.</summary>
    public static int Verdict(Rgb hidden, Rgb background, bool premultiplied)
    {
        if (Fringe(hidden, background, premultiplied) < 0.03) return 0;

        var edge = Edge(0.5, hidden, background, premultiplied);
        var clean = Clean(0.5, background);
        return edge.R + edge.G + edge.B > clean.R + clean.G + clean.B ? 1 : -1;
    }

    /// <summary>The sticker: a disc in a grid of <see cref="Size"/> × <see cref="Size"/> pixels.</summary>
    public const int Size = 8;

    public static bool Inside(int column, int row)
    {
        var dx = column - 3.5;
        var dy = row - 3.5;
        return (dx * dx) + (dy * dy) <= 3.3 * 3.3;
    }

    /// <summary>
    /// The sticker made <paramref name="scale"/> times bigger: the pixel at (<paramref name="u"/>, <paramref name="v"/>)
    /// mixes the four stored pixels nearest it.
    /// </summary>
    public static Pixel Enlarged(int u, int v, int scale, Rgb hidden, bool premultiplied)
    {
        var x = ((u + 0.5) / scale) - 0.5;
        var y = ((v + 0.5) / scale) - 0.5;
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var fx = x - x0;
        var fy = y - y0;
        Pixel At(int c, int r) => Stored(c is >= 0 and < Size && r is >= 0 and < Size && Inside(c, r), hidden, premultiplied);
        var top = Pixel.Lerp(At(x0, y0), At(x0 + 1, y0), fx);
        var bottom = Pixel.Lerp(At(x0, y0 + 1), At(x0 + 1, y0 + 1), fx);
        return Pixel.Lerp(top, bottom, fy);
    }
}
