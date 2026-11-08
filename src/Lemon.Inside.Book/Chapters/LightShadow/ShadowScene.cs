using System.Numerics;
using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// Spread 5: a square lamp over a floor with a box on it. The lamp's height, size and place change the box's
/// shadow: a small lamp gives a sharp one, a big one a soft one. The lamp has three grips: itself, to slide it
/// sideways; a hanger on its cord, to raise or lower it; and a tab off its edge, to make it bigger or smaller. Every
/// change means working the light out again, so the scene does not move by itself (the light is worked out coarsely
/// while a grip moves and in full once it stops, as for the sliders).
/// </summary>
public sealed class ShadowScene : SceneView
{
    public static readonly StyledProperty<double> LightHeightProperty =
        AvaloniaProperty.Register<ShadowScene, double>(nameof(LightHeight), 2.5, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> LightSizeProperty =
        AvaloniaProperty.Register<ShadowScene, double>(nameof(LightSize), 0.5, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> LightXProperty =
        AvaloniaProperty.Register<ShadowScene, double>(nameof(LightX), 0.6, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Samples along each side of the lamp: fewer while a slider moves, more once it stops.</summary>
    internal const int CoarseSamples = 4;
    internal const int FullSamples = 10;

    internal static readonly Box Block = new(new Vector3(-0.4f, 0, -0.4f), new Vector3(0.4f, 0.8f, 0.4f));

    /// <summary>The sliders' ranges and steps, in metres.</summary>
    private const double LowestLamp = 1.2;
    private const double HighestLamp = 4;
    private const double SmallestLamp = 0.05;
    private const double BiggestLamp = 2;
    private const double FurthestAside = 2;
    private const double SliderStep = 0.05;

    /// <summary>How far above the lamp its cord's hanger is, and how far its size tab stands off its edge, in metres.</summary>
    private const float HangerAbove = 0.45f;
    private const float TabOut = 0.32f;

    private static readonly IPen CordPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0xC0, 0xB8, 0xC0, 0xD0)), 1.5);

    private readonly AreaLight _lamp = new();
    private readonly int _lampFirstVertex;

    static ShadowScene()
    {
        LightHeightProperty.Changed.AddClassHandler<ShadowScene>((s, _) => s.LightChanged());
        LightSizeProperty.Changed.AddClassHandler<ShadowScene>((s, _) => s.LightChanged());
        LightXProperty.Changed.AddClassHandler<ShadowScene>((s, _) => s.LightChanged());
    }

    public ShadowScene()
    {
        Mesh.AddQuad(new Vector3(-3, 0, 3), new Vector3(6, 0, 0), new Vector3(0, 0, -6), 64, new Vector3(0.78f, 0.76f, 0.72f));
        Mesh.AddBox(Block, 8, new Vector3(0.85f, 0.55f, 0.3f));
        _lampFirstVertex = Mesh.VertexCount;
        // The lamp, a glowing square; its corners are put where the lamp is each time the light changes.
        Mesh.AddQuad(Vector3.Zero, Vector3.UnitX, -Vector3.UnitZ, 1, Vector3.Zero, Vector3.One);
    }

    /// <summary>Height of the lamp above the floor, in metres.</summary>
    public double LightHeight
    {
        get => GetValue(LightHeightProperty);
        set => SetValue(LightHeightProperty, value);
    }

    /// <summary>Side of the square lamp, in metres.</summary>
    public double LightSize
    {
        get => GetValue(LightSizeProperty);
        set => SetValue(LightSizeProperty, value);
    }

    /// <summary>How far the lamp is to the side of the box, in metres.</summary>
    public double LightX
    {
        get => GetValue(LightXProperty);
        set => SetValue(LightXProperty, value);
    }

    protected override Vector3[] ComputeRadiance(bool coarse)
    {
        _lamp.Center = new Vector3((float)LightX, (float)LightHeight, 0);
        _lamp.Size = (float)LightSize;
        _lamp.Samples = coarse ? CoarseSamples : FullSamples;
        var irradiance = _lamp.Irradiance(Mesh, [Block]);

        var radiance = new Vector3[Mesh.VertexCount];
        for (var i = 0; i < radiance.Length; i++)
        {
            // A matte surface sends out the light it gets, times its colour, spread over the half-sphere (1/π).
            radiance[i] = (Mesh.Albedo[i] * irradiance[i] / MathF.PI) + Mesh.Emission[i];
        }

        var half = _lamp.Size / 2;
        Vector3[] corners =
        [
            _lamp.Center + new Vector3(-half, 0, half), _lamp.Center + new Vector3(half, 0, half),
            _lamp.Center + new Vector3(-half, 0, -half), _lamp.Center + new Vector3(half, 0, -half),
        ];
        for (var i = 0; i < 4; i++)
        {
            Mesh.SetPosition(_lampFirstVertex + i, corners[i]);
            radiance[_lampFirstVertex + i] = new Vector3(4f);
        }

        Raster.Exposure = 1.2f;
        return radiance;
    }

    protected override void DrawOverlay(DrawingContext context, Rect bounds)
    {
        var lamp = new Vector3((float)LightX, (float)LightHeight, 0);
        var half = (float)LightSize / 2;
        Mark("lamp", new[] { new Vector3(-half, 0, -half), new Vector3(half, 0, half) }
            .Select(c => Project(lamp + c, bounds)).OfType<Point>(), pad: 8);
        Mark("box", BoxCorners(Block).Select(c => Project(c, bounds)).OfType<Point>(), pad: 2);

        // The shadow's far edge: where light from the lamp's middle just grazes the box's far top edge.
        var top = new Vector3(LightX >= 0 ? Block.Min.X : Block.Max.X, Block.Max.Y, 0);
        var t = lamp.Y / (lamp.Y - top.Y);
        var edge = lamp + ((top - lamp) * t);
        Mark("edge", new[] { edge + new Vector3(0, 0, Block.Min.Z), edge + new Vector3(0, 0, Block.Max.Z) }
            .Select(c => Project(c, bounds)).OfType<Point>(), pad: 10);

        DrawGrips(context, bounds, lamp, half);
    }

    /// <summary>The lamp's three grips, each working its value out from where its own path is drawn.</summary>
    private void DrawGrips(DrawingContext context, Rect bounds, Vector3 lamp, float half)
    {
        Point? P(Vector3 v) => Project(v, bounds);
        if (P(lamp) is not { } centre || P(lamp + new Vector3(0, HangerAbove, 0)) is not { } hanger
            || P(lamp + new Vector3(half, 0, 0)) is not { } rim || P(lamp + new Vector3(half + TabOut, 0, 0)) is not { } tab)
        {
            return;
        }

        // The cord up from the lamp, and a short arm out to the tab off its right edge.
        if (P(lamp + new Vector3(0, 6, 0)) is { } top) context.DrawLine(CordPen, centre, top);
        context.DrawLine(CordPen, rim, tab);

        // A lamp raised or moved out of the picture leaves its grips waiting at the edge, side by side.
        var at = LightGrips.Apart(bounds, LightGrips.Inside(hanger, bounds), LightGrips.Inside(tab, bounds), LightGrips.Inside(centre, bounds));

        // The hanger on the cord raises or lowers the lamp.
        Grip("height", at[0], GripAxis.Vertical, DiagramText.Get("Light_Grip_LampHeight"), to =>
            LightHeight = LightGrips.Snap(
                LightGrips.Nearest(h => P(new Vector3((float)LightX, (float)h + HangerAbove, 0)), LowestLamp, HighestLamp, to),
                LowestLamp, HighestLamp, SliderStep));

        // The tab, pulled out, grows the lamp.
        Grip("size", at[1], GripAxis.Free, DiagramText.Get("Light_Grip_LampSize"), to =>
            LightSize = LightGrips.Snap(
                LightGrips.Nearest(s => P(new Vector3((float)(LightX + (s / 2)) + TabOut, (float)LightHeight, 0)), SmallestLamp, BiggestLamp, to),
                SmallestLamp, BiggestLamp, SliderStep));

        // The lamp itself slides sideways.
        Grip("lamp", at[2], GripAxis.Free, DiagramText.Get("Light_Grip_LampSide"), to =>
            LightX = LightGrips.Snap(
                LightGrips.Nearest(x => P(new Vector3((float)x, (float)LightHeight, 0)), -FurthestAside, FurthestAside, to),
                -FurthestAside, FurthestAside, SliderStep));
    }

    internal static IEnumerable<Vector3> BoxCorners(Box box) =>
        from x in new[] { box.Min.X, box.Max.X }
        from y in new[] { box.Min.Y, box.Max.Y }
        from z in new[] { box.Min.Z, box.Max.Z }
        select new Vector3(x, y, z);
}
