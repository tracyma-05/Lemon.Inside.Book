using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;
using static Lemon.Inside.Book.Chapters.Weighing.WeighingParts;

namespace Lemon.Inside.Book.Chapters.Weighing;

/// <summary>
/// Spread 4: a kitchen scale from the side, in centimetres. On a single arm the pan's load turns the arm about the
/// post, and the spring under the arm's short end feels more the farther out the load sits. On two parallel links the
/// pan's column can only move straight down, the links take the turning, and the spring under the column feels only the
/// load's weight.
/// </summary>
public sealed class RobervalDiagram : GearPicture
{
    public static readonly StyledProperty<double> OffsetProperty =
        AvaloniaProperty.Register<RobervalDiagram, double>(nameof(Offset));

    public static readonly StyledProperty<bool> ParallelProperty =
        AvaloniaProperty.Register<RobervalDiagram, bool>(nameof(Parallel));

    /// <summary>The pan's height and half its width, cm.</summary>
    private const double PanY = 18;
    private const double PanHalf = 8;

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

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var offset = Math.Clamp(Offset, -6, 6);
        var parallel = Parallel;
        var reading = Roberval.Reading(offset, parallel);
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
            links = [v.At(0, 14), v.At(x, 14), v.At(0, 7), v.At(x, 7)];
            context.DrawLine(link, links[0], links[1]);
            context.DrawLine(link, links[2], links[3]);
            context.DrawRectangle(SteelBrush, Edge, v.Box(x - 0.8, 5, x + 0.8, PanY));
            foreach (var p in links) context.DrawEllipse(Dark, new ImmutablePen(DiagramText.Ink, 1), p, 3, 3);
            DrawCoil(context, v.At(x, 5), v.At(x, 0), 2.4 * scale, 5, SteelBrush);
            DiagramText.Draw(context, DiagramText.Get("Weighing_Roberval_TakesTurning"), v.At(x / 2, 10.5), 11, DiagramText.Faint, 0.5, 0.5);
        }
        else
        {
            // One arm through the post: the pan on its long end, a spring under its short end.
            links = [v.At(-6, 12), v.At(x, 12)];
            context.DrawLine(link, links[0], links[1]);
            context.DrawEllipse(Dark, new ImmutablePen(DiagramText.Ink, 1), v.At(0, 12), 3, 3);
            context.DrawRectangle(SteelBrush, Edge, v.Box(x - 0.8, 12, x + 0.8, PanY));
            DrawCoil(context, v.At(-6, 12), v.At(-6, 0), 2.4 * scale, 7, SteelBrush);
        }

        Mark("links", links, 6);

        // The pan and the load on it.
        var pan = new Rect(v.At(x - PanHalf, PanY + 0.8), v.At(x + PanHalf, PanY));
        context.DrawRectangle(Brass, Edge, pan, 3, 3);
        Mark("pan", pan);
        var load = v.Box(x + offset - 2.2, PanY + 0.8, x + offset + 2.2, PanY + 5);
        context.DrawRectangle(Load, Edge, load, 4, 4);
        DiagramText.Draw(context, Grams(Roberval.Load), load.Center, 11, Dark, 0.5, 0.5, bold: true);
        Mark("load", load);
        var mid = v.At(x, PanY + 0.8);
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 1, new ImmutableDashStyle([3, 3], 0)), mid, new Point(mid.X, mid.Y - (7 * scale)));

        // The dial.
        DrawDial(context, v.At(-17, 9), 5.5 * scale, reading / 1000, Grams(reading));
        DiagramText.Draw(context, DiagramText.Get(parallel ? "Weighing_Roberval_ParallelLabel" : "Weighing_Roberval_ArmLabel"),
            v.At(-17, 16.5), 12, Math.Abs(reading - Roberval.Load) < 0.5 ? DiagramText.Gold : Warn, 0.5, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Weighing_Roberval_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
