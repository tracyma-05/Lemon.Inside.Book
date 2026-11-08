using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;
using static Lemon.Inside.Book.Chapters.Weighing.WeighingParts;
using Bounce = Lemon.Inside.Book.Chapters.Bridges.Bounce;

namespace Lemon.Inside.Book.Chapters.Weighing;

/// <summary>
/// Spread 4: a kitchen scale from the side, in centimetres. On a single arm the pan's load turns the arm about the
/// post, and the spring under the arm's short end feels more the farther out the load sits. On two parallel links the
/// pan's column can only move straight down, the links take the turning, and the spring under the column feels only the
/// load's weight. The load slides across the pan; the spring gives a little under it, and the dial's needle swings to
/// its new reading and wobbles to rest like a real kitchen scale's. The reader drags the load across the pan and taps
/// the post to fit the second link or take it out.
/// </summary>
public sealed class RobervalDiagram : GearPicture
{
    public static readonly StyledProperty<double> OffsetProperty =
        AvaloniaProperty.Register<RobervalDiagram, double>(nameof(Offset), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> ParallelProperty =
        AvaloniaProperty.Register<RobervalDiagram, bool>(nameof(Parallel), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The pan's height and half its width, cm.</summary>
    private const double PanY = 18;
    private const double PanHalf = 8;

    /// <summary>How far the pan sinks for the whole reading of the dial (1 kg), cm.</summary>
    private const double Give = 1.6;

    /// <summary>Where the load is drawn, cm from the pan's middle, sliding toward where it was put; NaN until the first frame.</summary>
    private double _offset = double.NaN;

    /// <summary>The reading the spring and the dial show, g, swinging toward the scale's reading.</summary>
    private Bounce? _reading;

    static RobervalDiagram()
    {
        AffectsRender<RobervalDiagram>(OffsetProperty, ParallelProperty);
    }

    /// <summary>How far from the pan's middle the load sits, cm.</summary>
    public double Offset
    {
        get => GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    public bool Parallel
    {
        get => GetValue(ParallelProperty);
        set => SetValue(ParallelProperty, value);
    }

    /// <summary>The load slides at 20 cm a second; the spring and its needle follow, overshooting a little and settling.</summary>
    protected override void Advance(double seconds)
    {
        Start();
        _offset = Lifting.LiftingParts.Ease(_offset, Math.Clamp(Offset, -6, 6), 20, seconds);
        _reading = _reading!.Value.Step(Roberval.Reading(_offset, Parallel), seconds, 9, 0.2);
    }

    private void Start()
    {
        if (double.IsNaN(_offset)) _offset = Math.Clamp(Offset, -6, 6);
        _reading ??= Bounce.At(Roberval.Reading(_offset, Parallel));
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        Start();
        var offset = _offset;
        var parallel = Parallel;
        var reading = Math.Max(0, _reading!.Value.Value);
        var sink = reading / 1000 * Give;
        var scale = Math.Min((bounds.Width - 60) / 46, (bounds.Height - 140) / 30);
        var v = new View(new Point(bounds.Center.X + (1 * scale), bounds.Center.Y + (12 * scale)), scale);
        const double x = Roberval.Arm;

        context.DrawRectangle(Wood, Edge, v.Box(-24, -1.2, 21, 0), 3, 3);
        var post = v.Box(-1, 0, 1, 16);
        context.DrawRectangle(Iron, Edge, post);
        Mark("post", post);

        var link = new ImmutablePen(SteelBrush, Math.Max(4, 0.9 * scale), null, PenLineCap.Round);
        List<Point> links;
        if (parallel)
        {
            // Two links of equal length, pinned at both ends: the column between their far ends stays upright.
            links = [v.At(0, 14), v.At(x, 14 - sink), v.At(0, 7), v.At(x, 7 - sink)];
            context.DrawLine(link, links[0], links[1]);
            context.DrawLine(link, links[2], links[3]);
            context.DrawRectangle(SteelBrush, Edge, v.Box(x - 0.8, 5 - sink, x + 0.8, PanY - sink));
            foreach (var p in links) context.DrawEllipse(Dark, new ImmutablePen(DiagramText.Ink, 1), p, 3, 3);
            DrawCoil(context, v.At(x, 5 - sink), v.At(x, 0), 2.4 * scale, 5, SteelBrush);
            DiagramText.Draw(context, DiagramText.Get("Weighing_Roberval_TakesTurning"), v.At(x / 2, 10.5), 11, DiagramText.Faint, 0.5, 0.5);
        }
        else
        {
            // One arm through the post: the pan on its long end, a spring under its short end.
            // The arm turns about the post: the pan's end goes down, the short end up, stretching the spring.
            links = [v.At(-6, 12 + (sink * 6 / x)), v.At(x, 12 - sink)];
            context.DrawLine(link, links[0], links[1]);
            context.DrawEllipse(Dark, new ImmutablePen(DiagramText.Ink, 1), v.At(0, 12), 3, 3);
            context.DrawRectangle(SteelBrush, Edge, v.Box(x - 0.8, 12 - sink, x + 0.8, PanY - sink));
            DrawCoil(context, links[0], v.At(-6, 0), 2.4 * scale, 7, SteelBrush);
        }

        Mark("links", links, 6);

        // The pan and the load on it.
        var panY = PanY - sink;
        var pan = new Rect(v.At(x - PanHalf, panY + 0.8), v.At(x + PanHalf, panY));
        context.DrawRectangle(Brass, Edge, pan, 3, 3);
        Mark("pan", pan);
        var load = v.Box(x + offset - 2.2, panY + 0.8, x + offset + 2.2, panY + 5);
        context.DrawRectangle(Load, Edge, load, 4, 4);
        DiagramText.Draw(context, Grams(Roberval.Load), load.Center, 11, Dark, 0.5, 0.5, bold: true);
        Mark("load", load);
        var mid = v.At(x, panY + 0.8);
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 1, new ImmutableDashStyle([3, 3], 0)), mid, new Point(mid.X, mid.Y - (7 * scale)));

        // The dial.
        DrawDial(context, v.At(-17, 9), 5.5 * scale, reading / 1000, Grams(reading));
        DiagramText.Draw(context, DiagramText.Get(parallel ? "Weighing_Roberval_ParallelLabel" : "Weighing_Roberval_ArmLabel"),
            v.At(-17, 16.5), 12, Math.Abs(Roberval.Reading(Math.Clamp(Offset, -6, 6), parallel) - Roberval.Load) < 0.5 ? DiagramText.Gold : Warn, 0.5, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Weighing_Roberval_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);

        // The reader's handles: the load, dragged across the pan, and the post, tapped to fit the second link or take it out.
        Grip("offset", new Point(load.Center.X, load.Top), GripAxis.Horizontal, DiagramText.Get("Weighing_Grip_Offset"),
            at => Offset = Math.Clamp(Math.Round((v.X(at) - x) * 4) / 4, -6, 6));
        Grip("parallel", v.At(0, 3), GripAxis.Tap, DiagramText.Get(parallel ? "Weighing_Grip_OneArm" : "Weighing_Grip_TwoLinks"),
            _ => Parallel = !Parallel);
    }
}
