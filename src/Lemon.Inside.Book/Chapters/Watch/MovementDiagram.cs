using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Watch;

/// <summary>The layers of the movement, from the bottom up.</summary>
public enum MovementLayer
{
    Plate,
    Barrel,
    Train,
    Escapement,
    MotionWorks,
    Dial,
}

/// <summary>
/// Spread 6: the whole movement taken apart, layer above layer, seen from the side and above. The reader pulls the
/// layers apart and hides the ones in the way; the wheels keep turning.
/// </summary>
public sealed class MovementDiagram : GearPicture
{
    public static readonly StyledProperty<double> ExplodeProperty =
        AvaloniaProperty.Register<MovementDiagram, double>(nameof(Explode), 0.6);

    public static readonly StyledProperty<int> HiddenProperty =
        AvaloniaProperty.Register<MovementDiagram, int>(nameof(Hidden));

    /// <summary>Layers seen from above at a slant: circles look this much flatter.</summary>
    private const double Squash = 0.4;

    /// <summary>The watch runs this many times faster than life, so the slow wheels show they turn.</summary>
    private const double Faster = 6;

    private static readonly double Pressure = 20 * Math.PI / 180;
    private static readonly IImmutableBrush PlateBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x5A, 0x63, 0x72));
    private static readonly IImmutableBrush Edge = new ImmutableSolidColorBrush(Color.FromRgb(0x3A, 0x41, 0x4E));
    private static readonly IPen GuidePen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x40, 0xE6, 0xEA, 0xF2)), 1, new ImmutableDashStyle([3, 4], 0));

    private double _seconds = HandsDiagram.StartSeconds;
    private double _real;

    /// <summary>How far apart the layers are, 0 (put together) to 1.</summary>
    public double Explode
    {
        get => GetValue(ExplodeProperty);
        set => SetValue(ExplodeProperty, value);
    }

    /// <summary>The hidden layers, one bit per <see cref="MovementLayer"/>.</summary>
    public int Hidden
    {
        get => GetValue(HiddenProperty);
        set => SetValue(HiddenProperty, value);
    }

    protected override void Advance(double seconds)
    {
        _real += seconds;
        _seconds += seconds * Faster;
    }

    /// <summary>How high each layer sits, in units of the plate's radius, for this much pulling apart.</summary>
    public static double LayerHeight(MovementLayer layer, double explode) => (int)layer * (0.1 + (0.6 * Math.Clamp(explode, 0, 1)));

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var layers = Enum.GetValues<MovementLayer>();
        // As big as fits: putting the layers together leaves room to look closer.
        var top = LayerHeight(MovementLayer.Dial, Explode) + (2 * Squash) + 0.25;
        var size = Math.Min(bounds.Width * 0.36, bounds.Height * 0.86 / top);
        var cx = bounds.Width * 0.6;
        var baseY = bounds.Bottom - (size * Squash) - (bounds.Height * 0.06);

        // The axis the layers stack along.
        context.DrawLine(GuidePen, new Point(cx, baseY), new Point(cx, baseY - (LayerHeight(MovementLayer.Dial, Explode) * size)));
        foreach (var layer in layers)
        {
            var y = baseY - (LayerHeight(layer, Explode) * size);
            var label = new Point(bounds.X + 16, y);
            var hidden = (Hidden & (1 << (int)layer)) != 0;
            DiagramText.Draw(context, DiagramText.Get($"Watch_Layer_{layer}"), label, 12.5, hidden ? DiagramText.Faint : DiagramText.Ink, 0, 0.5, bold: !hidden);
            if (hidden)
            {
                context.DrawEllipse(null, GuidePen, new Point(cx, y), size, size * Squash);
                continue;
            }

            var centre = new Point(cx, y);
            var flat = new Rect(cx - size, y - (size * Squash), 2 * size, 2 * size * Squash);
            Mark(layer.ToString().ToLowerInvariant(), flat);
            Mark("movement", flat);
            using (context.PushTransform(Matrix.CreateTranslation(-cx, -y) * Matrix.CreateScale(1, Squash) * Matrix.CreateTranslation(cx, y)))
            {
                DrawLayer(context, layer, centre, size);
            }
        }
    }

    /// <summary>One layer laid flat, centred on <paramref name="centre"/>, the plate <paramref name="size"/> across from the middle.</summary>
    private void DrawLayer(DrawingContext context, MovementLayer layer, Point centre, double size)
    {
        Point At(double x, double y) => new(centre.X + (x * size), centre.Y + (y * size));
        var turns = GoingTrain.TurnsPerHour();
        double Angle(int wheel) => -2 * Math.PI * turns[wheel] * _seconds / 3600 * (wheel % 2 == 0 ? 1 : -1);

        switch (layer)
        {
            case MovementLayer.Plate:
                context.DrawEllipse(Edge, null, centre + new Point(0, size * 0.08), size, size);
                context.DrawEllipse(PlateBrush, null, centre, size, size);
                foreach (var (x, y) in new[] { (0.0, 0.0), (-0.42, -0.2), (0.4, 0.22), (0.55, -0.28), (0.3, -0.62), (-0.32, 0.5) })
                {
                    context.DrawEllipse(new ImmutableSolidColorBrush(WatchParts.Ruby), null, At(x, y), size * 0.025, size * 0.025);
                }

                break;
            case MovementLayer.Barrel:
                DrawGear(context, 80, Pressure, At(-0.42, -0.2), size * 0.4 / 40, Angle(0), WatchParts.Brass);
                break;
            case MovementLayer.Train:
                DrawGear(context, 64, Pressure, At(0, 0), size * 0.3 / 32, Angle(1), GearPicture.Gold);
                DrawGear(context, 60, Pressure, At(0.4, 0.22), size * 0.24 / 30, Angle(2), WatchParts.Steel);
                DrawGear(context, 80, Pressure, At(0.55, -0.28), size * 0.26 / 40, Angle(3), WatchParts.Ruby);
                break;
            case MovementLayer.Escapement:
                var escape = At(0.3, -0.62);
                using (context.PushTransform(Placement(escape, size * 0.14, Angle(4))))
                {
                    context.DrawGeometry(new ImmutableSolidColorBrush(Color.FromRgb(0xB8, 0xC6, 0xD8)), null,
                        Shape("escape", () => WatchParts.EscapeOutline(GoingTrain.EscapeTeeth)));
                }

                var balance = At(-0.32, 0.5);
                context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x7F, 0x8C, 0xA3)), Math.Max(3, size * 0.03), lineCap: PenLineCap.Round),
                    At(0.18, -0.42), At(-0.18, 0.3));
                var swing = Balance.Angle(_real, 200, 0.5);
                WatchParts.DrawBalance(context, balance, size * 0.3, swing, pin: false);
                WatchParts.DrawHairspring(context, balance, size * 0.05, size * 0.19, swing, coils: 6);
                break;
            case MovementLayer.MotionWorks:
                DrawGear(context, 36, Pressure, At(0.24, 0.14), size * 0.15 / 18, -Angle(1) / 3, WatchParts.Steel);
                DrawGear(context, 48, Pressure, At(0, 0), size * 0.2 / 24, Angle(1) / 12, WatchParts.Brass, mark: false);
                DrawGear(context, 12, Pressure, At(0, 0), size * 0.06 / 6, Angle(1), GearPicture.Gold, mark: false);
                break;
            default:
                context.DrawEllipse(Edge, null, centre + new Point(0, size * 0.05), size * 0.97, size * 0.97);
                HandsDiagram.DrawDial(context, centre, size * 0.97, _seconds);
                break;
        }
    }
}
