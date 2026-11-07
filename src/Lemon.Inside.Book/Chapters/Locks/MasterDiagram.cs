using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Locks;

/// <summary>
/// Spread 3: a master-keyed lock cut open, the chosen key fully in. Where the lock's own key and the master key differ,
/// a thin gold wafer sits between key pin and driver: the stack can split at the key pin's top or at the wafer's.
/// A dot under each pin says whether one of those splits is at the shear line.
/// </summary>
public sealed class MasterDiagram : GearPicture
{
    public static readonly StyledProperty<int> KeyChoiceProperty =
        AvaloniaProperty.Register<MasterDiagram, int>(nameof(KeyChoice));

    private static readonly IPen Blocked = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x5A)), 2.2);

    private double _turn;

    static MasterDiagram()
    {
        AffectsRender<MasterDiagram>(KeyChoiceProperty);
    }

    /// <summary>Which key is in the lock: own, master, neighbour's, mixed.</summary>
    public int KeyChoice
    {
        get => GetValue(KeyChoiceProperty);
        set => SetValue(KeyChoiceProperty, value);
    }

    private int[] Key => MasterKeyed.Keys[Math.Clamp(KeyChoice, 0, MasterKeyed.Keys.Count - 1)];

    protected override void Advance(double seconds) =>
        _turn = LocksParts.Ease(_turn, MasterKeyed.Blocking(Key) < 0 ? 1 : 0, 1.5, seconds);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var key = Key;
        var span = LocksParts.LockEnd + 16;
        var scale = Math.Min((bounds.Width - 32) / span, bounds.Height * 0.62 / (LocksParts.ShellTop - LocksParts.ShellBottom + 6));
        var origin = new Point(bounds.Width - 16 - ((LocksParts.LockEnd + 1) * scale), (bounds.Height * 0.12) + ((LocksParts.ShellTop + 2) * scale));
        var s = new LocksParts.Section(origin, scale);

        LocksParts.DrawBody(context, s);
        Mark("key", LocksParts.DrawKey(context, s, key, 0));

        var blocking = MasterKeyed.Blocking(key);
        for (var i = 0; i < PinTumbler.Pins; i++)
        {
            var top = MasterKeyed.KeyPinTop(key, i);
            var length = MasterKeyed.KeyPin(i);
            var wafer = MasterKeyed.Wafer(i);
            var (keyPin, waferBox, driver, _) = LocksParts.DrawStack(context, s, i, top - length, length, wafer, i == blocking ? Blocked : null);
            Mark("keypins", keyPin);
            Mark("drivers", driver);
            if (wafer > 0) Mark("wafers", waferBox);

            var ok = MasterKeyed.PinAllows(key, i);
            var dot = s.At(PinTumbler.PinAt(i), LocksParts.ShellBottom - 1.6);
            context.DrawEllipse(ok ? LocksParts.Good : LocksParts.Bad, LocksParts.Edge, dot, 6, 6);
        }

        DiagramText.Draw(context, DiagramText.Get("Locks_Pins_Shear"), s.At(LocksParts.LockEnd + 1, PinTumbler.ShearLine + 0.3), 11.5, DiagramText.Cyan, 1, 1);

        // Which key, and the lock from the front turning when it opens.
        var radius = Math.Min(scale * 4, bounds.Height * 0.1);
        var front = new Point(bounds.X + 20 + radius, bounds.Y + 16 + radius);
        LocksParts.DrawFront(context, front, radius, _turn);
        var name = DiagramText.Get($"Locks_Master_Key{Math.Clamp(KeyChoice, 0, MasterKeyed.Keys.Count - 1) + 1}");
        DiagramText.Draw(context, name, new Point(front.X + radius + 14, front.Y - 4), 16, DiagramText.Ink, 0, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Get(blocking < 0 ? "Locks_Master_Opens" : "Locks_Master_Stays"),
            new Point(front.X + radius + 14, front.Y + 2), 13, blocking < 0 ? DiagramText.Gold : LocksParts.Bad, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Locks_Master_WaferLabel"), new Point(s.At(0, 0).X, s.At(0, LocksParts.ShellBottom).Y + 34), 12, LocksParts.WaferBrush);
    }
}
