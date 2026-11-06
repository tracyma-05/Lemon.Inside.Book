using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.Infrastructures.Timing;
using Lemon.Inside.Book.Controls.Space;

namespace Lemon.Inside.Book.Controls.Scene;

/// <summary>
/// A 3D picture the reader turns round: a <see cref="SceneMesh"/> drawn on the CPU by <see cref="SoftwareRasterizer"/>,
/// with an <see cref="OrbitCamera"/> (drag to turn, wheel to come closer) and room for flat drawing on top (labels,
/// rays, charts). Working out the light is the slow part, so a subclass does it in two passes: a quick, coarse one
/// while the reader is moving a slider, and the full one once they have stopped for a moment.
/// </summary>
public abstract class SceneView : Control
{
    public static readonly StyledProperty<bool> ShowStatsProperty =
        AvaloniaProperty.Register<SceneView, bool>(nameof(ShowStats));

    /// <summary>How long after the last change the full-quality light is worked out.</summary>
    private const double RefineDelaySeconds = 0.25;

    private readonly BookStopwatch _clock = new();
    private WriteableBitmap? _bitmap;
    private Vector3[] _radiance = [];
    private bool _lightDirty = true;
    private bool _refinePending;
    private double _changedAt;
    private bool _attached;
    private Point? _dragFrom;
    private double _lightMs;
    private double _rasterMs;

    /// <summary>Something moved lately (a drag, the wheel, a slider): drawn plain until it has been still a moment.</summary>
    private bool _moving;
    private double _movedAt;
    private int[] _sharp = [];
    private PixelSize _pictureSize;

    protected SceneView()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    /// <summary>Shows how long the light and the drawing take (for measuring).</summary>
    public bool ShowStats
    {
        get => GetValue(ShowStatsProperty);
        set => SetValue(ShowStatsProperty, value);
    }

    public OrbitCamera Camera { get; } = new();

    protected SceneMesh Mesh { get; } = new();

    protected SoftwareRasterizer Raster { get; } = new();

    /// <summary>Picture pixels per screen pixel. The cost is mostly per triangle, not per pixel, so full size is affordable.</summary>
    protected double RenderScale { get; set; } = 1;

    /// <summary>The background, top to bottom (sRGB).</summary>
    protected ((byte R, byte G, byte B) Top, (byte R, byte G, byte B) Bottom) Sky { get; set; } =
        ((0x14, 0x1B, 0x2A), (0x08, 0x0B, 0x12));

    /// <summary>The last frame's picture size in screen pixels.</summary>
    internal PixelSize PictureSize => _pictureSize;

    /// <summary>Whether the last frame was drawn smooth (four samples per pixel), as it is once nothing moves.</summary>
    internal bool IsSharp { get; private set; }

    /// <summary>Whether the light on screen is the coarse, quick one (the full one follows shortly).</summary>
    internal bool IsCoarse => _refinePending;

    /// <summary>
    /// What each vertex of <see cref="Mesh"/> sends towards the eye, in linear light. <paramref name="coarse"/> asks for
    /// a quick, rougher answer (fewer samples on a lamp) while something is changing.
    /// </summary>
    protected abstract Vector3[] ComputeRadiance(bool coarse);

    /// <summary>Flat drawing over the 3D picture; <see cref="Project"/> finds where a point of the scene is.</summary>
    protected virtual void DrawOverlay(DrawingContext context, Rect bounds)
    {
    }

    /// <summary>The light has to be worked out again (a lamp or a surface changed): quickly now, fully soon.</summary>
    protected void LightChanged()
    {
        _lightDirty = true;
        _refinePending = true;
        _changedAt = _clock.Elapsed.TotalSeconds;
        Moved();
    }

    /// <summary>The picture is changing: draw it quickly now, smooth once it has been still a moment.</summary>
    private void Moved()
    {
        _moving = true;
        _movedAt = _clock.Elapsed.TotalSeconds;
        InvalidateVisual();
    }

    /// <summary>Works out the full light now (tests and exports, which do not wait).</summary>
    internal void Settle()
    {
        _radiance = ComputeRadiance(coarse: false);
        _lightDirty = false;
        _refinePending = false;
        _moving = false;
        InvalidateVisual();
    }

    /// <summary>Where <paramref name="point"/> of the scene is on screen, or null when it is behind the camera.</summary>
    protected Point? Project(Vector3 point, Rect bounds)
    {
        var clip = Vector4.Transform(new Vector4(point, 1), Camera.ViewProjection(bounds.Width / bounds.Height));
        if (clip.W < 0.05f)
        {
            return null;
        }

        return new Point(
            bounds.X + ((clip.X / clip.W) + 1) * 0.5 * bounds.Width,
            bounds.Y + (1 - (clip.Y / clip.W)) * 0.5 * bounds.Height);
    }

    #region lifecycle

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        _clock.Start();
        LocalizationService.Instance.PropertyChanged += OnLanguageChanged;
        RequestFrame();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        _clock.Stop();
        LocalizationService.Instance.PropertyChanged -= OnLanguageChanged;
        _bitmap?.Dispose();
        _bitmap = null;
    }

    private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();

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
        if (_refinePending && !_lightDirty && now - _changedAt >= RefineDelaySeconds)
        {
            Settle();
        }
        else if (_moving && _dragFrom is null && now - _movedAt >= RefineDelaySeconds)
        {
            _moving = false;
            InvalidateVisual();
        }

        Tick(_clock.Elapsed.TotalSeconds);
        RequestFrame();
    }

    /// <summary>Called every frame while on screen, with the book's clock in seconds (for animation).</summary>
    protected virtual void Tick(double seconds)
    {
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
        Camera.Drag(at.X - from.X, at.Y - from.Y);
        _dragFrom = at;
        e.Handled = true;
        Moved();
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
        Camera.Distance *= Math.Pow(0.9, e.Delta.Y);
        e.Handled = true;
        Moved();
    }

    #endregion

    #region drawing

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = new Rect(Bounds.Size);
        if (bounds.Width < 2 || bounds.Height < 2)
        {
            return;
        }

        if (_lightDirty || _radiance.Length != Mesh.VertexCount)
        {
            var light = Stopwatch.StartNew();
            _radiance = ComputeRadiance(coarse: _refinePending);
            _lightDirty = false;
            _lightMs = light.Elapsed.TotalMilliseconds;
        }

        var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var factor = scaling * Math.Clamp(RenderScale, 0.1, 1);
        var width = Math.Max(2, (int)(bounds.Width * factor));
        var height = Math.Max(2, (int)(bounds.Height * factor));

        // Still: drawn at twice the size each way and shrunk, four samples a pixel, so edges are smooth. Moving: plain.
        var sharp = !_moving && _dragFrom is null;
        var samples = sharp ? 2 : 1;
        var watch = Stopwatch.StartNew();
        Raster.Resize(width * samples, height * samples);
        Raster.Clear(Sky.Top, Sky.Bottom);
        Raster.Draw(Mesh, _radiance, Camera.ViewProjection(bounds.Width / bounds.Height));
        var pixels = Raster.Pixels;
        if (sharp)
        {
            if (_sharp.Length != width * height)
            {
                _sharp = new int[width * height];
            }

            Raster.Downsample2x(_sharp);
            pixels = _sharp;
        }

        if (_bitmap is null || _bitmap.PixelSize.Width != width || _bitmap.PixelSize.Height != height)
        {
            _bitmap?.Dispose();
            _bitmap = new WriteableBitmap(new PixelSize(width, height), new Avalonia.Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        }

        SpaceDrawing.WritePixels(_bitmap, pixels, width, height);
        IsSharp = sharp;
        _pictureSize = new PixelSize(width, height);
        _rasterMs = (_rasterMs * 0.8) + (watch.Elapsed.TotalMilliseconds * 0.2);
        SpaceDrawing.DrawSmooth(context, _bitmap, bounds);

        DrawOverlay(context, bounds);
        if (ShowStats)
        {
            DrawStats(context, width, height);
        }
    }

    private void DrawStats(DrawingContext context, int width, int height)
    {
        var text = string.Format(
            CultureInfo.InvariantCulture, "{0}×{1} px{5} · raster {2:0.0} ms · light {3:0} ms{4}",
            width, height, _rasterMs, _lightMs, _refinePending ? " (coarse)" : string.Empty, IsSharp ? " ×4" : string.Empty);
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 11, Brushes.White);
        context.FillRectangle(new SolidColorBrush(Color.FromArgb(0x99, 0, 0, 0)), new Rect(8, 8, formatted.Width + 12, formatted.Height + 6), 4);
        context.DrawText(formatted, new Point(14, 11));
    }

    #endregion
}
