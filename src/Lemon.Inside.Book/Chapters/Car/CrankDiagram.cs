using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Car;

/// <summary>
/// Spread 1: a cylinder cut open, its piston on a connecting rod to the crank, turning steadily. Below, how far the
/// piston has come down through one turn, against a plain wave: the rod makes it hurry near the top and dawdle at the bottom.
/// The reader drags the crank's bearing down, away from the cylinder, for a longer rod; the engine eases to its new shape.
/// </summary>
public sealed class CrankDiagram : GearPicture
{
    public static readonly StyledProperty<double> RodProperty =
        AvaloniaProperty.Register<CrankDiagram, double>(nameof(Rod), 3.5, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The rod's shortest and longest, crank radii (the slider's ends).</summary>
    private const double ShortestRod = 1.5;
    private const double LongestRod = 5;

    /// <summary>Crank turns a second (slowed right down).</summary>
    private const double Speed = 0.3;

    private static readonly IImmutableBrush Wall = new ImmutableSolidColorBrush(Color.FromRgb(0x3A, 0x42, 0x52));
    private static readonly IImmutableBrush Gas = new ImmutableSolidColorBrush(Color.FromArgb(0x50, 0xF2, 0xC9, 0x4C));

    private double _turns;

    /// <summary>The rod as drawn, easing toward <see cref="Rod"/>; NaN until the first frame.</summary>
    private double _rod = double.NaN;

    static CrankDiagram()
    {
        AffectsRender<CrankDiagram>(RodProperty);
    }

    /// <summary>The connecting rod's length, in crank radii.</summary>
    public double Rod
    {
        get => GetValue(RodProperty);
        set => SetValue(RodProperty, value);
    }

    protected override void Advance(double seconds)
    {
        _turns += Speed * seconds;
        var rod = Math.Clamp(Rod, ShortestRod, LongestRod);
        _rod = double.IsNaN(_rod) ? rod : Glide(_rod, rod, seconds, 0.15);
    }

    /// <summary>The crank's radius and the height of its bearing on screen, for a rod of <paramref name="rod"/> radii.</summary>
    private static (double Radius, double CentreY) Layout(Rect bounds, double rod)
    {
        var r = Math.Min(bounds.Width * 0.14, bounds.Height * 0.58 / (rod + 2.8));
        return (r, (bounds.Height * 0.06) + ((rod + 2.6) * r));
    }

    /// <summary>The rod (to a tenth) whose crank bearing sits nearest <paramref name="y"/>: the bearing drops as the rod grows.</summary>
    internal static double RodForBearing(Rect bounds, double y)
    {
        var best = ShortestRod;
        for (var rod = ShortestRod; rod <= LongestRod + 1e-9; rod += 0.1)
        {
            if (Math.Abs(Layout(bounds, rod).CentreY - y) < Math.Abs(Layout(bounds, best).CentreY - y)) best = rod;
        }

        return Math.Round(best, 1);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var rod = double.IsNaN(_rod) ? Math.Clamp(Rod, ShortestRod, LongestRod) : _rod;
        var (r, centreY) = Layout(bounds, rod);
        var centre = new Point(bounds.Width * 0.5, centreY);
        var angle = 2 * Math.PI * (_turns % 1);
        var (sin, cos) = Math.SinCos(angle);
        var pin = new Point(centre.X + (r * sin), centre.Y - (r * cos));
        var pistonPin = new Point(centre.X, centre.Y - ((cos * r) + Math.Sqrt((rod * rod * r * r) - (sin * sin * r * r))));

        // The cylinder: its walls and head, the gas above the piston.
        var bore = r * 1.7;
        var top = centre.Y - ((rod + 1) * r) - (r * 0.6);
        var head = top - (r * 0.25);
        context.FillRectangle(Wall, new Rect(centre.X - (bore / 2) - 10, head - 10, 10, (rod + 0.4) * r));
        context.FillRectangle(Wall, new Rect(centre.X + (bore / 2), head - 10, 10, (rod + 0.4) * r));
        context.FillRectangle(Wall, new Rect(centre.X - (bore / 2) - 10, head - 10, bore + 20, 10));
        var pistonTop = pistonPin.Y - (r * 0.55);
        context.FillRectangle(Gas, new Rect(centre.X - (bore / 2), head, bore, Math.Max(0, pistonTop - head)));

        // The bearing's mount, out to the handle that moves it.
        context.DrawLine(new ImmutablePen(Wall, Math.Max(6, r * 0.22), lineCap: PenLineCap.Round), centre, new Point(centre.X + (r * 1.25) + 22, centre.Y));

        // Crank web with its counterweight, the rod, the piston.
        context.DrawEllipse(CarParts.DarkMetalBrush, CarParts.Edge, centre, r * 1.25, r * 1.25);
        context.DrawEllipse(new ImmutableSolidColorBrush(Color.FromRgb(0x46, 0x4E, 0x60)), null, new Point(centre.X - (r * 0.6 * sin), centre.Y + (r * 0.6 * cos)), r * 0.55, r * 0.55);
        context.DrawLine(new ImmutablePen(CarParts.MetalBrush, Math.Max(6, r * 0.28), lineCap: PenLineCap.Round), pin, pistonPin);
        var piston = new Rect(centre.X - (bore / 2) + 2, pistonTop, bore - 4, r * 1.0);
        context.DrawRectangle(new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xCE, 0xD8)), CarParts.Edge, piston, 3, 3);
        for (var k = 1; k <= 2; k++)
        {
            context.DrawLine(new ImmutablePen(CarParts.Dark, 1.5), new Point(piston.X, piston.Y + (k * 6)), new Point(piston.Right, piston.Y + (k * 6)));
        }

        context.DrawEllipse(DiagramText.Gold, CarParts.Edge, pin, Math.Max(4, r * 0.16), Math.Max(4, r * 0.16));
        context.DrawEllipse(CarParts.Dark, null, pistonPin, Math.Max(3, r * 0.1), Math.Max(3, r * 0.1));
        context.DrawEllipse(CarParts.Dark, null, centre, Math.Max(3, r * 0.12), Math.Max(3, r * 0.12));
        Mark("piston", piston);
        Mark("rod", [pin, pistonPin], 6);
        Mark("crank", centre, r * 1.25);

        // The reader's handle: the crank's bearing, dragged down from the cylinder for a longer rod.
        Grip("rod", new Point(centre.X + (r * 1.25) + 22, centre.Y), GripAxis.Vertical, DiagramText.Get("Car_Grip_Rod"),
            at => Rod = RodForBearing(bounds, at.Y));

        DrawGraph(context, new Rect(bounds.X + 30, centre.Y + (r * 1.6) + 26, bounds.Width - 54, bounds.Bottom - centre.Y - (r * 1.6) - 56), rod, angle);
    }

    /// <summary>The piston's travel down through one turn, with a plain wave beside it and a dot for now.</summary>
    private void DrawGraph(DrawingContext context, Rect area, double rod, double angle)
    {
        if (area.Height < 30) return;

        double X(double radians) => area.X + (radians / (2 * Math.PI) * area.Width);
        double Y(double travel) => area.Y + (travel / 2 * area.Height);
        CarParts.DrawCycleAxis(context, area, 360, area.Y);
        var wave = new List<Point>();
        var real = new List<Point>();
        for (var i = 0; i <= 120; i++)
        {
            var a = i * 2 * Math.PI / 120;
            wave.Add(new Point(X(a), Y(SliderCrank.Wave(a))));
            real.Add(new Point(X(a), Y(SliderCrank.Travel(a, rod))));
        }

        CarParts.DrawLine(context, wave, new ImmutablePen(DiagramText.Faint, 1.2, new ImmutableDashStyle([4, 3], 0)));
        CarParts.DrawLine(context, real, new ImmutablePen(DiagramText.Gold, 2.5));
        Mark("graph", real, 4);
        var fastest = SliderCrank.FastestAt(rod) * Math.PI / 180;
        context.DrawLine(new ImmutablePen(DiagramText.Cyan, 1, new ImmutableDashStyle([2, 3], 0)), new Point(X(fastest), area.Y), new Point(X(fastest), area.Bottom));
        DiagramText.Draw(context, DiagramText.Get("Car_Crank_Fastest"), new Point(X(fastest) + 4, area.Y + 4), 11, DiagramText.Cyan);
        context.DrawEllipse(Brushes.White, CarParts.Edge, new Point(X(angle), Y(SliderCrank.Travel(angle, rod))), 5, 5);
        DiagramText.Draw(context, DiagramText.Get("Car_Crank_Down"), new Point(area.X + 4, area.Bottom - 4), 11, DiagramText.Faint, 0, 1);
        DiagramText.Draw(context, DiagramText.Get("Car_Crank_Wave"), new Point(area.Right, area.Bottom - 4), 11, DiagramText.Faint, 1, 1);
    }
}
