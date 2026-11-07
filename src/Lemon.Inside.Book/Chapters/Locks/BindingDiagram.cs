using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Locks;

/// <summary>
/// Spread 4: the lock cut open with no key, a gentle turning force on the plug. The pin holding the plug is outlined in
/// gold and nudged up and down; set pins have their drivers caught above the shear line. Under the lock, the plug's
/// holes seen from above, their errors made a hundred times bigger.
/// </summary>
public sealed class BindingDiagram : GearPicture
{
    public static readonly StyledProperty<int> SetProperty =
        AvaloniaProperty.Register<BindingDiagram, int>(nameof(Set));

    public static readonly StyledProperty<bool> PerfectProperty =
        AvaloniaProperty.Register<BindingDiagram, bool>(nameof(Perfect));

    /// <summary>How much bigger the holes' errors are drawn.</summary>
    private const double Exaggerate = 100;

    private static readonly IPen HoldingPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xF2, 0xC9, 0x4C)), 2.2);
    private static readonly IPen Jammed = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x5A)), 2.2);
    private static readonly IPen Line = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0xE6, 0xEA, 0xF2)), 1, new ImmutableDashStyle([4, 3], 0));

    private double _turn;

    static BindingDiagram()
    {
        AffectsRender<BindingDiagram>(SetProperty, PerfectProperty);
    }

    /// <summary>How many pins are set so far.</summary>
    public int Set
    {
        get => GetValue(SetProperty);
        set => SetValue(SetProperty, value);
    }

    /// <summary>Whether the holes are drilled perfectly in line.</summary>
    public bool Perfect
    {
        get => GetValue(PerfectProperty);
        set => SetValue(PerfectProperty, value);
    }

    protected override void Advance(double seconds) =>
        _turn = LocksParts.Ease(_turn, Set >= PinTumbler.Pins ? 1 : 0.06, 1.5, seconds);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var offsets = Perfect ? new double[PinTumbler.Pins] : Binding.Offsets;
        var order = Binding.Order(offsets);
        var set = Math.Clamp(Set, 0, PinTumbler.Pins);
        var isSet = order.Take(set).ToHashSet();
        var holding = Binding.Binds(offsets, set).ToHashSet();

        var span = LocksParts.LockEnd + 14;
        var scale = Math.Min((bounds.Width - 32) / span, bounds.Height * 0.5 / (LocksParts.ShellTop - LocksParts.ShellBottom + 4));
        var origin = new Point(bounds.Width - 16 - ((LocksParts.LockEnd + 1) * scale), (bounds.Height * 0.06) + ((LocksParts.ShellTop + 1) * scale));
        var s = new LocksParts.Section(origin, scale);
        LocksParts.DrawBody(context, s);
        Mark("set", s.Box(0, PinTumbler.ShearLine - 0.6, LocksParts.LockEnd, PinTumbler.ShearLine + 0.6));

        // The turning tool along the bottom of the keyway, its handle hanging out of the lock.
        var wrench = new ImmutablePen(LocksParts.KeyBrush, Math.Max(3, scale * 0.8), lineCap: PenLineCap.Round);
        context.DrawLine(wrench, s.At(2.5, LocksParts.KeywayBottom + 0.5), s.At(-3, LocksParts.KeywayBottom + 0.5));
        context.DrawLine(wrench, s.At(-3, LocksParts.KeywayBottom + 0.5), s.At(-3, LocksParts.ShellBottom - 5));
        Mark("turn", s.Box(-4, LocksParts.ShellBottom - 5.5, 3, LocksParts.KeywayBottom + 1.1));

        // The pins: set ones caught above the line, the holding one lifted a little by the pick, the rest resting.
        var lift = holding.Count == 1 ? (0.5 + (0.5 * Math.Sin(Seconds * 3))) : 0;
        var pickAt = holding.Count == 1 ? holding.First() : -1;
        for (var i = 0; i < PinTumbler.Pins; i++)
        {
            var length = PinTumbler.KeyPinLength(PinsSpread.LockCuts[i]);
            var room = PinTumbler.ShearLine - PinTumbler.Floor - length - 0.15;
            var bottom = PinTumbler.Floor + (i == pickAt ? room * lift : 0);
            var pen = holding.Contains(i) ? (holding.Count > 1 ? Jammed : HoldingPen) : null;
            var (keyPin, _, driver, _) = LocksParts.DrawStack(context, s, i, bottom, length, 0, pen,
                isSet.Contains(i) ? PinTumbler.ShearLine + 0.05 : null);
            if (holding.Contains(i)) Mark("binding", keyPin.Union(driver));
            if (isSet.Contains(i)) Mark("set", driver);
        }

        // The pick, reaching in under the pin that holds.
        if (pickAt >= 0)
        {
            var x = PinTumbler.PinAt(pickAt);
            var length = PinTumbler.KeyPinLength(PinsSpread.LockCuts[pickAt]);
            var tip = PinTumbler.Floor + ((PinTumbler.ShearLine - PinTumbler.Floor - length - 0.15) * lift);
            var pick = new ImmutablePen(LocksParts.KeyBrush, Math.Max(2, scale * 0.4), lineCap: PenLineCap.Round);
            context.DrawLine(pick, s.At(-9, 1.2), s.At(x - 1.6, 1.2));
            context.DrawLine(pick, s.At(x - 1.6, 1.2), s.At(x, tip));
        }

        // The plug from the front, nudged by the turning tool.
        var radius = Math.Min(scale * 3.6, bounds.Height * 0.09);
        var front = new Point(bounds.X + 18 + radius, bounds.Y + 14 + radius);
        LocksParts.DrawFront(context, front, radius, _turn);
        DiagramText.DrawArrowAlong(context, new Point(front.X + radius + 6, front.Y - (radius * 0.6)), new Point(front.X + radius + 6, front.Y + (radius * 0.4)), DiagramText.Gold, 8);

        // The holes from above: the shell's in a straight row, the plug's each a little off it.
        var row = new Rect(s.At(0, 0).X, bounds.Height * 0.72, (LocksParts.LockEnd * scale), bounds.Height * 0.18);
        var mid = row.Center.Y;
        context.DrawLine(Line, new Point(row.X, mid), new Point(row.Right, mid));
        for (var i = 0; i < PinTumbler.Pins; i++)
        {
            var x = s.At(PinTumbler.PinAt(i), 0).X;
            var y = mid - (offsets[i] * Exaggerate * scale);
            var brush = isSet.Contains(i) ? LocksParts.Good : holding.Contains(i) ? DiagramText.Gold : LocksParts.DriverBrush;
            context.DrawEllipse(null, Line, new Point(x, mid), scale * 1.2, scale * 1.2);
            context.DrawEllipse(brush, LocksParts.Edge, new Point(x, y), scale * 1.2, scale * 1.2);
            DiagramText.Draw(context, (order.ToList().IndexOf(i) + 1).ToString(System.Globalization.CultureInfo.CurrentCulture), new Point(x, y), 10.5, LocksParts.Dark, 0.5, 0.5, bold: true);
        }

        DiagramText.Draw(context, DiagramText.Format("Locks_Binding_HolesLabel", Exaggerate), new Point(bounds.X + 16, row.Bottom + 6), 11.5, DiagramText.Faint);
        Mark("holes", row.Inflate(new Thickness(scale * 1.4)));
    }
}
