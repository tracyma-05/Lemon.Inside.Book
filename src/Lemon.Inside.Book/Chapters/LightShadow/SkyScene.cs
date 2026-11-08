using System.Numerics;
using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// Spread 6, in 3D: what a spot on the floor "sees". Over it, a half-sphere of sky; a square lamp shows up on that
/// half-sphere as a patch, and pressed flat onto the circle at its foot, the patch's share of the circle is how much
/// of the lamp's light the spot gets. Low lamps squash flat (the slant of spread 2), far ones shrink (spread 3).
/// The reader drags the lamp across the sky, and pulls the handle beyond it to move it nearer or further; the lamp
/// glides to a new place (only flat drawing moves, nothing has to be lit again), then the picture stops redrawing.
/// </summary>
public sealed class SkyScene : SceneView
{
    public static readonly StyledProperty<double> ElevationProperty =
        AvaloniaProperty.Register<SkyScene, double>(nameof(Elevation), 50, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> DistanceProperty =
        AvaloniaProperty.Register<SkyScene, double>(nameof(Distance), 2.5, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Radius of the half-sphere drawn over the spot.</summary>
    private const float Dome = 1.4f;

    /// <summary>Side of the square lamp, in metres.</summary>
    internal const float LampSize = 1.2f;

    private const int EdgeSteps = 16;

    /// <summary>The sliders' ranges: degrees above the horizon, metres away.</summary>
    private const double LowestLamp = 5;
    private const double HighestLamp = 90;
    private const double NearestLamp = 1.6;
    private const double FurthestLamp = 5;

    /// <summary>How far beyond the lamp's middle its pull handle is, in metres.</summary>
    private const float HandleBeyond = 0.6f;

    private static readonly IPen DomePen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x50, 0xE6, 0xEA, 0xF2)), 1);
    private static readonly IPen RimPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0xA0, 0xE6, 0xEA, 0xF2)), 1.2);
    private static readonly IPen RayPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x70, 0xF2, 0xC9, 0x4C)), 1);
    private static readonly IPen DropPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x60, 0x6F, 0xD8, 0xFF)), 1, new ImmutableDashStyle([3, 3], 0));
    private static readonly IImmutableBrush LampBrush = new ImmutableSolidColorBrush(Color.FromArgb(0xE0, 0xFF, 0xF4, 0xD0));
    private static readonly IImmutableBrush OnDomeBrush = new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0xF2, 0xC9, 0x4C));
    private static readonly IImmutableBrush OnFloorBrush = new ImmutableSolidColorBrush(Color.FromArgb(0xC0, 0x6F, 0xD8, 0xFF));
    private static readonly IImmutableBrush DiskBrush = new ImmutableSolidColorBrush(Color.FromArgb(0x28, 0xE6, 0xEA, 0xF2));
    private static readonly IPen HandlePen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0xFF, 0xF4, 0xD0)), 1.2, new ImmutableDashStyle([2, 3], 0));

    /// <summary>The lamp's place drawn, easing toward <see cref="Elevation"/> and <see cref="Distance"/> (NaN until the first frame).</summary>
    private double _shownElevation = double.NaN;
    private double _shownDistance = double.NaN;
    private double _lastTick = double.NaN;

    static SkyScene()
    {
        AffectsRender<SkyScene>(ElevationProperty, DistanceProperty);
    }

    public SkyScene()
    {
        Camera.Target = new Vector3(0, 0.9f, 0);
        Camera.Yaw = 30;
        Camera.Pitch = 22;
        Camera.Distance = 8;
    }

    /// <summary>How high the lamp is above the horizon seen from the spot, in degrees.</summary>
    public double Elevation
    {
        get => GetValue(ElevationProperty);
        set => SetValue(ElevationProperty, value);
    }

    /// <summary>How far the lamp's centre is from the spot, in metres.</summary>
    public double Distance
    {
        get => GetValue(DistanceProperty);
        set => SetValue(DistanceProperty, value);
    }

    protected override Vector3[] ComputeRadiance(bool coarse) => [];

    /// <summary>Eases the lamp toward its new place; redraws only while it is on its way.</summary>
    protected override void Tick(double seconds)
    {
        var step = double.IsNaN(_lastTick) ? 0 : Math.Min(seconds - _lastTick, 0.1);
        _lastTick = seconds;
        if (double.IsNaN(_shownElevation) || (Math.Abs(_shownElevation - Elevation) < 1e-4 && Math.Abs(_shownDistance - Distance) < 1e-4))
        {
            return;
        }

        var arrived = Math.Abs(_shownElevation - Elevation) < 0.05 && Math.Abs(_shownDistance - Distance) < 0.003;
        _shownElevation = Gripping || arrived ? Elevation : LightGrips.Ease(_shownElevation, Elevation, step, 0.1);
        _shownDistance = Gripping || arrived ? Distance : LightGrips.Ease(_shownDistance, Distance, step, 0.1);
        InvalidateVisual();
    }

    /// <summary>The middle of the lamp, <paramref name="distance"/> metres from the spot at <paramref name="elevationDegrees"/>.</summary>
    internal static Vector3 LampCentre(double elevationDegrees, double distance)
    {
        var e = elevationDegrees * Math.PI / 180;
        return new Vector3((float)(Math.Cos(e) * distance), (float)(Math.Sin(e) * distance), 0);
    }

    /// <summary>The lamp's outline, a few points per edge, facing the spot at the origin.</summary>
    internal static Vector3[] LampOutline(double elevationDegrees, double distance)
    {
        var e = (float)(elevationDegrees * Math.PI / 180);
        var toLamp = new Vector3(MathF.Cos(e), MathF.Sin(e), 0);
        var center = toLamp * (float)distance;
        var side = Vector3.UnitZ;
        var up = Vector3.Cross(side, toLamp); // in the lamp's plane, at right angles to the side
        var h = LampSize / 2;
        Vector3[] corners = [center + (side * h) + (up * h), center - (side * h) + (up * h), center - (side * h) - (up * h), center + (side * h) - (up * h)];
        var outline = new List<Vector3>();
        for (var c = 0; c < 4; c++)
        {
            for (var s = 0; s < EdgeSteps; s++)
            {
                outline.Add(Vector3.Lerp(corners[c], corners[(c + 1) % 4], s / (float)EdgeSteps));
            }
        }

        return [.. outline];
    }

    /// <summary>
    /// The share of the circle at the dome's foot covered by the lamp pressed onto the dome and then down onto the
    /// floor: the share of a full sky's light the spot gets from the lamp (its view factor).
    /// </summary>
    internal static double FloorShare(double elevationDegrees, double distance)
    {
        var flat = LampOutline(elevationDegrees, distance).Select(p => Vector3.Normalize(p)).Select(d => new Vector2(d.X, d.Z)).ToArray();
        var area = 0.0;
        for (var i = 0; i < flat.Length; i++)
        {
            var a = flat[i];
            var b = flat[(i + 1) % flat.Length];
            area += (a.X * b.Y) - (b.X * a.Y);
        }

        return Math.Abs(area) / 2 / Math.PI;
    }

    protected override void DrawOverlay(DrawingContext context, Rect bounds)
    {
        if (double.IsNaN(_shownElevation)) (_shownElevation, _shownDistance) = (Elevation, Distance);
        var (elevation, distance) = (_shownElevation, _shownDistance);
        var outline = LampOutline(elevation, distance);
        var onDome = outline.Select(p => Vector3.Normalize(p) * Dome).ToArray();
        var onFloor = onDome.Select(p => new Vector3(p.X, 0, p.Z)).ToArray();

        // The circle at the foot of the half-sphere, a few lines of latitude and longitude over it.
        Fill(context, bounds, Circle(Dome, 0), DiskBrush, RimPen);
        for (var lat = 30; lat < 90; lat += 30)
        {
            var a = lat * MathF.PI / 180;
            Fill(context, bounds, Circle(Dome * MathF.Cos(a), Dome * MathF.Sin(a)), null, DomePen);
        }

        for (var lon = 0; lon < 180; lon += 30)
        {
            var a = lon * MathF.PI / 180;
            var arc = Enumerable.Range(0, 33).Select(k =>
            {
                var t = k * MathF.PI / 32;
                return new Vector3(MathF.Cos(t) * MathF.Cos(a), MathF.Sin(t), MathF.Cos(t) * MathF.Sin(a)) * Dome;
            }).ToArray();
            Polyline(context, bounds, arc, DomePen);
        }

        // Lines from the spot to the lamp's corners; the lamp; its patch on the dome; that patch pressed flat.
        for (var c = 0; c < 4; c++)
        {
            Polyline(context, bounds, [Vector3.Zero, outline[c * EdgeSteps]], RayPen);
            Polyline(context, bounds, [onDome[c * EdgeSteps], onFloor[c * EdgeSteps]], DropPen);
        }

        Fill(context, bounds, outline, LampBrush, null);
        Fill(context, bounds, onFloor, OnFloorBrush, null);
        Fill(context, bounds, onDome, OnDomeBrush, null);
        Mark("lamp", outline.Select(p => Project(p, bounds)).OfType<Point>(), pad: 2);
        Mark("patch", onFloor.Select(p => Project(p, bounds)).OfType<Point>(), pad: 2);
        Mark("circle", Circle(Dome, 0).Select(p => Project(p, bounds)).OfType<Point>(), pad: -8);
        if (Project(Vector3.Zero, bounds) is { } spot)
        {
            context.DrawEllipse(DiagramText.Ink, null, spot, 4, 4);
            Mark("spot", spot, 8);
            DiagramText.Draw(context, DiagramText.Get("Light_Sky_Spot"), new Point(spot.X, spot.Y + 8), 11.5, DiagramText.Faint, 0.5, 0);
        }

        DiagramText.Draw(context,
            DiagramText.Format("Light_Sky_Readout", (FloorShare(elevation, distance) * 100).ToString("0.0")),
            new Point(bounds.X + 16, bounds.Bottom - 16), 13, DiagramText.Ink, 0, 1);

        // The lamp's own grip slides it across the sky; the handle beyond it pulls it nearer or further along its line.
        Point? P(Vector3 v) => Project(v, bounds);
        var centre = LampCentre(elevation, distance);
        var beyond = LampCentre(elevation, distance + HandleBeyond);
        if (P(centre) is { } lamp && P(beyond) is { } handle)
        {
            context.DrawLine(HandlePen, lamp, handle);

            // A lamp far up out of the picture: both grips wait at its edge, side by side rather than on top of each other.
            var grips = LightGrips.Apart(bounds, LightGrips.Inside(lamp, bounds), LightGrips.Inside(handle, bounds));
            var (lampGrip, reachGrip) = (grips[0], grips[1]);

            Grip("reach", reachGrip, GripAxis.Free, DiagramText.Get("Light_Grip_SkyDistance"), at =>
                Distance = LightGrips.Snap(
                    LightGrips.Nearest(d => P(LampCentre(Elevation, d + HandleBeyond)), NearestLamp, FurthestLamp, at), NearestLamp, FurthestLamp, 0.1));
            Grip("lamp", lampGrip, GripAxis.Free, DiagramText.Get("Light_Grip_Elevation"), at =>
                Elevation = LightGrips.Snap(
                    LightGrips.Nearest(e => P(LampCentre(e, Distance)), LowestLamp, HighestLamp, at), LowestLamp, HighestLamp, 1));
        }
    }

    private static Vector3[] Circle(float radius, float height) =>
        [.. Enumerable.Range(0, 64).Select(k => new Vector3(MathF.Cos(k * MathF.Tau / 64) * radius, height, MathF.Sin(k * MathF.Tau / 64) * radius))];

    private void Polyline(DrawingContext context, Rect bounds, Vector3[] points, IPen pen)
    {
        for (var i = 1; i < points.Length; i++)
        {
            if (Project(points[i - 1], bounds) is { } a && Project(points[i], bounds) is { } b)
            {
                context.DrawLine(pen, a, b);
            }
        }
    }

    private void Fill(DrawingContext context, Rect bounds, Vector3[] points, IBrush? brush, IPen? pen)
    {
        var projected = points.Select(p => Project(p, bounds)).ToArray();
        if (projected.Any(p => p is null))
        {
            return;
        }

        var shape = new StreamGeometry();
        using (var g = shape.Open())
        {
            g.BeginFigure(projected[0]!.Value, brush is not null);
            foreach (var p in projected.Skip(1))
            {
                g.LineTo(p!.Value);
            }

            g.EndFigure(true);
        }

        context.DrawGeometry(brush, pen, shape);
    }
}
