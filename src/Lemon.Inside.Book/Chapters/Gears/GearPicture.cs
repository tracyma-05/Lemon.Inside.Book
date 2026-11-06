using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Book.Shell.Infrastructures.Localization;
using Lemon.Book.Shell.Infrastructures.Timing;

namespace Lemon.Inside.Book.Chapters.Gears;

/// <summary>
/// The base of the Gears chapter's pictures: flat drawings on a dark plate, moving on the book's clock (so a video
/// export steps them frame by frame). Gears are drawn from <see cref="GearGeometry"/> outlines in units of the module,
/// with y up, at a centre and scale on screen.
/// </summary>
public abstract class GearPicture : Control
{
    public static readonly IImmutableBrush Plate = new ImmutableLinearGradientBrush(
        [new ImmutableGradientStop(0, Color.FromRgb(0x14, 0x1B, 0x2A)), new ImmutableGradientStop(1, Color.FromRgb(0x08, 0x0B, 0x12))],
        startPoint: new RelativePoint(0, 0, RelativeUnit.Relative),
        endPoint: new RelativePoint(0, 1, RelativeUnit.Relative));

    public static readonly Color Gold = Color.FromRgb(0xF2, 0xC9, 0x4C);
    public static readonly Color Steel = Color.FromRgb(0x8F, 0xA3, 0xBF);
    public static readonly Color Copper = Color.FromRgb(0xD0, 0x8A, 0x5A);
    public static readonly Color Teal = Color.FromRgb(0x5F, 0xC2, 0xB0);

    private readonly BookStopwatch _clock = new();
    private readonly Dictionary<string, Geometry> _shapes = [];
    private bool _attached;
    private double _lastFrame;

    protected GearPicture()
    {
        ClipToBounds = true;
    }

    /// <summary>Whether the picture moves by itself (and so is redrawn every frame).</summary>
    protected virtual bool Animates => true;

    /// <summary>Seconds on the book's clock since the picture came on screen.</summary>
    protected double Seconds => _clock.Elapsed.TotalSeconds;

    /// <summary>Called every frame while on screen with the seconds since the last one (for motion that can change speed).</summary>
    protected virtual void Advance(double seconds)
    {
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        _clock.Start();
        _lastFrame = Seconds;
        LocalizationService.Instance.PropertyChanged += OnLanguageChanged;
        RequestFrame();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _attached = false;
        _clock.Stop();
        LocalizationService.Instance.PropertyChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => InvalidateVisual();

    private void RequestFrame()
    {
        if (_attached && Animates)
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

        var now = Seconds;
        Advance(Math.Min(now - _lastFrame, 0.1));
        _lastFrame = now;
        InvalidateVisual();
        RequestFrame();
    }

    public sealed override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(Plate, bounds);
        if (bounds.Width >= 60 && bounds.Height >= 60)
        {
            Draw(context, bounds);
        }
    }

    protected abstract void Draw(DrawingContext context, Rect bounds);

    /// <summary>A point of the model (y up), <paramref name="scale"/> pixels per module from <paramref name="origin"/>.</summary>
    protected static Point ToScreen(Point model, Point origin, double scale) =>
        new(origin.X + (model.X * scale), origin.Y - (model.Y * scale));

    /// <summary>An outline (closed, in model units) as a shape, kept for the next frame under <paramref name="key"/>.</summary>
    protected Geometry Shape(string key, Func<IReadOnlyList<Point>> outline)
    {
        if (!_shapes.TryGetValue(key, out var shape))
        {
            if (_shapes.Count > 64)
            {
                _shapes.Clear();
            }

            var geometry = new StreamGeometry();
            using (var g = geometry.Open())
            {
                AddFigure(g, outline());
            }

            _shapes[key] = shape = geometry;
        }

        return shape;
    }

    /// <summary>A ring with teeth inside: a disc with the tooth spaces cut out of it.</summary>
    protected Geometry RingShape(int teeth, double pressureAngle, double outerRadius)
    {
        var key = $"ring:{teeth}:{pressureAngle:0.0000}:{outerRadius:0.00}";
        if (!_shapes.TryGetValue(key, out var shape))
        {
            var geometry = new StreamGeometry();
            using (var g = geometry.Open())
            {
                g.SetFillRule(FillRule.EvenOdd);
                const int Segments = 96;
                var circle = Enumerable.Range(0, Segments)
                    .Select(i => new Point(outerRadius * Math.Cos(2 * Math.PI * i / Segments), outerRadius * Math.Sin(2 * Math.PI * i / Segments)))
                    .ToList();
                AddFigure(g, circle);
                AddFigure(g, GearGeometry.RingSpaces(teeth, pressureAngle));
            }

            _shapes[key] = shape = geometry;
        }

        return shape;
    }

    private static void AddFigure(StreamGeometryContext g, IReadOnlyList<Point> points)
    {
        g.BeginFigure(points[0], true);
        for (var i = 1; i < points.Count; i++)
        {
            g.LineTo(points[i]);
        }

        g.EndFigure(true);
    }

    /// <summary>
    /// Draws a gear of <paramref name="teeth"/> at <paramref name="centre"/> (screen), turned to <paramref name="angle"/>
    /// (counter-clockwise, radians), <paramref name="scale"/> pixels per module, with a hub and a mark on tooth 0 so its
    /// turning shows.
    /// </summary>
    protected void DrawGear(DrawingContext context, int teeth, double pressureAngle, Point centre, double scale, double angle,
        Color color, double opacity = 1, bool mark = true)
    {
        var shape = Shape($"gear:{teeth}:{pressureAngle:0.0000}", () => GearGeometry.Outline(teeth, pressureAngle));
        var fill = new ImmutableSolidColorBrush(Shade(color, 0.88, opacity));
        using (context.PushTransform(Placement(centre, scale, angle)))
        {
            context.DrawGeometry(fill, new ImmutablePen(new ImmutableSolidColorBrush(Shade(color, 0.45, opacity)), 1.2 / scale), shape);
        }

        var r = GearGeometry.PitchRadius(teeth) * scale;
        var hub = Math.Max(4, Math.Min(r * 0.22, 14));
        context.DrawEllipse(new ImmutableSolidColorBrush(Shade(color, 0.5, opacity)), null, centre, hub, hub);
        context.DrawEllipse(new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E)), null, centre, hub * 0.4, hub * 0.4);
        if (mark)
        {
            var at = ToScreen(new Point(Math.Cos(angle) * (r / scale - 1.6), Math.Sin(angle) * (r / scale - 1.6)), centre, scale);
            var dot = Math.Max(2.5, Math.Min(scale * 0.55, 6));
            context.DrawEllipse(new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E)), null, at, dot, dot);
        }
    }

    /// <summary>Model units, turned by <paramref name="angle"/>, flipped to y down, scaled and moved to <paramref name="centre"/>.</summary>
    protected static Matrix Placement(Point centre, double scale, double angle) =>
        Matrix.CreateRotation(angle) * Matrix.CreateScale(scale, -scale) * Matrix.CreateTranslation(centre.X, centre.Y);

    /// <summary>A circle drawn dashed, e.g. a gear's rolling or base circle.</summary>
    protected static void DrawCircle(DrawingContext context, Point centre, double radius, IBrush brush, double thickness = 1, bool dashed = true) =>
        context.DrawEllipse(null, new ImmutablePen((IImmutableBrush)brush, thickness, dashed ? new ImmutableDashStyle([4, 4], 0) : null), centre, radius, radius);

    protected static Color Shade(Color color, double light, double opacity = 1) => Color.FromArgb(
        (byte)(255 * Math.Clamp(opacity, 0, 1)),
        (byte)Math.Clamp(color.R * light, 0, 255),
        (byte)Math.Clamp(color.G * light, 0, 255),
        (byte)Math.Clamp(color.B * light, 0, 255));
}
