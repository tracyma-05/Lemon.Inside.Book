using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Lifting;

/// <summary>
/// Spread 5: a bottle jack cut open. Left to right: the oil tank, the narrow pump with its handle, and the wide ram
/// with the car's weight on it; a ball valve lets oil from the tank into the pump, another from the pump to the ram.
/// The arrows on the two pistons are the forces the same oil pressure gives them; the ram's height grows with the
/// strokes.
/// </summary>
public sealed class JackDiagram : GearPicture
{
    public static readonly StyledProperty<double> RamProperty =
        AvaloniaProperty.Register<JackDiagram, double>(nameof(Ram), 4);

    public static readonly StyledProperty<int> StrokesProperty =
        AvaloniaProperty.Register<JackDiagram, int>(nameof(Strokes));

    /// <summary>The highest the ram is drawn, mm (forty strokes of the narrowest ram's rise).</summary>
    private const double MostRise = JackSpread.MostStrokes * Jack.Stroke / 4;

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

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var ram = Math.Clamp(Ram, 2, 10);
        var rise = Jack.Rise(ram, Math.Clamp(Strokes, 0, JackSpread.MostStrokes));
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

        // The pump: its piston partway down, oil under it.
        var pumpBore = new Rect(pumpX - (pumpW / 2), cylinderTop, pumpW, baseTop - cylinderTop);
        var pumpPiston = cylinderTop + (pumpBore.Height * 0.35);
        context.FillRectangle(Dark, pumpBore);
        context.FillRectangle(Oil, new Rect(pumpBore.X, pumpPiston, pumpW, baseTop - pumpPiston));
        // The handle: pivoted to the right of the pump rod, the hand far out to the left; the rod hangs from it.
        var pivot = new Point(pumpX + (w * 0.05), cylinderTop - (h * 0.12));
        var handEnd = new Point(pivot.X - (w * 0.05 * Jack.HandleLever * 0.75), pivot.Y - (h * 0.06));
        var rodTop = pivot.Y + ((pumpX - pivot.X) * (handEnd.Y - pivot.Y) / (handEnd.X - pivot.X));
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

        // The valves: balls on their seats, letting oil only one way (tank → pump → ram).
        var r = Math.Max(4, pumpW * 0.32);
        var inlet = new Point((tank.Right + pumpBore.Left) / 2, passage.Center.Y);
        var outlet = new Point((pumpBore.Right + ramBore.Left) / 2, passage.Center.Y);
        foreach (var at in new[] { inlet, outlet })
        {
            LiftingParts.DrawPolygon(context, DarkSteelBrush, Edge, [new Point(at.X - (r * 1.3), at.Y - (r * 1.6)), new Point(at.X - (r * 1.3), at.Y + (r * 1.6)), new Point(at.X - (r * 0.2), at.Y + (r * 0.7)), new Point(at.X - (r * 0.2), at.Y - (r * 0.7))]);
            context.DrawEllipse(DiagramText.Gold, Edge, at, r, r);
        }

        Mark("valves", [new Point(inlet.X - (r * 1.5), inlet.Y - (r * 1.7)), new Point(outlet.X + r, outlet.Y + (r * 1.7))], 4);
        DiagramText.Draw(context, "→", new Point(inlet.X + (r * 2), inlet.Y), 14, DiagramText.Ink, 0, 0.5, bold: true);
        DiagramText.Draw(context, "→", new Point(outlet.X + (r * 2), outlet.Y), 14, DiagramText.Ink, 0, 0.5, bold: true);

        DiagramText.Draw(context, DiagramText.Format("Lifting_Jack_Area", (ram * ram).ToString("0", CultureInfo.CurrentCulture)),
            new Point(bounds.X + 16, bounds.Y + 16), 13, DiagramText.Ink, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Lifting_Jack_Up", rise.ToString("0.#", CultureInfo.CurrentCulture)),
            new Point(bounds.X + 16, bounds.Y + 38), 13, DiagramText.Faint);
    }
}
