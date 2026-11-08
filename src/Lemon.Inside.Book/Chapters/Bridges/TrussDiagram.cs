using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Bridges.BridgesParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Bridges;

/// <summary>
/// Spread 3: a Warren truss over a river, with a lorry on its deck. Every member is coloured by whether it is
/// squeezed or stretched and drawn as thick as its share of the biggest force, its force written beside it; the banks
/// push up under each end with their share of the lorry. Move the lorry and it drives there, wheels turning, the
/// forces in the members changing under it as it goes; the truss grows or shrinks smoothly. The reader drags the lorry
/// along the deck and a joint of the top chord up or down.
/// </summary>
public sealed class TrussDiagram : GearPicture
{
    public static readonly StyledProperty<double> TrussHeightProperty =
        AvaloniaProperty.Register<TrussDiagram, double>(nameof(TrussHeight), 2, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> PositionProperty =
        AvaloniaProperty.Register<TrussDiagram, double>(nameof(Position), 0.5, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>How fast the lorry drives, m a second.</summary>
    private const double Drive = 5;

    /// <summary>Where the lorry is drawn, 0..1 of the span, and the truss's drawn height, m; NaN until the first frame.</summary>
    private double _position = double.NaN;
    private double _height = double.NaN;

    static TrussDiagram()
    {
        AffectsRender<TrussDiagram>(TrussHeightProperty, PositionProperty);
    }

    /// <summary>The truss's height, m (not <c>Height</c>: that is the control's own).</summary>
    public double TrussHeight
    {
        get => GetValue(TrussHeightProperty);
        set => SetValue(TrussHeightProperty, value);
    }

    /// <summary>Where the lorry is, 0..1 of the span.</summary>
    public double Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    /// <summary>The lorry drives to where it was put at a steady speed; the truss eases to its new height.</summary>
    protected override void Advance(double seconds)
    {
        Start();
        _position = Lifting.LiftingParts.Ease(_position, Math.Clamp(Position, 0, 1), Drive / Truss.Span, seconds);
        _height = Glide(_height, Math.Clamp(TrussHeight, 1, 4), seconds, 0.15);
    }

    private void Start()
    {
        if (double.IsNaN(_position)) _position = Math.Clamp(Position, 0, 1);
        if (double.IsNaN(_height)) _height = Math.Clamp(TrussHeight, 1, 4);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        Start();
        var height = _height;
        var position = _position;
        var scale = Math.Min((bounds.Width - 60) / 19, (bounds.Height - 150) / 8);
        var v = new View(new Point(bounds.Center.X - (Truss.Span / 2 * scale), bounds.Center.Y + (1.6 * scale)), scale);

        // The river and the banks the truss rests on.
        context.FillRectangle(River, v.Box(0, -3, Truss.Span, -2.3));
        foreach (var (x0, x1) in new[] { (-2.5, 0.4), (Truss.Span - 0.4, Truss.Span + 2.5) })
        {
            DrawPolygon(context, Ground, null, [v.At(x0, -0.9), v.At(x1, -0.9), v.At(x1, -3.5), v.At(x0, -3.5)]);
        }

        // The supports: a pin on the left, a roller on the right (so the deck can grow in the sun).
        var r = Math.Max(4, 0.14 * scale);
        foreach (var x in new[] { 0.0, Truss.Span })
        {
            var p = v.At(x, -0.3);
            DrawPolygon(context, Concrete, Edge, [new Point(p.X, p.Y), new Point(p.X - (0.45 * scale), v.At(0, -0.9).Y), new Point(p.X + (0.45 * scale), v.At(0, -0.9).Y)]);
        }

        // The deck under the bottom chord, and the lorry on it.
        context.DrawRectangle(Concrete, Edge, v.Box(-0.3, -0.3, Truss.Span + 0.3, -0.08));
        var lorryX = Math.Clamp(position * Truss.Span, 1.1, Truss.Span - 1.1);
        var lorry = v.Box(lorryX - 1.1, 0, lorryX + 1.1, 1.3);
        context.DrawRectangle(Load, Edge, lorry, 4, 4);

        // Its wheels, turning as it drives (a mark on each shows the turn).
        var wheel = 0.28;
        foreach (var x in new[] { lorryX - 0.65, lorryX + 0.65 })
        {
            var hub = v.At(x, wheel - 0.08);
            context.DrawEllipse(Dark, new ImmutablePen(Idle, 1.2), hub, wheel * scale, wheel * scale);
            var turn = -lorryX / wheel;
            var spoke = new Vector(Math.Cos(turn), -Math.Sin(turn)) * (wheel * scale * 0.75);
            context.DrawLine(new ImmutablePen(Idle, 1.5), hub - spoke, hub + spoke);
        }

        DiagramText.Draw(context, Tonnes(Truss.Lorry), new Point(lorry.Center.X, lorry.Top - 2), 11.5, Load, 0.5, 1, bold: true);
        Mark("load", lorry);

        // The members, by the force in each.
        var forces = Truss.Forces(height, position);
        var most = forces.Max(Math.Abs);
        var top = new List<Point>();
        var bottom = new List<Point>();
        var diagonals = new List<Point>();
        for (var m = 0; m < Truss.Members.Count; m++)
        {
            var (j0, j1, kind) = Truss.Members[m];
            var a = At(v, j0, height);
            var b = At(v, j1, height);
            DrawMember(context, a, b, forces[m], most, 2.5, Math.Max(6, 0.24 * scale));
            (kind switch { Truss.Kind.Top => top, Truss.Kind.Bottom => bottom, _ => diagonals }).AddRange([a, b]);
        }

        for (var j = 0; j <= 2 * Truss.Panels; j++) DrawPin(context, At(v, j, height), r * 0.8);

        // Each member's force beside it, once it carries anything worth saying.
        for (var m = 0; m < Truss.Members.Count; m++)
        {
            if (Math.Abs(forces[m]) < 0.05) continue;
            var (j0, j1, kind) = Truss.Members[m];
            var a = At(v, j0, height);
            var b = At(v, j1, height);
            var mid = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);
            var offset = kind switch { Truss.Kind.Top => -14.0, Truss.Kind.Bottom => 22.0, _ => 0.0 };
            DiagramText.Draw(context, Number(Math.Abs(forces[m]), "0.#"), new Point(mid.X + (kind == Truss.Kind.Diagonal ? 10 : 0), mid.Y + offset),
                11, ForBrush(forces[m], most * 0.01), kind == Truss.Kind.Diagonal ? 0 : 0.5, 0.5, bold: true);
        }

        Mark("top", top, 6);
        Mark("bottom", bottom, 6);
        Mark("diagonals", diagonals, 6);

        // The banks pushing up: each end's share of the lorry.
        foreach (var (x, share) in new[] { (0.0, 1 - position), (Truss.Span, position) })
        {
            if (share < 0.005) continue;
            var to = v.At(x, -0.95);
            DrawPush(context, new Point(to.X, to.Y + (1.4 * scale)), to, Lifting.LiftingParts.Push, Tonnes(Truss.Lorry * share), x > 0 ? -0.15 : 1.15, 0.5);
        }

        DiagramText.Draw(context, DiagramText.Get("Bridges_Truss_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
        DrawLegend(context, bounds);

        // The reader's handles: the lorry, dragged along the deck, and a joint of the top chord (the one away from the
        // lorry, so the two never sit on each other), dragged up for a taller truss.
        var origin = v;
        Grip("lorry", v.At(lorryX, 0.65), GripAxis.Horizontal, DiagramText.Get("Bridges_Grip_Lorry"),
            at => Position = Math.Clamp(Math.Round(origin.X(at) / Truss.Span, 2), 0, 1));
        var joint = lorryX < Truss.Span / 2 ? 7 : 6;
        Grip("height", At(v, joint, height), GripAxis.Vertical, DiagramText.Get("Bridges_Grip_TrussHeight"),
            at => TrussHeight = Math.Clamp(Math.Round(origin.Y(at), 1), 1, 4));
    }

    private static Point At(View v, int joint, double height)
    {
        var (x, y) = Truss.Joint(joint, height);
        return v.At(x, y);
    }
}
