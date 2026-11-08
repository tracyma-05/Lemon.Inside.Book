using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;
using static Lemon.Inside.Book.Chapters.Plumbing.PlumbingParts;

namespace Lemon.Inside.Book.Chapters.Plumbing;

/// <summary>
/// Spread 1: a water tower and a tall building, joined by a pipe under the ground. The tower's water level is carried
/// across as a dashed line; the tap on the building's riser shows the pressure the height above it gives, and its
/// stream reaches farther the faster the water comes out.
/// </summary>
public sealed class TowerDiagram : GearPicture
{
    public static readonly StyledProperty<double> SurfaceProperty =
        AvaloniaProperty.Register<TowerDiagram, double>(nameof(Surface), 30, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> TapHeightProperty =
        AvaloniaProperty.Register<TowerDiagram, double>(nameof(TapHeight), 1, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The building's top floor and roof, m; where it stands, m.</summary>
    private const double Top = 36;
    private const double Roof = 39;
    private const double Left = 17;
    private const double Right = 27;
    private const double Riser = 19;

    static TowerDiagram()
    {
        AffectsRender<TowerDiagram>(SurfaceProperty, TapHeightProperty);
    }

    /// <summary>The height of the tower's water above the ground, m.</summary>
    public double Surface
    {
        get => GetValue(SurfaceProperty);
        set => SetValue(SurfaceProperty, value);
    }

    /// <summary>The tap's height above the ground, m.</summary>
    public double TapHeight
    {
        get => GetValue(TapHeightProperty);
        set => SetValue(TapHeightProperty, value);
    }

    /// <summary>The tank's height as drawn: it glides to a new setting rather than jumping (a grip's drag moves it at once).</summary>
    private double _shown = double.NaN;

    private double Shown => double.IsNaN(_shown) ? _shown = Math.Clamp(Surface, 15, 40) : _shown;

    protected override void Advance(double seconds) => _shown = Glide(Shown, Math.Clamp(Surface, 15, 40), seconds, 0.25);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var surface = Shown;
        var tapAt = Math.Clamp(TapHeight, 1, Top + 1);
        var scale = Math.Min((bounds.Height - 60) / 44, (bounds.Width - 40) / 33);
        var v = new View(new Point(bounds.Center.X - (16 * scale), bounds.Bottom - (3 * scale) - 16), scale);
        var bore = Math.Max(4, 0.55 * scale);

        context.FillRectangle(Ground, new Rect(bounds.X, v.At(0, 0).Y, bounds.Width, bounds.Height));

        // The tower: legs up to a tank holding the water a few metres deep.
        var tankBottom = surface - 4;
        var tank = v.Box(2, tankBottom, 10, surface + 1.2);
        var legPen = new ImmutablePen(SteelBrush, 2.5);
        foreach (var (x0, x1) in new[] { (2.5, 3.5), (9.5, 8.5) }) context.DrawLine(legPen, v.At(x0, 0), v.At(x1, tankBottom));
        for (var y = 4.0; y < tankBottom - 2; y += 6)
        {
            context.DrawLine(new ImmutablePen(SteelBrush, 1.2), v.At(2.5 + (y / tankBottom), y), v.At(9.5 - (y / tankBottom), y + 6));
            context.DrawLine(new ImmutablePen(SteelBrush, 1.2), v.At(9.5 - (y / tankBottom), y), v.At(2.5 + (y / tankBottom), y + 6));
        }

        // The pipe: down from the tank, under the ground, up the building's riser.
        var head = Tower.Head(surface, tapAt);
        var flows = Tower.Flows(surface, tapAt);
        List<Point> pipe = [v.At(6, tankBottom), v.At(6, -1.6), v.At(Riser, -1.6), v.At(Riser, Math.Min(Top + 1.5, Roof - 0.5))];
        DrawPipe(context, pipe, bore, full: false);
        var wet = Math.Min(surface, Top + 1.5);
        DrawPipe(context, [v.At(6, tankBottom), v.At(6, -1.6), v.At(Riser, -1.6), v.At(Riser, wet)], bore, full: true);
        if (flows)
        {
            // Water on its way from the tank to the open tap, the faster the more head there is behind it.
            DrawFlow(context, [v.At(6, tankBottom), v.At(6, -1.6), v.At(Riser, -1.6), v.At(Riser, tapAt)], bore, Seconds * 8 * Water.Speed(head));
        }

        Mark("pipe", pipe, 8);

        context.DrawRectangle(AirBrush, Edge, tank, 4, 4);
        context.FillRectangle(WaterBrush, v.Box(2, tankBottom, 10, surface).Deflate(2));
        context.DrawRectangle(null, new ImmutablePen(SteelBrush, 3), tank, 4, 4);
        Mark("tank", tank);

        // The building: floors every storey, the riser inside.
        var building = v.Box(Left, 0, Right, Roof);
        context.DrawRectangle(Wall, Edge, building);
        for (var y = Tower.Storey; y < Roof; y += Tower.Storey)
        {
            context.DrawLine(new ImmutablePen(Brick, 1.5), v.At(Left, y), v.At(Right, y));
        }

        // The water level carried across: water in any pipe joined to the tank would stand this high.
        var levelLine = new[] { v.At(10.3, surface), v.At(Right + 1.5, surface) };
        context.DrawLine(new ImmutablePen(DiagramText.Cyan, 1.5, new ImmutableDashStyle([5, 4], 0)), levelLine[0], levelLine[1]);
        DiagramText.Draw(context, DiagramText.Get("Plumbing_Tower_Level"), new Point(levelLine[0].X + 4, levelLine[0].Y - 3), 11.5, DiagramText.Cyan, 0, 1);
        Mark("level", levelLine, 7);

        // The tap on the riser, its spout to the right, and its stream.
        var tapPoint = v.At(Riser, tapAt);
        var spoutEnd = new Point(tapPoint.X + (1.6 * scale), tapPoint.Y);
        context.DrawLine(new ImmutablePen(Brass, Math.Max(4, 0.45 * scale), null, PenLineCap.Round), tapPoint, spoutEnd);
        context.DrawLine(new ImmutablePen(Brass, Math.Max(3, 0.3 * scale), null, PenLineCap.Round), spoutEnd, new Point(spoutEnd.X, spoutEnd.Y + (0.4 * scale)));
        var tapBox = new Rect(tapPoint.X - 6, tapPoint.Y - (0.6 * scale), spoutEnd.X - tapPoint.X + 12, 1.2 * scale);
        context.DrawEllipse(Brass, Edge, new Point(tapPoint.X + (0.5 * scale), tapPoint.Y - (0.35 * scale)), 0.3 * scale, 0.18 * scale);
        Mark("tap", tapBox);

        var bubbleAt = new Point(spoutEnd.X + 8, tapPoint.Y - (0.9 * scale));
        if (flows)
        {
            // A stream that curves down to the floor; the faster it leaves, the farther it reaches before it lands.
            var speed = Water.Speed(head);
            var start = new Point(spoutEnd.X, spoutEnd.Y + (0.4 * scale));
            var drop = 0.9 * scale;
            var reach = Math.Min(Right - Riser - 2.2, 0.12 * speed) * scale;
            var stream = new List<Point>();
            for (var i = 0; i <= 12; i++)
            {
                var t = i / 12.0;
                stream.Add(new Point(start.X + (reach * t), start.Y + (drop * t * t) + (drop * 0.15 * t)));
            }

            var streamWidth = Math.Clamp(speed / 6, 1.5, 4.5);
            context.DrawGeometry(null, new ImmutablePen(WaterBrush, streamWidth, null, PenLineCap.Round), Polyline(stream));
            DrawFlow(context, stream, streamWidth + 1, Seconds * 6 * speed);
            DiagramText.Draw(context, DiagramText.Format("Plumbing_Bar", Number(Water.Pressure(head), "0.0")), bubbleAt, 12.5, DiagramText.Gold, 0, 0.5, bold: true);
            DiagramText.Draw(context, DiagramText.Format("Plumbing_Speed", Number(speed, "0")), new Point(bubbleAt.X, bubbleAt.Y + 15), 11.5, DiagramText.Ink, 0, 0.5);

            // The head: from the tap up to the tower's water.
            var x = v.At(Right + 0.8, 0).X;
            var a = new Point(x, v.At(0, tapAt).Y);
            var b = new Point(x, v.At(0, surface).Y);
            context.DrawLine(new ImmutablePen(DiagramText.Gold, 1.5), a, b);
            DiagramText.DrawArrowAlong(context, a, b, DiagramText.Gold, 8);
            DiagramText.DrawArrowAlong(context, b, a, DiagramText.Gold, 8);
            DiagramText.Draw(context, DiagramText.Format("Plumbing_Metres", Number(head, "0.#")), new Point(x + 6, (a.Y + b.Y) / 2), 12.5,
                DiagramText.Gold, 0, 0.5, bold: true);
        }
        else
        {
            DiagramText.Draw(context, DiagramText.Get("Plumbing_Tower_NoWater"), bubbleAt, 12.5, DiagramText.Faint, 0, 0.5, bold: true);
        }

        DiagramText.Draw(context, DiagramText.Get("Plumbing_Tower_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);

        // The reader's handles: the tank's water lifted or lowered on its tower, and the tap slid up or down the riser.
        Grip("surface", v.At(6, surface), GripAxis.Vertical, DiagramText.Get("Plumbing_Grip_Tank"), at =>
        {
            Surface = Snap(v.Y(at), 0.5, 15, 40);
            _shown = Surface;
        });
        Grip("tap", new Point(tapPoint.X + (0.5 * scale), tapPoint.Y - (0.35 * scale)), GripAxis.Vertical, DiagramText.Get("Plumbing_Grip_Tap"),
            at => TapHeight = Snap(v.Y(at) - 0.35, 1, 1, 37));
    }
}
