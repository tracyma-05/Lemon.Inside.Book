using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Bridges;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Bridges_</c>.</summary>
public abstract class BridgesSpread(string key) : Spread("Bridges", key)
{
    protected static string Number(double value, string format = "0") => value.ToString(format, CultureInfo.CurrentCulture);

    protected static string Metres(double value) => Text.Format("Bridges_Metres", Number(value, "0.#"));

    protected static string Centimetres(double value) => Text.Format("Bridges_Centimetres", Number(value, Math.Abs(value) < 10 ? "0.#" : "0"));

    /// <summary>A force said as the weight it would hold up: "20 t".</summary>
    protected static string Tonnes(double value) => Text.Format("Bridges_Tonnes", Number(value, value < 10 ? "0.#" : "N0"));
}

/// <summary>1. The beam: longer sags much more, deeper sags much less.</summary>
public sealed partial class BeamSpread() : BridgesSpread("Beam")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpanText), nameof(ReadoutText), nameof(Sag))]
    private double _span = 6;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DepthText), nameof(ReadoutText), nameof(Sag))]
    private double _depth = 0.4;

    /// <summary>How far the middle sags, cm.</summary>
    public double Sag => Beam.Sag(Span, Depth) * 100;

    public string SpanText => Metres(Span);

    public string DepthText => Centimetres(Depth * 100);

    public string ReadoutText => Text.Format("Bridges_Beam_Readout", Centimetres(Sag));

    public override IReadOnlyList<string> Parts => ["beam", "load", "top", "banks"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "load", () => (Span, Depth) = (6, 0.4)),
        Say(2, "top"),
        TryIt(3, () => Span >= 12, () => Span = 12, highlight: "banks"),
        Say(4, "beam", () => Depth = 0.8),
        Quiz(5, answer: 0, highlight: "beam"),
    ];
}

/// <summary>2. Triangles: a square of pinned bars folds; a brace makes two triangles that cannot.</summary>
public sealed partial class FrameSpread() : BridgesSpread("Frame")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PushText), nameof(ReadoutText), nameof(Lean))]
    private double _push;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Lean))]
    private bool _brace;

    public double Lean => Frame.Lean(Push, Brace);

    public string PushText => Tonnes(Push);

    public string ReadoutText => Brace
        ? Text.Format("Bridges_Frame_Braced", Tonnes(Frame.BraceForce(Push)))
        : Push <= 0.001
            ? Text.GetString("Bridges_Frame_Still")
            : Text.Format("Bridges_Frame_Leans", Number(Lean));

    public override IReadOnlyList<string> Parts => ["bars", "pins", "brace", "hand"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "pins", () => (Push, Brace) = (0, false)),
        Say(2, "hand", () => Push = 1),
        TryIt(3, () => Brace, () => Brace = true, highlight: "brace"),
        Say(4, "brace", () => Push = Frame.MostPush),
        Quiz(5, answer: 1, highlight: "bars"),
    ];
}

/// <summary>3. The truss: the top squeezed, the bottom stretched, the diagonals carrying the load across.</summary>
public sealed partial class TrussSpread() : BridgesSpread("Truss")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeightText), nameof(ReadoutText), nameof(Squeeze))]
    private double _height = 2;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PositionText), nameof(ReadoutText), nameof(Squeeze))]
    private double _position = 0.5;

    /// <summary>The biggest squeeze in the top chord, tonnes.</summary>
    public double Squeeze => Truss.Chords(Height, Position).Squeeze;

    public string HeightText => Metres(Height);

    public string PositionText => Metres(Position * Truss.Span);

    public string ReadoutText
    {
        get
        {
            var (squeeze, stretch) = Truss.Chords(Height, Position);
            return Text.Format("Bridges_Truss_Readout", Tonnes(squeeze), Tonnes(stretch));
        }
    }

    public override IReadOnlyList<string> Parts => ["load", "top", "bottom", "diagonals"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "load", () => (Height, Position) = (2, 0.5)),
        Say(2, "top"),
        Say(3, "bottom"),
        TryIt(4, () => Squeeze <= 10.01, () => Height = 4, highlight: "top"),
        Quiz(5, answer: 2, highlight: "diagonals"),
    ];
}

/// <summary>4. The arch: stones squeezed together, pushing out on the banks.</summary>
public sealed partial class ArchSpread() : BridgesSpread("Arch")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RiseText), nameof(ReadoutText), nameof(Thrust))]
    private double _rise = 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _abutments = true;

    public double Thrust => Arch.Thrust(Rise);

    public string RiseText => Metres(Rise);

    public string ReadoutText => Abutments
        ? Text.Format("Bridges_Arch_Readout", Tonnes(Thrust), Tonnes(Arch.Weight / 2))
        : Text.GetString("Bridges_Arch_Falls");

    public override IReadOnlyList<string> Parts => ["stones", "keystone", "abutments", "road"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "stones", () => (Rise, Abutments) = (3, true)),
        Say(2, "keystone"),
        TryIt(3, () => Thrust >= 80, () => Rise = 1.5, highlight: "abutments"),
        Say(4, "abutments", () => Abutments = false),
        Quiz(5, answer: 1, highlight: "stones"),
    ];
}

/// <summary>5. The suspension cable: the flatter it hangs, the harder it pulls.</summary>
public sealed partial class CableSpread() : BridgesSpread("Cable")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SagText), nameof(ReadoutText), nameof(Pull))]
    private double _sag = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _anchored = true;

    public double Pull => Cable.Pull(Sag);

    public string SagText => Metres(Sag);

    public string ReadoutText => Anchored
        ? Text.Format("Bridges_Cable_Readout", Tonnes(Pull), Tonnes(Cable.AtTower(Sag)))
        : Text.Format("Bridges_Cable_Loose", Tonnes(Pull));

    public override IReadOnlyList<string> Parts => ["cable", "towers", "hangers", "anchors"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "cable", () => (Sag, Anchored) = (20, true)),
        Say(2, "hangers"),
        TryIt(3, () => Sag <= 8, () => Sag = 8, highlight: "cable"),
        Say(4, "anchors", () => Anchored = false),
        Quiz(5, answer: 2, highlight: "cable"),
    ];
}

/// <summary>6. The expansion joint: room for the deck to grow and shrink.</summary>
public sealed partial class ExpansionSpread() : BridgesSpread("Expansion")
{
    /// <summary>The joint's gap on a mild day, cm.</summary>
    public const double MildGap = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemperatureText), nameof(ReadoutText), nameof(Change))]
    private double _temperature = Deck.Mild;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _joint = true;

    /// <summary>How much longer than on a mild day, cm.</summary>
    public double Change => Deck.Change(Temperature);

    public string TemperatureText => Text.Format("Bridges_Degrees", Number(Temperature));

    public string ReadoutText => !Joint
        ? Text.Format("Bridges_Expansion_Held", Tonnes(Deck.Push(Temperature)))
        : Text.Format(Change >= 0 ? "Bridges_Expansion_Longer" : "Bridges_Expansion_Shorter", Centimetres(Math.Abs(Change)),
            Centimetres(MildGap - Change));

    public override IReadOnlyList<string> Parts => ["deck", "joint", "bearing", "abutment"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "deck", () => (Temperature, Joint) = (Deck.Mild, true)),
        Say(2, "joint", () => Temperature = 40),
        TryIt(3, () => Temperature <= -10, () => Temperature = -10, highlight: "joint"),
        Say(4, "abutment", () => (Joint, Temperature) = (false, 40)),
        Quiz(5, answer: 0, highlight: "deck"),
    ];
}
