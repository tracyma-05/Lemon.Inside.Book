using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Lemon.Inside.Book.Chapters.Locks;

/// <summary>
/// Drawing the chapter's own things: a pin-tumbler lock cut open from the side (shell, plug, pin stacks, springs) in
/// millimetres with y up, a key's blade, and the lock seen from the front.
/// </summary>
internal static class LocksParts
{
    public static readonly IImmutableBrush ShellBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x8A, 0x6E, 0x34));
    public static readonly IImmutableBrush PlugBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xC9, 0xA2, 0x4E));
    public static readonly IImmutableBrush ChamberBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x1A, 0x1F, 0x2B));
    public static readonly IImmutableBrush KeyBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xB8, 0xC2, 0xD0));
    public static readonly IImmutableBrush KeyPinBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xD8, 0x84, 0x52));
    public static readonly IImmutableBrush DriverBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x8F, 0xA3, 0xBF));
    public static readonly IImmutableBrush WaferBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xF2, 0xC9, 0x4C));
    public static readonly IImmutableBrush Good = new ImmutableSolidColorBrush(Color.FromRgb(0x7A, 0xD9, 0x8C));
    public static readonly IImmutableBrush Bad = new ImmutableSolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x5A));
    public static readonly IImmutableBrush Dark = new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E));
    public static readonly IImmutableBrush Track = new ImmutableSolidColorBrush(Color.FromArgb(0x30, 0xE6, 0xEA, 0xF2));
    public static readonly IPen Edge = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x10, 0x14, 0x1E)), 1.2);
    public static readonly IPen SpringPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xD0, 0xDC)), 1.4);
    public static readonly IPen ShearPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x6F, 0xD8, 0xFF)), 1.4, new ImmutableDashStyle([5, 3], 0));

    /// <summary>The shell's bottom and top, mm.</summary>
    public const double ShellBottom = -5;

    public const double ShellTop = PinTumbler.ShearLine + 10;

    /// <summary>The plug's bottom; the keyway's floor lies a little under the blade.</summary>
    public const double PlugBottom = -3;

    public const double KeywayBottom = -0.6;

    /// <summary>A driver pin's length; a spring fills the rest of its chamber.</summary>
    public const double Driver = 4.5;

    /// <summary>A pin's width (and its chamber's, a little more).</summary>
    public const double PinWidth = 2.4;

    /// <summary>The far end of the lock, past the last pin.</summary>
    public static double LockEnd => PinTumbler.KeyLength + 1.5;

    /// <summary>Millimetres on screen: <see cref="Scale"/> pixels each from <see cref="Origin"/> (the lock's face at the blade's bottom).</summary>
    public readonly record struct Section(Point Origin, double Scale)
    {
        public Point At(double x, double y) => new(Origin.X + (x * Scale), Origin.Y - (y * Scale));

        public Rect Box(double x0, double y0, double x1, double y1) => new(At(x0, y1), At(x1, y0));
    }

    /// <summary>
    /// The shell and plug, cut open: chambers above each pin, the plug's holes below them, the shear line between.
    /// Returns the plug's and the shell's boxes on screen.
    /// </summary>
    public static (Rect Plug, Rect Shell) DrawBody(DrawingContext context, Section s, IBrush? plugTint = null)
    {
        var shell = s.Box(-1, ShellBottom, LockEnd + 1, ShellTop);
        var plug = s.Box(0, PlugBottom, LockEnd, PinTumbler.ShearLine);
        context.DrawRectangle(ShellBrush, Edge, shell, 6, 6);
        context.DrawRectangle(plugTint ?? PlugBrush, Edge, plug);
        context.FillRectangle(ChamberBrush, s.Box(0, KeywayBottom, LockEnd, PinTumbler.Blade + 0.3)); // the keyway
        for (var i = 0; i < PinTumbler.Pins; i++)
        {
            var x = PinTumbler.PinAt(i);
            context.FillRectangle(ChamberBrush, s.Box(x - (PinWidth / 2) - 0.15, PinTumbler.Blade, x + (PinWidth / 2) + 0.15, ShellTop - 0.8));
        }

        context.DrawLine(ShearPen, s.At(-1, PinTumbler.ShearLine), s.At(LockEnd + 1, PinTumbler.ShearLine));
        return (plug, shell);
    }

    /// <summary>
    /// One pin stack: a key pin standing on <paramref name="bottom"/>, an optional wafer, a driver pin and its spring up
    /// to the chamber's top. The driver normally rests on what is below it; <paramref name="driverBottom"/> holds it
    /// higher (a set pin, its driver caught on the plug's edge). Returns the key pin's, wafer's, driver's and spring's boxes.
    /// </summary>
    public static (Rect KeyPin, Rect Wafer, Rect Driver, Rect Spring) DrawStack(
        DrawingContext context, Section s, int i, double bottom, double keyPin, double wafer = 0, IPen? outline = null,
        double? driverBottom = null)
    {
        var x = PinTumbler.PinAt(i);
        var (l, r) = (x - (PinWidth / 2), x + (PinWidth / 2));
        var pen = outline ?? Edge;

        // A key pin has a rounded tip where it rides on the key.
        var pinBox = s.Box(l, bottom, r, bottom + keyPin);
        context.DrawRectangle(KeyPinBrush, pen, pinBox, s.Scale * 0.5, s.Scale * 0.5);
        var top = bottom + keyPin;
        var waferBox = default(Rect);
        if (wafer > 0)
        {
            waferBox = s.Box(l, top, r, top + wafer);
            context.DrawRectangle(WaferBrush, pen, waferBox);
            top += wafer;
        }

        top = Math.Max(top, driverBottom ?? top);
        var driverBox = s.Box(l, top, r, top + Driver);
        context.DrawRectangle(DriverBrush, pen, driverBox);
        top += Driver;

        // The spring: a zigzag from the driver to the top of the chamber.
        var springTop = ShellTop - 0.8;
        var coils = 7;
        var points = new List<Point>();
        for (var k = 0; k <= coils * 2; k++)
        {
            var y = top + ((springTop - top) * k / (coils * 2));
            points.Add(s.At(k % 2 == 0 ? l + 0.2 : r - 0.2, y));
        }

        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(points[0], false);
            foreach (var p in points.Skip(1)) g.LineTo(p);
            g.EndFigure(false);
        }

        context.DrawGeometry(null, SpringPen, geometry);
        return (pinBox, waferBox, driverBox, s.Box(l, top, r, springTop));
    }

    /// <summary>The key's blade and bow, its shoulder at <paramref name="shoulder"/> mm from the lock's face. Returns its blade's box.</summary>
    public static Rect DrawKey(DrawingContext context, Section s, IReadOnlyList<int> cuts, double shoulder)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(s.At(shoulder, 0), true);
            const int Samples = 240;
            for (var k = 0; k <= Samples; k++)
            {
                var x = PinTumbler.KeyLength * k / Samples;
                g.LineTo(s.At(shoulder + x, PinTumbler.KeyTop(cuts, x)));
            }

            g.LineTo(s.At(shoulder + PinTumbler.KeyLength, PinTumbler.Floor));
            g.LineTo(s.At(shoulder + PinTumbler.KeyLength - 1, 0));
            g.EndFigure(true);
        }

        context.DrawGeometry(KeyBrush, Edge, geometry);

        // The bow, to hold it by, and the shoulder that stops it at the lock's face.
        context.DrawRectangle(KeyBrush, Edge, s.Box(shoulder - 1, -2.2, shoulder, PinTumbler.Blade + 1.6));
        context.DrawRectangle(KeyBrush, Edge, s.Box(shoulder - 12, -4, shoulder - 1, PinTumbler.Blade + 3.4), s.Scale * 3, s.Scale * 3);
        context.DrawEllipse(Dark, null, s.At(shoulder - 8, (PinTumbler.Blade / 2) - 0.3), s.Scale * 1.6, s.Scale * 1.6);
        return s.Box(shoulder, 0, shoulder + PinTumbler.KeyLength, PinTumbler.Blade);
    }

    /// <summary>The lock from the front: the shell's round, the plug in it with its keyway, turned <paramref name="turn"/> (0–1 of a quarter turn).</summary>
    public static Rect DrawFront(DrawingContext context, Point centre, double radius, double turn)
    {
        context.DrawEllipse(ShellBrush, Edge, centre, radius, radius);
        context.DrawEllipse(PlugBrush, Edge, centre, radius * 0.66, radius * 0.66);
        using (context.PushTransform(Matrix.CreateTranslation(-centre.X, -centre.Y) * Matrix.CreateRotation(turn * Math.PI / 2) * Matrix.CreateTranslation(centre.X, centre.Y)))
        {
            context.FillRectangle(Dark, new Rect(centre.X - (radius * 0.08), centre.Y - (radius * 0.5), radius * 0.16, radius * 0.95), 2);
            context.DrawEllipse(KeyPinBrush, Edge, new Point(centre.X, centre.Y - (radius * 0.58)), radius * 0.07, radius * 0.07);
        }

        // The top pin chamber in the shell: the plug can turn only if nothing crosses into it.
        context.DrawEllipse(DriverBrush, Edge, new Point(centre.X, centre.Y - (radius * 0.82)), radius * 0.09, radius * 0.09);
        return new Rect(centre.X - radius, centre.Y - radius, 2 * radius, 2 * radius);
    }

    /// <summary>Moves <paramref name="value"/> toward <paramref name="target"/> at <paramref name="rate"/> a second.</summary>
    public static double Ease(double value, double target, double rate, double seconds)
    {
        var step = rate * seconds;
        return Math.Abs(target - value) <= step ? target : value + (Math.Sign(target - value) * step);
    }
}
