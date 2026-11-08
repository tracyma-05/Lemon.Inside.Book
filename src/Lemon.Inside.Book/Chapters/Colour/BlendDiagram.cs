using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Colour.ColourParts;

namespace Lemon.Inside.Book.Chapters.Colour;

/// <summary>
/// Spread 4: fine black and white stripes beside a plain grey patch, to be matched by eye; below, a red-to-green blend
/// made twice, once by averaging the stored numbers (it sags dark in the middle) and once as light (it passes through
/// yellow). The reader drags the patch's level and a marker along the blends.
/// </summary>
public sealed class BlendDiagram : GearPicture
{
    public static readonly StyledProperty<double> GreyProperty =
        AvaloniaProperty.Register<BlendDiagram, double>(nameof(Grey), 128, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> PositionProperty =
        AvaloniaProperty.Register<BlendDiagram, double>(nameof(Position), 0.5, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>How many bands each blend is drawn with.</summary>
    private const int Bands = 48;

    static BlendDiagram()
    {
        AffectsRender<BlendDiagram>(GreyProperty, PositionProperty);
    }

    /// <summary>The patch's stored number, 0 to 255.</summary>
    public double Grey
    {
        get => GetValue(GreyProperty);
        set => SetValue(GreyProperty, value);
    }

    /// <summary>Where along the blends the marker is, 0 (red) to 1 (green).</summary>
    public double Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var grey = Math.Round(Math.Clamp(Grey, 0, 255));
        var position = Math.Clamp(Position, 0, 1);
        var w = bounds.Width;

        // The stripes, one black and one white line after another, each a whole pixel.
        var size = Math.Round(Math.Min(150, w * 0.28));
        var gap = 50;
        var x0 = Math.Round(bounds.Center.X - size - (gap / 2.0));
        var y0 = Math.Round(bounds.Y + 64);
        var stripes = new Rect(x0, y0, size, size);
        context.FillRectangle(Brush(Rgb.Black), stripes);
        for (var y = 0; y < size; y += 2)
        {
            context.FillRectangle(Brush(Rgb.White), new Rect(x0, y0 + y, size, 1));
        }

        context.DrawRectangle(null, Frame, stripes);
        Mark("stripes", stripes);
        DiagramText.Draw(context, DiagramText.Get("Colour_Blend_Stripes"), new Point(stripes.Center.X, stripes.Bottom + 8), 11.5, DiagramText.Faint, 0.5);

        // The patch, and its level beside it.
        var patch = new Rect(stripes.Right + gap, y0, size, size);
        context.DrawRectangle(Brush(Rgb.Grey(grey / 255)), Frame, patch);
        Mark("patch", patch);
        DiagramText.Draw(context, DiagramText.Format("Colour_Blend_Patch", Number(grey)), new Point(patch.Center.X, patch.Bottom + 8), 11.5,
            DiagramText.Faint, 0.5);
        var levelX = patch.Right + 22;
        var top = DrawLevel(context, new Point(levelX, patch.Bottom), size, 10, grey / 255, Brush(Rgb.Grey(grey / 255)));
        Grip("grey", top, GripAxis.Vertical, DiagramText.Get("Colour_Grip_Grey"),
            at => Grey = Math.Round(LevelAt(at, patch.Bottom, size) * 255));
        DiagramText.Draw(context, DiagramText.Get("Colour_Blend_Squint"), new Point(bounds.Center.X, stripes.Bottom + 32), 12, DiagramText.Gold, 0.5);

        // The two blends from red to green.
        var left = bounds.X + 30;
        var width = w - 60;
        var tall = 40;
        var y1 = stripes.Bottom + 100;
        var y2 = y1 + tall + 46;
        DrawBlend(context, new Rect(left, y1, width, tall), false);
        DrawBlend(context, new Rect(left, y2, width, tall), true);
        Mark("numbers", new Rect(left, y1, width, tall));
        Mark("light", new Rect(left, y2, width, tall));
        DiagramText.Draw(context, DiagramText.Get("Colour_Blend_ByNumbers"), new Point(left, y1 - 6), 12, DiagramText.Ink, 0, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Colour_Blend_InLight"), new Point(left, y2 - 6), 12, DiagramText.Ink, 0, 1, bold: true);

        // The marker across both, and the colour each blend has there.
        var x = left + (width * position);
        var pen = new ImmutablePen(DiagramText.Gold, 2);
        context.DrawLine(pen, new Point(x, y1 - 2), new Point(x, y2 + tall + 2));
        var grip = new Point(x, y2 + tall + 34);
        Grip("position", grip, GripAxis.Horizontal, DiagramText.Get("Colour_Grip_Position"),
            at => Position = Math.Round(Math.Clamp((at.X - left) / width, 0, 1) * 100) / 100);
        var byNumbers = Blend.Mix(Blend.From, Blend.To, position, false);
        var inLight = Blend.Mix(Blend.From, Blend.To, position, true);
        var anchor = position > 0.6 ? 1 : 0;
        var dx = anchor == 1 ? -8 : 8;
        DiagramText.Draw(context, Triple(byNumbers), new Point(x + dx, y1 + tall + 4), 11, DiagramText.Ink, anchor, 0);
        DiagramText.Draw(context, Triple(inLight), new Point(x + dx, y2 + tall + 4), 11, DiagramText.Ink, anchor, 0);

        DiagramText.Draw(context, DiagramText.Get("Colour_Blend_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }

    private static void DrawBlend(DrawingContext context, Rect box, bool inLight)
    {
        var band = box.Width / Bands;
        for (var i = 0; i < Bands; i++)
        {
            var colour = Blend.Mix(Blend.From, Blend.To, (i + 0.5) / Bands, inLight);
            context.FillRectangle(Brush(colour), new Rect(box.X + (i * band), box.Y, band + 0.6, box.Height));
        }

        context.DrawRectangle(null, Frame, box);
    }

    /// <summary>A colour's three stored numbers.</summary>
    private static string Triple(Rgb c) => $"{Lamps.Number(c.R)} · {Lamps.Number(c.G)} · {Lamps.Number(c.B)}";
}
