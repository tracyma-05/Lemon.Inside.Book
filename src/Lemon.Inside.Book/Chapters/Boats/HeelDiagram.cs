using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Boats;

/// <summary>
/// Spread 4: a boat seen end-on, tipped over by the chosen angle. The water it pushes aside is outlined; its centre
/// (where the water's push acts) has moved to the low side, while the weight still pulls down through the boat's own
/// centre. The sideways gap between them is the lever that turns the boat back.
/// </summary>
public sealed class HeelDiagram : GearPicture
{
    public static readonly StyledProperty<double> HeelAngleProperty =
        AvaloniaProperty.Register<HeelDiagram, double>(nameof(HeelAngle), 15);

    static HeelDiagram()
    {
        AffectsRender<HeelDiagram>(HeelAngleProperty);
    }

    /// <summary>How far the boat is tipped, degrees (the right side down).</summary>
    public double HeelAngle
    {
        get => GetValue(HeelAngleProperty);
        set => SetValue(HeelAngleProperty, value);
    }

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var boat = HeelSpread.Boat;
        var heel = HeelAngle * Math.PI / 180;
        var scale = Math.Min(bounds.Width * 0.6 / boat.Beam, bounds.Height * 0.62 / (boat.HullDepth + 3));
        var waterline = bounds.Height * 0.52;
        var drawn = BoatSection.Draw(context, boat, heel, new Point(bounds.Width * 0.5, waterline), scale, bounds);
        Mark("hull", drawn.Hull);

        // The push of the water up through its centre; the weight down through the boat's.
        var arrow = Math.Min(80, scale * 1.2);
        BoatsParts.DrawArrow(context, drawn.Weight, drawn.Weight + new Point(0, arrow), BoatsParts.Weight, 5);
        BoatsParts.DrawCentre(context, drawn.Weight, 7);
        BoatsParts.DrawArrow(context, drawn.Buoyancy, drawn.Buoyancy - new Point(0, arrow), BoatsParts.Lift, 5);
        context.DrawEllipse(BoatsParts.Lift, new ImmutablePen(BoatsParts.Dark, 1.5), drawn.Buoyancy, 6, 6);
        Mark("weight", [drawn.Weight, drawn.Weight + new Point(0, arrow)], 6);
        Mark("lift", [drawn.Buoyancy, drawn.Buoyancy - new Point(0, arrow)], 6);
        DiagramText.Draw(context, DiagramText.Get("Boats_Heel_WeightLabel"), drawn.Weight + new Point(-10, arrow), 12, BoatsParts.Weight, 1, 1);
        DiagramText.Draw(context, DiagramText.Get("Boats_Heel_LiftLabel"), drawn.Buoyancy - new Point(-10, arrow), 12, BoatsParts.Lift, 0, 0);

        // The lever: the sideways gap between the two lines of push, at the weight's height.
        var y = drawn.Weight.Y;
        var from = new Point(drawn.Weight.X, y);
        var to = new Point(drawn.Buoyancy.X, y);
        context.DrawLine(new ImmutablePen(BoatsParts.Lift, 1, new ImmutableDashStyle([4, 3], 0)), drawn.Buoyancy, to - new Point(0, 30));
        context.DrawLine(new ImmutablePen(DiagramText.Gold, 4, lineCap: PenLineCap.Round), from, to);
        Mark("lever", [from - new Point(0, 4), to + new Point(0, 4)], 4);
        var lever = boat.RightingLever(heel);
        DiagramText.Draw(context, DiagramText.Format("Boats_Heel_Lever", lever.ToString("0.00")), new Point((from.X + to.X) / 2, y - 12), 12.5, DiagramText.Gold, 0.5, 1, bold: true);

        DiagramText.Draw(context, DiagramText.Get(heel < 0.01 ? "Boats_Heel_Balanced" : lever >= 0 ? "Boats_Heel_Rights" : "Boats_Heel_TipsOver"),
            new Point(bounds.X + 18, bounds.Bottom - 40), 14, lever >= 0 ? DiagramText.Ink : BoatsParts.Danger, bold: true);
    }
}

/// <summary>Where a drawn section's parts landed on screen.</summary>
internal readonly record struct DrawnSection(IReadOnlyList<Point> Hull, Point Weight, Point Buoyancy, Point Deck);

/// <summary>A boat's section drawn heeled over, floating at its waterline: hull, deck, mast, the water over it, the water it pushes aside.</summary>
internal static class BoatSection
{
    private static readonly IPen Mast = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xC8, 0xB0, 0x86)), 3, lineCap: PenLineCap.Round);

    public static DrawnSection Draw(DrawingContext context, Section boat, double heel, Point keelAtWater, double scale, Rect bounds,
        double cargoHeight = 0)
    {
        var (level, under, buoyancy, weight) = boat.At(heel);
        Point Map(Point p) => new(keelAtWater.X + (p.X * scale), keelAtWater.Y - ((p.Y - level) * scale));
        Point Heeled(double x, double y) => Map(Section.Heeled(new Point(x, y), heel));

        var hull = boat.Corners.Select(c => Map(Section.Heeled(c, heel))).ToList();

        // The mast first, so the hull covers its foot; cargo stacked on deck when asked.
        context.DrawLine(Mast, Heeled(0, boat.HullDepth), Heeled(0, boat.HullDepth + (boat.Beam * 0.55)));
        if (cargoHeight > boat.HullDepth)
        {
            var w = boat.Beam * 0.5;
            var stack = new[] { Heeled(-w / 2, boat.HullDepth), Heeled(w / 2, boat.HullDepth), Heeled(w / 2, cargoHeight), Heeled(-w / 2, cargoHeight) };
            context.DrawGeometry(new ImmutableSolidColorBrush(Color.FromRgb(0xD9, 0xB3, 0x5B)), new ImmutablePen(BoatsParts.Dark, 1.2), BoatsParts.Outline(stack));
        }

        context.DrawGeometry(BoatsParts.HullFill, new ImmutablePen(BoatsParts.Dark, 1.5), BoatsParts.Outline(hull));
        context.DrawLine(new ImmutablePen(BoatsParts.Deck, 4), Heeled(-boat.Beam / 2, boat.HullDepth), Heeled(boat.Beam / 2, boat.HullDepth));

        BoatsParts.DrawWater(context, new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Bottom - 56), keelAtWater.Y, 0.62);
        if (under.Count > 2)
        {
            context.DrawGeometry(null, new ImmutablePen(BoatsParts.Lift, 1.5, new ImmutableDashStyle([5, 3], 0)), BoatsParts.Outline(under.Select(Map)));
        }

        return new DrawnSection(hull, Map(weight), Map(buoyancy), Heeled(0, boat.HullDepth));
    }
}
