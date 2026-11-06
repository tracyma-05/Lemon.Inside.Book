using System.Numerics;
using Avalonia;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// Spread 5: a square lamp over a floor with a box on it. The lamp's height, size and place change the box's
/// shadow: a small lamp gives a sharp one, a big one a soft one.
/// </summary>
public sealed class ShadowScene : SceneView
{
    public static readonly StyledProperty<double> LightHeightProperty =
        AvaloniaProperty.Register<ShadowScene, double>(nameof(LightHeight), 2.5);

    public static readonly StyledProperty<double> LightSizeProperty =
        AvaloniaProperty.Register<ShadowScene, double>(nameof(LightSize), 0.5);

    public static readonly StyledProperty<double> LightXProperty =
        AvaloniaProperty.Register<ShadowScene, double>(nameof(LightX), 0.6);

    /// <summary>Samples along each side of the lamp: fewer while a slider moves, more once it stops.</summary>
    internal const int CoarseSamples = 4;
    internal const int FullSamples = 10;

    internal static readonly Box Block = new(new Vector3(-0.4f, 0, -0.4f), new Vector3(0.4f, 0.8f, 0.4f));

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
}
