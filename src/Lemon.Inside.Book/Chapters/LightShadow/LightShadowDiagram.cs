using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Lemon.Book.Shell.Infrastructures.Timing;
using Lemon.Inside.Book.Controls.Scene;
using Lemon.Inside.Book.Controls.Space;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// A square lamp over a floor with a box on it, drawn on the CPU by <see cref="SoftwareRasterizer"/>. Drag to look
/// round, use the wheel to come closer. The lamp's height, size and place change the box's shadow: a small lamp
/// gives a sharp one, a big one a soft one. (P0 spike: it also shows how long each frame takes.)
/// </summary>
public sealed class LightShadowDiagram : Control
{
    #region properties

    public static readonly StyledProperty<double> LightHeightProperty =
        AvaloniaProperty.Register<LightShadowDiagram, double>(nameof(LightHeight), 2.5);

    public static readonly StyledProperty<double> LightSizeProperty =
        AvaloniaProperty.Register<LightShadowDiagram, double>(nameof(LightSize), 0.5);

    public static readonly StyledProperty<double> LightXProperty =
        AvaloniaProperty.Register<LightShadowDiagram, double>(nameof(LightX), 0.6);

    public static readonly StyledProperty<int> SamplesProperty =
        AvaloniaProperty.Register<LightShadowDiagram, int>(nameof(Samples), 8);

    public static readonly StyledProperty<double> RenderScaleProperty =
        AvaloniaProperty.Register<LightShadowDiagram, double>(nameof(RenderScale), 0.5);

    public static readonly StyledProperty<bool> SpinProperty =
        AvaloniaProperty.Register<LightShadowDiagram, bool>(nameof(Spin));

    public static readonly StyledProperty<bool> ShowStatsProperty =
        AvaloniaProperty.Register<LightShadowDiagram, bool>(nameof(ShowStats), true);

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

    /// <summary>Points sampled along each side of the lamp.</summary>
    public int Samples
    {
        get => GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    /// <summary>Picture pixels per screen pixel (the picture is drawn small and smoothed up).</summary>
    public double RenderScale
    {
        get => GetValue(RenderScaleProperty);
        set => SetValue(RenderScaleProperty, value);
    }

    /// <summary>Turns the camera round by itself (to measure a steady frame rate).</summary>
    public bool Spin
    {
        get => GetValue(SpinProperty);
        set => SetValue(SpinProperty, value);
    }

    public bool ShowStats
    {
        get => GetValue(ShowStatsProperty);
        set => SetValue(ShowStatsProperty, value);
    }

    static LightShadowDiagram()
    {
        AffectsRender<LightShadowDiagram>(RenderScaleProperty, ShowStatsProperty);
        LightHeightProperty.Changed.AddClassHandler<LightShadowDiagram>((d, _) => d.LightChanged());
        LightSizeProperty.Changed.AddClassHandler<LightShadowDiagram>((d, _) => d.LightChanged());
        LightXProperty.Changed.AddClassHandler<LightShadowDiagram>((d, _) => d.LightChanged());
        SamplesProperty.Changed.AddClassHandler<LightShadowDiagram>((d, _) => d.LightChanged());
    }

    #endregion

    private static readonly Box Block = new(new Vector3(-0.4f, 0, -0.4f), new Vector3(0.4f, 0.8f, 0.4f));

    private readonly OrbitCamera _camera = new();
    private readonly SoftwareRasterizer _raster = new();
    private readonly AreaLight _lamp = new();
    private readonly BookStopwatch _clock = new();
    private readonly SceneMesh _scene = new();
    private readonly int _lampFirstVertex;
    private Vector3[] _radiance = [];
    private bool _lightDirty = true;
    private WriteableBitmap? _bitmap;
    private bool _attached;
    private Point? _dragFrom;
    private double _lastSeconds;

    // Timings for the spike's readout.
    private double _lightMs;
    private double _rasterMs;
    private double _fps;
    private int _frames;
    private double _fpsSince;

    public LightShadowDiagram()
    {
        ClipToBounds = true;
        Focusable = true;
        _scene.AddQuad(new Vector3(-3, 0, 3), new Vector3(6, 0, 0), new Vector3(0, 0, -6), 64, new Vector3(0.78f, 0.76f, 0.72f));
        _scene.AddBox(Block, 8, new Vector3(0.85f, 0.55f, 0.3f));
        _lampFirstVertex = _scene.VertexCount;
        // The lamp, drawn as a glowing square; its corners are moved to the lamp each time the light changes.
        _scene.AddQuad(Vector3.Zero, Vector3.UnitX, -Vector3.UnitZ, 1, Vector3.Zero, Vector3.One);
    }

    /// <summary>The camera, for tests and for the spike's readout.</summary>
    internal OrbitCamera Camera => _camera;

    /// <summary>The last frame's picture size in pixels.</summary>
    internal PixelSize PictureSize => new(_raster.Width, _raster.Height);

    private void LightChanged()
    {
        _lightDirty = true;
        InvalidateVisual();
    }

    #region lifecycle

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        _clock.Start();
        _lastSeconds = _clock.Elapsed.TotalSeconds;
        _fpsSince = _lastSeconds;
        RequestFrame();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        _clock.Stop();
        _bitmap?.Dispose();
        _bitmap = null;
    }

    private void RequestFrame()
    {
        if (_attached)
        {
            TopLevel.GetTopLevel(this)?.RequestAnimationFrame(OnFrame);
        }
    }

    private void OnFrame(TimeSpan time)
    {
        if (!_attached)
        {
            return;
        }

        var now = _clock.Elapsed.TotalSeconds;
        var dt = Math.Clamp(now - _lastSeconds, 0, 0.1);
        _lastSeconds = now;
        if (Spin)
        {
            _camera.Yaw += dt * 30;
            InvalidateVisual();
        }

        RequestFrame();
    }

    #endregion

    #region input

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _dragFrom = e.GetPosition(this);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragFrom is not { } from)
        {
            return;
        }

        var at = e.GetPosition(this);
        _camera.Drag(at.X - from.X, at.Y - from.Y);
        _dragFrom = at;
        e.Handled = true;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _dragFrom = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _dragFrom = null;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        _camera.Distance *= Math.Pow(0.9, e.Delta.Y);
        e.Handled = true;
        InvalidateVisual();
    }

    #endregion

    #region drawing

    /// <summary>Puts the lamp where the properties say and works out the light on every vertex.</summary>
    private void UpdateLight()
    {
        var watch = Stopwatch.StartNew();
        _lamp.Center = new Vector3((float)LightX, (float)LightHeight, 0);
        _lamp.Size = (float)LightSize;
        _lamp.Samples = Math.Clamp(Samples, 1, 32);
        var irradiance = _lamp.Irradiance(_scene, [Block]);

        _radiance = new Vector3[_scene.VertexCount];
        for (var i = 0; i < _scene.VertexCount; i++)
        {
            // A matte surface sends out the light it gets, times its colour, spread over the half-sphere (1/π).
            _radiance[i] = (_scene.Albedo[i] * irradiance[i] / MathF.PI) + _scene.Emission[i];
        }

        // The lamp's square: placed by hand, shown as a bright surface.
        var half = _lamp.Size / 2;
        Vector3[] corners =
        [
            _lamp.Center + new Vector3(-half, 0, half), _lamp.Center + new Vector3(half, 0, half),
            _lamp.Center + new Vector3(-half, 0, -half), _lamp.Center + new Vector3(half, 0, -half),
        ];
        for (var i = 0; i < 4; i++)
        {
            _scene.SetPosition(_lampFirstVertex + i, corners[i]);
            _radiance[_lampFirstVertex + i] = new Vector3(4f);
        }

        _lightMs = watch.Elapsed.TotalMilliseconds;
        _lightDirty = false;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = new Rect(Bounds.Size);
        if (bounds.Width < 2 || bounds.Height < 2)
        {
            return;
        }

        if (_lightDirty)
        {
            UpdateLight();
        }

        var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var factor = scaling * Math.Clamp(RenderScale, 0.1, 1);
        var width = Math.Max(2, (int)(bounds.Width * factor));
        var height = Math.Max(2, (int)(bounds.Height * factor));

        var watch = Stopwatch.StartNew();
        _raster.Resize(width, height);
        _raster.Exposure = 1.2f;
        _raster.Clear((0x14, 0x1B, 0x2A), (0x08, 0x0B, 0x12));
        _raster.Draw(_scene, _radiance, _camera.ViewProjection(bounds.Width / bounds.Height));

        if (_bitmap is null || _bitmap.PixelSize.Width != width || _bitmap.PixelSize.Height != height)
        {
            _bitmap?.Dispose();
            _bitmap = new WriteableBitmap(new PixelSize(width, height), new Avalonia.Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        }

        SpaceDrawing.WritePixels(_bitmap, _raster.Pixels, width, height);
        _rasterMs = (_rasterMs * 0.8) + (watch.Elapsed.TotalMilliseconds * 0.2);
        SpaceDrawing.DrawSmooth(context, _bitmap, bounds);

        CountFrame();
        if (ShowStats)
        {
            DrawStats(context, width, height);
        }
    }

    private void CountFrame()
    {
        _frames++;
        var now = _clock.Elapsed.TotalSeconds;
        if (now - _fpsSince >= 1)
        {
            _fps = _frames / (now - _fpsSince);
            _frames = 0;
            _fpsSince = now;
        }
    }

    private void DrawStats(DrawingContext context, int width, int height)
    {
        var text = string.Format(
            CultureInfo.InvariantCulture,
            "{0}×{1} px · raster {2:0.0} ms · light {3:0} ms ({4}² samples) · {5:0} fps",
            width, height, _rasterMs, _lightMs, _lamp.Samples, _fps);
        var formatted = new FormattedText(
            text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 11, Brushes.White);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(0x99, 0, 0, 0)), new Rect(8, 8, formatted.Width + 12, formatted.Height + 6), 4);
        context.DrawText(formatted, new Point(14, 11));
    }

    #endregion
}
