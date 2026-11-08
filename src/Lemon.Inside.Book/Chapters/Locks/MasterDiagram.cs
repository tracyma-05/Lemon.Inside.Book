using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Locks;

/// <summary>
/// Spread 3: a master-keyed lock cut open, the chosen key fully in. Where the lock's own key and the master key differ,
/// a thin gold wafer sits between key pin and driver: the stack can split at the key pin's top or at the wafer's.
/// A dot under each pin says whether one of those splits is at the shear line. The reader taps the key's bow to try
/// the next key: the one in the lock slides out, pins dropping off its cuts, and the next slides in, the pins riding
/// up its slopes into place.
/// </summary>
public sealed class MasterDiagram : GearPicture
{
    public static readonly StyledProperty<int> KeyChoiceProperty =
        AvaloniaProperty.Register<MasterDiagram, int>(nameof(KeyChoice), defaultBindingMode: BindingMode.TwoWay);

    private static readonly IPen Blocked = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x5A)), 2.2);

    /// <summary>How fast a key slides, of its length a second.</summary>
    private const double Slide = 2.2;

    private double _turn;

    /// <summary>The key in the lock as drawn, and how far it is in: it comes out before the chosen one goes in.</summary>
    private int _shown = -1;
    private double _insert = 1;

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

    private int Choice => Math.Clamp(KeyChoice, 0, MasterKeyed.Keys.Count - 1);

    protected override void Advance(double seconds)
    {
        if (_shown < 0) _shown = Choice;
        if (_shown != Choice)
        {
            // The plug turns back first, then the key comes out, then the chosen one goes in.
            _turn = LocksParts.Ease(_turn, 0, 3, seconds);
            if (_turn <= 0) _insert = LocksParts.Ease(_insert, 0, Slide, seconds);
            if (_insert <= 0) _shown = Choice;
            return;
        }

        _insert = LocksParts.Ease(_insert, 1, Slide, seconds);
        _turn = LocksParts.Ease(_turn, _insert >= 1 && MasterKeyed.Blocking(MasterKeyed.Keys[_shown]) < 0 ? 1 : 0, 1.5, seconds);
    }

    /// <summary>The top of key pin <paramref name="i"/> with <paramref name="key"/> pushed <paramref name="insert"/> of the way in.</summary>
    internal static double KeyPinTop(IReadOnlyList<int> key, int i, double insert) =>
        insert >= 1 ? MasterKeyed.KeyPinTop(key, i) : PinTumbler.KeyPinTop(key, i, MasterKeyed.KeyPin(i), insert);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var key = MasterKeyed.Keys[_shown < 0 ? Choice : _shown];
        var insert = _shown < 0 ? 1 : _insert;
        var span = LocksParts.LockEnd + 16;
        var scale = Math.Min((bounds.Width - 32) / span, bounds.Height * 0.62 / (LocksParts.ShellTop - LocksParts.ShellBottom + 6));
        var origin = new Point(bounds.Width - 16 - ((LocksParts.LockEnd + 1) * scale), (bounds.Height * 0.12) + ((LocksParts.ShellTop + 2) * scale));
        var s = new LocksParts.Section(origin, scale);
        var shoulder = -(1 - insert) * PinTumbler.KeyLength;

        LocksParts.DrawBody(context, s);
        Mark("key", LocksParts.DrawKey(context, s, key, shoulder));

        var home = insert >= 1;
        var blocking = home ? MasterKeyed.Blocking(key) : -1;
        for (var i = 0; i < PinTumbler.Pins; i++)
        {
            var top = KeyPinTop(key, i, insert);
            var length = MasterKeyed.KeyPin(i);
            var wafer = MasterKeyed.Wafer(i);
            var (keyPin, waferBox, driver, _) = LocksParts.DrawStack(context, s, i, top - length, length, wafer, i == blocking ? Blocked : null);
            Mark("keypins", keyPin);
            Mark("drivers", driver);
            if (wafer > 0) Mark("wafers", waferBox);

            var dot = s.At(PinTumbler.PinAt(i), LocksParts.ShellBottom - 1.6);
            if (home)
            {
                context.DrawEllipse(MasterKeyed.PinAllows(key, i) ? LocksParts.Good : LocksParts.Bad, LocksParts.Edge, dot, 6, 6);
            }
            else
            {
                context.DrawEllipse(LocksParts.Track, LocksParts.Edge, dot, 6, 6);
            }
        }

        DiagramText.Draw(context, DiagramText.Get("Locks_Pins_Shear"), s.At(LocksParts.LockEnd + 1, PinTumbler.ShearLine + 0.3), 11.5, DiagramText.Cyan, 1, 1);

        // Which key, and the lock from the front turning when it opens.
        var radius = Math.Min(scale * 4, bounds.Height * 0.1);
        var front = new Point(bounds.X + 20 + radius, bounds.Y + 16 + radius);
        LocksParts.DrawFront(context, front, radius, _turn);
        var name = DiagramText.Get($"Locks_Master_Key{Choice + 1}");
        var opens = MasterKeyed.Blocking(MasterKeyed.Keys[Choice]) < 0;
        DiagramText.Draw(context, name, new Point(front.X + radius + 14, front.Y - 4), 16, DiagramText.Ink, 0, 1, bold: true);
        if (home)
        {
            DiagramText.Draw(context, DiagramText.Get(opens ? "Locks_Master_Opens" : "Locks_Master_Stays"),
                new Point(front.X + radius + 14, front.Y + 2), 13, opens ? DiagramText.Gold : LocksParts.Bad, bold: true);
        }

        DiagramText.Draw(context, DiagramText.Get("Locks_Master_WaferLabel"), new Point(s.At(0, 0).X, s.At(0, LocksParts.ShellBottom).Y + 34), 12, LocksParts.WaferBrush);

        // The reader's handle: a tap on the key's bow tries the next key.
        var bow = s.At(shoulder - 8, (PinTumbler.Blade / 2) - 0.3);
        Grip("key", new Point(Math.Max(bounds.X + 16, bow.X), bow.Y), GripAxis.Tap, DiagramText.Get("Locks_Grip_NextKey"),
            _ => KeyChoice = (Choice + 1) % MasterKeyed.Keys.Count);
    }
}
