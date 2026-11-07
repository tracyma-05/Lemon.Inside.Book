using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Car;

/// <summary>
/// Spread 5: a car going round a left-hand bend, seen from above, its rear wheels' tracks marked as they roll (the outer
/// one longer). Below, the differential edge-on: the case with the ring gear, a side gear for each wheel, and the spider
/// gear between them, which spins whenever the wheels' speeds differ.
/// </summary>
public sealed class BendDiagram : GearPicture
{
    public static readonly StyledProperty<double> RadiusProperty =
        AvaloniaProperty.Register<BendDiagram, double>(nameof(Radius), 12);

    /// <summary>The car's speed, metres a second (slowed down for the picture).</summary>
    private const double Speed = 2;

    private static readonly IImmutableBrush Road = new ImmutableSolidColorBrush(Color.FromRgb(0x2E, 0x34, 0x40));
    private static readonly IImmutableBrush Body = new ImmutableSolidColorBrush(Color.FromRgb(0xE0, 0x7A, 0x4F));
    private static readonly IImmutableBrush Stripe = new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0x10, 0x14, 0x1E));
    private static readonly IImmutableBrush TyreBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x2A, 0x2E, 0x36));

    private double _metres;
    private double _inner;
    private double _outer;

    static BendDiagram()
    {
        AffectsRender<BendDiagram>(RadiusProperty);
    }

    /// <summary>The bend's radius to the middle of the car, metres.</summary>
    public double Radius
    {
        get => GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    protected override void Advance(double seconds)
    {
        var (inner, outer) = Differential.OnBend(Radius);
        _metres += Speed * seconds;
        _inner += Speed * inner * seconds;
        _outer += Speed * outer * seconds;
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var top = new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height * 0.52);
        var perMetre = top.Width * 0.04;
        var car = new Point(top.Width * 0.62, top.Height * 0.62);
        var centre = new Point(car.X - (Radius * perMetre), car.Y);
        var half = Differential.Track / 2;

        // The road: a ring round the bend's centre, the car's rear axle on its line, heading up the page.
        using (context.PushClip(top))
        {
            var lane = 3.6 * perMetre;
            context.DrawEllipse(null, new ImmutablePen(Road, lane), centre, Radius * perMetre, Radius * perMetre);
            foreach (var (r, travelled, brush) in new[] { (Radius - half, _inner, DiagramText.Cyan), (Radius + half, _outer, DiagramText.Gold) })
            {
                // Marks left by each wheel every metre it rolls: farther apart on the outside.
                var radius = r * perMetre;
                for (var k = -30; k < 30; k++)
                {
                    var along = k - (travelled % 1);
                    var a = -(along / r);
                    var p = new Point(centre.X + (radius * Math.Cos(a)), centre.Y + (radius * Math.Sin(a)));
                    context.DrawEllipse(brush, null, p, 2.2, 2.2);
                }
            }

            context.DrawRectangle(Body, new ImmutablePen(CarParts.Dark, 1.5), new Rect(car.X - (0.9 * perMetre), car.Y - (3.6 * perMetre), 1.8 * perMetre, 4.2 * perMetre), 6, 6);
            context.FillRectangle(TyreBrush, new Rect(car.X - ((half + 0.12) * perMetre), car.Y - (0.3 * perMetre), 0.24 * perMetre, 0.6 * perMetre));
            context.FillRectangle(TyreBrush, new Rect(car.X + ((half - 0.12) * perMetre), car.Y - (0.3 * perMetre), 0.24 * perMetre, 0.6 * perMetre));
        }

        Mark("road", [new Point(car.X - (half * perMetre), car.Y - (4 * perMetre)), new Point(car.X + (half * perMetre), car.Y + (4 * perMetre))], 10);
        DiagramText.Draw(context, DiagramText.Format("Car_Bend_Centre", Radius.ToString("0")), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);

        DrawDifferential(context, new Rect(bounds.X + 20, top.Bottom + 10, bounds.Width - 40, bounds.Height - top.Bottom - 30));
    }

    /// <summary>The differential edge-on: inner wheel's side gear left, outer's right, the case and spider between.</summary>
    private void DrawDifferential(DrawingContext context, Rect area)
    {
        var mid = new Point(area.Center.X, area.Y + (area.Height * 0.42));
        var size = Math.Min(area.Height * 0.62, area.Width * 0.3);
        var caseTurns = Differential.Case(_inner, _outer);

        // Half-shafts out to the wheels, the wheels themselves edge-on at the ends.
        context.DrawLine(new ImmutablePen(CarParts.MetalBrush, size * 0.08), new Point(area.X + 30, mid.Y), new Point(area.Right - 30, mid.Y));
        CarParts.DrawEdgeOn(context, new Rect(area.X, mid.Y - (size * 0.5), size * 0.22, size), _inner * 0.4, TyreBrush, Stripe, 8);
        CarParts.DrawEdgeOn(context, new Rect(area.Right - (size * 0.22), mid.Y - (size * 0.5), size * 0.22, size), _outer * 0.4, TyreBrush, Stripe, 8);

        // The case and its ring gear, turning at the average.
        var caseBox = new Rect(mid.X - (size * 0.5), mid.Y - (size * 0.45), size, size * 0.9);
        CarParts.DrawEdgeOn(context, caseBox, caseTurns * 0.4, new ImmutableSolidColorBrush(Color.FromArgb(0x60, 0x5A, 0x64, 0x78)), Stripe, 10, 8);
        var ring = new Rect(caseBox.X - (size * 0.12), mid.Y - (size * 0.55), size * 0.12, size * 1.1);
        CarParts.DrawEdgeOn(context, ring, caseTurns * 0.4, DiagramText.Gold, Stripe, 12, 3);
        Mark("case", caseBox.Union(ring));

        // Side gears, each with its wheel.
        var side = size * 0.16;
        CarParts.DrawEdgeOn(context, new Rect(mid.X - (size * 0.36), mid.Y - (size * 0.3), side, size * 0.6), _inner * 0.4, DiagramText.Cyan, Stripe, 6, 2);
        CarParts.DrawEdgeOn(context, new Rect(mid.X + (size * 0.2), mid.Y - (size * 0.3), side, size * 0.6), _outer * 0.4, DiagramText.Gold, Stripe, 6, 2);

        // The spider between them, face-on, turning by half the difference.
        var spider = Differential.Spider(_inner, _outer) * 2 * Math.PI * 0.4 * 3;
        var spiderCentre = new Point(mid.X - (size * 0.02), mid.Y);
        DrawGear(context, 10, 20 * Math.PI / 180, spiderCentre, size * 0.24 / 6, spider, Copper);
        Mark("spider", spiderCentre, size * 0.24);

        Mark("wheels", [new Point(area.X, mid.Y - (size * 0.5)), new Point(area.X + (size * 0.22), mid.Y + (size * 0.5))], 4);
        var (inner, outer) = Differential.OnBend(Radius);
        var y = mid.Y + (size * 0.6) + 6;
        DiagramText.Draw(context, DiagramText.Format("Car_Bend_Inner", (inner * 100).ToString("0")), new Point(area.X, y), 12, DiagramText.Cyan);
        DiagramText.Draw(context, DiagramText.Format("Car_Bend_Outer", (outer * 100).ToString("0")), new Point(area.Right, y), 12, DiagramText.Gold, 1);
        DiagramText.Draw(context, DiagramText.Get("Car_Bend_CaseLabel"), new Point(mid.X, caseBox.Y - 6), 12, DiagramText.Ink, 0.5, 1);
    }
}
