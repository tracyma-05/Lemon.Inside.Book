using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Car;

/// <summary>
/// Spread 6: the back of a car, its left rear wheel on a patch of ice and the right on dry road. Bars under each tyre
/// show how hard it pushes the car along. With the differential open, the icy wheel spins and both push only as hard as
/// the ice allows; locked, the wheels turn together and the dry one does the work.
/// </summary>
public sealed class IceDiagram : GearPicture
{
    public static readonly StyledProperty<double> LeftGripProperty =
        AvaloniaProperty.Register<IceDiagram, double>(nameof(LeftGrip), IceSpread.DryGrip);

    public static readonly StyledProperty<bool> LockedProperty =
        AvaloniaProperty.Register<IceDiagram, bool>(nameof(Locked));

    private static readonly IImmutableBrush Asphalt = new ImmutableSolidColorBrush(Color.FromRgb(0x3A, 0x40, 0x4C));
    private static readonly IImmutableBrush TyreBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x2A, 0x2E, 0x36));
    private static readonly IImmutableBrush Stripe = new ImmutableSolidColorBrush(Color.FromArgb(0xB0, 0x8A, 0x92, 0xA2));
    private static readonly IImmutableBrush Body = new ImmutableSolidColorBrush(Color.FromRgb(0xE0, 0x7A, 0x4F));

    private double _left;
    private double _right;

    static IceDiagram()
    {
        AffectsRender<IceDiagram>(LeftGripProperty, LockedProperty);
    }

    /// <summary>How much the left (icy) wheel can grip, as a share of the engine's turning force.</summary>
    public double LeftGrip
    {
        get => GetValue(LeftGripProperty);
        set => SetValue(LeftGripProperty, value);
    }

    public bool Locked
    {
        get => GetValue(LockedProperty);
        set => SetValue(LockedProperty, value);
    }

    /// <summary>Each wheel's turns a second: the car's pace, or much faster for a wheel spinning on ice.</summary>
    private (double Left, double Right) Speeds()
    {
        var push = Differential.Push(LeftGrip, IceSpread.DryGrip, Locked);
        var pace = 0.25 + push;
        if (Locked || !Differential.Spins(LeftGrip, IceSpread.DryGrip, Locked)) return (pace, pace);

        // Open: the case turns at the engine's pace; the stuck side crawls, so the icy side makes up the rest.
        var right = pace * 0.6;
        return ((2 * 1.4) - right, right);
    }

    protected override void Advance(double seconds)
    {
        var (left, right) = Speeds();
        _left += left * seconds;
        _right += right * seconds;
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var ground = bounds.Height * 0.62;
        var wheelHeight = Math.Min(bounds.Height * 0.3, bounds.Width * 0.28);
        var wheelWidth = wheelHeight * 0.38;
        var leftX = bounds.Width * 0.18;
        var rightX = bounds.Width * 0.82 - wheelWidth;

        // The road: ice under the left wheel, dry asphalt under the right.
        context.FillRectangle(Asphalt, new Rect(bounds.X, ground, bounds.Width, bounds.Height * 0.08));
        var icy = Math.Clamp(1 - (LeftGrip / IceSpread.DryGrip), 0, 1);
        context.FillRectangle(new ImmutableSolidColorBrush(Color.FromArgb((byte)(0xD0 * icy), 0xBF, 0xE8, 0xFF)),
            new Rect(leftX - (wheelWidth * 1.5), ground, wheelWidth * 4, bounds.Height * 0.03), 4);

        // The body and axle, the differential in the middle.
        var axle = ground - (wheelHeight / 2);
        context.DrawRectangle(Body, new ImmutablePen(CarParts.Dark, 1.5),
            new Rect(leftX + (wheelWidth * 0.3), axle - (wheelHeight * 1.25), rightX - leftX + (wheelWidth * 0.4), wheelHeight * 0.95), 14, 14);
        context.DrawLine(new ImmutablePen(CarParts.MetalBrush, wheelHeight * 0.07), new Point(leftX + wheelWidth, axle), new Point(rightX, axle));
        var diff = new Rect(bounds.Width * 0.5 - (wheelHeight * 0.28), axle - (wheelHeight * 0.24), wheelHeight * 0.56, wheelHeight * 0.48);
        context.DrawRectangle(CarParts.DarkMetalBrush, new ImmutablePen(CarParts.Dark, 1.5), diff, 10, 10);
        if (Locked)
        {
            context.DrawLine(new ImmutablePen(DiagramText.Gold, 4, lineCap: PenLineCap.Round), new Point(diff.X + 6, axle), new Point(diff.Right - 6, axle));
        }

        DiagramText.Draw(context, DiagramText.Get(Locked ? "Car_Ice_DiffLocked" : "Car_Ice_DiffOpen"), new Point(diff.Center.X, diff.Bottom + 6), 12, DiagramText.Ink, 0.5, 0, bold: true);
        Mark("diff", diff);

        // The wheels, their tread sliding as they turn; a spinning one spins fast.
        var left = new Rect(leftX, ground - wheelHeight, wheelWidth, wheelHeight);
        var right = new Rect(rightX, ground - wheelHeight, wheelWidth, wheelHeight);
        CarParts.DrawEdgeOn(context, left, _left, TyreBrush, Stripe, 7, 8);
        CarParts.DrawEdgeOn(context, right, _right, TyreBrush, Stripe, 7, 8);
        Mark("slippy", left);
        Mark("firm", right);
        var spins = Differential.Spins(LeftGrip, IceSpread.DryGrip, Locked);
        if (spins)
        {
            DiagramText.Draw(context, DiagramText.Get("Car_Ice_Spinning"), new Point(left.Center.X, axle - (wheelHeight * 1.25) - 8), 14, CarParts.Hot, 0.5, 1, bold: true);
        }

        DiagramText.Draw(context, DiagramText.Get("Car_Ice_IceLabel"), new Point(left.Center.X, ground + (bounds.Height * 0.03) + 6), 11.5, DiagramText.Cyan, 0.5);
        DiagramText.Draw(context, DiagramText.Get("Car_Ice_DryLabel"), new Point(right.Center.X, ground + (bounds.Height * 0.03) + 6), 11.5, DiagramText.Faint, 0.5);

        // How hard each tyre pushes the car along: an open differential gives both the same, no more than the ice takes.
        var (leftPush, rightPush) = Locked
            ? (Math.Min(LeftGrip, 1), Math.Min(IceSpread.DryGrip, Math.Max(0, 1 - LeftGrip)))
            : (Math.Min(LeftGrip, 0.5), Math.Min(LeftGrip, 0.5));
        var bars = new Rect(bounds.X + 24, bounds.Height * 0.78, bounds.Width - 48, bounds.Height * 0.16);
        DrawPush(context, new Rect(bars.X, bars.Y, bars.Width * 0.45, bars.Height), leftPush, DiagramText.Get("Car_Ice_LeftPush"));
        DrawPush(context, new Rect(bars.X + (bars.Width * 0.55), bars.Y, bars.Width * 0.45, bars.Height), rightPush, DiagramText.Get("Car_Ice_RightPush"));
        DiagramText.Draw(context, DiagramText.Format("Car_Ice_Total", ((leftPush + rightPush) * 100).ToString("0")), new Point(bars.X, bars.Bottom + 4), 13, DiagramText.Gold, bold: true);
        Mark("push", bars);
    }

    private static void DrawPush(DrawingContext context, Rect area, double share, string label)
    {
        DiagramText.Draw(context, label, new Point(area.X, area.Y), 12, DiagramText.Ink);
        var bar = new Rect(area.X, area.Y + 20, area.Width, 10);
        context.FillRectangle(CarParts.Track, bar, 3);
        context.FillRectangle(DiagramText.Gold, new Rect(bar.X, bar.Y, bar.Width * Math.Clamp(share / 0.8, 0, 1), bar.Height), 3);
    }
}
