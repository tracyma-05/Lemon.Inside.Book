using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Locks;

/// <summary>
/// Spread 6: a padlock with three numbered wheels. Above, the lock from the front, each wheel showing its number in
/// the window. Below, the inside: each wheel's sleeve seen end on, its notch turned with it, and the bar that can drop
/// only when every notch is at the top. Then the shackle springs up.
/// </summary>
public sealed class DialsDiagram : GearPicture
{
    public static readonly StyledProperty<int> FirstProperty =
        AvaloniaProperty.Register<DialsDiagram, int>(nameof(First));

    public static readonly StyledProperty<int> SecondProperty =
        AvaloniaProperty.Register<DialsDiagram, int>(nameof(Second));

    public static readonly StyledProperty<int> ThirdProperty =
        AvaloniaProperty.Register<DialsDiagram, int>(nameof(Third));

    public static readonly StyledProperty<bool> SloppyProperty =
        AvaloniaProperty.Register<DialsDiagram, bool>(nameof(Sloppy));

    private static readonly IImmutableBrush Body = new ImmutableSolidColorBrush(Color.FromRgb(0x3E, 0x6E, 0xA8));
    private static readonly IImmutableBrush Wheel = new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x2A, 0x36));
    private static readonly IImmutableBrush Sleeve = new ImmutableSolidColorBrush(Color.FromRgb(0x8F, 0xA3, 0xBF));
    private static readonly IPen BlockPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x5A)), 2.4);

    /// <summary>How far the bar has dropped and the shackle risen, 0 to 1.</summary>
    private double _open;

    static DialsDiagram()
    {
        AffectsRender<DialsDiagram>(FirstProperty, SecondProperty, ThirdProperty, SloppyProperty);
    }

    public int First
    {
        get => GetValue(FirstProperty);
        set => SetValue(FirstProperty, value);
    }

    public int Second
    {
        get => GetValue(SecondProperty);
        set => SetValue(SecondProperty, value);
    }

    public int Third
    {
        get => GetValue(ThirdProperty);
        set => SetValue(ThirdProperty, value);
    }

    public bool Sloppy
    {
        get => GetValue(SloppyProperty);
        set => SetValue(SloppyProperty, value);
    }

    private int[] Shown => [First, Second, Third];

    private int Slop => Sloppy ? 1 : 0;

    protected override void Advance(double seconds) =>
        _open = LocksParts.Ease(_open, Dials.Blocking(Shown, Slop) < 0 ? 1 : 0, 2.5, seconds);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var shown = Shown;
        var blocking = Dials.Blocking(shown, Slop);
        var unit = Math.Min(bounds.Width / 12, bounds.Height / 15);
        var cx = bounds.Width / 2;

        // The shackle: a U standing in the body, rising when the bar drops.
        var bodyTop = bounds.Y + (unit * 4.4);
        var body = new Rect(cx - (unit * 3.6), bodyTop, unit * 7.2, unit * 3.4);
        var rise = unit * 0.9 * _open;
        var shackle = new StreamGeometry();
        using (var g = shackle.Open())
        {
            var l = cx - (unit * 2.4);
            var r = cx + (unit * 2.4);
            var y = bodyTop + (unit * 0.4) - rise;
            g.BeginFigure(new Point(l, y), false);
            g.LineTo(new Point(l, y - (unit * 1.4)));
            g.ArcTo(new Point(r, y - (unit * 1.4)), new Size(unit * 2.4, unit * 2.2), 0, false, SweepDirection.Clockwise);
            g.LineTo(new Point(r, y + (_open > 0.5 ? -unit * 0.2 : 0)));
            g.EndFigure(false);
        }

        context.DrawGeometry(null, new ImmutablePen(LocksParts.KeyBrush, unit * 0.55, lineCap: PenLineCap.Round), shackle);
        Mark("shackle", new Rect(cx - (unit * 2.8), bounds.Y + (unit * 0.2) - rise, unit * 5.6, unit * 3.4));
        context.DrawRectangle(Body, LocksParts.Edge, body, unit * 0.4, unit * 0.4);

        // The wheels in their windows: the number shown, its neighbours above and below.
        for (var i = 0; i < 3; i++)
        {
            var window = new Rect(cx - (unit * 2.7) + (i * unit * 1.9), bodyTop + (unit * 0.5), unit * 1.6, unit * 2.4);
            context.DrawRectangle(Wheel, i == blocking ? BlockPen : LocksParts.Edge, window, 4, 4);
            for (var k = -1; k <= 1; k++)
            {
                var n = (((shown[i] + k) % Dials.Numbers) + Dials.Numbers) % Dials.Numbers;
                DiagramText.Draw(context, n.ToString(CultureInfo.CurrentCulture), new Point(window.Center.X, window.Center.Y + (k * unit * 0.8)),
                    k == 0 ? unit * 0.75 : unit * 0.45, k == 0 ? DiagramText.Ink : DiagramText.Faint, 0.5, 0.5, bold: k == 0);
            }

            Mark("wheels", window);
        }

        // The inside: each sleeve end on, its notch turned as far from the top as the wheel is from its number.
        var inside = bodyTop + (unit * 7);
        var radius = unit * 0.85;
        var barDrop = unit * 0.55 * _open;
        DiagramText.Draw(context, DiagramText.Get("Locks_Dials_Inside"), new Point(cx, inside - (radius * 2) - (unit * 0.9)), 12, DiagramText.Faint, 0.5, 1);
        for (var i = 0; i < 3; i++)
        {
            var centre = new Point(cx + ((i - 1) * unit * 2.4), inside);
            context.DrawEllipse(Sleeve, i == blocking ? BlockPen : LocksParts.Edge, centre, radius, radius);
            var off = ((shown[i] - Dials.Code[i]) % Dials.Numbers + Dials.Numbers) % Dials.Numbers;
            var turn = off * 2 * Math.PI / Dials.Numbers;
            var width = (0.22 + (0.55 * Slop)) / 2;
            var notch = new StreamGeometry();
            using (var g = notch.Open())
            {
                g.BeginFigure(centre, true);
                g.LineTo(centre + (new Vector(Math.Sin(turn - width), -Math.Cos(turn - width)) * radius * 1.02));
                g.LineTo(centre + (new Vector(Math.Sin(turn + width), -Math.Cos(turn + width)) * radius * 1.02));
                g.EndFigure(true);
            }

            using (context.PushClip(new Rect(centre.X - radius, centre.Y - radius, 2 * radius, 2 * radius)))
            {
                context.DrawGeometry(LocksParts.Dark, null, notch);
            }

            context.DrawEllipse(Sleeve, null, centre, radius * 0.6, radius * 0.6);
            context.DrawEllipse(LocksParts.Dark, null, centre, radius * 0.22, radius * 0.22);
            Mark("notches", centre, radius);
        }

        // The bar, resting on the sleeves, dropping into the notches when they all line up.
        var bar = new Rect(cx - (unit * 3.6), inside - radius - (unit * 0.35) + barDrop, unit * 7.2, unit * 0.35);
        context.DrawRectangle(DiagramText.Gold, LocksParts.Edge, bar, 3, 3);
        Mark("bar", bar);

        DiagramText.Draw(context, DiagramText.Format("Locks_Dials_Tries", Dials.Tries(Slop).ToString("N0", CultureInfo.CurrentCulture)),
            new Point(cx, inside + radius + (unit * 0.8)), 14, Sloppy ? LocksParts.Bad : DiagramText.Gold, 0.5, 0, bold: true);
    }
}
