using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Colour;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Colour_</c>.</summary>
public abstract class ColourSpread(string key) : Spread("Colour", key)
{
    protected static string Number(double value, string format = "0") => value.ToString(format, CultureInfo.CurrentCulture);

    protected static string Percent(double share) => Text.Format("Colour_Percent", Number(100 * share));

    protected static string Name(Rgb colour) => Text.GetString(ColourNames.Key(colour));
}

/// <summary>1. Three lamps, red, green and blue: their light adds up, and the eye sees the sum as one colour.</summary>
public sealed partial class LampsSpread() : ColourSpread("Lamps")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RedText), nameof(ReadoutText), nameof(Mix))]
    private double _red = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GreenText), nameof(ReadoutText), nameof(Mix))]
    private double _green = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BlueText), nameof(ReadoutText), nameof(Mix))]
    private double _blue = 1;

    public Rgb Mix => Lamps.Mix(Red, Green, Blue);

    public string RedText => Percent(Red);

    public string GreenText => Percent(Green);

    public string BlueText => Percent(Blue);

    public string ReadoutText => Text.Format("Colour_Lamps_Readout", Name(Mix), Lamps.Number(Red), Lamps.Number(Green), Lamps.Number(Blue));

    public override IReadOnlyList<string> Parts => ["lamps", "overlap", "pixel", "mix"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "lamps", () => (Red, Green, Blue) = (1, 1, 1)),
        Say(2, "overlap"),
        TryIt(3, () => ColourNames.Key(Mix) == "Colour_Name_Orange", () => (Red, Green, Blue) = (1, 0.5, 0), highlight: "mix"),
        Say(4, "pixel"),
        Quiz(5, answer: 1, highlight: "overlap"),
    ];
}

/// <summary>2. Inks: each takes one of the three lights away from what the white paper sends back.</summary>
public sealed partial class InksSpread() : ColourSpread("Inks")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CyanText), nameof(ReadoutText))]
    private double _cyan = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MagentaText), nameof(ReadoutText))]
    private double _magenta = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(YellowText), nameof(ReadoutText))]
    private double _yellow = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _real;

    public string CyanText => Percent(Cyan);

    public string MagentaText => Percent(Magenta);

    public string YellowText => Percent(Yellow);

    /// <summary>What comes back from where all three inks overlap.</summary>
    public Rgb Middle => Inks.Back(Cyan, Magenta, Yellow, Real);

    public string ReadoutText => Text.Format("Colour_Inks_Readout", Percent(Inks.Brightness(Middle)), Name(Middle));

    public override IReadOnlyList<string> Parts => ["paper", "ink", "middle", "light"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "paper", () => (Cyan, Magenta, Yellow, Real) = (1, 1, 1, false)),
        Say(2, "ink"),
        TryIt(3, () => Real, () => Real = true, highlight: "middle"),
        Say(4, "light"),
        Quiz(5, answer: 0, highlight: "ink"),
    ];
}

/// <summary>3. Numbers and light: a stored number of 128 gives out only about a fifth of white's light.</summary>
public sealed partial class StepsSpread() : ColourSpread("Steps")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NumberText), nameof(ReadoutText))]
    private double _value = 128;

    public string NumberText => Number(Math.Round(Value));

    public string ReadoutText
    {
        get
        {
            var light = Numbers.Light(Math.Round(Value));
            return Text.Format("Colour_Steps_Readout", Number(Math.Round(Value)), Percent(light), Percent(Numbers.Looks(light)));
        }
    }

    public override IReadOnlyList<string> Parts => ["numbers", "light", "marker", "meter"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "numbers", () => Value = 128),
        Say(2, "light"),
        TryIt(3, () => Math.Abs(Numbers.Light(Math.Round(Value)) - 0.5) < 0.03, () => Value = 188, highlight: "marker"),
        Say(4, "meter"),
        Quiz(5, answer: 2, highlight: "numbers"),
    ];
}

/// <summary>4. Mixing: averaging the stored numbers comes out too dark; mixing the light they stand for does not.</summary>
public sealed partial class BlendSpread() : ColourSpread("Blend")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GreyText), nameof(ReadoutText))]
    private double _grey = 128;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PositionText), nameof(ReadoutText))]
    private double _position = 0.5;

    public string GreyText => Number(Math.Round(Grey));

    public string PositionText => Percent(Position);

    /// <summary>Is the patch within a few numbers of the stripes' grey?</summary>
    public bool Matches => Math.Abs(Math.Round(Grey) - Blend.StripesMatch) <= 6;

    public string ReadoutText
    {
        get
        {
            var patch = Text.Format(Matches ? "Colour_Blend_Match" : Grey < Blend.StripesMatch ? "Colour_Blend_Darker" : "Colour_Blend_Lighter",
                Percent(Numbers.Light(Math.Round(Grey))));
            var mix = Text.Format("Colour_Blend_MixReadout", Name(Blend.Mix(Blend.From, Blend.To, Position, false)),
                Name(Blend.Mix(Blend.From, Blend.To, Position, true)));
            return $"{patch} {mix}";
        }
    }

    public override IReadOnlyList<string> Parts => ["stripes", "patch", "numbers", "light"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "stripes", () => (Grey, Position) = (128, 0.5)),
        Say(2, "patch"),
        TryIt(3, () => Matches, () => Grey = 188, highlight: "patch"),
        Say(4, "light"),
        Quiz(5, answer: 1, highlight: "numbers"),
    ];
}

/// <summary>5. See-through panes: each lets a share of what is behind through; two hide more, but never all.</summary>
public sealed partial class PanesSpread() : ColourSpread("Panes")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RedText), nameof(ReadoutText))]
    private double _red = 0.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BlueText), nameof(ReadoutText))]
    private double _blue = 0.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _blueOnTop = true;

    public string RedText => Percent(Red);

    public string BlueText => Percent(Blue);

    public string ReadoutText
    {
        get
        {
            var hidden = Panes.Hidden(Red, Blue);
            var front = Text.GetString(BlueOnTop ? "Colour_Panes_BlueFront" : "Colour_Panes_RedFront");
            return $"{Text.Format("Colour_Panes_Readout", Percent(hidden), Percent(1 - hidden))} {front}";
        }
    }

    public override IReadOnlyList<string> Parts => ["behind", "red", "blue", "overlap"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "behind", () => (Red, Blue, BlueOnTop) = (0.5, 0.5, true)),
        Say(2, "red"),
        TryIt(3, () => !BlueOnTop, () => BlueOnTop = false, highlight: "overlap"),
        Say(4, "overlap"),
        Quiz(5, answer: 1, highlight: "overlap"),
    ];
}

/// <summary>6. The ring round a cut-out: the colour hidden in see-through pixels leaks into the edge when it is mixed.</summary>
public sealed partial class HaloSpread() : ColourSpread("Halo")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HiddenText), nameof(ReadoutText))]
    private double _hidden = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackdropText), nameof(ReadoutText))]
    private double _backdrop = 0.15;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _premultiplied;

    public string HiddenText => Name(Rgb.Grey(Hidden));

    public string BackdropText => Name(Rgb.Grey(Backdrop));

    public double Fringe => Halo.Fringe(Rgb.Grey(Hidden), Rgb.Grey(Backdrop), Premultiplied);

    public string ReadoutText => Text.Format(Halo.Verdict(Rgb.Grey(Hidden), Rgb.Grey(Backdrop), Premultiplied) switch
    {
        1 => "Colour_Halo_Light",
        -1 => "Colour_Halo_Dark",
        _ => "Colour_Halo_Clean",
    }, Percent(Fringe));

    public override IReadOnlyList<string> Parts => ["small", "invisible", "edge", "switch"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "small", () => (Hidden, Backdrop, Premultiplied) = (1, 0.15, false)),
        Say(2, "edge"),
        TryIt(3, () => Premultiplied, () => Premultiplied = true, highlight: "switch"),
        Say(4, "invisible"),
        Quiz(5, answer: 2, highlight: "edge"),
    ];
}
