using Avalonia;
using Avalonia.Data;
using Lemon.Inside.Book.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Watch;

/// <summary>
/// Spread 5: the back of an automatic watch. The rotor swings as the wrist moves; through the reverser it turns the
/// winding wheel, always forwards, and the mainspring fills; meanwhile the watch keeps using it. The reader can take the
/// rotor and swing it by hand (it winds just the same), then let go and watch it swing back down under its own weight;
/// a tap on the winding wheel puts the reverser in or takes it out.
/// </summary>
public sealed class RotorDiagram : GearPicture
{
    public static readonly StyledProperty<double> ActivityProperty =
        AvaloniaProperty.Register<RotorDiagram, double>(nameof(Activity), 0.6, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> ReverserProperty =
        AvaloniaProperty.Register<RotorDiagram, bool>(nameof(Reverser), true, defaultBindingMode: BindingMode.TwoWay);

    private static readonly double Pressure = 20 * Math.PI / 180;
    private static readonly IImmutableBrush Case = new ImmutableSolidColorBrush(Color.FromRgb(0x2A, 0x30, 0x3C));
    private static readonly IImmutableBrush Track = new ImmutableSolidColorBrush(Color.FromArgb(0x30, 0xE6, 0xEA, 0xF2));

    private readonly AutoWinding _winding = new();
    private double _seconds;
    private double? _rotor;

    /// <summary>How far the rotor is from where the wrist would have it (radians), and how fast that is changing: a hand
    /// leaves it off, and it swings back like a pendulum, dying away.</summary>
    private double _offset, _offsetSpeed;

    /// <summary>The rotor's swing back after a hand lets go: its spring (per s²) and damping (per s).</summary>
    private const double Pull = 18, Damping = 2.2;

    /// <summary>How lively the wrist is, 0 (still) to 1.</summary>
    public double Activity
    {
        get => GetValue(ActivityProperty);
        set => SetValue(ActivityProperty, value);
    }

    /// <summary>Whether swings both ways wind the spring.</summary>
    public bool Reverser
    {
        get => GetValue(ReverserProperty);
        set => SetValue(ReverserProperty, value);
    }

    internal AutoWinding Winding => _winding;

    protected override void Advance(double seconds)
    {
        _seconds += seconds;
        var wrist = AutoWinding.RotorAngle(_seconds, Activity);
        if (Gripping)
        {
            // Held, it stays where the hand has it.
            _offset = (_rotor ?? wrist) - wrist;
            _offsetSpeed = 0;
        }
        else
        {
            // Let go, the weight swings back toward where the wrist would have it, the short way round (the same
            // place on the dial, so nothing is wound by the change).
            if (Math.Abs(_offset) > Math.PI)
            {
                var wrapped = Math.IEEERemainder(_offset, 2 * Math.PI);
                _rotor += wrapped - _offset;
                _offset = wrapped;
            }

            _offsetSpeed += ((-Pull * _offset) - (Damping * _offsetSpeed)) * seconds;
            _offset += _offsetSpeed * seconds;
        }

        Turn(wrist + _offset, seconds);
    }

    /// <summary>Moves the rotor to <paramref name="rotor"/>, winding the spring by however far it went.</summary>
    private void Turn(double rotor, double seconds)
    {
        _winding.Reverser = Reverser;
        _winding.Step(rotor - (_rotor ?? rotor), seconds);
        _rotor = rotor;
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var radius = Math.Min(bounds.Width * 0.36, bounds.Height * 0.34);
        var centre = new Point(bounds.Center.X, (bounds.Height * 0.08) + radius + 10);
        context.DrawEllipse(Case, new ImmutablePen(new ImmutableSolidColorBrush(WatchParts.Steel), 6), centre, radius, radius);

        // The winding wheel (it only ever turns one way) and the barrel it fills, under the rotor.
        var wheel = new Point(centre.X + (radius * 0.45), centre.Y - (radius * 0.42));
        DrawGear(context, 30, Pressure, wheel, radius * 0.2 / 15, -_winding.WheelTurned * 0.6, WatchParts.Brass);
        var barrel = new Point(centre.X - (radius * 0.4), centre.Y - (radius * 0.45));
        DrawGear(context, 48, Pressure, barrel, radius * 0.26 / 24, -_winding.Wound * 6, WatchParts.Brass, mark: false);

        // The rotor: a half-moon weight on the centre, hanging down when still.
        var down = (Math.PI / 2) + (_rotor ?? 0);
        var moon = new StreamGeometry();
        using (var g = moon.Open())
        {
            var r = radius * 0.94;
            g.BeginFigure(WatchParts.At(centre, r, down - (Math.PI / 2)), true);
            g.ArcTo(WatchParts.At(centre, r, down + (Math.PI / 2)), new Size(r, r), 0, false, SweepDirection.Clockwise);
            g.LineTo(WatchParts.At(centre, radius * 0.18, down + (Math.PI / 2)));
            g.ArcTo(WatchParts.At(centre, radius * 0.18, down - (Math.PI / 2)), new Size(radius * 0.18, radius * 0.18), 0, false, SweepDirection.CounterClockwise);
            g.EndFigure(true);
        }

        context.DrawGeometry(new ImmutableSolidColorBrush(Color.FromArgb(0xD8, 0xC8, 0x9A, 0x4A)),
            new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x7A, 0x5C, 0x2A)), 1.5), moon);
        context.DrawEllipse(new ImmutableSolidColorBrush(WatchParts.Steel), null, centre, radius * 0.08, radius * 0.08);
        Mark("rotor", centre, radius * 0.94);
        Mark("wheel", wheel, radius * 0.22);
        Mark("barrel", barrel, radius * 0.28);

        // The rotor's weight, to swing by hand: it follows the finger and winds as it goes.
        Grip("rotor", WatchParts.At(centre, radius * 0.4, down), GripAxis.Free, DiagramText.Get("Watch_Grip_Rotor"), at =>
        {
            var now = _rotor ?? 0;
            var towards = Math.Atan2(at.Y - centre.Y, at.X - centre.X) - (Math.PI / 2);
            var rotor = now + Math.IEEERemainder(towards - now, 2 * Math.PI);
            _offset = rotor - AutoWinding.RotorAngle(_seconds, Activity);
            _offsetSpeed = 0;
            Turn(rotor, 0);
        });
        Grip("reverser", wheel, GripAxis.Tap, DiagramText.Get("Watch_Grip_Reverser"), _ => Reverser = !Reverser);

        DiagramText.Draw(context, DiagramText.Get("Watch_Rotor_Rotor"), WatchParts.At(centre, radius * 0.66, down), 12, DiagramText.Ink, 0.5, 0.5, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Watch_Rotor_Wheel"), wheel + new Point(0, -(radius * 0.24)), 11.5, DiagramText.Faint, 0.5, 1);

        // The spring's reserve, as a bar.
        var bar = new Rect(bounds.X + 40, centre.Y + radius + 36, bounds.Width - 80, 12);
        context.FillRectangle(Track, bar, 4);
        Mark("reserve", bar);
        context.FillRectangle(DiagramText.Gold, new Rect(bar.X, bar.Y, bar.Width * _winding.Wound, bar.Height), 4);
        DiagramText.Draw(context, DiagramText.Format("Watch_Rotor_Reserve", (_winding.Wound * 100).ToString("0")), new Point(bar.X, bar.Y - 6), 12.5, DiagramText.Ink, 0, 1);
        if (_winding.Wound >= 0.999)
        {
            DiagramText.Draw(context, DiagramText.Get("Watch_Rotor_Full"), new Point(bar.Right, bar.Y - 6), 12, DiagramText.Gold, 1, 1);
        }
    }
}
