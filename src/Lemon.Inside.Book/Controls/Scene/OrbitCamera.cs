using System.Numerics;

namespace Lemon.Inside.Book.Controls.Scene;

/// <summary>
/// A camera that circles a point: dragging sideways turns it round (<see cref="Yaw"/>), dragging up and down raises or
/// lowers it (<see cref="Pitch"/>, kept above the floor), and the wheel moves it nearer or further.
/// </summary>
public sealed class OrbitCamera
{
    private double _pitch = 28;
    private double _distance = 8;

    public Vector3 Target { get; set; } = new(0, 0.5f, 0);

    /// <summary>Degrees round the vertical axis.</summary>
    public double Yaw { get; set; } = 35;

    /// <summary>Degrees above the horizon, between <see cref="MinPitch"/> and <see cref="MaxPitch"/>.</summary>
    public double Pitch
    {
        get => _pitch;
        set => _pitch = Math.Clamp(value, MinPitch, MaxPitch);
    }

    public double MinPitch { get; init; } = 6;

    public double MaxPitch { get; init; } = 85;

    public double Distance
    {
        get => _distance;
        set => _distance = Math.Clamp(value, MinDistance, MaxDistance);
    }

    public double MinDistance { get; init; } = 4;

    public double MaxDistance { get; init; } = 16;

    /// <summary>Vertical field of view in degrees.</summary>
    public double FieldOfView { get; set; } = 40;

    public Vector3 Eye
    {
        get
        {
            var yaw = Yaw * Math.PI / 180;
            var pitch = Pitch * Math.PI / 180;
            var offset = new Vector3(
                (float)(Math.Cos(pitch) * Math.Sin(yaw)),
                (float)Math.Sin(pitch),
                (float)(Math.Cos(pitch) * Math.Cos(yaw)));
            return Target + (offset * (float)Distance);
        }
    }

    /// <summary>World to clip space for a picture <paramref name="aspect"/> times wider than it is tall.</summary>
    public Matrix4x4 ViewProjection(double aspect) =>
        Matrix4x4.CreateLookAt(Eye, Target, Vector3.UnitY)
        * Matrix4x4.CreatePerspectiveFieldOfView((float)(FieldOfView * Math.PI / 180), (float)Math.Max(aspect, 0.01), 0.1f, 100f);

    /// <summary>Turns the camera by a drag of (<paramref name="dx"/>, <paramref name="dy"/>) DIPs.</summary>
    public void Drag(double dx, double dy)
    {
        Yaw -= dx * 0.4;
        Pitch += dy * 0.3;
    }
}
