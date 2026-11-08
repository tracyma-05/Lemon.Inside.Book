using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Bicycle;

/// <summary>
/// Spread 4: the back hub cut open. The inner driver (turned by the sprocket and so the pedals) carries two pawls
/// pressed outwards by springs; the wheel's shell has ratchet teeth inside. The pawls push the shell round, or, when
/// the wheel runs ahead, slide up its teeth and drop off each one with a click. The reader works the pedals with a grip
/// on the driver, dragged the way its top turns: to the right to pedal forwards, faster the further, to the left to
/// pedal backwards, at the middle to stop.
/// </summary>
public sealed class FreewheelDiagram : GearPicture
{
    public static readonly StyledProperty<double> PedalSpeedProperty =
        AvaloniaProperty.Register<FreewheelDiagram, double>(nameof(PedalSpeed), 0.6, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The picture runs at this share of real time, so the pawls can be followed.</summary>
    private const double TimeScale = 0.35;

    /// <summary>Where the first pawl's tip sits on the driver, radians (screen, clockwise from 3 o'clock).</summary>
    private const double PawlAngle = -Math.PI / 2;

    private static readonly IImmutableBrush Shell = new ImmutableSolidColorBrush(Color.FromRgb(0x5A, 0x64, 0x78));
    private static readonly IImmutableBrush DriverFill = new ImmutableSolidColorBrush(Color.FromRgb(0x8A, 0x6E, 0x2E));
    private static readonly IImmutableBrush PawlFill = new ImmutableSolidColorBrush(Color.FromRgb(0xF2, 0xC9, 0x4C));
    private static readonly IPen SpringPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x8F, 0xD8, 0xF0)), 1.4);
    private static readonly IImmutableBrush Click = new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0xE6, 0x8A));

    /// <summary>The slider's range of pedalling, turns a second.</summary>
    private const double LeastPedal = -0.4;
    private const double MostPedal = 1;

    private readonly Freewheel _hub = new();
    private int _clicksSeen;
    private double _sinceClick = 10;

    static FreewheelDiagram()
    {
        AffectsRender<FreewheelDiagram>(PedalSpeedProperty);
    }

    /// <summary>The pedals' (and the driver's) speed, turns a second; negative is backwards.</summary>
    public double PedalSpeed
    {
        get => GetValue(PedalSpeedProperty);
        set => SetValue(PedalSpeedProperty, value);
    }

    internal Freewheel Hub => _hub;

    protected override void Advance(double seconds)
    {
        _hub.Step(seconds * TimeScale, PedalSpeed);
        _sinceClick += seconds;
        if (_hub.Clicks != _clicksSeen)
        {
            _clicksSeen = _hub.Clicks;
            _sinceClick = 0;
        }
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var radius = Math.Min(bounds.Width * 0.4, bounds.Height * 0.34);
        var centre = new Point(bounds.Width * 0.5, (bounds.Height * 0.12) + radius + 20);
        var valley = radius * 0.66;
        var tip = radius * 0.57;
        var driverRadius = radius * 0.4;
        var shellAngle = 2 * Math.PI * _hub.Wheel;
        var driverAngle = 2 * Math.PI * _hub.Driver;
        var pitch = 2 * Math.PI / Freewheel.Teeth;

        // The shell: its flanges with spoke holes outside, ratchet teeth inside. A tooth's steep face is at each
        // multiple of the pitch; from there the inside rises gently (anticlockwise) to the next steep face.
        var shell = new StreamGeometry();
        using (var g = shell.Open())
        {
            g.SetFillRule(FillRule.EvenOdd);
            const int Segments = 96;
            for (var i = 0; i < Segments; i++)
            {
                var p = BicycleParts.At(centre, radius, 2 * Math.PI * i / Segments);
                if (i == 0) g.BeginFigure(p, true);
                else g.LineTo(p);
            }

            g.EndFigure(true);
            var face = shellAngle + PawlAngle + (pitch * 0.02);
            for (var j = 0; j < Freewheel.Teeth; j++)
            {
                var a = face + (j * pitch);
                var first = BicycleParts.At(centre, valley, a);
                if (j == 0) g.BeginFigure(first, true);
                else g.LineTo(first);
                g.LineTo(BicycleParts.At(centre, tip, a));
                g.LineTo(BicycleParts.At(centre, valley, a + pitch - 0.001));
            }

            g.EndFigure(true);
        }

        context.DrawGeometry(Shell, new ImmutablePen(BicycleParts.Dark, 1.5), shell);
        for (var k = 0; k < 16; k++)
        {
            var hole = BicycleParts.At(centre, radius * 0.86, shellAngle + (k * Math.PI / 8));
            context.DrawEllipse(BicycleParts.Dark, null, hole, radius * 0.025 + 1, radius * 0.025 + 1);
        }

        Mark("ratchet", centre, radius);

        // The driver, with the two pawls and their springs.
        context.DrawEllipse(DriverFill, new ImmutablePen(BicycleParts.Dark, 1.5), centre, driverRadius, driverRadius);
        var spoke = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x80, 0x10, 0x14, 0x1E)), 3);
        for (var k = 0; k < 6; k++)
        {
            var a = driverAngle + (k * Math.PI / 3);
            context.DrawLine(spoke, BicycleParts.At(centre, driverRadius * 0.3, a), BicycleParts.At(centre, driverRadius * 0.85, a));
        }

        context.DrawEllipse(new ImmutableSolidColorBrush(Gold), null, centre, driverRadius * 0.25, driverRadius * 0.25);
        DiagramText.Draw(context, DiagramText.Get("Bicycle_Freewheel_Driver"), centre + new Point(0, driverRadius * 0.45), 11.5, DiagramText.Ink, 0.5, 0.5);
        Mark("driver", centre, driverRadius * 0.6);

        // The pedals' grip, on the driver above its axle: as far right of the middle as the pedals turn forwards.
        var reach = driverRadius * 0.85;
        var gripY = centre.Y - (driverRadius * 0.45);
        var gripAt = new Point(centre.X + (reach * Math.Clamp(PedalSpeed, LeastPedal, MostPedal)), gripY);
        var track = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x70, 0xFF, 0xF4, 0xD6)), 2, new ImmutableDashStyle([3, 3], 0));
        context.DrawLine(track, new Point(centre.X + (reach * LeastPedal), gripY), new Point(centre.X + reach, gripY));
        context.DrawLine(track, new Point(centre.X, gripY - 5), new Point(centre.X, gripY + 5));
        Grip("pedals", gripAt, GripAxis.Horizontal, DiagramText.Get("Bicycle_Grip_Pedals"),
            at => PedalSpeed = Math.Round(Math.Clamp((at.X - centre.X) / reach, LeastPedal, MostPedal) * 20) / 20);

        var tipRadius = valley - (_hub.Lift * (valley - tip)) - 1.5;
        for (var n = 0; n < 2; n++)
        {
            var at = driverAngle + PawlAngle + (n * Math.PI);
            var pivot = BicycleParts.At(centre, driverRadius * 0.92, at - 0.42);
            var end = BicycleParts.At(centre, tipRadius, at);
            DrawPawl(context, pivot, end, radius * 0.05);
            var hold = BicycleParts.At(centre, driverRadius * 0.82, at - 0.12);
            var push = Lerp(pivot, end, 0.6);
            DrawSpring(context, hold, push);
            if (n == 0)
            {
                Mark("pawl", [pivot, end], 4);
                Mark("spring", [hold, push], 0);
                if (_sinceClick < 0.15)
                {
                    DiagramText.Draw(context, DiagramText.Get("Bicycle_Freewheel_Click"), BicycleParts.At(centre, radius * 1.08, at), 15, Click, 0.5, 1, bold: true);
                }
            }
        }

        // Which way each turns, and what the pawls are doing.
        var status = new Point(bounds.X + 18, centre.Y + radius + 24);
        var line = DiagramText.Draw(context, DiagramText.Get(_hub.Engaged ? "Bicycle_Freewheel_Driving" : "Bicycle_Freewheel_Coasting"),
            status, 14, _hub.Engaged ? DiagramText.Gold : DiagramText.Cyan, bold: true);
        line = DiagramText.Draw(context, DiagramText.Format("Bicycle_Freewheel_Speeds", PedalSpeed.ToString("0.00"), _hub.WheelSpeed.ToString("0.00")),
            new Point(status.X, line.Bottom + 6), 12.5, DiagramText.Ink);
        DiagramText.Draw(context, DiagramText.Format("Bicycle_Freewheel_Clicks", _hub.Clicks), new Point(status.X, line.Bottom + 4), 12.5, DiagramText.Faint);
    }

    /// <summary>A pawl: a tapered bar from its pivot to the tip that catches the teeth.</summary>
    private static void DrawPawl(DrawingContext context, Point pivot, Point end, double width)
    {
        var d = end - pivot;
        var length = Math.Max(1e-6, Math.Sqrt((d.X * d.X) + (d.Y * d.Y)));
        var n = new Point(-d.Y / length, d.X / length);
        var shape = new StreamGeometry();
        using (var g = shape.Open())
        {
            g.BeginFigure(pivot + (n * width), true);
            g.LineTo(end + (n * width * 0.25));
            g.LineTo(end - (n * width * 0.25));
            g.LineTo(pivot - (n * width));
            g.EndFigure(true);
        }

        context.DrawGeometry(PawlFill, new ImmutablePen(BicycleParts.Dark, 1.2), shape);
        context.DrawEllipse(BicycleParts.Dark, null, pivot, width * 0.5, width * 0.5);
    }

    /// <summary>A small coil spring drawn as a zigzag.</summary>
    private static void DrawSpring(DrawingContext context, Point from, Point to)
    {
        var d = to - from;
        var length = Math.Max(1e-6, Math.Sqrt((d.X * d.X) + (d.Y * d.Y)));
        var n = new Point(-d.Y / length, d.X / length) * 3;
        var zig = new StreamGeometry();
        using (var g = zig.Open())
        {
            g.BeginFigure(from, false);
            const int Turns = 6;
            for (var i = 1; i < Turns; i++)
            {
                g.LineTo(Lerp(from, to, (double)i / Turns) + (n * (i % 2 == 0 ? -1 : 1)));
            }

            g.LineTo(to);
            g.EndFigure(false);
        }

        context.DrawGeometry(null, SpringPen, zig);
    }

    private static Point Lerp(Point a, Point b, double t) => new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t));
}
