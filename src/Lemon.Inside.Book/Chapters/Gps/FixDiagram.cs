using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Gps.GpsParts;

namespace Lemon.Inside.Book.Chapters.Gps;

/// <summary>
/// Spread 2: a flat map with three satellites (where they are straight above), and a circle round each one used, as far
/// out as the receiver. One circle leaves a whole ring of places; two cross at two points; three meet at one. The reader
/// drags the receiver about and taps the switch to use one, two or three satellites.
/// </summary>
public sealed class FixDiagram : GearPicture
{
    public static readonly StyledProperty<double> CountProperty =
        AvaloniaProperty.Register<FixDiagram, double>(nameof(Count), 2, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> EastProperty =
        AvaloniaProperty.Register<FixDiagram, double>(nameof(East), 30, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> NorthProperty =
        AvaloniaProperty.Register<FixDiagram, double>(nameof(North), 10, defaultBindingMode: BindingMode.TwoWay);

    private const double Half = 230;

    static FixDiagram()
    {
        AffectsRender<FixDiagram>(CountProperty, EastProperty, NorthProperty);
    }

    public double Count
    {
        get => GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public double East
    {
        get => GetValue(EastProperty);
        set => SetValue(EastProperty, value);
    }

    public double North
    {
        get => GetValue(NorthProperty);
        set => SetValue(NorthProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var count = (int)Math.Round(Math.Clamp(Count, 1, 3));
        var you = new MapPoint(Math.Clamp(East, -200, 200), Math.Clamp(North, -200, 200));
        var size = Math.Min(bounds.Width - 60, bounds.Height - 230);
        var box = new Rect(bounds.Center.X - (size / 2), bounds.Y + 60, size, size);
        DrawMap(context, box, Half);
        Point P(MapPoint p) => OnMap(p, box, Half);
        var pxPerKm = box.Width / (2 * Half);

        using (context.PushClip(box))
        {
            for (var i = 0; i < count; i++)
            {
                var r = Map.Satellites[i].DistanceTo(you) * pxPerKm;
                context.DrawEllipse(null, Line(SignalInk, 2), P(Map.Satellites[i]), r, r);
            }
        }

        var firstRadius = Map.Satellites[0].DistanceTo(you);
        Mark("circle", P(Map.Satellites[0] + new MapPoint(0, -firstRadius)), 12);
        for (var i = 0; i < 3; i++)
        {
            var at = P(Map.Satellites[i]);
            DrawSatellite(context, at, 12, i < count ? 1 : 0.3);
            DiagramText.Draw(context, Number(i + 1), at + new Vector(0, 12), 12, i < count ? DiagramText.Ink : DiagramText.Faint, 0.5, 0, bold: true);
        }

        Mark("satellites", Map.Satellites.Select(P), 18);

        // Where you could be.
        var places = Map.Places(you, count);
        MapPoint other;
        if (places is null)
        {
            other = Map.Satellites[0] + ((Map.Satellites[0] - you) * (1 / Math.Max(1e-6, firstRadius)) * firstRadius);
        }
        else
        {
            foreach (var p in places) context.DrawEllipse(null, Line(PlaceInk, 2.5), P(p), 9, 9);
            other = places.OrderByDescending(p => p.DistanceTo(you)).First();
        }

        Mark("crossing", P(other), 12);
        var here = P(you);
        context.DrawEllipse(PlaceInk, Edge, here, 6, 6);
        Mark("you", here, 10);
        Grip("you", here + new Vector(14, 14), GripAxis.Free, DiagramText.Get("Gps_Grip_You"), at =>
        {
            var m = FromMap(at - new Vector(14, 14), box, Half);
            (East, North) = (Math.Round(Math.Clamp(m.X, -200, 200)), Math.Round(Math.Clamp(m.Y, -200, 200)));
        });

        var sw = Colour.ColourParts.DrawSwitch(context, new Point(bounds.Center.X, box.Bottom + 34), DiagramText.Format("Gps_Fix_Using", count), false);
        Grip("count", new Point(sw.Left - 2, sw.Center.Y), GripAxis.Tap, DiagramText.Get("Gps_Grip_Count"), _ => Count = count % 3 + 1);
        DiagramText.Draw(context, DiagramText.Get(count switch { 1 => "Gps_Fix_ShowOne", 2 => "Gps_Fix_ShowTwo", _ => "Gps_Fix_ShowThree" }),
            new Point(bounds.Center.X, box.Bottom + 62), 13, PlaceInk, 0.5, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Gps_Fix_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
