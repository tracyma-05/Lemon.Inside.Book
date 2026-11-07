using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Friction.FrictionParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Friction;

/// <summary>
/// Spread 6: two metal surfaces magnified until their bumps show, the top one sliding to the right. Dry, they press
/// together and touch at their peaks (marked in red); oiled, a film between them thickens with speed until the bumps
/// no longer meet. Below, friction against speed for the oiled pair, with the present speed marked.
/// </summary>
public sealed class OilDiagram : GearPicture
{
    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<OilDiagram, double>(nameof(Speed), 50);

    public static readonly StyledProperty<bool> OiledProperty =
        AvaloniaProperty.Register<OilDiagram, bool>(nameof(Oiled));

    static OilDiagram()
    {
        AffectsRender<OilDiagram>(SpeedProperty, OiledProperty);
    }

    /// <summary>The top surface's speed, % of full speed.</summary>
    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    public bool Oiled
    {
        get => GetValue(OiledProperty);
        set => SetValue(OiledProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var speed = Math.Clamp(Speed, 0, 100);
        var oiled = Oiled;
        DrawSurfaces(context, bounds, speed, oiled);
        DrawChart(context, bounds, speed, oiled);
        DiagramText.Draw(context, DiagramText.Get("Friction_Oil_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }

    /// <summary>The bumps of a surface, 0..1, at <paramref name="x"/> pixels.</summary>
    private static double Bump(double x, double seed) =>
        0.5 + (0.25 * Math.Sin((x * 0.045) + seed)) + (0.17 * Math.Sin((x * 0.11) + (seed * 2.3))) + (0.08 * Math.Sin((x * 0.27) + (seed * 3.1)));

    private void DrawSurfaces(DrawingContext context, Rect bounds, double speed, bool oiled)
    {
        var box = new Rect(bounds.X + 24, bounds.Y + 48, bounds.Width - 48, bounds.Height * 0.4);
        context.DrawRectangle(Dark, new ImmutablePen(DiagramText.Faint, 1), box, 6, 6);
        Mark("surfaces", box);
        DiagramText.Draw(context, DiagramText.Get("Friction_Oil_Magnified"), new Point(box.X + 10, box.Y + 8), 11, DiagramText.Faint);

        // One micrometre to this many pixels; bumps one micrometre tall; the film between the surfaces' middles.
        var unit = box.Height * 0.12;
        var film = oiled ? Lubrication.Film(speed) : Lubrication.Roughness * 0.6;
        var baseY = box.Bottom - (box.Height * 0.22);
        var shift = Seconds * speed * 0.8;
        var x0 = box.X + 2;
        var x1 = box.Right - 2;
        var bottom = new List<Point>();
        var top = new List<Point>();
        var contacts = new List<Point>();
        for (var x = x0; x <= x1; x += 2)
        {
            var yb = baseY - (Bump(x - x0, 1.3) * unit * Lubrication.Roughness);
            var yt = baseY - (2 * film * unit) + (Bump(x - x0 + shift, 4.7) * unit * Lubrication.Roughness); // the gap is 2 × film − both bumps: they meet only when the film is thinner
            bottom.Add(new Point(x, yb));
            top.Add(new Point(x, yt));
            if (yt > yb - 1) contacts.Add(new Point(x, (yb + yt) / 2));
        }

        // Oil fills the gap; the metals are drawn over it.
        if (oiled)
        {
            DrawPolygon(context, OilBrush, null, [.. top, .. Enumerable.Reverse(bottom)]);
        }

        DrawPolygon(context, Metal, Edge, [.. bottom, new Point(x1, baseY + (box.Height * 0.18)), new Point(x0, baseY + (box.Height * 0.18))]);
        var topEdge = box.Y + 30;
        DrawPolygon(context, new ImmutableSolidColorBrush(Color.FromRgb(0xB0, 0xBC, 0xCC)), Edge, [.. top, new Point(x1, topEdge), new Point(x0, topEdge)]);
        foreach (var c in contacts.Where((_, i) => i % 3 == 0)) context.DrawEllipse(GripBrush, null, c, 3.5, 3.5);
        Mark("bumps", new Rect(x0, baseY - (unit * 2 * Math.Max(film, Lubrication.Roughness)), x1 - x0, (unit * 2 * Math.Max(film, Lubrication.Roughness)) + 4));
        if (oiled)
        {
            var middle = (int)(top.Count * 0.7);
            Mark("oil", new Rect(top[middle].X - 30, top[middle].Y, 60, Math.Max(8, bottom[middle].Y - top[middle].Y)));
        }
        else
        {
            Mark("oil", new Rect(x1 - 80, baseY - (unit * 2), 70, unit));
        }

        if (speed > 0.5)
        {
            Arrow(context, new Point(box.Center.X - 50, topEdge + 18), new Point(box.Center.X + 50, topEdge + 18), PullBrush, string.Empty, 0.5, 1);
        }

        var label = !oiled ? DiagramText.Get("Friction_Oil_DryLabel")
            : contacts.Count > 0 ? DiagramText.Format("Friction_Oil_TouchLabel", Number(Lubrication.Film(speed), "0.0"))
            : DiagramText.Format("Friction_Oil_FloatLabel", Number(Lubrication.Film(speed), "0.0"));
        DiagramText.Draw(context, label, new Point(box.Center.X, box.Bottom + 8), 12.5, contacts.Count > 0 ? GripBrush : DiagramText.Gold, 0.5, 0, bold: true);
    }

    private void DrawChart(DrawingContext context, Rect bounds, double speed, bool oiled)
    {
        var box = new Rect(bounds.X + 24, bounds.Y + (bounds.Height * 0.58), bounds.Width - 48, bounds.Height * 0.4 - 60);
        var plot = DrawChartFrame(context, box, DiagramText.Get("Friction_Oil_ChartSpeed"), DiagramText.Get("Friction_Oil_ChartFriction"));
        const double top = 0.12;
        Point P(double s, double f) => new(plot.X + (s / 100 * plot.Width), plot.Bottom - (Math.Min(f, top) / top * plot.Height));
        var curve = Enumerable.Range(0, 101).Select(s => P(s, Lubrication.Friction(s, true))).ToList();
        context.DrawGeometry(null, new ImmutablePen(OilBrush, 2.5), Plumbing.PlumbingParts.Polyline(curve));
        foreach (var f in new[] { 0.05, 0.1 })
        {
            DiagramText.Draw(context, Number(f * 100, "0") + "%", new Point(plot.X - 6, P(0, f).Y), 10.5, DiagramText.Faint, 1, 0.5);
        }

        var thick = Enumerable.Range(0, 101).First(s => Lubrication.Film(s) >= Lubrication.Roughness);
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 1, new ImmutableDashStyle([3, 3], 0)), P(thick, 0), new Point(P(thick, 0).X, plot.Top));
        DiagramText.Draw(context, DiagramText.Get("Friction_Oil_FloatsHere"), new Point(P(thick, 0).X + 4, plot.Top + 2), 10.5, DiagramText.Faint, 0, 0);
        if (oiled)
        {
            context.DrawEllipse(DiagramText.Gold, Edge, P(speed, Lubrication.Friction(speed, true)), 6, 6);
        }
        else
        {
            DiagramText.Draw(context, DiagramText.Format("Friction_Oil_OffChart", Number(Lubrication.Dry * 100, "0")), new Point(plot.Right - 4, plot.Top + 2), 11.5, GripBrush, 1, 0, bold: true);
        }

        Mark("chart", box);
    }
}
