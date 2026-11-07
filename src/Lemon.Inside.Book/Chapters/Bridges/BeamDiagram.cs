using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Bridges.BridgesParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Bridges;

/// <summary>
/// Spread 1: a plank across a stream, from the side, with a load in the middle. The plank bends (drawn five times
/// deeper than it is); its upper half is squeezed and its lower half stretched. The same scale for every span, so a
/// longer plank is drawn longer.
/// </summary>
public sealed class BeamDiagram : GearPicture
{
    public static readonly StyledProperty<double> SpanProperty =
        AvaloniaProperty.Register<BeamDiagram, double>(nameof(Span), 6);

    public static readonly StyledProperty<double> DepthProperty =
        AvaloniaProperty.Register<BeamDiagram, double>(nameof(Depth), 0.4);

    /// <summary>How much bigger than life the sag is drawn.</summary>
    public const double Exaggeration = 5;

    static BeamDiagram()
    {
        AffectsRender<BeamDiagram>(SpanProperty, DepthProperty);
    }

    public double Span
    {
        get => GetValue(SpanProperty);
        set => SetValue(SpanProperty, value);
    }

    public double Depth
    {
        get => GetValue(DepthProperty);
        set => SetValue(DepthProperty, value);
    }

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var span = Math.Clamp(Span, 4, 12);
        var depth = Math.Clamp(Depth, 0.2, 0.8);
        var sag = Beam.Sag(span, depth);
        var drawn = Math.Min(sag * Exaggeration, 2);
        var scale = Math.Min((bounds.Width - 60) / 15.5, (bounds.Height - 120) / 7);
        var v = new View(new Point(bounds.Center.X - (span / 2 * scale), bounds.Center.Y - (0.3 * scale)), scale);

        // The banks and the stream between them.
        context.FillRectangle(River, v.Box(0, -3.2, span, -2.4));
        foreach (var (x0, x1) in new[] { (-4.0, 0.0), (span, span + 4) })
        {
            DrawPolygon(context, Ground, null, [v.At(x0, -depth), v.At(x1, -depth), v.At(x1, -4), v.At(x0, -4)]);
        }

        Mark("banks", [v.At(-1, -depth), v.At(0, -depth - 1), v.At(span, -depth - 1), v.At(span + 1, -depth)], 8);

        // The plank: its middle line bends down as load × x(3L² − 4x²) for a load in the middle.
        double Down(double x)
        {
            var u = Math.Clamp(x, 0, span);
            if (u > span / 2) u = span - u;
            return drawn * u * ((3 * span * span) - (4 * u * u)) / (span * span * span);
        }

        var top = new List<Point>();
        var middle = new List<Point>();
        var bottom = new List<Point>();
        for (var i = 0; i <= 40; i++)
        {
            var x = -0.4 + ((span + 0.8) * i / 40);
            top.Add(v.At(x, -Down(x)));
            middle.Add(v.At(x, -Down(x) - (depth / 2)));
            bottom.Add(v.At(x, -Down(x) - depth));
        }

        DrawPolygon(context, SqueezeBrush, null, [.. top, .. Enumerable.Reverse(middle)]);
        DrawPolygon(context, StretchBrush, null, [.. middle, .. Enumerable.Reverse(bottom)]);
        DrawPolygon(context, Brushes.Transparent, new ImmutablePen(Dark, 1.5), [.. top, .. Enumerable.Reverse(bottom)]);
        Mark("beam", [.. top, .. bottom], 4);
        Mark("top", top, 5);

        DiagramText.Draw(context, DiagramText.Get("Bridges_Squeezed"), v.At(span * 0.22, -Down(span * 0.22) + 0.15), 12, SqueezeBrush, 0.5, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Bridges_Stretched"), v.At(span * 0.22, -Down(span * 0.22) - depth - 0.15), 12, StretchBrush, 0.5, 0, bold: true);

        // The load in the middle, riding down with the plank.
        var mid = span / 2;
        var load = v.Box(mid - 0.6, -Down(mid), mid + 0.6, -Down(mid) + 0.9);
        Mark("load", DrawWeight(context, load, Tonnes(Beam.Load)));

        // The sag: from the straight line between the banks down to the plank's middle.
        var straight = v.At(mid, -depth).Y;
        var low = v.At(mid, -Down(mid) - depth).Y;
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 1, new ImmutableDashStyle([4, 4], 0)), v.At(0, -depth), v.At(span, -depth));
        var dimX = v.At(mid + 0.9, 0).X;
        if (low - straight > 3)
        {
            context.DrawLine(new ImmutablePen(DiagramText.Gold, 1.5), new Point(dimX, straight), new Point(dimX, low));
            DiagramText.DrawArrowAlong(context, new Point(dimX, straight), new Point(dimX, low), DiagramText.Gold, 7);
        }

        DiagramText.Draw(context, DiagramText.Format("Bridges_Beam_SagLabel", Number(sag * 100)), new Point(dimX + 8, low + 4), 12.5,
            DiagramText.Gold, 0, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Bridges_Metres", Number(span)), v.At(mid, -3.6), 12, DiagramText.Ink, 0.5, 0.5);
        DiagramText.Draw(context, DiagramText.Get("Bridges_Beam_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
        DrawLegend(context, bounds);
    }
}
