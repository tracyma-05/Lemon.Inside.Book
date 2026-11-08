using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Colour.ColourParts;

namespace Lemon.Inside.Book.Chapters.Colour;

/// <summary>
/// Spread 2: three blots of ink, cyan, magenta and yellow, overlapping on white paper. Each region shows the light the
/// paper sends back once the inks over it have taken theirs: green where cyan and yellow overlap, and black (or, with
/// real inks, a muddy brown) where all three do. Three bars show the red, green and blue light the middle still sends
/// back. The reader drags each ink's level and taps the switch between ideal and real inks.
/// </summary>
public sealed class InksDiagram : GearPicture
{
    public static readonly StyledProperty<double> CyanProperty =
        AvaloniaProperty.Register<InksDiagram, double>(nameof(Cyan), 1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MagentaProperty =
        AvaloniaProperty.Register<InksDiagram, double>(nameof(Magenta), 1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> YellowProperty =
        AvaloniaProperty.Register<InksDiagram, double>(nameof(Yellow), 1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> RealProperty =
        AvaloniaProperty.Register<InksDiagram, bool>(nameof(Real), defaultBindingMode: BindingMode.TwoWay);

    static InksDiagram()
    {
        AffectsRender<InksDiagram>(CyanProperty, MagentaProperty, YellowProperty, RealProperty);
    }

    public double Cyan
    {
        get => GetValue(CyanProperty);
        set => SetValue(CyanProperty, value);
    }

    public double Magenta
    {
        get => GetValue(MagentaProperty);
        set => SetValue(MagentaProperty, value);
    }

    public double Yellow
    {
        get => GetValue(YellowProperty);
        set => SetValue(YellowProperty, value);
    }

    public bool Real
    {
        get => GetValue(RealProperty);
        set => SetValue(RealProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var inks = new[] { Math.Clamp(Cyan, 0, 1), Math.Clamp(Magenta, 0, 1), Math.Clamp(Yellow, 0, 1) };
        var w = bounds.Width;
        var h = bounds.Height;

        // The paper, and the three blots on it: cyan top left, magenta top right, yellow below.
        var r = Math.Min(w * 0.2, h * 0.16);
        var centre = new Point(bounds.X + ((w - 130) * 0.5), bounds.Y + (h * 0.36));
        var paper = new Rect(centre.X - (r * 1.9), centre.Y - (r * 1.75), r * 3.8, r * 3.6);
        context.DrawRectangle(Brush(Inks.Back(0, 0, 0, Real)), Edge, paper, 4, 4);
        Mark("paper", new Rect(paper.X + 4, paper.Y + 4, r * 0.55, r * 0.55));
        Point[] centres =
        [
            centre + new Vector(-r * 0.55, -r * 0.32),
            centre + new Vector(r * 0.55, -r * 0.32),
            centre + new Vector(0, r * 0.62),
        ];
        DrawThreeDiscs(context, centres, r, mask => Inks.Back(
            (mask & 1) != 0 ? inks[0] : 0,
            (mask & 2) != 0 ? inks[1] : 0,
            (mask & 4) != 0 ? inks[2] : 0,
            Real));
        Mark("ink", OnlyIn(centres, 0, r), r * 0.3);
        var middle = Middle(centres);
        Mark("middle", middle, r * 0.22);

        // The inks' levels, at the right.
        var height = Math.Min(h * 0.32, 2.2 * r);
        var baseY = centre.Y + (height / 2);
        Rgb[] hues = [new(0, 0.75, 0.9), new(0.9, 0, 0.6), new(1, 0.9, 0)];
        string[] grips = ["cyan", "magenta", "yellow"];
        for (var k = 0; k < 3; k++)
        {
            var x = bounds.Right - 104 + (k * 36);
            var top = DrawLevel(context, new Point(x, baseY), height, 16, inks[k], Brush(hues[k]));
            var index = k;
            Grip(grips[k], top, GripAxis.Vertical, DiagramText.Get("Colour_Grip_Ink"), at =>
            {
                var share = Math.Round(LevelAt(at, baseY, height) * 100) / 100;
                if (index == 0) Cyan = share;
                else if (index == 1) Magenta = share;
                else Yellow = share;
            });
        }

        DiagramText.Draw(context, DiagramText.Get("Colour_Inks_Levels"), new Point(bounds.Right - 68, baseY + 10), 11, DiagramText.Faint, 0.5);

        // Ideal or real inks.
        var switchAt = new Point(bounds.Right - 68, baseY - height - 44);
        var box = DrawSwitch(context, switchAt, DiagramText.Get(Real ? "Colour_Inks_RealLabel" : "Colour_Inks_Ideal"), Real);
        Grip("real", new Point(box.Left - 2, switchAt.Y), GripAxis.Tap, DiagramText.Get("Colour_Grip_Real"), _ => Real = !Real);

        // What the middle still sends back, as three bars.
        var back = Inks.Back(inks[0], inks[1], inks[2], Real);
        var meterLeft = new Point(paper.X, Math.Min(bounds.Bottom - 150, paper.Bottom + 44));
        DiagramText.Draw(context, DiagramText.Get("Colour_Inks_Back"), meterLeft + new Vector(0, -24), 12, DiagramText.Faint);
        var meterWidth = Math.Min(220, w * 0.4);
        double[] lights = [back.R, back.G, back.B];
        string[] names = ["Colour_Name_Red", "Colour_Name_Green", "Colour_Name_Blue"];
        Rgb[] beams = [new(1, 0.25, 0.25), new(0.25, 1, 0.35), new(0.3, 0.55, 1)];
        var meterPoints = new List<Point>();
        for (var k = 0; k < 3; k++)
        {
            var at = meterLeft + new Vector(64, k * 20);
            DiagramText.Draw(context, DiagramText.Get(names[k]), at + new Vector(-8, 0), 11, DiagramText.Faint, 1, 0.5);
            DrawMeter(context, at, meterWidth, 12, lights[k], Brush(beams[k]));
            DiagramText.Draw(context, Percent(lights[k]), at + new Vector(meterWidth + 8, 0), 11, DiagramText.Ink, 0, 0.5);
            meterPoints.AddRange([at, at + new Vector(meterWidth, 0)]);
        }

        Mark("light", meterPoints, 8);
        DiagramText.Draw(context, DiagramText.Format("Colour_Inks_Sum", DiagramText.Get(ColourNames.Key(back))),
            meterLeft + new Vector(0, 66), 13, DiagramText.Gold, 0, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Colour_Inks_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
