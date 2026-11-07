using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Bicycle;

/// <summary>
/// Spread 1: a wheel rolling slowly under a load, its spokes coloured by how hard each one pulls. The spokes passing
/// under the hub pull less; with spokes built too loose they go slack (red, bowed).
/// </summary>
public sealed class WheelDiagram : GearPicture
{
    public static readonly StyledProperty<double> TensionProperty =
        AvaloniaProperty.Register<WheelDiagram, double>(nameof(Tension), 1000);

    public static readonly StyledProperty<double> LoadProperty =
        AvaloniaProperty.Register<WheelDiagram, double>(nameof(Load), 60);

    /// <summary>The wheel's roll, radians per second.</summary>
    private const double RollSpeed = 0.18;

    /// <summary>The pull the colours run up to, newtons.</summary>
    private const double MostPull = 1500;

    private static readonly Color Loose = Color.FromRgb(0x4A, 0x55, 0x6A);
    private static readonly Color Taut = Color.FromRgb(0xF2, 0xC9, 0x4C);
    private static readonly Color Slack = Color.FromRgb(0xE8, 0x5A, 0x5A);
    private static readonly IPen Ground = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0xE6, 0xEA, 0xF2)), 2);
    private static readonly IPen GroundMarks = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x50, 0xE6, 0xEA, 0xF2)), 1.5);

    private double _turned;

    static WheelDiagram()
    {
        AffectsRender<WheelDiagram>(TensionProperty, LoadProperty);
    }

    public double Tension
    {
        get => GetValue(TensionProperty);
        set => SetValue(TensionProperty, value);
    }

    /// <summary>The load on this wheel, kilograms.</summary>
    public double Load
    {
        get => GetValue(LoadProperty);
        set => SetValue(LoadProperty, value);
    }

    protected override void Advance(double seconds) => _turned += RollSpeed * seconds;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var radius = Math.Min(bounds.Width * 0.36, bounds.Height * 0.32);
        var centre = new Point(bounds.Width * 0.5, (bounds.Height * 0.1) + radius + 24);
        var tyre = radius * 0.07;
        var floor = centre.Y + radius + tyre;

        // The road, its marks moving back as the wheel rolls forwards (to the right, turning clockwise).
        context.DrawLine(Ground, new Point(bounds.X + 16, floor), new Point(bounds.Right - 16, floor));
        var travelled = _turned * (radius + tyre);
        for (var x = -(travelled % 40); x < bounds.Width; x += 40)
        {
            context.DrawLine(GroundMarks, new Point(x, floor + 6), new Point(x + 14, floor + 6));
        }

        var tensions = SpokedWheel.Tensions(Tension, Load, _turned);
        var hubRadius = radius * 0.09;
        var rim = radius * 0.96;
        for (var i = 0; i < tensions.Length; i++)
        {
            // Spoke i points _turned + i·step from straight down, clockwise.
            var a = (Math.PI / 2) + _turned + (i * 2 * Math.PI / tensions.Length);
            var from = BicycleParts.At(centre, hubRadius, a);
            var to = BicycleParts.At(centre, rim, a);
            if (tensions[i] <= 0)
            {
                // Slack: no longer straight, it bows.
                var bow = BicycleParts.At(BicycleParts.At(centre, (hubRadius + rim) / 2, a), radius * 0.05, a + (Math.PI / 2));
                var curve = new StreamGeometry();
                using (var g = curve.Open())
                {
                    g.BeginFigure(from, false);
                    g.QuadraticBezierTo(bow, to);
                    g.EndFigure(false);
                }

                context.DrawGeometry(null, new ImmutablePen(new ImmutableSolidColorBrush(Slack), 1.6, new ImmutableDashStyle([3, 2], 0)), curve);
            }
            else
            {
                var f = Math.Clamp(tensions[i] / MostPull, 0, 1);
                context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Mix(Loose, Taut, f)), 1.1 + (2.4 * f)), from, to);
            }
        }

        context.DrawEllipse(null, new ImmutablePen(new ImmutableSolidColorBrush(BicycleParts.Tyre), tyre), centre, radius + (tyre / 2), radius + (tyre / 2));
        context.DrawEllipse(null, new ImmutablePen(new ImmutableSolidColorBrush(BicycleParts.Steel), Math.Max(2, radius * 0.035)), centre, rim, rim);
        context.DrawEllipse(new ImmutableSolidColorBrush(BicycleParts.Steel), new ImmutablePen(BicycleParts.Dark, 1.5), centre, hubRadius, hubRadius);

        Mark("hub", centre, hubRadius + 4);
        Mark("rim", centre, radius + tyre);
        Mark("bottom", Sector(centre, hubRadius, rim, Math.PI / 2));
        Mark("top", Sector(centre, hubRadius * 1.5, rim, -Math.PI / 2));

        // The load pressing on the axle.
        var arrowTop = new Point(centre.X, centre.Y - (radius * 0.42));
        var arrowTip = new Point(centre.X, centre.Y - hubRadius - 3);
        var width = 3 + (Load / 150 * 6);
        if (Load > 0.5)
        {
            context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x99, 0x05, 0x08, 0x10)), width + 4), arrowTop, arrowTip);
            context.DrawLine(new ImmutablePen(new ImmutableSolidColorBrush(GearPicture.Copper), width), arrowTop, arrowTip - new Point(0, 6));
            DiagramText.DrawArrowAlong(context, arrowTop, arrowTip, new ImmutableSolidColorBrush(GearPicture.Copper), 10 + width);
        }

        DiagramText.Draw(context, DiagramText.Format("Bicycle_Kilograms", Load.ToString("0")), arrowTop - new Point(0, 4), 13, DiagramText.Ink, 0.5, 1, bold: true);
        Mark("load", [arrowTop - new Point(0, 18), arrowTip], 6);

        // The spokes now straight up and straight down, and what they pull.
        int Nearest(double target)
        {
            var best = 0;
            var gap = double.MaxValue;
            for (var i = 0; i < tensions.Length; i++)
            {
                var d = Math.Abs(Math.IEEERemainder(_turned + (i * 2 * Math.PI / tensions.Length) - target, 2 * Math.PI));
                if (d < gap) (best, gap) = (i, d);
            }

            return best;
        }

        var up = tensions[Nearest(Math.PI)];
        var down = tensions[Nearest(0)];
        var labels = new Point(bounds.X + 18, floor + 30);
        var line = DiagramText.Draw(context, DiagramText.Format("Bicycle_Wheel_TopPull", up.ToString("0")), labels, 12.5, DiagramText.Ink);
        DiagramText.Draw(context, down <= 0 ? DiagramText.Get("Bicycle_Wheel_BottomSlack") : DiagramText.Format("Bicycle_Wheel_BottomPull", down.ToString("0")),
            new Point(labels.X, line.Bottom + 4), 12.5, down <= 0 ? new ImmutableSolidColorBrush(Slack) : DiagramText.Ink);

        // A key to the colours.
        var key = new Rect(bounds.Right - 18 - Math.Min(170, bounds.Width * 0.36), floor + 34, Math.Min(170, bounds.Width * 0.36), 8);
        context.FillRectangle(new ImmutableLinearGradientBrush(
            [new ImmutableGradientStop(0, Slack), new ImmutableGradientStop(0.08, Slack), new ImmutableGradientStop(0.1, Loose), new ImmutableGradientStop(1, Taut)],
            startPoint: new RelativePoint(0, 0, RelativeUnit.Relative), endPoint: new RelativePoint(1, 0, RelativeUnit.Relative)), key, 3);
        DiagramText.Draw(context, DiagramText.Get("Bicycle_Wheel_KeySlack"), new Point(key.X, key.Bottom + 3), 11.5, DiagramText.Faint);
        DiagramText.Draw(context, DiagramText.Get("Bicycle_Wheel_KeyTight"), new Point(key.Right, key.Bottom + 3), 11.5, DiagramText.Faint, 1);
    }

    /// <summary>The points of a slice of the wheel 70° wide round <paramref name="angle"/> (screen), from <paramref name="inner"/> to <paramref name="outer"/>.</summary>
    private static IEnumerable<Point> Sector(Point centre, double inner, double outer, double angle)
    {
        for (var k = -2; k <= 2; k++)
        {
            var a = angle + (k * 0.3);
            yield return BicycleParts.At(centre, inner, a);
            yield return BicycleParts.At(centre, outer, a);
        }
    }

    private static Color Mix(Color a, Color b, double t) => Color.FromRgb(
        (byte)(a.R + ((b.R - a.R) * t)), (byte)(a.G + ((b.G - a.G) * t)), (byte)(a.B + ((b.B - a.B) * t)));
}
