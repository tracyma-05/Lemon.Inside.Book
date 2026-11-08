using Avalonia.Media.Immutable;
using Avalonia.Media;
using Avalonia;
using Avalonia.Data;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Gears;

/// <summary>
/// Spread 3: a string unwound from a can; its end draws an involute. Faintly behind, a gear whose tooth has that very
/// curve as its side, so the reader sees where tooth shapes come from. The reader drags the pencil to unwind or wind
/// the string; set from the slider or the walkthrough, the string unwinds smoothly to its new length, drawing as it goes.
/// </summary>
public sealed class InvoluteDiagram : GearPicture
{
    public static readonly StyledProperty<double> UnwoundProperty =
        AvaloniaProperty.Register<InvoluteDiagram, double>(nameof(Unwound), 70, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The ghost gear: its base circle is the can.</summary>
    private const int GhostTeeth = 12;

    private const double MaxUnwound = 150;

    /// <summary>The slider's top: the picture stops drawing at <see cref="MaxUnwound"/>, the value goes on.</summary>
    private const double MostUnwound = 240;

    private static readonly double Pressure = 20 * Math.PI / 180;
    private static readonly IImmutableBrush Can = new ImmutableSolidColorBrush(Color.FromRgb(0x3A, 0x46, 0x5C));
    private static readonly IPen CanPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x8F, 0xA3, 0xBF)), 1.5);
    private static readonly IPen StringPen = new ImmutablePen(DiagramText.Gold, 2.5, lineCap: PenLineCap.Round);
    private static readonly IPen CurvePen = new ImmutablePen(DiagramText.Cyan, 3, lineCap: PenLineCap.Round);
    private static readonly IPen FuturePen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x50, 0x6F, 0xD8, 0xFF)), 1.5, new ImmutableDashStyle([3, 4], 0));
    private static readonly IPen RightAnglePen = new ImmutablePen(DiagramText.Faint, 1.2);

    static InvoluteDiagram()
    {
        AffectsRender<InvoluteDiagram>(UnwoundProperty);
    }

    /// <summary>How much string has come off the can, as the angle it covered, in degrees.</summary>
    public double Unwound
    {
        get => GetValue(UnwoundProperty);
        set => SetValue(UnwoundProperty, value);
    }

    /// <summary>The string as drawn, in degrees: it unwinds toward <see cref="Unwound"/> rather than jumping; NaN until first drawn.</summary>
    private double _shown = double.NaN;

    protected override void Advance(double seconds)
    {
        var target = Math.Clamp(Unwound, 0, MaxUnwound);
        _shown = double.IsNaN(_shown) ? target : Glide(_shown, target, seconds, 0.1);
    }

    /// <summary>
    /// How far the string is unwound for a pencil at <paramref name="distance"/> from the can's middle (both in the
    /// can's radii): the string's end is √(1 + θ²) radii out, whatever the angle.
    /// </summary>
    internal static double UnwoundAt(double distance) =>
        Math.Clamp(Math.Sqrt(Math.Max(0, (distance * distance) - 1)) * 180 / Math.PI, 0, MostUnwound);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var baseRadius = GearGeometry.BaseRadius(GhostTeeth, Pressure);
        // Fit the gear and the whole curve, with room for the labels.
        var gear = GearGeometry.PitchRadius(GhostTeeth) + GearGeometry.Addendum;
        var curve = Enumerable.Range(0, 41).Select(i => GearGeometry.Unwind(baseRadius, MaxUnwound * Math.PI / 180 * i / 40).End).ToList();
        double minX = Math.Min(-gear, curve.Min(c => c.X)), maxX = Math.Max(gear, curve.Max(c => c.X));
        double minY = Math.Min(-gear, curve.Min(c => c.Y)), maxY = Math.Max(gear, curve.Max(c => c.Y));
        var scale = Math.Min(bounds.Width * 0.78 / (maxX - minX), bounds.Height * 0.78 / (maxY - minY));
        var centre = new Point(bounds.Center.X - ((minX + maxX) / 2 * scale), bounds.Center.Y + ((minY + maxY) / 2 * scale));

        // The ghost gear: tooth 0's clockwise side starts on the can where the string does.
        var tooth = GearGeometry.HalfToothAngle(GhostTeeth, Pressure, baseRadius, thinning: 0);
        var ghost = Shape("ghost", () => GearGeometry.Outline(GhostTeeth, Pressure, thinning: 0));
        using (context.PushTransform(Placement(centre, scale, tooth)))
        {
            context.DrawGeometry(new ImmutableSolidColorBrush(Color.FromArgb(0x22, 0xE6, 0xEA, 0xF2)),
                new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x55, 0xE6, 0xEA, 0xF2)), 1 / scale), ghost);
        }

        context.DrawEllipse(Can, CanPen, centre, baseRadius * scale, baseRadius * scale);

        // The whole curve the pencil will draw (dashed), and the part drawn so far.
        if (double.IsNaN(_shown))
        {
            _shown = Math.Clamp(Unwound, 0, MaxUnwound);
        }

        var unwound = _shown * Math.PI / 180;
        context.DrawGeometry(null, FuturePen, Curve(baseRadius, MaxUnwound * Math.PI / 180, centre, scale));
        if (unwound > 0.01)
        {
            context.DrawGeometry(null, CurvePen, Curve(baseRadius, unwound, centre, scale));
        }

        // The string: from where it leaves the can, straight to the pencil, square to the radius there.
        var (end, leaves) = GearGeometry.Unwind(baseRadius, unwound);
        var p = ToScreen(end, centre, scale);
        var t = ToScreen(leaves, centre, scale);
        context.DrawLine(StringPen, t, p);
        if (unwound > 0.25)
        {
            var along = (p - t) / Math.Max(1, Math.Sqrt(((p - t).X * (p - t).X) + ((p - t).Y * (p - t).Y)));
            var inward = (centre - t) / (baseRadius * scale);
            const double Corner = 9;
            var a = t + (along * Corner);
            var b = a + (inward * Corner);
            var c = t + (inward * Corner);
            context.DrawLine(RightAnglePen, a, b);
            context.DrawLine(RightAnglePen, b, c);
        }

        context.DrawEllipse(DiagramText.Gold, null, t, 4, 4);
        context.DrawEllipse(Brushes.White, new ImmutablePen(DiagramText.Cyan, 2), p, 6, 6);
        Mark("can", centre, baseRadius * scale);
        Mark("pencil", p, 10);
        Mark("string", [t, p], pad: 4);
        Mark("curve", Enumerable.Range(0, 21).Select(i => ToScreen(GearGeometry.Unwind(baseRadius, unwound * i / 20).End, centre, scale)), pad: 4);

        DiagramText.Draw(context, DiagramText.Get("Gears_Involute_Can"), new Point(centre.X, centre.Y), 12, DiagramText.Ink, 0.5, 0.5);
        if (unwound > 0.4)
        {
            var mid = new Point((t.X + p.X) / 2, (t.Y + p.Y) / 2);
            DiagramText.Draw(context, DiagramText.Get("Gears_Involute_String"), mid + new Point(8, -6), 12, DiagramText.Gold, 0, 1);
        }

        DiagramText.Draw(context, DiagramText.Get("Gears_Involute_Pencil"), p + new Point(20, 4), 12, DiagramText.Cyan);
        DiagramText.Draw(context, DiagramText.Get("Gears_Involute_Ghost"), new Point(bounds.X + 16, bounds.Y + 14), 12, DiagramText.Faint);

        // The pencil: pulled away from the can it unwinds more string, pushed back it winds some up. The drawing
        // follows the finger at once.
        Grip("pencil", p, GripAxis.Free, DiagramText.Get("Gears_Grip_Pencil"), at =>
        {
            var d = at - centre;
            Unwound = Math.Round(UnwoundAt(Math.Sqrt((d.X * d.X) + (d.Y * d.Y)) / (baseRadius * scale)));
            _shown = Math.Clamp(Unwound, 0, MaxUnwound);
        });
    }

    private static StreamGeometry Curve(double radius, double upTo, Point centre, double scale)
    {
        var geometry = new StreamGeometry();
        using var g = geometry.Open();
        var steps = Math.Max(2, (int)(upTo * 30));
        g.BeginFigure(ToScreen(GearGeometry.Unwind(radius, 0).End, centre, scale), false);
        for (var i = 1; i <= steps; i++)
        {
            g.LineTo(ToScreen(GearGeometry.Unwind(radius, upTo * i / steps).End, centre, scale));
        }

        g.EndFigure(false);
        return geometry;
    }
}
