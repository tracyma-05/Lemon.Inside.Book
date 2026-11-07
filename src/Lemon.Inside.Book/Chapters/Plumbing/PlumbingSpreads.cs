using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Lemon.Book.Shell.Explore.Lessons;

namespace Lemon.Inside.Book.Chapters.Plumbing;

/// <summary>One spread of the chapter (see <see cref="Spread"/>); the strings start <c>Plumbing_</c>.</summary>
public abstract class PlumbingSpread(string key) : Spread("Plumbing", key)
{
    protected static string Number(double value, string format = "0") => value.ToString(format, CultureInfo.CurrentCulture);

    protected static string Metres(double value) => Text.Format("Plumbing_Metres", Number(value, "0.#"));

    protected static string Centimetres(double value) => Text.Format("Plumbing_Centimetres", Number(value, "0.#"));
}

/// <summary>1. The water tower: height is pressure.</summary>
public sealed partial class TowerSpread() : PlumbingSpread("Tower")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SurfaceText), nameof(ReadoutText), nameof(Head))]
    private double _surface = 30;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TapHeightText), nameof(ReadoutText), nameof(Head))]
    private double _tapHeight = 1;

    public double Head => Tower.Head(Surface, TapHeight);

    public string SurfaceText => Metres(Surface);

    public string TapHeightText => Metres(TapHeight);

    public string ReadoutText => Tower.Flows(Surface, TapHeight)
        ? Text.Format("Plumbing_Tower_Readout", Metres(Head), Text.Format("Plumbing_Bar", Number(Water.Pressure(Head), "0.0")),
            Text.Format("Plumbing_Speed", Number(Water.Speed(Head))))
        : Text.GetString("Plumbing_Tower_Dry");

    public override IReadOnlyList<string> Parts => ["tank", "pipe", "tap", "level"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "tank", () => (Surface, TapHeight) = (30, 1)),
        Say(2, "pipe"),
        TryIt(3, () => TapHeight >= 24, () => TapHeight = 24, highlight: "tap"),
        Say(4, "level", () => TapHeight = 30),
        Quiz(5, answer: 1, highlight: "tank"),
    ];
}

/// <summary>2. The tap: a screw lifts a washer off its seat.</summary>
public sealed partial class TapSpread() : PlumbingSpread("Tap")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TurnsText), nameof(ReadoutText), nameof(Flow))]
    private double _turns;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Flow))]
    private bool _worn;

    public double Flow => Tap.Flow(Turns, Worn);

    public string TurnsText => Text.Format("Plumbing_Turns", Number(Turns, "0.0#"));

    public string ReadoutText => Flow <= 0
        ? Text.GetString("Plumbing_Tap_Shut")
        : Tap.Opening(Turns) <= 0
            ? Text.GetString("Plumbing_Tap_Drips")
            : Text.Format("Plumbing_Tap_Readout", Number(Tap.Lift(Turns), "0.0#"),
                Text.Format("Plumbing_LitresPerMinute", Number(Flow, "0.#")));

    public override IReadOnlyList<string> Parts => ["handle", "spindle", "washer", "seat"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "seat", () => (Turns, Worn) = (0, false)),
        Say(2, "spindle", () => Turns = 0.5),
        TryIt(3, () => Tap.Opening(Turns) >= 1, () => Turns = 2, highlight: "handle"),
        Say(4, "washer", () => (Turns, Worn) = (0, true)),
        Quiz(5, answer: 2, highlight: "handle"),
    ];
}

/// <summary>3. The siphon: water climbing over a crest on its way down.</summary>
public sealed partial class SiphonSpread() : PlumbingSpread("Siphon")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Flows))]
    private bool _primed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutletText), nameof(ReadoutText), nameof(Flows))]
    private double _outlet = 0.5;

    public bool Flows => Siphon.Flows(Primed, Outlet);

    public string OutletText => Metres(Outlet);

    public string ReadoutText => !Primed
        ? Text.GetString("Plumbing_Siphon_Empty")
        : Flows
            ? Text.Format("Plumbing_Siphon_Readout", Metres(Siphon.Surface - Outlet),
                Text.Format("Plumbing_Speed", Number(Siphon.Speed(Primed, Outlet), "0.#")))
            : Text.GetString("Plumbing_Siphon_Stops");

    public override IReadOnlyList<string> Parts => ["tank", "hose", "crest", "outlet"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "tank", () => (Primed, Outlet) = (false, 0.5)),
        Say(2, "hose"),
        TryIt(3, () => Primed, () => Primed = true, highlight: "hose"),
        Say(4, "outlet", () => Outlet = 2.5),
        Quiz(5, answer: 1, highlight: "crest"),
    ];
}

/// <summary>4. The cistern: a float that turns its own water off.</summary>
public sealed partial class CisternSpread() : PlumbingSpread("Cistern")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SetText), nameof(ReadoutText))]
    private double _set = 28;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText))]
    private bool _flushing;

    public string SetText => Centimetres(Set);

    public string ReadoutText =>
        (Cistern.Spills(Set)
            ? Text.GetString("Plumbing_Cistern_Spills")
            : Text.Format("Plumbing_Cistern_Readout", Centimetres(Set), Centimetres(Cistern.Overflow - Set)))
        + (Flushing ? " " + Text.GetString("Plumbing_Cistern_Flushing") : string.Empty);

    public override IReadOnlyList<string> Parts => ["float", "valve", "overflow", "flush"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "float", () => (Set, Flushing) = (28, false)),
        Say(2, "valve"),
        TryIt(3, () => Flushing, () => Flushing = true, highlight: "flush"),
        Say(4, "overflow", () => (Flushing, Set) = (false, 37)),
        Quiz(5, answer: 0, highlight: "float"),
    ];
}

/// <summary>5. The trap: a plug of water between the room and the drain.</summary>
public sealed partial class TrapSpread() : PlumbingSpread("Trap")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SuctionText), nameof(ReadoutText), nameof(Broken))]
    private double _suction;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Broken))]
    private bool _vent;

    public bool Broken => Trap.Broken(Suction, Vent);

    public string SuctionText => Centimetres(Suction);

    public string ReadoutText => Broken
        ? Text.GetString("Plumbing_Trap_Broken")
        : Text.Format("Plumbing_Trap_Readout", Centimetres(Trap.Drop(Suction, Vent)), Centimetres(Trap.Left(Suction, Vent)));

    public override IReadOnlyList<string> Parts => ["seal", "sink", "drain", "vent"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "seal", () => (Suction, Vent) = (0, false)),
        Say(2, "drain", () => Suction = 3),
        TryIt(3, () => Broken, () => Suction = 6, highlight: "drain"),
        Say(4, "vent", () => Vent = true),
        Quiz(5, answer: 2, highlight: "seal"),
    ];
}

/// <summary>6. The hand pump: the air does the lifting, up to about ten metres.</summary>
public sealed partial class PumpSpread() : PlumbingSpread("Pump")
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DepthText), nameof(ReadoutText), nameof(Works))]
    private double _depth = 4;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadoutText), nameof(Works))]
    private bool _down;

    public bool Works => Pump.Works(Depth, Down);

    public string DepthText => Metres(Depth);

    public string ReadoutText => Down
        ? Text.Format("Plumbing_Pump_Pushes", Metres(Depth))
        : Works
            ? Text.Format("Plumbing_Pump_Readout", Metres(Depth))
            : Text.Format("Plumbing_Pump_Fails", Metres(Pump.Rise(Depth, Down)));

    public override IReadOnlyList<string> Parts => ["piston", "valves", "pipe", "water"];

    public override IReadOnlyList<LessonStep> Walkthrough() =>
    [
        Say(1, "piston", () => (Depth, Down) = (4, false)),
        Say(2, "water"),
        TryIt(3, () => Depth >= 11, () => Depth = 12, highlight: "pipe"),
        Say(4, "piston", () => Down = true),
        Quiz(5, answer: 0, highlight: "water"),
    ];
}
