using Avalonia;
using Avalonia.Data;
using Lemon.Inside.Book.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Watch;

/// <summary>
/// Spread 3: the lever escapement in slow motion. The balance (top) swings; as it passes the middle its pin flicks the
/// lever (centre) across; one red stone lets go of the escape wheel (bottom), which jumps half a tooth until the other
/// stone catches it. Everything comes from <see cref="Escapement.At"/>. The reader can take the balance's rim and swing
/// it by hand through a beat (time waits while it is held), and drag along the speed track at the top to slow time more
/// or less.
/// </summary>
public sealed class EscapementDiagram : GearPicture
{
    public static readonly StyledProperty<double> SlowMotionProperty =
        AvaloniaProperty.Register<EscapementDiagram, double>(nameof(SlowMotion), 0.1, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Where the two stones sit round the escape wheel, from 3 o'clock anticlockwise: 2.5 teeth apart.</summary>
    private const double ExitStone = 60, EntryStone = 120;

    /// <summary>The wheel's turn that puts a gap under the exit stone at rest.</summary>
    private const double WheelPhase = ExitStone - (180.0 / GoingTrain.EscapeTeeth);

    /// <summary>How far the lever swings either side, in degrees.</summary>
    private const double LeverSwing = 9;

    private static readonly IImmutableBrush Lever = new ImmutableSolidColorBrush(Color.FromRgb(0x7F, 0x8C, 0xA3));
    private static readonly IImmutableBrush Stone = new ImmutableSolidColorBrush(WatchParts.Ruby);
    private static readonly IImmutableBrush Wheel = new ImmutableSolidColorBrush(Color.FromRgb(0xB8, 0xC6, 0xD8));

    private double _seconds;

    /// <summary>How fast time runs in the picture: 1 is real time (2.5 swings a second, too fast to follow).</summary>
    public double SlowMotion
    {
        get => GetValue(SlowMotionProperty);
        set => SetValue(SlowMotionProperty, value);
    }

    /// <summary>The watch's own time in the picture, in seconds.</summary>
    internal double Time => _seconds;

    /// <summary>The slider's range of the slow motion.</summary>
    private const double Slowest = 0.02, Fastest = 0.5;

    private static readonly IPen TrackPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x50, 0xE6, 0xEA, 0xF2)), 4, lineCap: PenLineCap.Round);

    /// <summary>Time runs on, except while the reader holds the balance.</summary>
    protected override void Advance(double seconds)
    {
        if (!Gripping)
        {
            _seconds += seconds * SlowMotion;
        }
    }

    /// <summary>
    /// The moment, nearest <paramref name="seconds"/>, at which the balance stands at <paramref name="degrees"/>: in
    /// the same half swing (between one end of the swing and the other), so a drag moves it back and forth through the
    /// beat; beyond the swing's ends it stops there.
    /// </summary>
    internal static double TimeAt(double seconds, double degrees, double frequency = GoingTrain.Frequency)
    {
        var phase = 2 * Math.PI * frequency * seconds;
        var half = Math.Round(phase / Math.PI);
        var along = Math.Asin(Math.Clamp(degrees / Escapement.Amplitude, -0.999, 0.999));
        return ((half * Math.PI) + (((long)half % 2 == 0) ? along : -along)) / (2 * Math.PI * frequency);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var (balance, lever, halfTeeth, ticking) = Escapement.At(_seconds);
        var unit = Math.Min(bounds.Width / 2.6, bounds.Height / 3.6);
        var cx = bounds.Center.X;
        var wheelRadius = unit * 0.62;
        var wheelCentre = new Point(cx, bounds.Height * 0.74);
        var pivot = new Point(cx, wheelCentre.Y - (wheelRadius * 1.45));
        var balanceRadius = unit * 0.55;
        var balanceCentre = new Point(cx, pivot.Y - (unit * 0.42) - (balanceRadius * 0.9));

        // The escape wheel turns clockwise half a tooth each beat.
        var pitch = 360.0 / GoingTrain.EscapeTeeth;
        var wheelAngle = (WheelPhase - (halfTeeth * pitch / 2)) * Math.PI / 180;
        var outline = Shape("escape", () => WatchParts.EscapeOutline(GoingTrain.EscapeTeeth));
        using (context.PushTransform(Placement(wheelCentre, wheelRadius, wheelAngle)))
        {
            context.DrawGeometry(Wheel, new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x50, 0x5C, 0x70)), 1.2 / wheelRadius), outline);
        }

        context.DrawEllipse(WatchParts.Dark, null, wheelCentre, wheelRadius * 0.34, wheelRadius * 0.34);
        context.DrawEllipse(Wheel, null, wheelCentre, wheelRadius * 0.14, wheelRadius * 0.14);

        // The lever: +1 holds the wheel with its exit stone, -1 with its entry stone.
        var exitDepth = 0.86 + (0.3 * (1 - lever) / 2);
        var entryDepth = 0.86 + (0.3 * (1 + lever) / 2);
        var exit = OnWheel(wheelCentre, wheelRadius * exitDepth, ExitStone);
        var entry = OnWheel(wheelCentre, wheelRadius * entryDepth, EntryStone);
        var arm = new ImmutablePen(Lever, Math.Max(4, unit * 0.06), lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
        context.DrawLine(arm, pivot, exit);
        context.DrawLine(arm, pivot, entry);
        var tilt = lever * LeverSwing * Math.PI / 180;
        var fork = new Point(pivot.X + (Math.Sin(tilt) * unit * 0.42), pivot.Y - (Math.Cos(tilt) * unit * 0.42));
        context.DrawLine(arm, pivot, fork);
        context.DrawEllipse(Lever, null, fork, unit * 0.06, unit * 0.06);
        DrawStone(context, exit, wheelCentre, unit);
        DrawStone(context, entry, wheelCentre, unit);
        Mark("wheel", wheelCentre, wheelRadius);
        Mark("lever", [pivot, exit, entry, fork], pad: 8);
        Mark("stones", [exit, entry], pad: 10);
        Mark("balance", balanceCentre, balanceRadius);
        context.DrawEllipse(WatchParts.Dark, null, pivot, 4, 4);

        // The balance and its pin, which meets the lever's fork as it swings through the middle.
        WatchParts.DrawBalance(context, balanceCentre, balanceRadius, balance);

        DiagramText.Draw(context, DiagramText.Get("Watch_Escape_Balance"), new Point(balanceCentre.X + balanceRadius + 16, balanceCentre.Y), 12, DiagramText.Faint, 0, 0.5);
        DiagramText.Draw(context, DiagramText.Get("Watch_Escape_Lever"), new Point(pivot.X + (unit * 0.5), pivot.Y), 12, DiagramText.Faint, 0, 0.5);
        DiagramText.Draw(context, DiagramText.Get("Watch_Escape_Wheel"), new Point(wheelCentre.X + wheelRadius + 12, wheelCentre.Y + (wheelRadius * 0.5)), 12, DiagramText.Faint);
        if (ticking)
        {
            var even = ((long)Math.Floor(halfTeeth)) % 2 == 0;
            DiagramText.Draw(context, DiagramText.Get(even ? "Watch_Escape_Tick" : "Watch_Escape_Tock"),
                new Point(wheelCentre.X - wheelRadius - 20, wheelCentre.Y - (wheelRadius * 0.6)), 20, DiagramText.Gold, 1, 0.5, bold: true);
        }

        DiagramText.Draw(context, DiagramText.Format("Watch_Escape_Slow", (1 / Math.Max(0.001, SlowMotion)).ToString("0")),
            new Point(bounds.X + 16, bounds.Y + 14), 12.5, DiagramText.Ink);

        // The speed track under the words: left slower, right faster.
        var track = new Rect(bounds.X + 24, bounds.Y + 56, Math.Min(140, bounds.Width * 0.3), 0);
        context.DrawLine(TrackPen, track.TopLeft, track.TopRight);
        var slow = Math.Clamp(SlowMotion, Slowest, Fastest);
        Grip("speed", new Point(track.X + (track.Width * (slow - Slowest) / (Fastest - Slowest)), track.Y), GripAxis.Horizontal,
            DiagramText.Get("Watch_Grip_SlowMotion"),
            at => SlowMotion = Math.Round(Slowest + ((Fastest - Slowest) * Math.Clamp((at.X - track.X) / track.Width, 0, 1)), 2));

        // The balance's rim, where its pin points: dragged round, the balance swings by hand and the beat follows.
        Grip("balance", WatchParts.At(balanceCentre, balanceRadius, (balance * Math.PI / 180) + (Math.PI / 2)), GripAxis.Free,
            DiagramText.Get("Watch_Grip_SwingBalance"), at =>
            {
                var now = Escapement.At(_seconds).Balance * Math.PI / 180;
                var towards = Math.Atan2(at.Y - balanceCentre.Y, at.X - balanceCentre.X) - (Math.PI / 2);
                var degrees = (now + Math.IEEERemainder(towards - now, 2 * Math.PI)) * 180 / Math.PI;
                _seconds = TimeAt(_seconds, degrees);
            });
    }

    /// <summary>A point <paramref name="radius"/> from the wheel's centre at <paramref name="degrees"/> anticlockwise from 3 o'clock.</summary>
    private static Point OnWheel(Point centre, double radius, double degrees) =>
        new(centre.X + (radius * Math.Cos(degrees * Math.PI / 180)), centre.Y - (radius * Math.Sin(degrees * Math.PI / 180)));

    /// <summary>A pallet stone: a small red block pointing at the wheel's centre.</summary>
    private static void DrawStone(DrawingContext context, Point at, Point wheel, double unit)
    {
        var toward = Math.Atan2(wheel.Y - at.Y, wheel.X - at.X);
        var size = Math.Max(5, unit * 0.07);
        using (context.PushTransform(Matrix.CreateRotation(toward) * Matrix.CreateTranslation(at.X, at.Y)))
        {
            context.FillRectangle(Stone, new Rect(-size * 0.4, -size * 0.6, size * 1.8, size * 1.2), 1.5f);
        }
    }
}
