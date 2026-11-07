using System.Numerics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// Spread 3, in 3D: a small lamp and the light that goes through one square a metre away. Further out the same
/// light covers a bigger frame: 4 squares at 2 m, 9 at 3 m, so each square gets less. Drag to look round.
/// </summary>
public sealed class DistanceScene : SceneView
{
    public static readonly StyledProperty<double> DistanceProperty =
        AvaloniaProperty.Register<DistanceScene, double>(nameof(Distance), 2);

    /// <summary>Side of the square the light passes through at 1 m.</summary>
    private const float Square = 0.5f;

    private static readonly Vector3 Lamp = new(-2.2f, 1.2f, 0);
    private static readonly IPen EdgePen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0xB0, 0xF2, 0xC9, 0x4C)), 1.2);
    private static readonly IPen GridPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0xE6, 0xEA, 0xF2)), 1);
    private static readonly IPen GhostPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x40, 0xE6, 0xEA, 0xF2)), 1, new ImmutableDashStyle([3, 4], 0));
    private static readonly IPen AxisPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x60, 0xE6, 0xEA, 0xF2)), 1);

    static DistanceScene()
    {
        AffectsRender<DistanceScene>(DistanceProperty);
    }

    public DistanceScene()
    {
        Camera.Target = new Vector3(0, 1.2f, 0);
        Camera.Yaw = 50;
        Camera.Pitch = 18;
        Camera.Distance = 7.5;
    }

    /// <summary>How far the frame is from the lamp, in metres (1 to 4).</summary>
    public double Distance
    {
        get => GetValue(DistanceProperty);
        set => SetValue(DistanceProperty, value);
    }

    protected override Vector3[] ComputeRadiance(bool coarse) => [];

    protected override void DrawOverlay(DrawingContext context, Rect bounds)
    {
        var d = (float)Math.Clamp(Distance, 0.5, 4.5);
        Point? P(Vector3 v) => Project(v, bounds);

        // The line along which the light travels, with a dashed frame at each whole metre.
        if (P(Lamp) is { } lamp && P(Lamp + new Vector3(4.6f, 0, 0)) is { } end)
        {
            context.DrawLine(AxisPen, lamp, end);
        }

        for (var m = 1; m <= 4; m++)
        {
            if (Math.Abs(m - d) > 0.05)
            {
                DrawFrame(context, bounds, m, GhostPen, null);
            }

            if (P(Lamp + new Vector3(m, -(m * Square / 2) - 0.12f, 0)) is { } tick)
            {
                DiagramText.Draw(context, DiagramText.Format("Light_Metres", m.ToString()), tick, 11, DiagramText.Faint, 0.5, 0);
            }
        }

        // The four edges of the light that goes through the first square, out to the frame.
        foreach (var corner in Corners(d))
        {
            if (P(Lamp) is { } from && P(corner) is { } to)
            {
                context.DrawLine(EdgePen, from, to);
            }
        }

        // The first square, gold; the frame at the chosen distance, as bright as the light that reaches each square.
        DrawFrame(context, bounds, 1, EdgePen, new SolidColorBrush(Color.FromRgb(0xF2, 0xC9, 0x4C), 0.55));
        Mark("square", Corners(1).Select(c => P(c)).OfType<Point>(), pad: 2);
        Mark("frame", Corners(d).Select(c => P(c)).OfType<Point>(), pad: 2);
        var share = 1 / (d * d);
        DrawFrame(context, bounds, d, GridPen, new SolidColorBrush(Color.FromRgb(0xF2, 0xC9, 0x4C), 0.1 + (0.6 * share)), grid: true);
        if (P(Lamp) is { } bulb)
        {
            context.DrawEllipse(new SolidColorBrush(Color.FromRgb(0xFF, 0xF4, 0xD0)), null, bulb, 7, 7);
            Mark("lamp", bulb, 10);
        }

        DiagramText.Draw(context,
            DiagramText.Format("Light_Distance_Readout", (d * d).ToString("0.0"), (share * 100).ToString("0")),
            new Point(bounds.X + 16, bounds.Bottom - 16), 13, DiagramText.Ink, 0, 1);
    }

    /// <summary>The corners of the light's frame <paramref name="metres"/> from the lamp.</summary>
    private static Vector3[] Corners(float metres)
    {
        var h = metres * Square / 2;
        var x = Lamp.X + metres;
        return
        [
            new(x, Lamp.Y - h, -h), new(x, Lamp.Y - h, h), new(x, Lamp.Y + h, h), new(x, Lamp.Y + h, -h),
        ];
    }

    private void DrawFrame(DrawingContext context, Rect bounds, float metres, IPen pen, IBrush? fill, bool grid = false)
    {
        var corners = Corners(metres).Select(c => Project(c, bounds)).ToArray();
        if (corners.Any(c => c is null))
        {
            return;
        }

        var shape = new StreamGeometry();
        using (var g = shape.Open())
        {
            g.BeginFigure(corners[0]!.Value, fill is not null);
            for (var i = 1; i < 4; i++)
            {
                g.LineTo(corners[i]!.Value);
            }

            g.EndFigure(true);
        }

        context.DrawGeometry(fill, pen, shape);
        if (!grid)
        {
            return;
        }

        // Squares the size of the first one, from the frame's lower corner.
        var c = Corners(metres);
        for (var s = Square; s < (metres * Square) - 0.01f; s += Square)
        {
            Line(c[0] + new Vector3(0, s, 0), c[1] + new Vector3(0, s, 0));
            Line(c[0] + new Vector3(0, 0, s), c[3] + new Vector3(0, 0, s));
        }

        void Line(Vector3 a, Vector3 b)
        {
            if (Project(a, bounds) is { } pa && Project(b, bounds) is { } pb)
            {
                context.DrawLine(GridPen, pa, pb);
            }
        }
    }
}
