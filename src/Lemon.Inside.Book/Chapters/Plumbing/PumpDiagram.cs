using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;
using static Lemon.Inside.Book.Chapters.Plumbing.PlumbingParts;

namespace Lemon.Inside.Book.Chapters.Plumbing;

/// <summary>
/// Spread 6: a well and its hand pump, to scale in metres. With the pump at the top, each stroke lowers the pressure
/// in the pipe and the air pressing on the well's water pushes it up, never more than 10.3 m above that water; with the
/// pump down at the water, a long rod works it from above and it pushes the water all the way up.
/// </summary>
public sealed class PumpDiagram : GearPicture
{
    public static readonly StyledProperty<double> DepthProperty =
        AvaloniaProperty.Register<PumpDiagram, double>(nameof(Depth), 4);

    public static readonly StyledProperty<bool> DownProperty =
        AvaloniaProperty.Register<PumpDiagram, bool>(nameof(Down));

    /// <summary>The spout's height, the shaft's floor, m; how fast the water in the pipe climbs, m a second.</summary>
    private const double Spout = 1.25;
    private const double Floor = -15.8;
    private const double ClimbRate = 3;

    private double _column = double.NaN;

    static PumpDiagram()
    {
        AffectsRender<PumpDiagram>(DepthProperty, DownProperty);
    }

    /// <summary>How far below the ground the well's water stands, m.</summary>
    public double Depth
    {
        get => GetValue(DepthProperty);
        set => SetValue(DepthProperty, value);
    }

    public bool Down
    {
        get => GetValue(DownProperty);
        set => SetValue(DownProperty, value);
    }

    /// <summary>The top of the water in the pipe, m above the ground.</summary>
    private double Target => Pump.Works(Math.Clamp(Depth, 2, 14), Down) ? Spout : -Math.Clamp(Depth, 2, 14) + Water.Atmosphere;

    private double Column => double.IsNaN(_column) ? _column = Target : _column;

    protected override void Advance(double seconds) => _column = Ease(Column, Target, ClimbRate, seconds);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var depth = Math.Clamp(Depth, 2, 14);
        var down = Down;
        var surface = -depth;
        var scale = Math.Min((bounds.Height - 60) / 18.6, (bounds.Width - 40) / 8);
        var v = new View(new Point(bounds.Center.X - (0.6 * scale), bounds.Top + 30 + (2.6 * scale)), scale);
        var bore = Math.Max(5, 0.14 * scale);

        // The ground, the shaft and the water in it.
        context.FillRectangle(Earth, new Rect(bounds.X, v.At(0, 0).Y, bounds.Width, bounds.Bottom - v.At(0, 0).Y));
        context.FillRectangle(AirBrush, v.Box(-0.9, Floor, 0.9, 0));
        var well = v.Box(-0.9, Floor, 0.9, surface);
        context.FillRectangle(WaterBrush, well);
        Mark("water", well);
        DrawAirPushing(context, v.At(-0.85, surface), v.At(-0.2, surface), 0.6 * scale, 2, null);
        DrawAirPushing(context, v.At(0.2, surface), v.At(0.85, surface), 0.6 * scale, 2, null);
        DiagramText.Draw(context, DiagramText.Get("Plumbing_AirPushes"), v.At(1.1, surface + 0.4), 11.5, AirArrow, 0, 0.5);

        // The pipe, and the water the pump has drawn up it.
        var pipeBottom = down ? surface - 0.3 : surface - 0.8;
        List<Point> pipe = [v.At(0, pipeBottom), v.At(0, Spout)];
        DrawPipe(context, pipe, bore, false);
        var column = Column;
        DrawPipe(context, [v.At(0, pipeBottom), v.At(0, Math.Max(pipeBottom, column))], bore, true);
        Mark("pipe", pipe, 6);

        // The pump: a cylinder at the top with its piston, or down at the water on a long rod.
        var stroke = Math.Sin(Seconds * 4);
        var cylinderBottom = down ? surface - 1.6 : 0.15;
        var cylinder = v.Box(-0.42, cylinderBottom, 0.42, cylinderBottom + 1.3);
        var piston = cylinderBottom + 0.75 + (0.35 * stroke);
        context.DrawRectangle(down ? WaterBrush : AirBrush, null, cylinder);
        if (!down) context.FillRectangle(WaterBrush, v.Box(-0.42, cylinderBottom, 0.42, Math.Clamp(column, cylinderBottom, piston)));
        context.DrawRectangle(null, new ImmutablePen(SteelBrush, 3), cylinder, 2, 2);
        context.DrawRectangle(Rubber, Edge, v.Box(-0.4, piston - 0.1, 0.4, piston + 0.1));
        var top = 2.0 + (0.35 * stroke);
        context.DrawLine(new ImmutablePen(SteelBrush, down ? 1.5 : 2.5), v.At(0, piston), v.At(0, top));
        context.DrawLine(new ImmutablePen(SteelBrush, 4, null, PenLineCap.Round), v.At(-0.75, top), v.At(0.75, top));
        Mark("piston", cylinder);

        // Two balls on seats: one under the cylinder, one in the piston.
        Point[] valves = [v.At(0, cylinderBottom + 0.05), v.At(0, piston)];
        foreach (var valve in valves) context.DrawEllipse(DiagramText.Gold, Edge, valve, Math.Max(3, 0.07 * scale), Math.Max(3, 0.07 * scale));
        Mark("valves", valves, 8);

        // The spout, pouring once the water reaches it.
        var spout = new[] { v.At(0.4, Spout), v.At(1.1, Spout - 0.15) };
        context.DrawLine(new ImmutablePen(SteelBrush, bore + 4, null, PenLineCap.Round), spout[0], spout[1]);
        if (column >= Spout - 0.02)
        {
            var ground = v.At(0, 0).Y;
            context.FillRectangle(WaterBrush, new Rect(spout[1].X - 2, spout[1].Y, 4, ground - spout[1].Y));
            DrawFlow(context, [spout[1], new Point(spout[1].X, ground)], 4, Seconds * 60);
        }

        // How deep the water is, and how high the air can push it.
        var x = v.At(-1.5, 0).X;
        var a = new Point(x, v.At(0, 0).Y);
        var b = new Point(x, v.At(0, surface).Y);
        context.DrawLine(new ImmutablePen(DiagramText.Ink, 1.2), a, b);
        DiagramText.DrawArrowAlong(context, a, b, DiagramText.Ink, 7);
        DiagramText.DrawArrowAlong(context, b, a, DiagramText.Ink, 7);
        DiagramText.Draw(context, DiagramText.Format("Plumbing_Metres", Number(depth)), new Point(x - 6, (a.Y + b.Y) / 2), 12.5, DiagramText.Ink, 1, 0.5, bold: true);
        if (!down)
        {
            var limit = surface + Water.Atmosphere;
            var brush = Pump.Works(depth, down) ? DiagramText.Faint : DiagramText.Gold;
            context.DrawLine(new ImmutablePen(brush, 1.5, new ImmutableDashStyle([5, 4], 0)), v.At(-1.2, limit), v.At(2.2, limit));
            DiagramText.Draw(context, DiagramText.Get("Plumbing_Pump_LimitLabel"), v.At(2.3, limit), 11.5, brush, 0, 0.5, bold: true);
            if (!Pump.Works(depth, down) && column > limit - 0.3)
            {
                DiagramText.Draw(context, DiagramText.Get("Plumbing_Pump_EmptyLabel"), v.At(0.45, (limit + 0.15) / 2), 11.5, DiagramText.Faint, 0, 0.5);
            }
        }
        else
        {
            DiagramText.Draw(context, DiagramText.Get("Plumbing_Pump_DownLabel"), v.At(0.5, cylinderBottom + 0.65), 11.5, DiagramText.Ink, 0, 0.5, bold: true);
        }

        DiagramText.Draw(context, DiagramText.Get("Plumbing_Pump_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
