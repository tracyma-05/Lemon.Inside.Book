using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Locks;

/// <summary>
/// Spread 1: a pin-tumbler lock cut open from the side, the key sliding in from the left. Each key pin rides up and
/// down on the key's cuts; under the lock, a dot for each pin shows how far its top is from the shear line. When all
/// five are on it, the plug turns (shown from the front, top left).
/// </summary>
public sealed class PinsDiagram : GearPicture
{
    public static readonly StyledProperty<double> InsertProperty =
        AvaloniaProperty.Register<PinsDiagram, double>(nameof(Insert), 0.35);

    public static readonly StyledProperty<bool> WrongProperty =
        AvaloniaProperty.Register<PinsDiagram, bool>(nameof(Wrong));

    private double _turn;

    static PinsDiagram()
    {
        AffectsRender<PinsDiagram>(InsertProperty, WrongProperty);
    }

    /// <summary>How far the key is in, 0 to 1.</summary>
    public double Insert
    {
        get => GetValue(InsertProperty);
        set => SetValue(InsertProperty, value);
    }

    /// <summary>Whether the key in the lock is the wrong one.</summary>
    public bool Wrong
    {
        get => GetValue(WrongProperty);
        set => SetValue(WrongProperty, value);
    }

    private int[] Key => Wrong ? PinsSpread.WrongCuts : PinsSpread.LockCuts;

    protected override void Advance(double seconds) =>
        _turn = LocksParts.Ease(_turn, PinTumbler.Turns(PinsSpread.LockCuts, Key, Insert) ? 1 : 0, 1.5, seconds);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var insert = Math.Clamp(Insert, 0, 1);

        // The lock on the right, the key coming in from the left (its bow may run off the edge while it is out).
        var span = LocksParts.LockEnd + PinTumbler.KeyLength + 6;
        var scale = Math.Min((bounds.Width - 32) / span, bounds.Height * 0.5 / (LocksParts.ShellTop - LocksParts.ShellBottom + 4));
        var origin = new Point(bounds.Width - 16 - ((LocksParts.LockEnd + 1) * scale), (bounds.Height * 0.1) + ((LocksParts.ShellTop + 2) * scale));
        var s = new LocksParts.Section(origin, scale);

        var (plug, shell) = LocksParts.DrawBody(context, s);
        Mark("shell", shell);
        Mark("plug", plug);
        LocksParts.DrawKey(context, s, Key, -(1 - insert) * PinTumbler.KeyLength);

        var gaps = PinTumbler.Gaps(PinsSpread.LockCuts, Key, insert);
        for (var i = 0; i < PinTumbler.Pins; i++)
        {
            var length = PinTumbler.KeyPinLength(PinsSpread.LockCuts[i]);
            var bottom = PinTumbler.KeyPinTop(Key, i, length, insert) - length;
            var (keyPin, _, driver, spring) = LocksParts.DrawStack(context, s, i, bottom, length);
            Mark("keypins", keyPin);
            Mark("drivers", driver);
            Mark("springs", spring);
        }

        // Under the lock: how far each key pin's top is from the line, a dot on a track for each pin.
        var top = s.At(0, LocksParts.ShellBottom).Y + 34;
        var gauge = new Rect(s.At(0, 0).X, top, LocksParts.LockEnd * scale, Math.Max(40, bounds.Bottom - top - 70));
        var mid = gauge.Center.Y;
        var perMm = gauge.Height / 2 / 4;
        context.DrawLine(LocksParts.ShearPen, new Point(gauge.X - 6, mid), new Point(gauge.Right + 6, mid));
        for (var i = 0; i < PinTumbler.Pins; i++)
        {
            var x = s.At(PinTumbler.PinAt(i), 0).X;
            var at = Math.Abs(gaps[i]) <= PinTumbler.Tolerance;
            var y = mid - (Math.Clamp(gaps[i], -4, 4) * perMm);
            context.FillRectangle(LocksParts.Track, new Rect(x - 3, gauge.Y, 6, gauge.Height), 3);
            context.DrawLine(new ImmutablePen(at ? LocksParts.Good : LocksParts.Bad, 2), new Point(x, mid), new Point(x, y));
            context.DrawEllipse(at ? LocksParts.Good : LocksParts.Bad, LocksParts.Edge, new Point(x, y), 6, 6);
        }

        DiagramText.Draw(context, DiagramText.Get("Locks_Pins_Gauge"), new Point(gauge.Right, gauge.Bottom + 8), 11.5, DiagramText.Faint, 1);
        DiagramText.Draw(context, DiagramText.Get("Locks_Pins_Shear"), new Point(gauge.X - 12, mid), 11.5, DiagramText.Cyan, 1, 0.5);
        DiagramText.Draw(context, DiagramText.Get("Locks_Pins_High"), new Point(gauge.X - 12, gauge.Y), 11, DiagramText.Faint, 1);
        DiagramText.Draw(context, DiagramText.Get("Locks_Pins_Low"), new Point(gauge.X - 12, gauge.Bottom), 11, DiagramText.Faint, 1, 1);

        // The lock from the front, top left: the plug turns once every pin is at the line.
        var radius = Math.Min(scale * 4.2, bounds.Height * 0.09);
        var front = new Point(bounds.X + 20 + radius, bounds.Y + 20 + radius);
        LocksParts.DrawFront(context, front, radius, _turn);
        DiagramText.Draw(context, DiagramText.Get(_turn > 0.5 ? "Locks_Pins_Turns" : "Locks_Pins_Front"),
            new Point(front.X + radius + 10, front.Y), 12, _turn > 0.5 ? DiagramText.Gold : DiagramText.Faint, 0, 0.5, bold: _turn > 0.5);
    }
}
