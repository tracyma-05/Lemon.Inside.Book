using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Lifting;

/// <summary>
/// Spread 5: a bottle jack cut open. Left to right: the oil tank, the narrow pump with its handle, and the wide ram
/// with the car's weight on it; a ball valve lets oil from the tank into the pump, another from the pump to the ram.
/// The arrows on the two pistons are the forces the same oil pressure gives them; the ram's height grows with the
/// strokes. The reader taps the hand to pump once, taps the tank to open the release valve (the oil runs back and the
/// ram sinks) and drags the ram's side for a wider or narrower ram. Each stroke is played: the handle goes down and
/// the pump piston with it, the outlet ball lifts and the ram creeps up; on the way back the inlet ball lifts instead
/// and the pump fills from the tank.
/// </summary>
public sealed class JackDiagram : GearPicture
{
    public static readonly StyledProperty<double> RamProperty =
        AvaloniaProperty.Register<JackDiagram, double>(nameof(Ram), 4, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> StrokesProperty =
        AvaloniaProperty.Register<JackDiagram, int>(nameof(Strokes), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The highest the ram is drawn, mm (forty strokes of the narrowest ram's rise).</summary>
    private const double MostRise = JackSpread.MostStrokes * Jack.Stroke / 4;

    /// <summary>How far the handle turns on a full stroke, radians.</summary>
    private const double Swing = 0.4;

    /// <summary>The strokes as played so far (the part after the point is the stroke in hand); the ram's width as drawn.</summary>
    private double _strokes = double.NaN;
    private double _ram = double.NaN;
    private bool _releasing;

    static JackDiagram()
    {
        AffectsRender<JackDiagram>(RamProperty, StrokesProperty);
    }

    /// <summary>How many times as wide the ram is as the pump.</summary>
    public double Ram
    {
        get => GetValue(RamProperty);
        set => SetValue(RamProperty, value);
    }

    public int Strokes
    {
        get => GetValue(StrokesProperty);
        set => SetValue(StrokesProperty, value);
    }

    private int SetStrokes => Math.Clamp(Strokes, 0, JackSpread.MostStrokes);

    protected override void Advance(double seconds)
    {
        _ram = double.IsNaN(_ram) ? Math.Clamp(Ram, 2, 10) : Glide(_ram, Math.Clamp(Ram, 2, 10), seconds, 0.08);
        if (double.IsNaN(_strokes))
        {
            _strokes = SetStrokes;
            return;
        }

        // Pumping plays each stroke (faster when many are owed); letting oil back sinks the ram without pumping.
        var owed = SetStrokes - _strokes;
        _releasing = owed < 0;
        _strokes = LiftingParts.Ease(_strokes, SetStrokes, _releasing ? Math.Max(6, -owed * 2) : Math.Max(1.6, owed * 0.8), seconds);
    }

    /// <summary>
    /// A stroke in hand, <paramref name="phase"/> 0–1: how far the pump piston is down (0 up, 1 at the bottom), and
    /// which valve is open: the outlet while it goes down (oil to the ram), the inlet while it comes back up (oil from
    /// the tank). The ram rises only on the way down.
    /// </summary>
    internal static (double Depth, bool Outlet, bool Inlet, double RamShare) Stroke(double phase)
    {
        if (phase <= 0 || phase >= 1) return (0, false, false, phase >= 1 ? 1 : 0);
        return phase < 0.5 ? (phase * 2, true, false, phase * 2) : ((1 - phase) * 2, false, true, 1);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var ram = double.IsNaN(_ram) ? Math.Clamp(Ram, 2, 10) : _ram;
        var played = double.IsNaN(_strokes) ? SetStrokes : _strokes;
        var whole = Math.Floor(played + 1e-9);
        var (depth, outletOpen, inletOpen, share) = _releasing ? (0, false, false, played - whole) : Stroke(played - whole);
        var rise = Jack.RisePerStroke(ram) * (whole + share);
        var w = bounds.Width;
        var h = bounds.Height;
        var floor = bounds.Bottom - 24;
        var baseTop = floor - (h * 0.1);
        var cylinderTop = floor - (h * 0.5);
        var pumpW = w * 0.032;
        var ramW = Math.Min(pumpW * ram, w * 0.34);
        var tankL = bounds.X + (w * 0.08);
        var pumpX = bounds.X + (w * 0.3);
        var ramX = bounds.X + (w * 0.62);

        // The body: tank, pump bore, ram bore, and the passage along the base joining them.
        var tank = new Rect(tankL, cylinderTop + (h * 0.12), w * 0.12, baseTop - cylinderTop - (h * 0.12));
        context.DrawRectangle(DarkSteelBrush, Edge, new Rect(tankL - 8, cylinderTop - 10, ramX + (ramW / 2) + 14 - tankL + 8, floor - cylinderTop + 10), 8, 8);
        context.FillRectangle(Dark, new Rect(tank.X, tank.Y - (h * 0.06), tank.Width, tank.Height + (h * 0.06)));
        context.FillRectangle(Oil, tank);
        DiagramText.Draw(context, DiagramText.Get("Lifting_Jack_Tank"), new Point(tank.Center.X, tank.Y - (h * 0.06) - 4), 11.5, DiagramText.Faint, 0.5, 1);

        var passage = new Rect(tank.X, baseTop, ramX + (ramW / 2) - tank.X, floor - baseTop - 8);
        context.FillRectangle(Oil, passage);
        Mark("oil", passage);

        // The handle: pivoted to the right of the pump rod, the hand far out to the left; the rod hangs from it, so
        // pressing the hand down turns the handle and pushes the rod and the pump piston down.
        var pumpBore = new Rect(pumpX - (pumpW / 2), cylinderTop, pumpW, baseTop - cylinderTop);
        var pivot = new Point(pumpX + (w * 0.05), cylinderTop - (h * 0.12));
        var restEnd = new Point(pivot.X - (w * 0.05 * Jack.HandleLever * 0.75), pivot.Y - (h * 0.06));
        double RodTop(Point end) => pivot.Y + ((pumpX - pivot.X) * (end.Y - pivot.Y) / (end.X - pivot.X));
        var restPiston = cylinderTop + (pumpBore.Height * 0.35);
        var rodLength = restPiston - RodTop(restEnd);
        var (ts, tc) = Math.SinCos(-Swing * depth); // counter-clockwise on screen: the hand's end goes down
        var (dx, dy) = (restEnd.X - pivot.X, restEnd.Y - pivot.Y);
        var handEnd = new Point(pivot.X + (dx * tc) - (dy * ts), pivot.Y + (dx * ts) + (dy * tc));
        var rodTop = RodTop(handEnd);
        var pumpPiston = rodTop + rodLength;

        // The pump: its piston partway down, oil under it.
        context.FillRectangle(Dark, pumpBore);
        context.FillRectangle(Oil, new Rect(pumpBore.X, pumpPiston, pumpW, baseTop - pumpPiston));
        var pumpRod = new Rect(pumpX - (pumpW * 0.22), rodTop, pumpW * 0.44, pumpPiston - rodTop);
        context.DrawRectangle(SteelBrush, Edge, pumpRod);
        var pump = new Rect(pumpBore.X, pumpPiston - 8, pumpW, 10);
        context.DrawRectangle(SteelBrush, Edge, pump, 2, 2);
        Mark("pump", new Rect(pump.X - 4, pumpRod.Y, pump.Width + 8, pump.Bottom - pumpRod.Y));

        context.DrawLine(new ImmutablePen(DarkSteelBrush, 7), pivot, handEnd);
        context.DrawEllipse(SteelBrush, Edge, pivot, 5, 5);
        context.DrawLine(new ImmutablePen(SteelBrush, 3), pivot, new Point(pivot.X, cylinderTop - 10));
        var hand = DrawHand(context, handEnd, h * 0.06);
        var effort = Jack.Effort(Jack.Load, ram);
        DrawForce(context, new Point(hand.Center.X, hand.Top - (h * 0.1)), new Point(hand.Center.X, hand.Top - 2), LiftingParts.Push, Kg(effort));

        // The ram: wide, lifted by the oil under it, the car's weight on its top.
        var ramBore = new Rect(ramX - (ramW / 2), cylinderTop, ramW, baseTop - cylinderTop);
        context.FillRectangle(Dark, ramBore);
        var perMm = (baseTop - cylinderTop) * 0.55 / MostRise;
        var ramBottom = baseTop - (h * 0.04) - (Math.Min(rise, MostRise) * perMm);
        context.FillRectangle(Oil, new Rect(ramBore.X, ramBottom, ramW, baseTop - ramBottom));
        var ramBox = new Rect(ramBore.X + 1, ramBottom - (h * 0.42), ramW - 2, h * 0.42);
        context.DrawRectangle(SteelBrush, Edge, ramBox, 3, 3);
        Mark("ram", ramBox);
        var car = new Rect(ramBox.Center.X - Math.Max(ramW * 0.7, w * 0.09), ramBox.Top - (h * 0.09), 2 * Math.Max(ramW * 0.7, w * 0.09), h * 0.09);
        DrawWeight(context, car, Kg(Jack.Load), Stone);
        DrawForce(context, new Point(car.Right + 12, car.Top - (h * 0.04)), new Point(car.Right + 12, car.Bottom + (h * 0.06)), Weight, Kg(Jack.Load));

        // The valves: balls on their seats, letting oil only one way (tank → pump → ram); an open one is pushed off
        // its seat by the oil going through.
        var r = Math.Max(4, pumpW * 0.32);
        var inlet = new Point((tank.Right + pumpBore.Left) / 2, passage.Center.Y);
        var outlet = new Point((pumpBore.Right + ramBore.Left) / 2, passage.Center.Y);
        foreach (var (at, open) in new[] { (inlet, inletOpen), (outlet, outletOpen) })
        {
            DrawPolygon(context, DarkSteelBrush, Edge, [new Point(at.X - (r * 1.3), at.Y - (r * 1.6)), new Point(at.X - (r * 1.3), at.Y + (r * 1.6)), new Point(at.X - (r * 0.2), at.Y + (r * 0.7)), new Point(at.X - (r * 0.2), at.Y - (r * 0.7))]);
            context.DrawEllipse(DiagramText.Gold, Edge, new Point(at.X + (open ? r * 0.7 : 0), at.Y), r, r);
        }

        Mark("valves", [new Point(inlet.X - (r * 1.5), inlet.Y - (r * 1.7)), new Point(outlet.X + r, outlet.Y + (r * 1.7))], 4);
        DiagramText.Draw(context, "→", new Point(inlet.X + (r * 2), inlet.Y), 14, DiagramText.Ink, 0, 0.5, bold: true);
        DiagramText.Draw(context, "→", new Point(outlet.X + (r * 2), outlet.Y), 14, DiagramText.Ink, 0, 0.5, bold: true);

        DiagramText.Draw(context, DiagramText.Format("Lifting_Jack_Area", (ram * ram).ToString("0", CultureInfo.CurrentCulture)),
            new Point(bounds.X + 16, bounds.Y + 16), 13, DiagramText.Ink, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Lifting_Jack_Up", rise.ToString("0.#", CultureInfo.CurrentCulture)),
            new Point(bounds.X + 16, bounds.Y + 38), 13, DiagramText.Faint);

        // The reader's handles: a tap on the hand pumps one stroke, a tap on the tank opens the release valve (all the
        // oil under the ram runs back), and the ram's right side is dragged out or in for a wider or narrower ram.
        Grip("pump", hand.Center, GripAxis.Tap, DiagramText.Get("Lifting_Grip_PumpOnce"),
            _ => Strokes = Math.Min(JackSpread.MostStrokes, Strokes + 1));
        Grip("release", tank.Center, GripAxis.Tap, DiagramText.Get("Lifting_Grip_Release"), _ => Strokes = 0);
        Grip("ram", new Point(ramBox.Right, ramBox.Top + (ramBox.Height * 0.4)), GripAxis.Horizontal, DiagramText.Get("Lifting_Grip_Ram"),
            at => Ram = Math.Clamp(Math.Round(2 * (at.X - ramX) / pumpW / 0.5) * 0.5, 2, 10));
    }
}
