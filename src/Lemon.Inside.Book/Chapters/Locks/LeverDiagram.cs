using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Locks;

/// <summary>
/// Spread 5: a lever lock with its three levers pulled apart into rows, so each can be seen. Every lever has a window
/// the bolt's stump sits in and a gate running left from it; the key's bit (left) lifts each lever by its own step.
/// While every gate is level with the stump the bit can draw the bolt back out of the door frame (bottom). The reader
/// turns the key by dragging its bit round and taps the key's barrel to swap it for one with a step too high; the key
/// turns smoothly to where it is set, so the levers are seen rising on the bit and the bolt sliding as it goes.
/// </summary>
public sealed class LeverDiagram : GearPicture
{
    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<LeverDiagram, double>(nameof(Angle), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> WrongProperty =
        AvaloniaProperty.Register<LeverDiagram, bool>(nameof(Wrong), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>A lever's plate, mm: its width and height, the stump's place when locked, how far the bolt travels.</summary>
    private const double PlateWidth = 30;
    private const double PlateHeight = 14;
    private const double Rest = 22;
    private const double Travel = 12;

    /// <summary>The stump's bottom in a lever's own frame when the lever is down, and its size.</summary>
    private const double StumpLow = 9;
    private const double StumpSize = 3;

    /// <summary>Each row's height, mm.</summary>
    private const double Row = 21;

    private static readonly Color[] LeverColors =
    [
        Color.FromRgb(0xC9, 0xA2, 0x4E), Color.FromRgb(0x5F, 0xC2, 0xB0), Color.FromRgb(0xD8, 0x84, 0x52),
    ];

    /// <summary>The key's turn as drawn, degrees, easing toward the angle set.</summary>
    private double _angle = double.NaN;

    static LeverDiagram()
    {
        AffectsRender<LeverDiagram>(AngleProperty, WrongProperty);
    }

    /// <summary>How far the key has turned, degrees.</summary>
    public double Angle
    {
        get => GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    public bool Wrong
    {
        get => GetValue(WrongProperty);
        set => SetValue(WrongProperty, value);
    }

    private double SetAngle => Math.Clamp(Angle, 0, 180);

    protected override void Advance(double seconds) =>
        _angle = double.IsNaN(_angle) ? SetAngle : LocksParts.Ease(_angle, SetAngle, Math.Max(90, Math.Abs(SetAngle - _angle) * 4), seconds);

    /// <summary>
    /// The key's angle, degrees, with its bit pointing at <paramref name="pointer"/> from <paramref name="centre"/>: at
    /// 0 the bit points left, at 90 up, at 180 right (the way the picture turns it); pointing lower down on either
    /// side holds it at that end. Snapped like the slider.
    /// </summary>
    internal static double AngleAt(Point centre, Point pointer)
    {
        var (dx, dy) = (pointer.X - centre.X, pointer.Y - centre.Y);
        var degrees = (Math.Atan2(dx, -dy) * 180 / Math.PI) + 90; // −90 (down, from the left) … 270 (down, from the right)
        return Math.Clamp(Math.Round(degrees / 2) * 2, 0, 180);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var key = Wrong ? LeverLock.WrongKey : LeverLock.Gates;
        var angle = double.IsNaN(_angle) ? SetAngle : _angle;
        var bolt = LeverLock.Bolt(key, angle);
        var scale = Math.Min((bounds.Width - 24) / 66, (bounds.Height - 30) / 96);
        var left = bounds.X + 12 + (26 * scale);
        var top = bounds.Y + 20;
        Point At(double x, double y) => new(left + (x * scale), top + (y * scale)); // y down here, from the top
        var stumpX = Rest - (Travel * bolt);

        // The levers, one row each, lifted by the bit; the stump (gold) in each row stays where the bolt holds it.
        for (var i = 0; i < key.Length; i++)
        {
            var rowTop = (i * Row) + 7.5;
            var lift = LeverLock.Lift(key[i], angle);
            var plateTop = rowTop - lift;
            var aligned = Math.Abs(lift - LeverLock.Gates[i]) <= LeverLock.Tolerance;
            var plate = new Rect(At(0, plateTop), At(PlateWidth, plateTop + PlateHeight));
            context.DrawRectangle(new ImmutableSolidColorBrush(Shade(LeverColors[i], 0.85)), LocksParts.Edge, plate, 4, 4);
            context.DrawEllipse(LocksParts.Dark, null, At(3, plateTop + (PlateHeight / 2)), scale * 1.2, scale * 1.2); // its pivot

            // The window the stump sits in, tall enough for any lift, and the gate running left from it.
            var stumpLocal = StumpLow; // from the plate's bottom, lever down
            var window = new Rect(At(Rest - 1.8, plateTop + PlateHeight - stumpLocal - StumpSize - 0.2), At(Rest + 1.8, plateTop + PlateHeight - stumpLocal + 7.5));
            var gateTop = plateTop + PlateHeight - stumpLocal - StumpSize - 0.2 + LeverLock.Gates[i];
            var gate = new Rect(At(Rest - Travel - 1.8, gateTop), At(Rest, gateTop + StumpSize + 0.4));
            context.FillRectangle(LocksParts.Dark, window);
            context.FillRectangle(LocksParts.Dark, gate);
            context.DrawRectangle(null, new ImmutablePen(aligned ? LocksParts.Good : LocksParts.Track, aligned ? 2 : 1), gate);
            Mark("levers", plate);

            var stumpTop = rowTop + PlateHeight - stumpLocal - StumpSize;
            var stump = new Rect(At(stumpX - 1.5, stumpTop), At(stumpX + 1.5, stumpTop + StumpSize));
            context.DrawRectangle(DiagramText.Gold, LocksParts.Edge, stump, 2, 2);
            Mark("stump", stump);
        }

        // The bolt along the bottom, the stump's post on it, its end in the door frame while locked.
        var boltTop = (key.Length * Row) + 8;
        var frame = new Rect(At(PlateWidth + 6, boltTop - 4), At(PlateWidth + 16, boltTop + 10));
        context.DrawRectangle(new ImmutableSolidColorBrush(Color.FromRgb(0x5A, 0x48, 0x38)), LocksParts.Edge, frame, 3, 3);
        DiagramText.Draw(context, DiagramText.Get("Locks_Lever_Frame"), new Point(frame.Center.X, frame.Bottom + 4), 11, DiagramText.Faint, 0.5);
        context.DrawLine(new ImmutablePen(DiagramText.Faint, 1), At(PlateWidth + 4, 0), At(PlateWidth + 4, boltTop + 12));
        var boltBox = new Rect(At(stumpX - 6, boltTop), At(stumpX - 6 + 38, boltTop + 6));
        context.DrawRectangle(LocksParts.KeyBrush, LocksParts.Edge, boltBox, 3, 3);
        context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x80, 0xF2, 0xC9, 0x4C)), Math.Max(2, scale * 1.2)), At(stumpX, boltTop), At(stumpX, 7.5 + PlateHeight - StumpLow));
        Mark("bolt", boltBox);

        // The key, end on: its bit swings round, a step for each lever.
        var centre = At(-11, (key.Length * Row * 0.5) + 6);
        var radius = 3.2 * scale;
        context.DrawEllipse(LocksParts.KeyBrush, LocksParts.Edge, centre, radius, radius);
        var turn = (angle - 90) * Math.PI / 180;
        var (sin, cos) = Math.SinCos(turn);
        var along = new Vector(sin, -cos);
        var across = new Vector(cos, sin);
        var bit = new List<Point> { centre };
        for (var i = 0; i < key.Length; i++)
        {
            var reach = radius + ((key[i] + 1.5) * scale);
            var a = centre + (across * ((i - 1.5) * 2.2 * scale)) + (along * radius * 0.6);
            var b = a + (along * (reach - (radius * 0.6)));
            var c = b + (across * 2.2 * scale);
            var d = a + (across * 2.2 * scale);
            var step = new StreamGeometry();
            using (var g = step.Open())
            {
                g.BeginFigure(a, true);
                g.LineTo(b);
                g.LineTo(c);
                g.LineTo(d);
                g.EndFigure(true);
            }

            context.DrawGeometry(new ImmutableSolidColorBrush(Shade(LeverColors[i], 0.85)), LocksParts.Edge, step);
            bit.Add(b);
            bit.Add(c);
        }

        context.DrawEllipse(LocksParts.Dark, null, centre, radius * 0.35, radius * 0.35);
        Mark("bit", bit, 8);
        DiagramText.Draw(context, DiagramText.Get("Locks_Lever_Exploded"), new Point(left, bounds.Bottom - 10), 11, DiagramText.Faint, 0, 1);

        // The reader's handles: the bit's far end, dragged round to turn the key, and a tap on the barrel to swap keys.
        var gripReach = radius + (5.5 * scale);
        Grip("turn", centre + (along * gripReach), GripAxis.Free, DiagramText.Get("Locks_Grip_TurnKey"), at => Angle = AngleAt(centre, at));
        Grip("swap", centre, GripAxis.Tap, DiagramText.Get("Locks_Grip_SwapKey"), _ => Wrong = !Wrong);
    }
}
