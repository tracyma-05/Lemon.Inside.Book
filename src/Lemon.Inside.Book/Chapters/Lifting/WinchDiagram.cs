using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Lifting;

/// <summary>
/// Spread 4: a hand winch from the side. The rope winds on a small drum; the crank turns it straight, or turns a worm
/// under a toothed wheel on the drum. Let go, the load falls and spins the crank, unless the worm holds it; taken
/// back, the crank winds it up again. The reader drags the hand on the crank in or out for a longer or shorter crank,
/// taps the worm's place to put the worm in or take it out, and taps beside the load to let go of the crank or take it
/// back; the load drops, or stays, or is wound up, with the drum and the crank turning as the rope moves.
/// </summary>
public sealed class WinchDiagram : GearPicture
{
    public static readonly StyledProperty<double> HandleProperty =
        AvaloniaProperty.Register<WinchDiagram, double>(nameof(Handle), 0.3, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> WormProperty =
        AvaloniaProperty.Register<WinchDiagram, bool>(nameof(Worm), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> LetGoProperty =
        AvaloniaProperty.Register<WinchDiagram, bool>(nameof(LetGo), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The load's height when wound up, m; how fast it falls, and how fast the hand winds it.</summary>
    private const double Up = 1.4;
    private const double FallRate = 3;
    private const double WindRate = 0.35;

    /// <summary>The drum drawn larger than life (it would be a thin spool at this scale), m.</summary>
    private const double DrumDrawn = 0.16;

    private double _height = Up;

    static WinchDiagram()
    {
        AffectsRender<WinchDiagram>(HandleProperty, WormProperty, LetGoProperty);
    }

    /// <summary>The crank's radius, m.</summary>
    public double Handle
    {
        get => GetValue(HandleProperty);
        set => SetValue(HandleProperty, value);
    }

    public bool Worm
    {
        get => GetValue(WormProperty);
        set => SetValue(WormProperty, value);
    }

    public bool LetGo
    {
        get => GetValue(LetGoProperty);
        set => SetValue(LetGoProperty, value);
    }

    protected override void Advance(double seconds)
    {
        var falling = LetGo && !Winch.Holds(Worm);
        // LiftingParts.Ease, a steady rate (the picture's own Ease, inherited, would take the rate for a time and jump).
        _height = LiftingParts.Ease(_height, falling ? 0 : Up, falling ? FallRate : WindRate, seconds);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var scale = (bounds.Height - 40) / 4.2;
        var v = new View(new Point(bounds.Center.X - (0.65 * scale), bounds.Bottom - 20), scale);
        var handle = Math.Clamp(Handle, 0.1, 0.5);
        var worm = Worm;

        context.FillRectangle(Ground, new Rect(bounds.X, v.At(0, 0).Y, bounds.Width, bounds.Height));

        // The frame: a post and an arm holding the drum's axle out over the load.
        var axle = v.At(0.9, 2.5);
        context.DrawRectangle(Wood, Edge, v.Box(0.2, 0, 0.36, 2.57));
        context.DrawRectangle(Wood, Edge, v.Box(0.2, 2.43, 0.98, 2.57));
        context.DrawRectangle(Wood, Edge, v.Box(-0.1, 0, 0.7, 0.12));

        // The drum turns with the rope; through the worm the crank turns twenty times as often.
        var drumTurn = (Up - _height) / Winch.Drum;
        var crankTurn = worm ? drumTurn * Winch.WormRatio : drumTurn;
        var drumR = DrumDrawn * scale;

        if (worm)
        {
            // The worm wheel on the drum, the worm under it.
            var pitch = 0.36 * scale;
            var module = pitch / Controls.Mechanics.GearGeometry.PitchRadius(Winch.WormRatio);
            DrawGear(context, Winch.WormRatio, 20 * Math.PI / 180, axle, module, -drumTurn, Copper, mark: false);
        }

        var drum = new Rect(axle.X - drumR, axle.Y - drumR, 2 * drumR, 2 * drumR);
        context.DrawEllipse(SteelBrush, Edge, axle, drumR, drumR);
        context.DrawEllipse(null, new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xE9, 0xD8, 0xA6)), 3), axle, drumR * 0.85, drumR * 0.85);
        var (s, c) = Math.SinCos(-drumTurn);
        context.DrawLine(new ImmutablePen(Dark, 2), axle, new Point(axle.X + (c * drumR * 0.8), axle.Y + (s * drumR * 0.8)));
        Mark("drum", worm ? new Rect(axle.X - (0.4 * scale), axle.Y - (0.4 * scale), 0.8 * scale, 0.8 * scale) : drum);

        // The rope off the drum's right side, down to the load.
        var loadTop = v.At(0, 0.45 + _height).Y;
        var ropeX = axle.X + drumR;
        DrawRope(context, new Point(ropeX, axle.Y), new Point(ropeX, loadTop - (0.1 * scale)));
        DrawHook(context, new Point(ropeX, loadTop - (0.1 * scale)), 0.09 * scale);
        Mark("rope", [new Point(ropeX, axle.Y), new Point(ropeX, loadTop)], 6);
        var load = new Rect(ropeX - (0.25 * scale) - (0.045 * scale), loadTop, 0.5 * scale, 0.45 * scale);
        Mark("load", DrawWeight(context, load, Kg(Winch.Load)));

        // The crank: on the drum's axle, or on the worm's end (its axis across the page, so it swings up and down).
        Rect knob;
        var effort = Winch.Effort(Winch.Load, handle, worm);
        if (!worm)
        {
            var (ks, kc) = Math.SinCos(-crankTurn + (Math.PI * 0.75));
            var end = new Point(axle.X + (kc * handle * scale), axle.Y + (ks * handle * scale));
            context.DrawLine(new ImmutablePen(DarkSteelBrush, 6), axle, end);
            knob = DrawHand(context, end, 0.2 * scale);
            Mark("handle", [axle, end], 8);

            // Where the worm would go: an outline, for the reader's tap.
            context.DrawRectangle(null, Dashed, new Rect(axle.X - (0.48 * scale), axle.Y - (0.54 * scale), 0.84 * scale, 0.18 * scale), 4, 4);
        }
        else
        {
            var wormCentre = new Point(axle.X - (0.06 * scale), axle.Y - (0.36 * scale) - (0.09 * scale));
            var wormBox = new Rect(wormCentre.X - (0.42 * scale), wormCentre.Y - (0.09 * scale), 0.84 * scale, 0.18 * scale);
            context.DrawRectangle(SteelBrush, Edge, wormBox, 4, 4);
            var shift = crankTurn / (2 * Math.PI) % 1 * 0.1 * scale;
            for (var x = wormBox.Left - (0.1 * scale) + shift; x < wormBox.Right; x += 0.1 * scale)
            {
                var a = new Point(Math.Max(wormBox.Left, x), wormBox.Bottom);
                var b = new Point(Math.Min(wormBox.Right, x + (0.05 * scale)), wormBox.Top);
                if (a.X < b.X) context.DrawLine(new ImmutablePen(DarkSteelBrush, 1.6), a, b);
            }

            var shaftEnd = new Point(wormBox.Left - (0.25 * scale), wormCentre.Y);
            context.DrawLine(new ImmutablePen(DarkSteelBrush, 4), new Point(wormBox.Left, wormCentre.Y), shaftEnd);
            var swing = Math.Cos(crankTurn) * handle * scale;
            var end = new Point(shaftEnd.X, shaftEnd.Y - swing);
            context.DrawLine(new ImmutablePen(DarkSteelBrush, 6), shaftEnd, end);
            knob = DrawHand(context, end, 0.2 * scale);
            Mark("handle", [shaftEnd, end, new Point(wormBox.Right, wormBox.Bottom)], 8);
            DiagramText.Draw(context, DiagramText.Get("Lifting_Winch_WormLabel"), new Point(wormBox.Center.X, wormBox.Top - 4), 12, DiagramText.Faint, 0.5, 1);
        }

        if (!LetGo)
        {
            DrawForce(context, new Point(knob.Left - 14, knob.Center.Y - (0.4 * scale)), new Point(knob.Left - 14, knob.Center.Y), LiftingParts.Push, Kg(effort), -1);
        }

        DrawForce(context, new Point(load.Right + 14, load.Top), new Point(load.Right + 14, load.Top + (0.6 * scale)), Weight, null);
        var state = !LetGo ? "Lifting_Winch_Holding" : Winch.Holds(worm) ? "Lifting_Winch_Stays" : "Lifting_Winch_Falls";
        DiagramText.Draw(context, DiagramText.Get(state), new Point(bounds.X + 16, bounds.Y + 16), 14,
            LetGo && !Winch.Holds(worm) ? Weight : DiagramText.Ink, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Lifting_Winch_Turns", Winch.TurnsPerMetre(worm).ToString("0", CultureInfo.CurrentCulture)),
            new Point(bounds.X + 16, bounds.Y + 40), 12.5, DiagramText.Faint);

        // The reader's handles: the hand on the crank, dragged out or in for a longer or shorter crank (measured from
        // the drum's axle, or along the worm's crank, which swings across the page); a tap on the worm's place puts the
        // worm in or takes it out; a tap beside the load lets go of the crank, or takes it back to wind the load up.
        var pivot = worm ? new Point(axle.X - (0.06 * scale) - (0.67 * scale), axle.Y - (0.45 * scale)) : axle;
        Grip("handle", knob.Center, worm ? GripAxis.Vertical : GripAxis.Free, DiagramText.Get("Lifting_Grip_Handle"), at =>
        {
            var reach = worm ? Math.Abs(at.Y - pivot.Y) : Math.Sqrt(((at.X - pivot.X) * (at.X - pivot.X)) + ((at.Y - pivot.Y) * (at.Y - pivot.Y)));
            Handle = Math.Clamp(Math.Round(reach / scale / 0.01) * 0.01, 0.1, 0.5);
        });
        Grip("worm", new Point(axle.X - (0.06 * scale), axle.Y - (0.45 * scale)), GripAxis.Tap, DiagramText.Get("Lifting_Grip_Worm"),
            _ => Worm = !Worm);
        Grip("letgo", new Point(load.Left - 18, load.Center.Y), GripAxis.Tap, DiagramText.Get(LetGo ? "Lifting_Grip_TakeBack" : "Lifting_Grip_LetGo"),
            _ => LetGo = !LetGo);
    }
}
