using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;
using static Lemon.Inside.Book.Chapters.Plumbing.PlumbingParts;

namespace Lemon.Inside.Book.Chapters.Plumbing;

/// <summary>
/// Spread 4: a toilet cistern cut open, in centimetres. The inlet valve on the left pours in while its float hangs low;
/// as the water lifts the float, its arm closes the valve, until it shuts at the level it is set to. The flush lever
/// lifts a flap in the floor; the overflow pipe on the right takes any water that rises to its top.
/// </summary>
public sealed class CisternDiagram : GearPicture
{
    public static readonly StyledProperty<double> SetProperty =
        AvaloniaProperty.Register<CisternDiagram, double>(nameof(Set), 28);

    public static readonly StyledProperty<bool> FlushingProperty =
        AvaloniaProperty.Register<CisternDiagram, bool>(nameof(Flushing));

    /// <summary>The float arm's pivot on the valve, and its length, cm.</summary>
    private const double PivotX = 11;
    private const double PivotY = 38;
    private const double Arm = 26;

    private double _level = double.NaN;

    static CisternDiagram()
    {
        AffectsRender<CisternDiagram>(SetProperty, FlushingProperty);
    }

    /// <summary>The level the float shuts the valve at, cm.</summary>
    public double Set
    {
        get => GetValue(SetProperty);
        set => SetValue(SetProperty, value);
    }

    public bool Flushing
    {
        get => GetValue(FlushingProperty);
        set => SetValue(FlushingProperty, value);
    }

    private double Level => double.IsNaN(_level) ? _level = Cistern.Settles(Set) : _level;

    protected override void Advance(double seconds) => _level = Cistern.Step(Level, Set, Flushing, seconds);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var set = Math.Clamp(Set, 20, 38);
        var level = Level;
        var opening = Cistern.Opening(level, set);
        var scale = Math.Min((bounds.Width - 40) / 74, (bounds.Height - 70) / 62);
        var v = new View(new Point(bounds.Center.X - (27 * scale), bounds.Bottom - 24 - (12 * scale)), scale);

        // The tank and its water.
        var tank = v.Box(0, 0, 50, Cistern.Depth);
        context.FillRectangle(AirBrush, tank);
        context.FillRectangle(WaterBrush, v.Box(0, 0, 50, level));
        context.DrawRectangle(null, new ImmutablePen(Porcelain, Math.Max(3, 1.2 * scale)), tank.Inflate(0.6 * scale));
        context.DrawRectangle(Porcelain, Edge, v.Box(-2, Cistern.Depth + 0.6, 52, Cistern.Depth + 3), 2, 2);

        // The flush: a pipe down from the floor, a flap over its mouth, a chain up to the lever.
        var flushing = Flushing;
        var draining = flushing && level > 0.2;
        DrawPipe(context, [v.At(25, 0), v.At(25, -12)], 9 * scale, draining, SteelBrush);
        if (draining) DrawFlow(context, [v.At(25, 0), v.At(25, -12)], 3 * scale, Seconds * 140);
        var hinge = v.At(19, 1);
        var tilt = flushing ? -50 * Math.PI / 180 : 0;
        Point Turned(double along, double up) => new(hinge.X + (along * scale * Math.Cos(tilt)) + (up * scale * Math.Sin(tilt)),
            hinge.Y + (along * scale * Math.Sin(tilt)) - (up * scale * Math.Cos(tilt)));
        Point[] flap = [Turned(0, -0.6), Turned(12, -0.6), Turned(12, 1.2), Turned(0, 1.2)];
        DrawPolygon(context, Rubber, Edge, flap);
        var leverEnd = v.At(28, flushing ? 47 : Cistern.Depth + 4.5);
        context.DrawLine(new ImmutablePen(SteelBrush, 3, null, PenLineCap.Round), v.At(-5, Cistern.Depth + 4.5), leverEnd);
        context.DrawRectangle(SteelBrush, Edge, v.Box(-8, Cistern.Depth + 3, -3, Cistern.Depth + 6), 2, 2);
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 1.2, new ImmutableDashStyle([1.5, 1.5], 0)), leverEnd, Turned(10, 1.2));
        Mark("flush", [.. flap, leverEnd, v.At(-5, Cistern.Depth + 4.5)], 6);

        // The overflow pipe: open at its top, out through the floor.
        var spilling = level >= Cistern.Overflow - 0.05 && opening > 0;
        List<Point> overflow = [v.At(46, Cistern.Overflow), v.At(46, -12)];
        DrawPipe(context, overflow, 3 * scale, spilling, SteelBrush);
        if (spilling) DrawFlow(context, overflow, 3 * scale, Seconds * 90);
        Mark("overflow", overflow, 6);
        DiagramText.Draw(context, DiagramText.Get("Plumbing_Cistern_OverflowLabel"), v.At(53.5, Cistern.Overflow), 11.5, DiagramText.Ink, 0, 0.5);

        // The inlet: a supply pipe up to the valve, water pouring from it as far as it is open.
        DrawPipe(context, [v.At(6, -12), v.At(6, 34)], 2.4 * scale, true, SteelBrush);
        if (opening > 0.01)
        {
            var w = Math.Max(2, 2 * opening * scale);
            var mouth = v.At(10, 33.5);
            var surface = v.At(0, level).Y;
            context.FillRectangle(WaterBrush, new Rect(mouth.X - (w / 2), mouth.Y, w, Math.Max(0, surface - mouth.Y)));
            DrawFlow(context, [mouth, new Point(mouth.X, surface)], w, Seconds * 80 * opening);
        }

        var valve = v.Box(3, 33.5, 12, 40);
        context.DrawRectangle(Brass, Edge, valve, 2, 2);
        Mark("valve", valve);

        // The float on its arm: it rides on the water until the arm has shut the valve, then stays put.
        var floatY = Math.Clamp(Math.Min(level, set) + 1.5, 6, PivotY + 4);
        var dy = PivotY - floatY;
        var floatX = PivotX + Math.Sqrt(Math.Max(0, (Arm * Arm) - (dy * dy)));
        var floatCentre = v.At(floatX, floatY);
        context.DrawLine(new ImmutablePen(SteelBrush, 2.5), v.At(PivotX, PivotY), floatCentre);
        var floatBox = new Rect(floatCentre.X - (6 * scale), floatCentre.Y - (4.5 * scale), 12 * scale, 9 * scale);
        context.DrawEllipse(Load, Edge, floatBox.Center, floatBox.Width / 2, floatBox.Height / 2);
        context.DrawEllipse(Dark, null, v.At(PivotX, PivotY), 3, 3);
        Mark("float", floatBox);

        // Where it is set to shut, how open it is, and the water's depth.
        var setLine = new[] { v.At(0, set), v.At(50, set) };
        context.DrawLine(new ImmutablePen(DiagramText.Gold, 1.2, new ImmutableDashStyle([5, 4], 0)), setLine[0], setLine[1]);
        DiagramText.Draw(context, DiagramText.Format("Plumbing_Cistern_SetLabel", Number(set)), v.At(13, set + 0.3), 11.5,
            DiagramText.Gold, 0, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Plumbing_Cistern_ValveLabel", Number(opening * 100, "0")), v.At(13, 32.8), 12,
            DiagramText.Ink, 0, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Plumbing_Centimetres", Number(level, "0")), v.At(53.5, level), 12, DiagramText.Cyan, 0, 0.5, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Plumbing_Cistern_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
