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
/// Spread 5: a suspension bridge from the side. The main cable hangs in a parabola between two towers, the deck hung
/// from it by hangers, its ends running back down to anchor blocks in the ground. Without the anchors the cable's pull
/// drags the tower tops inward. The cable's pull is written on it. The cable eases to a new sag and the towers bend
/// in or straighten as the anchors are let go or tied back, swaying a little before they settle. The reader drags the
/// cable's lowest point up or down and taps an anchor block to let the cable go or tie it back.
/// </summary>
public sealed class CableDiagram : GearPicture
{
    public static readonly StyledProperty<double> SagProperty =
        AvaloniaProperty.Register<CableDiagram, double>(nameof(Sag), 20, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> AnchoredProperty =
        AvaloniaProperty.Register<CableDiagram, bool>(nameof(Anchored), true, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The towers' tops above the deck, m; where the anchors are, m from the middle; hangers apart, m.</summary>
    private const double TowerTop = 62;
    private const double AnchorX = 135;
    private const double HangerGap = 12.5;

    /// <summary>How far the tower tops are pulled in with nothing holding the cable back, m.</summary>
    private const double MostPullIn = 6;

    /// <summary>The drawn sag, m, and how far the tower tops are drawn pulled in, m; null until the first frame.</summary>
    private double _sag = double.NaN;
    private Bounce? _pullIn;

    static CableDiagram()
    {
        AffectsRender<CableDiagram>(SagProperty, AnchoredProperty);
    }

    /// <summary>How far the cable's middle hangs below the tower tops, m.</summary>
    public double Sag
    {
        get => GetValue(SagProperty);
        set => SetValue(SagProperty, value);
    }

    public bool Anchored
    {
        get => GetValue(AnchoredProperty);
        set => SetValue(AnchoredProperty, value);
    }

    protected override void Advance(double seconds)
    {
        Start();
        _sag = Glide(_sag, Math.Clamp(Sag, 5, 50), seconds, 0.15);
        _pullIn = _pullIn!.Value.Step(Anchored ? 0 : MostPullIn, seconds, 5, 0.25);
    }

    private void Start()
    {
        if (double.IsNaN(_sag)) _sag = Math.Clamp(Sag, 5, 50);
        _pullIn ??= Bounce.At(Anchored ? 0 : MostPullIn);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        Start();
        var sag = _sag;
        var anchored = Anchored;
        var scale = Math.Min((bounds.Width - 40) / 295, (bounds.Height - 150) / 95);
        var v = new View(new Point(bounds.Center.X, bounds.Center.Y + (25 * scale)), scale);
        const double half = Cable.Span / 2;

        // Water and the banks.
        context.FillRectangle(River, new Rect(bounds.X, v.At(0, -12).Y, bounds.Width, 6 * scale));
        foreach (var side in new[] { -1, 1 })
        {
            var x0 = side * (half + 8);
            var x1 = side * 170;
            DrawPolygon(context, Ground, null, [v.At(x0, -2), v.At(x1, -2), v.At(x1, -30), v.At(x0, -30)]);
        }

        // Unanchored, the tops are pulled in and the cable hangs lower.
        var pullIn = _pullIn!.Value.Value;
        var topX = half - pullIn;
        var lowSag = sag;
        double CableY(double x) => TowerTop - lowSag + Cable.Height(x * half / topX, lowSag);

        // The deck, and the hangers from the cable down to it.
        var deck = v.Box(-AnchorX - 5, -1.2, AnchorX + 5, 0);
        context.DrawRectangle(Concrete, Edge, deck);
        var hangerPen = new ImmutablePen(Idle, 1.2);
        var hangers = new List<Point>();
        for (var x = -half + HangerGap; x < half - 1; x += HangerGap)
        {
            if (Math.Abs(x) > topX - 1) continue;
            var a = v.At(x, CableY(x));
            var b = v.At(x, 0);
            context.DrawLine(hangerPen, a, b);
            hangers.AddRange([a, b]);
        }

        Mark("hangers", hangers, 4);

        // The towers, leaning in when nothing holds the cable back.
        var towers = new List<Point>();
        var towerPen = new ImmutablePen(Concrete, Math.Max(6, 4.5 * scale), null, PenLineCap.Square);
        foreach (var side in new[] { -1, 1 })
        {
            var foot = v.At(side * half, -12);
            var top = v.At(side * topX, TowerTop);
            context.DrawLine(towerPen, foot, top);
            towers.AddRange([foot, top]);
        }

        Mark("towers", towers, 8);

        // The main cable: the parabola between the towers, and the back stays down to the anchors.
        var cable = new List<Point>();
        for (var i = 0; i <= 60; i++)
        {
            var x = -topX + (2 * topX * i / 60);
            cable.Add(v.At(x, CableY(x)));
        }

        var cablePen = new ImmutablePen(StretchBrush, 3.5, null, PenLineCap.Round, PenLineJoin.Round);
        var anchors = new List<Point>();
        foreach (var side in new[] { -1, 1 })
        {
            var block = v.Box((side * AnchorX) - 8, -6, (side * AnchorX) + 8, 4);
            anchors.AddRange([block.TopLeft, block.BottomRight]);
            if (anchored)
            {
                context.DrawRectangle(StoneDark, Edge, block);
                context.DrawLine(cablePen, v.At(side * topX, TowerTop), v.At(side * AnchorX, 3));
            }
            else
            {
                context.DrawRectangle(null, new ImmutablePen(DiagramText.Faint, 1.2, new ImmutableDashStyle([4, 4], 0)), block);
                DrawPush(context, v.At(side * (topX + 2), TowerTop + 4), v.At(side * (topX - 16), TowerTop + 4), SqueezeBrush, string.Empty, 0.5, 1);
            }
        }

        Mark("anchors", anchors, 4);
        context.DrawGeometry(null, cablePen, Plumbing.PlumbingParts.Polyline(cable));
        Mark("cable", cable, 6);

        // The sag and the pull.
        var lowest = v.At(0, CableY(0));
        var topLine = v.At(0, TowerTop).Y;
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 1, new ImmutableDashStyle([4, 4], 0)), v.At(-topX, TowerTop), v.At(topX, TowerTop));
        context.DrawLine(new ImmutablePen(DiagramText.Gold, 1.5), new Point(lowest.X, topLine), lowest);
        DiagramText.DrawArrowAlong(context, new Point(lowest.X, topLine), lowest, DiagramText.Gold, 7);
        DiagramText.Draw(context, DiagramText.Format("Bridges_Cable_SagLabel", Number(lowSag, "0")), new Point(lowest.X + 20, (topLine + lowest.Y) / 2), 12.5,
            DiagramText.Gold, 0, 0.5, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Bridges_Cable_PullLabel", Tonnes(Cable.Pull(lowSag))), v.At(-half * 0.55, CableY(-half * 0.55) + 3), 12.5,
            StretchBrush, 0.5, 1, bold: true);
        if (!anchored)
        {
            DiagramText.Draw(context, DiagramText.Get("Bridges_Cable_PulledIn"), v.At(0, TowerTop + 12), 13, SqueezeBrush, 0.5, 1, bold: true);
        }

        DiagramText.Draw(context, DiagramText.Get("Bridges_Cable_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
        DrawLegend(context, bounds);

        // The reader's handles: the cable's lowest point, dragged up or down, and the right anchor block, tapped.
        Grip("sag", lowest, GripAxis.Vertical, DiagramText.Get("Bridges_Grip_Sag"),
            at => Sag = Math.Clamp(Math.Round((TowerTop - v.Y(at)) * 2) / 2, 5, 50));
        Grip("anchor", v.At(AnchorX, -1), GripAxis.Tap, DiagramText.Get(anchored ? "Bridges_Grip_AnchorOff" : "Bridges_Grip_AnchorOn"),
            _ => Anchored = !Anchored);
    }
}
