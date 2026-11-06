using System.Numerics;
using Avalonia;
using Avalonia.Media;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// Spread 1: a small lamp over a floor, and a chart of how the light on the floor turns into how bright it looks.
/// Twice the light does not look twice as bright, and past a point the floor is simply white.
/// </summary>
public sealed class BrightnessScene : SceneView
{
    public static readonly StyledProperty<double> PowerProperty =
        AvaloniaProperty.Register<BrightnessScene, double>(nameof(Power), 30);

    /// <summary>How the light on screen is scaled before the screen's curve: 100 W under the lamp is about white.</summary>
    internal const float Exposure = 0.3f;

    private const float LampHeight = 1.6f;
    private const float LampSize = 0.16f;

    private static readonly IPen AxisPen = new Pen(DiagramText.Faint, 1);
    private static readonly IPen CurvePen = new Pen(DiagramText.Gold, 2);
    private static readonly IPen CordPen = new Pen(new SolidColorBrush(Color.FromRgb(0x70, 0x78, 0x88)), 1.5);
    private static readonly IBrush ClipBrush = new SolidColorBrush(Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF));
    private static readonly IBrush PanelBrush = new SolidColorBrush(Color.FromArgb(0xB0, 0x08, 0x0B, 0x12));

    private readonly AreaLight _lamp = new() { Center = new Vector3(0, LampHeight, 0), Size = LampSize };
    private readonly int _lampFirstVertex;
    private float _underLamp;

    static BrightnessScene()
    {
        PowerProperty.Changed.AddClassHandler<BrightnessScene>((s, _) => s.LightChanged());
    }

    public BrightnessScene()
    {
        Camera.Target = new Vector3(0, 0.5f, 0);
        Camera.Pitch = 24;
        Camera.Distance = 7;
        Mesh.AddQuad(new Vector3(-3, 0, 3), new Vector3(6, 0, 0), new Vector3(0, 0, -6), 40, new Vector3(0.75f));
        _lampFirstVertex = Mesh.VertexCount;
        var h = LampSize / 2;
        Mesh.AddQuad(new Vector3(-h, LampHeight, h), new Vector3(LampSize, 0, 0), new Vector3(0, 0, -LampSize), 1, Vector3.Zero, Vector3.One);
    }

    /// <summary>The lamp's power in watts.</summary>
    public double Power
    {
        get => GetValue(PowerProperty);
        set => SetValue(PowerProperty, value);
    }

    /// <summary>The light under the lamp after exposure (1 is the brightest the screen shows).</summary>
    internal double UnderLamp => _underLamp;

    protected override Vector3[] ComputeRadiance(bool coarse)
    {
        _lamp.Power = (float)Power;
        _lamp.Samples = coarse ? 2 : 3;
        var irradiance = _lamp.Irradiance(Mesh, []);
        var radiance = new Vector3[Mesh.VertexCount];
        for (var i = 0; i < radiance.Length; i++)
        {
            radiance[i] = (Mesh.Albedo[i] * irradiance[i] / MathF.PI) + Mesh.Emission[i];
        }

        for (var i = 0; i < 4; i++)
        {
            radiance[_lampFirstVertex + i] = new Vector3(10f);
        }

        // The floor's middle vertex (40 × 40 cells) is straight under the lamp.
        _underLamp = radiance[(20 * 41) + 20].X * Exposure;
        Raster.Exposure = Exposure;
        return radiance;
    }

    protected override void DrawOverlay(DrawingContext context, Rect bounds)
    {
        if (Project(new Vector3(0, LampHeight, 0), bounds) is { } lamp && Project(new Vector3(0, 4, 0), bounds) is { } ceiling)
        {
            context.DrawLine(CordPen, ceiling, lamp);
            DiagramText.Draw(context, DiagramText.Format("Light_Watts", Power.ToString("0")), new Point(lamp.X + 14, lamp.Y), 13, DiagramText.Gold, 0, 0.5, bold: true);
        }

        DrawChart(context, bounds);
    }

    /// <summary>Light in (across) against how bright it looks (up): the screen's curve, flat once it reaches white.</summary>
    private void DrawChart(DrawingContext context, Rect bounds)
    {
        const double maxLight = 1.6;
        var panel = new Rect(bounds.X + 12, bounds.Bottom - 170, Math.Min(250, bounds.Width - 24), 158);
        context.FillRectangle(PanelBrush, panel, 8);
        var plot = new Rect(panel.X + 34, panel.Y + 12, panel.Width - 48, panel.Height - 52);

        double X(double light) => plot.X + (plot.Width * light / maxLight);
        double Y(double looks) => plot.Bottom - (plot.Height * looks);

        context.FillRectangle(ClipBrush, new Rect(new Point(X(1), plot.Y), plot.BottomRight));
        DiagramText.Draw(context, DiagramText.Get("Light_Brightness_White"), new Point(X(1.3), plot.Y + 4), 10, DiagramText.Ink, 0.5, 0);
        context.DrawLine(AxisPen, plot.BottomLeft, plot.BottomRight);
        context.DrawLine(AxisPen, plot.BottomLeft, plot.TopLeft);

        var curve = new StreamGeometry();
        using (var g = curve.Open())
        {
            g.BeginFigure(new Point(X(0), Y(0)), false);
            for (var i = 1; i <= 80; i++)
            {
                var light = maxLight * i / 80;
                g.LineTo(new Point(X(light), Y(Looks(light))));
            }

            g.EndFigure(false);
        }

        context.DrawGeometry(null, CurvePen, curve);
        var dot = new Point(X(Math.Min(_underLamp, maxLight)), Y(Looks(_underLamp)));
        context.DrawEllipse(DiagramText.Cyan, null, dot, 5, 5);

        DiagramText.Draw(context, DiagramText.Get("Light_Brightness_LightIn"), new Point(plot.Center.X, plot.Bottom + 6), 10.5, DiagramText.Faint, 0.5, 0);
        DiagramText.Draw(context, DiagramText.Get("Light_Brightness_Looks"), new Point(plot.X + 6, plot.Y), 10.5, DiagramText.Faint);
        DiagramText.Draw(context, DiagramText.Get("Light_Brightness_UnderLamp"), new Point(panel.X + 12, panel.Bottom - 20), 10.5, DiagramText.Cyan, 0, 0);
    }

    /// <summary>How bright a light looks on screen (0 to 1): the sRGB curve, white from 1 up.</summary>
    internal static double Looks(double light)
    {
        var l = Math.Clamp(light, 0, 1);
        return l <= 0.0031308 ? 12.92 * l : (1.055 * Math.Pow(l, 1 / 2.4)) - 0.055;
    }
}
