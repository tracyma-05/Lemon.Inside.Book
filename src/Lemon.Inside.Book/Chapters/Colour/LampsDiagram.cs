using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Colour.ColourParts;

namespace Lemon.Inside.Book.Chapters.Colour;

/// <summary>
/// Spread 1: a red, a green and a blue lamp shine on a dark wall, their pools of light overlapping. Each region is
/// lit by the sum of the lamps that reach it: yellow where red and green meet, white where all three do. Beside them, one
/// screen pixel close up (three stripes, one per lamp) and the same pixel seen from a step back. The reader drags each
/// lamp's level up or down.
/// </summary>
public sealed class LampsDiagram : GearPicture
{
    public static readonly StyledProperty<double> RedProperty =
        AvaloniaProperty.Register<LampsDiagram, double>(nameof(Red), 1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> GreenProperty =
        AvaloniaProperty.Register<LampsDiagram, double>(nameof(Green), 1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> BlueProperty =
        AvaloniaProperty.Register<LampsDiagram, double>(nameof(Blue), 1, defaultBindingMode: BindingMode.TwoWay);

    static LampsDiagram()
    {
        AffectsRender<LampsDiagram>(RedProperty, GreenProperty, BlueProperty);
    }

    public double Red
    {
        get => GetValue(RedProperty);
        set => SetValue(RedProperty, value);
    }

    public double Green
    {
        get => GetValue(GreenProperty);
        set => SetValue(GreenProperty, value);
    }

    public double Blue
    {
        get => GetValue(BlueProperty);
        set => SetValue(BlueProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var levels = new[] { Math.Clamp(Red, 0, 1), Math.Clamp(Green, 0, 1), Math.Clamp(Blue, 0, 1) };
        var w = bounds.Width;
        var h = bounds.Height;

        // The three pools of light, red top left, green top right, blue below.
        var r = Math.Min(w * 0.2, h * 0.17);
        var centre = new Point(bounds.X + ((w - 130) * 0.5), bounds.Y + (h * 0.36));
        Point[] centres =
        [
            centre + new Vector(-r * 0.55, -r * 0.32),
            centre + new Vector(r * 0.55, -r * 0.32),
            centre + new Vector(0, r * 0.62),
        ];
        DrawThreeDiscs(context, centres, r, mask => new Rgb(
            (mask & 1) != 0 ? levels[0] : 0,
            (mask & 2) != 0 ? levels[1] : 0,
            (mask & 4) != 0 ? levels[2] : 0));
        var middle = Middle(centres);
        Mark("overlap", middle, r * 0.22);

        // The lamps' levels, at the right: drag each up or down.
        var height = Math.Min(h * 0.36, 2.4 * r);
        var baseY = centre.Y + (height / 2);
        Rgb[] hues = [new(1, 0, 0), new(0, 1, 0), new(0, 0.45, 1)];
        string[] grips = ["red", "green", "blue"];
        var lampTops = new List<Point>();
        for (var k = 0; k < 3; k++)
        {
            var x = bounds.Right - 104 + (k * 36);
            var top = DrawLevel(context, new Point(x, baseY), height, 16, levels[k], Brush(hues[k]));
            var bulb = new Point(x, baseY - height - 32);
            context.DrawEllipse(Brush(hues[k], 0.25 + (0.75 * levels[k])), Edge, bulb, 9, 9);
            lampTops.Add(bulb + new Vector(0, -10));
            lampTops.Add(new Point(x, baseY));
            var index = k;
            Grip(grips[k], top, GripAxis.Vertical, DiagramText.Get("Colour_Grip_Lamp"), at =>
            {
                var share = Math.Round(LevelAt(at, baseY, height) * 100) / 100;
                if (index == 0) Red = share;
                else if (index == 1) Green = share;
                else Blue = share;
            });
        }

        Mark("lamps", lampTops, 14);
        DiagramText.Draw(context, DiagramText.Get("Colour_Lamps_Levels"), new Point(bounds.Right - 68, baseY + 10), 11, DiagramText.Faint, 0.5);

        // One pixel close up, and the same pixel from a step back.
        var size = Math.Min(96, w * 0.17);
        var rowY = Math.Min(bounds.Bottom - size - 120, centre.Y + r * 1.6 + 40);
        var pixel = new Rect(bounds.X + 36, rowY, size, size);
        context.FillRectangle(Brush(Rgb.Black), pixel);
        for (var k = 0; k < 3; k++)
        {
            var stripe = new Rect(pixel.X + (size * ((k / 3.0) + 0.04)), pixel.Y + (size * 0.06), size * 0.25, size * 0.88);
            context.FillRectangle(Brush(hues[k] * levels[k]), stripe);
        }

        context.DrawRectangle(null, Frame, pixel);
        Mark("pixel", pixel);
        DiagramText.Draw(context, DiagramText.Get("Colour_Lamps_Close"), new Point(pixel.Center.X, pixel.Bottom + 8), 11.5, DiagramText.Faint, 0.5);

        var mix = Lamps.Mix(levels[0], levels[1], levels[2]);
        var swatch = new Rect(pixel.Right + 70, rowY, size, size);
        context.DrawRectangle(Brush(mix), Frame, swatch, 6, 6);
        Mark("mix", swatch);
        DiagramText.Draw(context, DiagramText.Get("Colour_Lamps_Far"), new Point(swatch.Center.X, swatch.Bottom + 8), 11.5, DiagramText.Faint, 0.5);
        DiagramText.DrawArrowAlong(context, new Point(pixel.Right + 12, pixel.Center.Y), new Point(swatch.X - 12, swatch.Center.Y), DiagramText.Faint, 9);
        context.DrawLine(Frame, new Point(pixel.Right + 12, pixel.Center.Y), new Point(swatch.X - 12, swatch.Center.Y));
        DiagramText.Draw(context, DiagramText.Get(ColourNames.Key(mix)), new Point(swatch.Right + 14, swatch.Center.Y), 15, DiagramText.Gold, 0, 0.5, bold: true);

        DiagramText.Draw(context, DiagramText.Get("Colour_Lamps_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
        DiagramText.Draw(context, DiagramText.Format("Colour_Lamps_Count", Number(Lamps.Colours, "N0")),
            new Point(bounds.X + 16, pixel.Bottom + 34), 12, DiagramText.Faint);
    }
}
