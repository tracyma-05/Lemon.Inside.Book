using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Tesseract;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Tesseract_</c>.</summary>
public abstract class TesseractSpread(string key) : Spread("Tesseract", key)
{
    protected static string Number(double value, string format = "0") => value.ToString(format, CultureInfo.CurrentCulture);

    protected static string Degrees(double value) => Text.Format("Tesseract_Degrees", Number(value));
}

/// <summary>1. Building by sliding: a point slid makes a line, a line a square, a square a cube, a cube a tesseract.</summary>
public sealed partial class BuildSpread() : TesseractSpread("Build")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DimensionText), nameof(ReadoutText), nameof(Directions))]
    private double _dimension = 4;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SlideText), nameof(ReadoutText))]
    private double _slide = 1;

    /// <summary>How many directions the shape being built has, 1 to 4.</summary>
    public int Directions => (int)Math.Round(Math.Clamp(Dimension, 1, 4));

    public string DimensionText => Text.GetString(Build.NameKey(Directions));

    public string SlideText => Text.Format("Tesseract_Percent", Number(100 * Slide));

    public string ReadoutText
    {
        get
        {
            var n = Directions;
            return Slide < 0.99
                ? Text.Format("Tesseract_Build_Sliding", Text.GetString(Build.NameKey(n - 1)), Text.GetString(Build.NameKey(n)))
                : Text.Format("Tesseract_Build_Readout", Text.GetString(Build.NameKey(n)), HyperCube.Corners(n), HyperCube.Edges(n),
                    Text.GetString(Build.NameKey(n - 1)));
        }
    }

    public override IReadOnlyList<string> Parts => ["original", "copy", "joins", "count"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "original", () => (Dimension, Slide) = (2, 1)),
        Say(2, "joins", () => (Dimension, Slide) = (3, 0.6)),
        TryIt(3, () => Directions == 4 && Slide >= 0.99, () => (Dimension, Slide) = (4, 1), highlight: "copy"),
        Say(4, "count"),
        Quiz(5, answer: 1, highlight: "count"),
    ];
}

/// <summary>2. Shadows lose a direction: a cube's shadow is flat, and changes shape as the cube turns.</summary>
public sealed partial class ShadowSpread() : TesseractSpread("Shadow")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TurnText), nameof(ReadoutText), nameof(ShadowArea))]
    private double _turn;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TiltText), nameof(ReadoutText), nameof(ShadowArea))]
    private double _tilt;

    public double ShadowArea => Shadow.Area(Turn, Tilt);

    public string TurnText => Degrees(Turn);

    public string TiltText => Degrees(Tilt);

    public string ReadoutText => Text.Format("Tesseract_Shadow_Readout", Shadow.Outline(Turn, Tilt).Count, Number(ShadowArea, "0.00"));

    public override IReadOnlyList<string> Parts => ["cube", "shadow", "sun", "floor"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "cube", () => (Turn, Tilt) = (0, 0)),
        Say(2, "shadow", () => (Turn, Tilt) = (20, 25)),
        TryIt(3, () => ShadowArea >= 1.7, () => (Turn, Tilt) = (45, 55), highlight: "shadow"),
        Say(4, "floor"),
        Quiz(5, answer: 1, highlight: "sun"),
    ];
}

/// <summary>3. A cube inside a cube: a tesseract seen in perspective along its fourth direction.</summary>
public sealed partial class NestedSpread() : TesseractSpread("Nested")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DistanceText), nameof(ReadoutText))]
    private double _distance = 2.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TurnText))]
    private double _turn = 30;

    public string DistanceText => Number(Distance, "0.0");

    public string TurnText => Degrees(Turn);

    public string ReadoutText => Text.Format("Tesseract_Nested_Readout", Text.Format("Tesseract_Percent", Number(100 * Look.InnerRatio(Distance))));

    public override IReadOnlyList<string> Parts => ["square", "outer", "inner", "between"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "square", () => (Distance, Turn) = (2.5, 30)),
        Say(2, "outer"),
        TryIt(3, () => Look.InnerRatio(Distance) < 0.5, () => Distance = 1.2, highlight: "inner"),
        Say(4, "between"),
        Quiz(5, answer: 2, highlight: "between"),
    ];
}

/// <summary>4. Turning in four directions: turns happen in planes, and a plane can stay still.</summary>
public sealed partial class SpinSpread() : TesseractSpread("Spin")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AngleText), nameof(ReadoutText))]
    private double _angle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _both;

    public string AngleText => Degrees(Angle);

    public string ReadoutText => Text.Format(Both ? "Tesseract_Spin_Both" : "Tesseract_Spin_Single", Number(Angle));

    public override IReadOnlyList<string> Parts => ["outer", "inner", "dial", "axes"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "outer", () => (Angle, Both) = (0, false)),
        Say(2, "inner", () => Angle = 40),
        TryIt(3, () => Math.Abs(Angle - 180) <= 10, () => Angle = 180, highlight: "dial"),
        Say(4, "axes"),
        Quiz(5, answer: 2, highlight: "axes"),
    ];
}

/// <summary>5. Slices: what we would see of a tesseract passing through our space.</summary>
public sealed partial class SliceSpread() : TesseractSpread("Slice")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DepthText), nameof(ReadoutText), nameof(SolidCorners))]
    private double _depth = 0.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(SolidCorners))]
    private bool _cornerFirst;

    public int SolidCorners => Slice.Points(4, Depth, CornerFirst).Count;

    public string DepthText => Text.Format("Tesseract_Percent", Number(100 * Depth));

    public string ReadoutText => Text.Format("Tesseract_Slice_Readout",
        Text.GetString(Slice.FlatKey(Slice.Points(3, Depth, CornerFirst).Count)), Text.GetString(Slice.SolidKey(SolidCorners)), SolidCorners);

    public override IReadOnlyList<string> Parts => ["flat", "solid", "gauge", "count"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "flat", () => (Depth, CornerFirst) = (0.5, false)),
        Say(2, "solid"),
        TryIt(3, () => CornerFirst && SolidCorners == 6, () => (CornerFirst, Depth) = (true, 0.5), highlight: "solid"),
        Say(4, "gauge", () => (CornerFirst, Depth) = (true, 0.15)),
        Quiz(5, answer: 1, highlight: "solid"),
    ];
}

/// <summary>6. Unfolding: a cube opens into a cross of squares, a tesseract into a cross of cubes.</summary>
public sealed partial class NetSpread() : TesseractSpread("Net")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FoldText))]
    private double _fold = 0.6;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChosenText), nameof(ReadoutText), nameof(Cube))]
    private double _chosen = 1;

    /// <summary>The chosen cube, 0 (A) to 7 (H).</summary>
    public int Cube => (int)Math.Round(Math.Clamp(Chosen, 0, Net.Count - 1));

    public string FoldText => Text.Format("Tesseract_Percent", Number(100 * Fold));

    public string ChosenText => Net.Letter(Cube);

    public string ReadoutText => Text.Format("Tesseract_Net_Readout", Net.Letter(Cube), Net.TouchingInCross(Cube).Count, Net.Touching(Cube).Count,
        Net.Letter(Net.Opposite(Cube)));

    public override IReadOnlyList<string> Parts => ["cross", "net", "chosen", "opposite"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "cross", () => (Fold, Chosen) = (0.6, 1)),
        Say(2, "net"),
        TryIt(3, () => Cube == 3, () => Chosen = 3, highlight: "net"),
        Say(4, "opposite"),
        Quiz(5, answer: 1, highlight: "chosen"),
    ];
}
