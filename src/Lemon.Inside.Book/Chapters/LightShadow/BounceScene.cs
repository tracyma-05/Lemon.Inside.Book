using System.Numerics;
using Avalonia;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// Spread 9: a lamp, a white floor, a white box and a coloured wall. With no bounces only what the lamp reaches is
/// lit; each bounce lets the lit surfaces light the others, so the floor by the wall takes on its colour and the
/// box's shadow side is no longer black.
/// </summary>
public sealed class BounceScene : SceneView
{
    public static readonly StyledProperty<int> BouncesProperty =
        AvaloniaProperty.Register<BounceScene, int>(nameof(Bounces), 2);

    public static readonly StyledProperty<WallColor> WallProperty =
        AvaloniaProperty.Register<BounceScene, WallColor>(nameof(Wall), WallColor.Red);

    internal static readonly Box Block = new(new Vector3(0.1f, 0, -0.35f), new Vector3(0.8f, 0.7f, 0.35f));

    private static readonly Vector3 White = new(0.8f);
    private readonly AreaLight _lamp = new() { Center = new Vector3(0.4f, 2.4f, 0), Size = 0.7f, Power = 30 };
    private readonly int _wallFirst;
    private readonly int _wallCount;

    static BounceScene()
    {
        BouncesProperty.Changed.AddClassHandler<BounceScene>((s, _) => s.LightChanged());
        WallProperty.Changed.AddClassHandler<BounceScene>((s, _) => s.Repaint());
    }

    public BounceScene()
    {
        Camera.Target = new Vector3(0, 0.6f, 0);
        Camera.Yaw = 40;
        Camera.Pitch = 26;
        Camera.Distance = 6.5;
        Mesh.AddQuad(new Vector3(-1.5f, 0, 2), new Vector3(3.5f, 0, 0), new Vector3(0, 0, -4), 22, White);
        _wallFirst = Mesh.VertexCount;
        // The coloured wall stands along the floor's left edge, facing in (+x).
        Mesh.AddQuad(new Vector3(-1.5f, 0, -2), new Vector3(0, 2.4f, 0), new Vector3(0, 0, 4), 18, WallColor.Red.Albedo());
        _wallCount = Mesh.VertexCount - _wallFirst;
        Mesh.AddBox(Block, 6, White);
        var h = _lamp.Size / 2;
        Mesh.AddQuad(_lamp.Center + new Vector3(-h, 0, h), new Vector3(_lamp.Size, 0, 0), new Vector3(0, 0, -_lamp.Size), 1, Vector3.Zero, Vector3.One);
    }

    /// <summary>How many times the light bounces between the surfaces (0 is the lamp's light only).</summary>
    public int Bounces
    {
        get => GetValue(BouncesProperty);
        set => SetValue(BouncesProperty, value);
    }

    public WallColor Wall
    {
        get => GetValue(WallProperty);
        set => SetValue(WallProperty, value);
    }

    private void Repaint()
    {
        var albedo = Wall.Albedo();
        for (var i = 0; i < _wallCount; i++)
        {
            Mesh.SetAlbedo(_wallFirst + i, albedo);
        }

        LightChanged();
    }

    protected override Vector3[] ComputeRadiance(bool coarse)
    {
        _lamp.Samples = coarse ? 3 : 6;
        var irradiance = _lamp.Irradiance(Mesh, [Block]);
        var direct = irradiance.Select(e => new Vector3(e)).ToArray();
        var arriving = Radiosity.Bounce(Mesh, direct, Math.Clamp(Bounces, 0, 4), coarse ? 4 : 1);

        var radiance = new Vector3[Mesh.VertexCount];
        for (var i = 0; i < radiance.Length; i++)
        {
            radiance[i] = (Mesh.Albedo[i] * arriving[i] / MathF.PI) + (Mesh.Emission[i] * 4);
        }

        Raster.Exposure = 0.9f;
        return radiance;
    }
}

/// <summary>The colours the wall can be painted.</summary>
public enum WallColor
{
    Red,
    Green,
    Blue,
}

internal static class WallColors
{
    /// <summary>The share of each of red, green and blue light the paint gives back.</summary>
    public static Vector3 Albedo(this WallColor color) => color switch
    {
        WallColor.Green => new Vector3(0.12f, 0.6f, 0.15f),
        WallColor.Blue => new Vector3(0.1f, 0.25f, 0.75f),
        _ => new Vector3(0.75f, 0.1f, 0.08f),
    };
}
