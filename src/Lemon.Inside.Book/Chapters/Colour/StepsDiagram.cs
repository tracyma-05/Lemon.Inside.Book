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
/// Spread 3: two strips of greys from black to white. The top one goes up in even steps of the stored number and looks
/// even; the bottom one goes up in even steps of light and looks lopsided, big jumps in the dark and steps too small to
/// see in the bright. A marker on the top strip picks a number; a line runs down to where its light falls on the bottom
/// strip, and two bars show the light the screen gives out and how bright that looks. The reader drags the marker.
/// </summary>
public sealed class StepsDiagram : GearPicture
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<StepsDiagram, double>(nameof(Value), 128, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>How many patches each strip has.</summary>
    private const int Patches = 16;

    static StepsDiagram()
    {
        AffectsRender<StepsDiagram>(ValueProperty);
    }

    /// <summary>The stored number, 0 to 255.</summary>
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var number = Math.Round(Math.Clamp(Value, 0, 255));
        var light = Numbers.Light(number);
        var left = bounds.X + 30;
        var right = bounds.Right - 30;
        var width = right - left;
        var cell = width / Patches;
        var tall = Math.Min(64, bounds.Height * 0.09);

        // Even steps of number.
        var y1 = bounds.Y + 74;
        var top = new Rect(left, y1, width, tall);
        for (var k = 0; k < Patches; k++)
        {
            var n = 255.0 * k / (Patches - 1);
            context.FillRectangle(Brush(Rgb.Grey(n / 255)), new Rect(left + (k * cell), y1, cell + 0.5, tall));
        }

        context.DrawRectangle(null, Frame, top);
        Mark("numbers", top);
        DiagramText.Draw(context, DiagramText.Get("Colour_Steps_ByNumber"), new Point(left, y1 - 8), 12, DiagramText.Ink, 0, 1, bold: true);

        // Even steps of light.
        var y2 = y1 + tall + 110;
        var bottom = new Rect(left, y2, width, tall);
        for (var k = 0; k < Patches; k++)
        {
            var n = Numbers.Number((double)k / (Patches - 1));
            context.FillRectangle(Brush(Rgb.Grey(n / 255)), new Rect(left + (k * cell), y2, cell + 0.5, tall));
        }

        context.DrawRectangle(null, Frame, bottom);
        Mark("light", bottom);
        DiagramText.Draw(context, DiagramText.Get("Colour_Steps_ByLight"), new Point(left, y2 - 8), 12, DiagramText.Ink, 0, 1, bold: true);

        // The chosen number on the top strip, and where its light falls on the bottom one.
        var xNumber = left + (width * number / 255);
        var xLight = left + (width * light);
        var gold = new ImmutablePen(DiagramText.Gold, 2);
        context.DrawLine(gold, new Point(xNumber, y1 - 2), new Point(xNumber, y1 + tall + 2));
        context.DrawLine(new ImmutablePen(DiagramText.Gold, 1.2, new ImmutableDashStyle([4, 4], 0)), new Point(xNumber, y1 + tall + 2), new Point(xLight, y2 - 2));
        DiagramText.DrawArrowAlong(context, new Point(xNumber, y1 + tall + 2), new Point(xLight, y2 - 2), DiagramText.Gold, 9);
        context.DrawLine(gold, new Point(xLight, y2 - 2), new Point(xLight, y2 + tall + 2));
        var grip = new Point(xNumber, y1 + tall + 14);
        Mark("marker", [new Point(xNumber, y1), grip], 8);
        DiagramText.Draw(context, Number(number), new Point(xNumber + 20, y1 + tall + 14), 13, DiagramText.Gold, 0, 0.5, bold: true);
        Grip("number", grip, GripAxis.Horizontal, DiagramText.Get("Colour_Grip_Number"),
            at => Value = Math.Round(Math.Clamp((at.X - left) / width, 0, 1) * 255));

        // Light given out, and how bright it looks.
        var meterY = y2 + tall + 58;
        var labelWidth = 150;
        var meterWidth = width - labelWidth - 56;
        var given = new Point(left + labelWidth, meterY);
        var looks = new Point(left + labelWidth, meterY + 30);
        DiagramText.Draw(context, DiagramText.Get("Colour_Steps_Light"), given + new Vector(-10, 0), 12, DiagramText.Faint, 1, 0.5);
        DrawMeter(context, given, meterWidth, 14, light, DiagramText.Gold);
        DiagramText.Draw(context, Percent(light), given + new Vector(meterWidth + 8, 0), 12, DiagramText.Ink, 0, 0.5);
        var seen = Numbers.Looks(light);
        DiagramText.Draw(context, DiagramText.Get("Colour_Steps_Looks"), looks + new Vector(-10, 0), 12, DiagramText.Faint, 1, 0.5);
        DrawMeter(context, looks, meterWidth, 14, seen, DiagramText.Cyan);
        DiagramText.Draw(context, Percent(seen), looks + new Vector(meterWidth + 8, 0), 12, DiagramText.Ink, 0, 0.5);
        Mark("meter", [given, looks + new Vector(meterWidth, 0)], 10);

        DiagramText.Draw(context, DiagramText.Format("Colour_Steps_Dark", Numbers.DarkNumbers(true), Numbers.DarkNumbers(false)),
            new Point(left, meterY + 64), 12, DiagramText.Faint);
        DiagramText.Draw(context, DiagramText.Get("Colour_Steps_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
