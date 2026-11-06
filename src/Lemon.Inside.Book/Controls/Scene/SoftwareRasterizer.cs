using System.Numerics;

namespace Lemon.Inside.Book.Controls.Scene;

/// <summary>
/// Draws a <see cref="SceneMesh"/> into a pixel buffer on the CPU: a depth buffer, colours interpolated across each
/// triangle with perspective correction, then exposure and the display's gamma. It runs the same on the desktop and in
/// the browser (where .NET has one thread), so it is kept simple and is drawn small and scaled up.
/// </summary>
public sealed class SoftwareRasterizer
{
    private const int ToneSteps = 4096;
    private static readonly byte[] Gamma = BuildGamma();

    private float[] _depth = [];
    private Vector4[] _clip = [];

    public int Width { get; private set; }

    public int Height { get; private set; }

    /// <summary>Premultiplied BGRA pixels, one <see cref="int"/> each (opaque).</summary>
    public int[] Pixels { get; private set; } = [];

    /// <summary>Multiplies the light before the gamma curve: how bright a given radiance looks.</summary>
    public float Exposure { get; set; } = 1f;

    public void Resize(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        if (width == Width && height == Height)
        {
            return;
        }

        Width = width;
        Height = height;
        Pixels = new int[width * height];
        _depth = new float[width * height];
    }

    /// <summary>Fills the picture with a vertical gradient from <paramref name="top"/> to <paramref name="bottom"/> (sRGB bytes).</summary>
    public void Clear((byte R, byte G, byte B) top, (byte R, byte G, byte B) bottom)
    {
        Array.Fill(_depth, float.MaxValue);
        for (var y = 0; y < Height; y++)
        {
            var t = Height > 1 ? y / (float)(Height - 1) : 0;
            var r = (int)(top.R + ((bottom.R - top.R) * t));
            var g = (int)(top.G + ((bottom.G - top.G) * t));
            var b = (int)(top.B + ((bottom.B - top.B) * t));
            Array.Fill(Pixels, unchecked((int)0xFF000000) | (r << 16) | (g << 8) | b, y * Width, Width);
        }
    }

    /// <summary>
    /// Draws every triangle of <paramref name="mesh"/>; <paramref name="radiance"/> is each vertex's colour in linear
    /// light (what it sends towards the eye).
    /// </summary>
    public void Draw(SceneMesh mesh, Vector3[] radiance, Matrix4x4 viewProjection)
    {
        if (_clip.Length < mesh.VertexCount)
        {
            _clip = new Vector4[mesh.VertexCount];
        }

        for (var i = 0; i < mesh.VertexCount; i++)
        {
            _clip[i] = Vector4.Transform(new Vector4(mesh.Positions[i], 1), viewProjection);
        }

        var indices = mesh.Indices;
        for (var t = 0; t + 2 < indices.Count; t += 3)
        {
            int a = indices[t], b = indices[t + 1], c = indices[t + 2];
            DrawTriangle(_clip[a], _clip[b], _clip[c], radiance[a], radiance[b], radiance[c]);
        }
    }

    private void DrawTriangle(Vector4 p0, Vector4 p1, Vector4 p2, Vector3 c0, Vector3 c1, Vector3 c2)
    {
        // The camera never goes inside the scene, so a triangle behind the eye is skipped rather than clipped.
        const float near = 0.05f;
        if (p0.W < near || p1.W < near || p2.W < near)
        {
            return;
        }

        float iw0 = 1 / p0.W, iw1 = 1 / p1.W, iw2 = 1 / p2.W;
        float x0 = (p0.X * iw0 + 1) * 0.5f * Width, y0 = (1 - (p0.Y * iw0)) * 0.5f * Height;
        float x1 = (p1.X * iw1 + 1) * 0.5f * Width, y1 = (1 - (p1.Y * iw1)) * 0.5f * Height;
        float x2 = (p2.X * iw2 + 1) * 0.5f * Width, y2 = (1 - (p2.Y * iw2)) * 0.5f * Height;
        float z0 = p0.Z * iw0, z1 = p1.Z * iw1, z2 = p2.Z * iw2;

        var area = ((x1 - x0) * (y2 - y0)) - ((x2 - x0) * (y1 - y0));
        if (MathF.Abs(area) < 1e-6f)
        {
            return;
        }

        var minX = Math.Max(0, (int)MathF.Floor(MathF.Min(x0, MathF.Min(x1, x2))));
        var maxX = Math.Min(Width - 1, (int)MathF.Ceiling(MathF.Max(x0, MathF.Max(x1, x2))));
        var minY = Math.Max(0, (int)MathF.Floor(MathF.Min(y0, MathF.Min(y1, y2))));
        var maxY = Math.Min(Height - 1, (int)MathF.Ceiling(MathF.Max(y0, MathF.Max(y1, y2))));
        if (minX > maxX || minY > maxY)
        {
            return;
        }

        var inv = 1 / area;
        Vector3 q0 = c0 * iw0, q1 = c1 * iw1, q2 = c2 * iw2;
        var scale = Exposure * (ToneSteps - 1);
        for (var y = minY; y <= maxY; y++)
        {
            var py = y + 0.5f;
            var row = y * Width;
            for (var x = minX; x <= maxX; x++)
            {
                var px = x + 0.5f;
                var w0 = (((x1 - px) * (y2 - py)) - ((x2 - px) * (y1 - py))) * inv;
                var w1 = (((x2 - px) * (y0 - py)) - ((x0 - px) * (y2 - py))) * inv;
                var w2 = 1 - w0 - w1;
                if (w0 < 0 || w1 < 0 || w2 < 0)
                {
                    continue;
                }

                var z = (w0 * z0) + (w1 * z1) + (w2 * z2);
                var i = row + x;
                if (z >= _depth[i])
                {
                    continue;
                }

                _depth[i] = z;
                var color = ((q0 * w0) + (q1 * w1) + (q2 * w2)) / ((w0 * iw0) + (w1 * iw1) + (w2 * iw2));
                Pixels[i] = unchecked((int)0xFF000000)
                    | (Tone(color.X, scale) << 16) | (Tone(color.Y, scale) << 8) | Tone(color.Z, scale);
            }
        }
    }

    private static int Tone(float value, float scale)
    {
        var i = (int)(value * scale);
        return Gamma[i < 0 ? 0 : i >= ToneSteps ? ToneSteps - 1 : i];
    }

    private static byte[] BuildGamma()
    {
        var table = new byte[ToneSteps];
        for (var i = 0; i < ToneSteps; i++)
        {
            // The sRGB curve, so twice the light does not look twice as bright.
            var l = i / (double)(ToneSteps - 1);
            var s = l <= 0.0031308 ? 12.92 * l : (1.055 * Math.Pow(l, 1 / 2.4)) - 0.055;
            table[i] = (byte)Math.Round(Math.Clamp(s, 0, 1) * 255);
        }

        return table;
    }
}
