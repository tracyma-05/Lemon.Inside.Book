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
/// Spread 3: a tank on a table, a hose from it up over a crest and down to a bucket on the floor. Full of water and
/// with its end below the tank's surface, the hose runs; full of air, or with its end raised above the surface, the
/// water in it only stands as high as the tank's own. Arrows show the air pressing on the tank's water.
/// </summary>
public sealed class SiphonDiagram : GearPicture
{
    public static readonly StyledProperty<bool> PrimedProperty =
        AvaloniaProperty.Register<SiphonDiagram, bool>(nameof(Primed), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> OutletProperty =
        AvaloniaProperty.Register<SiphonDiagram, double>(nameof(Outlet), 0.5, defaultBindingMode: BindingMode.TwoWay);

    private static readonly IImmutableBrush HoseWall = new ImmutableSolidColorBrush(Color.FromRgb(0x4E, 0x7A, 0x58));

    /// <summary>The hose's two legs, m from the picture's left edge.</summary>
    private const double InLeg = 1.35;
    private const double OutLeg = 2.85;

    static SiphonDiagram()
    {
        AffectsRender<SiphonDiagram>(PrimedProperty, OutletProperty);
    }

    public bool Primed
    {
        get => GetValue(PrimedProperty);
        set => SetValue(PrimedProperty, value);
    }

    /// <summary>The hose end's height above the floor, m.</summary>
    public double Outlet
    {
        get => GetValue(OutletProperty);
        set => SetValue(OutletProperty, value);
    }

    /// <summary>How far along the hose the water has got, 0..1: priming fills it from the tank end, unpriming lets it run back.</summary>
    private double _fill = double.NaN;

    /// <summary>The hose's end as drawn: it is lowered or raised smoothly (a grip's drag moves it at once).</summary>
    private double _outlet = double.NaN;

    private double Fill => double.IsNaN(_fill) ? _fill = Primed ? 1 : 0 : _fill;

    private double Shown => double.IsNaN(_outlet) ? _outlet = Math.Clamp(Outlet, 0, Siphon.Crest - 0.3) : _outlet;

    protected override void Advance(double seconds)
    {
        _fill = Toward(Fill, Primed ? 1 : 0, 0.8, seconds);
        _outlet = Glide(Shown, Math.Clamp(Outlet, 0, Siphon.Crest - 0.3), seconds, 0.2);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var outlet = Shown;
        var fill = Fill;
        var filling = fill is > 0 and < 1;
        var flows = Siphon.Flows(Primed, outlet) && !filling;
        var scale = Math.Min((bounds.Width - 40) / 5.2, (bounds.Height - 70) / 3.7);
        var v = new View(new Point(bounds.Center.X - (2.3 * scale), bounds.Bottom - 24), scale);
        var bore = Math.Max(5, 0.07 * scale);
        const double surface = Siphon.Surface;
        const double crest = Siphon.Crest;

        context.FillRectangle(Ground, new Rect(bounds.X, v.At(0, 0).Y, bounds.Width, bounds.Height));

        // The table and the tank on it.
        context.DrawRectangle(Wood, Edge, v.Box(0.15, 1.08, 2.05, 1.18));
        foreach (var x in new[] { 0.25, 1.85 }) context.DrawRectangle(Wood, Edge, v.Box(x, 0, x + 0.08, 1.08));
        var tank = v.Box(0.35, 1.18, 1.75, 2.35);
        context.FillRectangle(AirBrush, tank);
        context.FillRectangle(WaterBrush, v.Box(0.35, 1.18, 1.75, surface));
        context.DrawGeometry(null, new ImmutablePen(Porcelain, 3), Polyline([v.At(0.35, 2.35), v.At(0.35, 1.18), v.At(1.75, 1.18), v.At(1.75, 2.35)]));
        Mark("tank", tank);
        DrawAirPushing(context, v.At(0.45, surface), v.At(1.2, surface), 0.22 * scale, 3, null);
        DiagramText.Draw(context, DiagramText.Get("Plumbing_AirPushes"), v.At(1.25, 2.45), 11.5, AirArrow, 1, 1);

        // The bucket on the floor.
        var bucket = v.Box(2.45, 0, 3.25, 0.55);
        context.FillRectangle(AirBrush, bucket);
        context.FillRectangle(WaterBrush, v.Box(2.45, 0, 3.25, flows ? 0.3 : 0.12));
        context.DrawGeometry(null, new ImmutablePen(SteelBrush, 2.5), Polyline([v.At(2.45, 0.55), v.At(2.45, 0), v.At(3.25, 0), v.At(3.25, 0.55)]));

        // The hose: in the tank, up to the crest, over and down to its end.
        List<Point> hose =
        [
            v.At(InLeg, 1.3), v.At(InLeg, crest - 0.25), v.At(InLeg + 0.25, crest), v.At(OutLeg - 0.25, crest), v.At(OutLeg, crest - 0.25),
            v.At(OutLeg, outlet),
        ];
        DrawPipe(context, hose, bore, full: flows, HoseWall);
        if (flows)
        {
            var speed = Siphon.Speed(Primed, outlet);
            DrawFlow(context, hose, bore, Seconds * 22 * speed);
            var end = v.At(OutLeg, outlet);
            var floor = v.At(0, outlet > 0.3 ? 0.3 : 0).Y;
            if (floor > end.Y) context.FillRectangle(WaterBrush, new Rect(end.X - (bore / 2), end.Y, bore, floor - end.Y));
            DiagramText.Draw(context, DiagramText.Format("Plumbing_Speed", Number(speed)), v.At(OutLeg + 0.12, outlet + 0.55), 12.5,
                DiagramText.Ink, 0, 0.5, bold: true);
        }
        else
        {
            // Only the leg in the tank holds water, as high as the tank's own surface; while it is being primed, the
            // water creeps on along the hose (or runs back out of it).
            DrawPipe(context, [v.At(InLeg, 1.3), v.At(InLeg, surface)], bore, full: true, HoseWall);
            if (filling) DrawPipe(context, Part(hose, fill), bore, full: true, HoseWall);
            DiagramText.Draw(context, DiagramText.Get(Primed ? "Plumbing_Siphon_StoppedLabel" : "Plumbing_Siphon_AirLabel"),
                v.At((InLeg + OutLeg) / 2, crest + 0.12), 12, DiagramText.Faint, 0.5, 1, bold: true);
        }

        Mark("hose", hose, 7);
        Mark("crest", v.At((InLeg + OutLeg) / 2, crest), 0.3 * scale);
        Mark("outlet", v.At(OutLeg, outlet), Math.Max(12, 0.15 * scale));

        // The tank's surface carried across, and the drop from it to the hose's end.
        var lineEnd = v.At(3.9, surface);
        context.DrawLine(new ImmutablePen(DiagramText.Cyan, 1.5, new ImmutableDashStyle([5, 4], 0)), v.At(1.75, surface), lineEnd);
        DiagramText.Draw(context, DiagramText.Get("Plumbing_Siphon_Surface"), new Point(lineEnd.X + 4, lineEnd.Y), 11.5, DiagramText.Cyan, 0, 0.5);
        if (Math.Abs(surface - outlet) > 0.05)
        {
            var a = v.At(3.6, surface);
            var b = v.At(3.6, outlet);
            var brush = flows ? DiagramText.Gold : DiagramText.Faint;
            context.DrawLine(new ImmutablePen(brush, 1.5), a, b);
            DiagramText.DrawArrowAlong(context, a, b, brush, 8);
            context.DrawLine(new ImmutablePen(brush, 1, new ImmutableDashStyle([3, 3], 0)), b, v.At(OutLeg + 0.1, outlet));
            DiagramText.Draw(context, DiagramText.Format(outlet < surface ? "Plumbing_Siphon_Below" : "Plumbing_Siphon_Above", Number(Math.Abs(surface - outlet), "0.0#")),
                new Point(a.X + 8, (a.Y + b.Y) / 2), 12.5, brush, 0, 0.5, bold: true);
        }

        DiagramText.Draw(context, DiagramText.Get("Plumbing_Siphon_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);

        // The reader's handles: the crest tapped to fill the hose (or let air in), and the hose's end raised or lowered.
        Grip("prime", v.At((InLeg + OutLeg) / 2, crest), GripAxis.Tap, DiagramText.Get("Plumbing_Grip_Prime"), _ => Primed = !Primed);
        Grip("outlet", v.At(OutLeg, outlet), GripAxis.Vertical, DiagramText.Get("Plumbing_Grip_Outlet"), at =>
        {
            Outlet = Snap(v.Y(at), 0.05, 0, 3);
            _outlet = Math.Clamp(Outlet, 0, Siphon.Crest - 0.3);
        });
    }
}
