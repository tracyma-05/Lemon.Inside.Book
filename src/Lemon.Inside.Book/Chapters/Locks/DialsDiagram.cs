using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Locks;

/// <summary>
/// Spread 6: a padlock with three numbered wheels. Above, the lock from the front, each wheel showing its number in
/// the window. Below, the inside: each wheel's sleeve seen end on, its notch turned with it, and the bar that can drop
/// only when every notch is at the top. Then the shackle springs up. The reader turns each wheel by dragging its
/// sleeve's notch round, and taps the bar to make the notches loose or tight. The wheels roll from number to number
/// (the window's digits scrolling, the sleeve turning with them) and the bar only drops once they have stopped.
/// </summary>
public sealed class DialsDiagram : GearPicture
{
    public static readonly StyledProperty<int> FirstProperty =
        AvaloniaProperty.Register<DialsDiagram, int>(nameof(First), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> SecondProperty =
        AvaloniaProperty.Register<DialsDiagram, int>(nameof(Second), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> ThirdProperty =
        AvaloniaProperty.Register<DialsDiagram, int>(nameof(Third), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> SloppyProperty =
        AvaloniaProperty.Register<DialsDiagram, bool>(nameof(Sloppy), defaultBindingMode: BindingMode.TwoWay);

    private static readonly IImmutableBrush Body = new ImmutableSolidColorBrush(Color.FromRgb(0x3E, 0x6E, 0xA8));
    private static readonly IImmutableBrush Wheel = new ImmutableSolidColorBrush(Color.FromRgb(0x24, 0x2A, 0x36));
    private static readonly IImmutableBrush Sleeve = new ImmutableSolidColorBrush(Color.FromRgb(0x8F, 0xA3, 0xBF));
    private static readonly string[] WheelGrips = ["first", "second", "third"];

    private static readonly IPen BlockPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xE8, 0x5A, 0x5A)), 2.4);

    /// <summary>How far the bar has dropped and the shackle risen, 0 to 1.</summary>
    private double _open;

    /// <summary>The number each wheel shows as drawn, rolling toward the one set (the short way round).</summary>
    private readonly double[] _wheel = new double[3];
    private bool _started;

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

    protected override void Advance(double seconds)
    {
        var shown = Shown;
        var still = true;
        for (var i = 0; i < 3; i++)
        {
            var target = Wrap(shown[i]);
            if (!_started)
            {
                _wheel[i] = target;
                continue;
            }

            // Roll the short way round, then keep the number between 0 and 10.
            var ahead = target - _wheel[i];
            ahead -= Dials.Numbers * Math.Round(ahead / Dials.Numbers);
            var step = Math.Max(4, Math.Abs(ahead) * 5) * seconds;
            _wheel[i] = Math.Abs(ahead) <= step ? target : Wrap(_wheel[i] + (Math.Sign(ahead) * step));
            still &= Math.Abs(_wheel[i] - target) < 1e-9;
        }

        _started = true;
        _open = LocksParts.Ease(_open, still && Dials.Blocking(shown, Slop) < 0 ? 1 : 0, 2.5, seconds);
    }

    private static double Wrap(double number) => ((number % Dials.Numbers) + Dials.Numbers) % Dials.Numbers;

    /// <summary>
    /// The number wheel <paramref name="wheel"/> shows with its sleeve's tab (opposite the notch) turned toward
    /// <paramref name="pointer"/> from the sleeve's <paramref name="centre"/>: the notch is at the top at the code (the
    /// tab at the bottom), a tenth of a turn clockwise for each number past it.
    /// </summary>
    internal static int NumberAt(int wheel, Point centre, Point pointer)
    {
        var turn = Math.Atan2(centre.X - pointer.X, pointer.Y - centre.Y); // the notch's turn, clockwise from the top
        var off = (int)Math.Round(turn / (2 * Math.PI / Dials.Numbers));
        return (int)Wrap(Dials.Code[wheel] + off);
    }

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
            var rolled = _started ? _wheel[i] : Wrap(shown[i]);
            var whole = Math.Floor(rolled);
            using (context.PushClip(window.Deflate(2)))
            {
                for (var k = -2; k <= 2; k++)
                {
                    var n = (int)Wrap(whole + k);
                    var place = k - (rolled - whole); // 0 in the middle of the window
                    var near = Math.Abs(place) < 0.5;
                    DiagramText.Draw(context, n.ToString(CultureInfo.CurrentCulture), new Point(window.Center.X, window.Center.Y + (place * unit * 0.8)),
                        near ? unit * 0.75 : unit * 0.45, near ? DiagramText.Ink : DiagramText.Faint, 0.5, 0.5, bold: near);
                }
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
            var off = Wrap((_started ? _wheel[i] : shown[i]) - Dials.Code[i]);
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

            // The reader's handle on this wheel: a tab on the sleeve opposite its notch (so the notch stays in sight),
            // dragged round.
            var wheel = i;
            var tab = centre - (new Vector(Math.Sin(turn), -Math.Cos(turn)) * radius * 0.78);
            Grip(WheelGrips[i], tab, GripAxis.Free, DiagramText.Get("Locks_Grip_Wheel"), at =>
            {
                var number = NumberAt(wheel, centre, at);
                switch (wheel)
                {
                    case 0: First = number; break;
                    case 1: Second = number; break;
                    default: Third = number; break;
                }
            });
        }

        // The bar, resting on the sleeves, dropping into the notches when they all line up.
        var bar = new Rect(cx - (unit * 3.6), inside - radius - (unit * 0.35) + barDrop, unit * 7.2, unit * 0.35);
        context.DrawRectangle(DiagramText.Gold, LocksParts.Edge, bar, 3, 3);
        Mark("bar", bar);
        Grip("sloppy", new Point(bar.Right - 14, bar.Center.Y), GripAxis.Tap, DiagramText.Get(Sloppy ? "Locks_Grip_Tight" : "Locks_Grip_Loose"),
            _ => Sloppy = !Sloppy);

        DiagramText.Draw(context, DiagramText.Format("Locks_Dials_Tries", Dials.Tries(Slop).ToString("N0", CultureInfo.CurrentCulture)),
            new Point(cx, inside + radius + (unit * 0.8)), 14, Sloppy ? LocksParts.Bad : DiagramText.Gold, 0.5, 0, bold: true);
    }
}
