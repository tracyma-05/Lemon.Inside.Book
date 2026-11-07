using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Watch;

/// <summary>
/// Spread 2: the dial and, below, the going train laid out in a row, from the barrel to the escape wheel, each wheel
/// with how fast it turns. Time runs at the speed the reader picks.
/// </summary>
public sealed class HandsDiagram : GearPicture
{
    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<HandsDiagram, double>(nameof(Speed), 60);

    /// <summary>The time the picture starts at: 10:08:30, a watchmaker's favourite.</summary>
    public const double StartSeconds = (10 * 3600) + (8 * 60) + 30;

    private static readonly double Pressure = 20 * Math.PI / 180;
    private static readonly IImmutableBrush Face = new ImmutableSolidColorBrush(Color.FromRgb(0xF4, 0xEF, 0xE4));
    private static readonly IPen TickPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x30, 0x34, 0x3C)), 1);
    private static readonly IPen HourTickPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x30, 0x34, 0x3C)), 3);

    private double _seconds = StartSeconds;

    /// <summary>How many times faster than real time the watch runs.</summary>
    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    /// <summary>The watch's time of day, in seconds.</summary>
    internal double Time => _seconds;

    protected override void Advance(double seconds) => _seconds += seconds * Speed;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var dial = Math.Min(bounds.Width * 0.2, bounds.Height * 0.2);
        var centre = new Point(bounds.Center.X, (bounds.Height * 0.06) + dial + 8);
        DrawDial(context, centre, dial, _seconds);
        Mark("dial", centre, dial);
        var clock = TimeSpan.FromSeconds(_seconds % 86400);
        DiagramText.Draw(context, clock.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture), new Point(bounds.Right - 16, bounds.Y + 14), 14, DiagramText.Ink, 1, 0, bold: true);
        var train = new Rect(bounds.X + 12, centre.Y + dial + 16, bounds.Width - 24, bounds.Bottom - centre.Y - dial - 24);
        DrawTrain(context, train, _seconds);
        Mark("train", train.Deflate(8));
    }

    /// <summary>A white dial with hour and minute marks and three hands.</summary>
    public static void DrawDial(DrawingContext context, Point centre, double radius, double seconds)
    {
        context.DrawEllipse(Face, new ImmutablePen(new ImmutableSolidColorBrush(WatchParts.Brass), 4), centre, radius, radius);
        for (var i = 0; i < 60; i++)
        {
            var a = i * Math.PI / 30;
            var inner = i % 5 == 0 ? 0.84 : 0.91;
            context.DrawLine(i % 5 == 0 ? HourTickPen : TickPen, Hand(centre, radius * inner, a), Hand(centre, radius * 0.96, a));
        }

        var (hour, minute, second) = GoingTrain.Hands(seconds);
        var ink = new ImmutableSolidColorBrush(Color.FromRgb(0x20, 0x24, 0x2C));
        context.DrawLine(new ImmutablePen(ink, Math.Max(3, radius * 0.06), lineCap: PenLineCap.Round), centre, Hand(centre, radius * 0.5, hour * Math.PI / 180));
        context.DrawLine(new ImmutablePen(ink, Math.Max(2, radius * 0.04), lineCap: PenLineCap.Round), centre, Hand(centre, radius * 0.78, minute * Math.PI / 180));
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(WatchParts.Ruby), 1.5, lineCap: PenLineCap.Round),
            Hand(centre, -radius * 0.15, second * Math.PI / 180), Hand(centre, radius * 0.88, second * Math.PI / 180));
        context.DrawEllipse(ink, null, centre, 4, 4);
    }

    /// <summary>A point <paramref name="length"/> from the centre at <paramref name="angle"/> clockwise from 12.</summary>
    private static Point Hand(Point centre, double length, double angle) =>
        new(centre.X + (length * Math.Sin(angle)), centre.Y - (length * Math.Cos(angle)));

    /// <summary>
    /// The going train in a row. Each pair of wheel and pinion has its own tooth size, so every wheel is drawn the same
    /// size; a wheel and the pinion on its axle turn together.
    /// </summary>
    private void DrawTrain(DrawingContext context, Rect area, double seconds)
    {
        var wheels = GoingTrain.Wheels;
        var turns = GoingTrain.TurnsPerHour();
        var count = wheels.Count;
        var radius = Math.Min(area.Width / 6.9, area.Height * 0.24);

        // Pixels per tooth size: the wheel on axle i and the pinion it drives (on axle i + 1) share one.
        var modules = wheels.Select(w => radius / (w.Wheel / 2.0)).ToArray();
        var axles = new Point[count];
        var towards = new double[count];
        axles[0] = new Point(area.X + radius + 4, area.Y + (area.Height * 0.55));
        for (var i = 0; i < count - 1; i++)
        {
            var step = radius + (modules[i] * wheels[i].NextPinion / 2.0);
            towards[i] = (i % 2 == 0 ? 1 : -1) * 0.38;
            axles[i + 1] = axles[i] + new Point(step * Math.Cos(towards[i]), -step * Math.Sin(towards[i]));
        }

        var shift = (area.Right - radius - 4 - axles[^1].X) / 2;
        axles = axles.Select(a => a + new Point(Math.Max(0, shift), 0)).ToArray();

        var angles = new double[count];
        angles[0] = -2 * Math.PI * turns[0] * seconds / 3600;
        for (var i = 0; i < count - 1; i++)
        {
            angles[i + 1] = GearGeometry.MeshAngle(wheels[i].Wheel, angles[i], wheels[i].NextPinion, towards[i]);
        }

        for (var i = 0; i < count; i++)
        {
            if (wheels[i].Key == "Escape")
            {
                // The escape wheel has its own tooth shape, smaller than the train's wheels.
                using (context.PushTransform(Placement(axles[i], radius * 0.8, angles[i])))
                {
                    context.DrawGeometry(new ImmutableSolidColorBrush(Color.FromRgb(0xB8, 0xC6, 0xD8), 0.9), null,
                        Shape("escape", () => WatchParts.EscapeOutline(GoingTrain.EscapeTeeth)));
                }

                continue;
            }

            var color = i == 1 ? GearPicture.Gold : i == 3 ? WatchParts.Ruby : i == 0 ? WatchParts.Brass : WatchParts.Steel;
            DrawGear(context, wheels[i].Wheel, Pressure, axles[i], modules[i], angles[i], color, 0.88);
        }

        for (var i = 1; i < count; i++)
        {
            DrawGear(context, wheels[i - 1].NextPinion, Pressure, axles[i], modules[i - 1], angles[i], WatchParts.Steel, mark: false);
        }

        for (var i = 0; i < count; i++)
        {
            // Labels above the raised axles and below the others, so neighbours do not collide.
            var above = i % 2 == 1;
            var at = new Point(axles[i].X, axles[i].Y + (above ? -radius - 34 : radius + 8));
            var r = DiagramText.Draw(context, DiagramText.Get($"Watch_Train_{wheels[i].Key}"), at, 12, DiagramText.Ink, 0.5, 0, bold: true);
            DiagramText.Draw(context, DiagramText.Get($"Watch_Train_{wheels[i].Key}Speed"), new Point(at.X, r.Bottom + 2), 11.5, DiagramText.Faint, 0.5);
        }
    }
}
