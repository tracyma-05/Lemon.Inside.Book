using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Colour.ColourParts;

namespace Lemon.Inside.Book.Chapters.Colour;

/// <summary>
/// Spread 5: a background of black and white bands with a yellow sun, and in front of it a red and a blue pane of
/// tinted glass, overlapping in the middle. Each pane lets through the share of what is behind it that its opacity does
/// not cover. The reader drags each pane's level up or down and taps the overlap to swap which pane is in front.
/// </summary>
public sealed class PanesDiagram : GearPicture
{
    public static readonly StyledProperty<double> RedProperty =
        AvaloniaProperty.Register<PanesDiagram, double>(nameof(Red), 0.5, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> BlueProperty =
        AvaloniaProperty.Register<PanesDiagram, double>(nameof(Blue), 0.5, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> BlueOnTopProperty =
        AvaloniaProperty.Register<PanesDiagram, bool>(nameof(BlueOnTop), true, defaultBindingMode: BindingMode.TwoWay);

    static PanesDiagram()
    {
        AffectsRender<PanesDiagram>(RedProperty, BlueProperty, BlueOnTopProperty);
    }

    /// <summary>The red pane's opacity, 0 to 1.</summary>
    public double Red
    {
        get => GetValue(RedProperty);
        set => SetValue(RedProperty, value);
    }

    /// <summary>The blue pane's opacity, 0 to 1.</summary>
    public double Blue
    {
        get => GetValue(BlueProperty);
        set => SetValue(BlueProperty, value);
    }

    public bool BlueOnTop
    {
        get => GetValue(BlueOnTopProperty);
        set => SetValue(BlueOnTopProperty, value);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var red = Math.Clamp(Red, 0, 1);
        var blue = Math.Clamp(Blue, 0, 1);
        var w = bounds.Width;
        var h = bounds.Height;

        // The background: bands of white and near-black, and a sun.
        var scene = new Rect(bounds.X + 50, bounds.Y + 60, w - 100, Math.Min(h * 0.5, w * 0.62));
        var band = scene.Width / 10;
        for (var i = 0; i < 10; i++)
        {
            var shade = i % 2 == 0 ? Rgb.Grey(0.95) : Rgb.Grey(0.12);
            context.FillRectangle(Brush(shade), new Rect(scene.X + (i * band), scene.Y, band + 0.5, scene.Height));
        }

        var sun = new Point(scene.Center.X, scene.Y + (scene.Height * 0.3));
        context.DrawEllipse(Brush(new Rgb(1, 0.82, 0.15)), null, sun, scene.Height * 0.16, scene.Height * 0.16);
        context.DrawRectangle(null, Frame, scene);
        Mark("behind", new Rect(scene.X + 4, scene.Y + 4, band * 1.6, scene.Height * 0.16));

        // The two panes, the one behind drawn first; each covers its own share and lets the rest through.
        var paneW = scene.Width * 0.46;
        var paneH = scene.Height * 0.62;
        var paneY = scene.Y + (scene.Height * 0.3);
        var redPane = new Rect(scene.X + (scene.Width * 0.12), paneY, paneW, paneH);
        var bluePane = new Rect(scene.Right - (scene.Width * 0.12) - paneW, paneY - (scene.Height * 0.1), paneW, paneH);
        void DrawPane(Rect pane, Rgb colour, double opacity) => context.DrawRectangle(Brush(colour, opacity), Edge, pane);
        if (BlueOnTop)
        {
            DrawPane(redPane, Panes.Red, red);
            DrawPane(bluePane, Panes.Blue, blue);
        }
        else
        {
            DrawPane(bluePane, Panes.Blue, blue);
            DrawPane(redPane, Panes.Red, red);
        }

        var overlap = redPane.Intersect(bluePane);
        Mark("red", new Rect(redPane.X, redPane.Y, overlap.X - redPane.X, redPane.Height));
        Mark("blue", new Rect(overlap.Right, bluePane.Y, bluePane.Right - overlap.Right, bluePane.Height));
        Mark("overlap", overlap.Deflate(6));
        DiagramText.Draw(context, DiagramText.Format("Colour_Panes_Hidden", Percent(Panes.Hidden(red, blue))),
            new Point(overlap.Center.X, overlap.Bottom + 10), 13, DiagramText.Gold, 0.5, 0, bold: true);
        var front = BlueOnTop ? bluePane : redPane;
        // "In front", on a dark tab so it reads over the white bands too.
        var tab = DiagramText.Draw(context, DiagramText.Get("Colour_Panes_Front"), new Point(-1000, -1000), 11.5, DiagramText.Ink);
        var tabBox = new Rect(front.Center.X - (tab.Width / 2) - 8, front.Y - tab.Height - 10, tab.Width + 16, tab.Height + 6);
        context.DrawRectangle(Brush(new Rgb(0.08, 0.1, 0.16), 0.85), null, tabBox, 4, 4);
        DiagramText.Draw(context, DiagramText.Get("Colour_Panes_Front"), tabBox.Center, 11.5, DiagramText.Ink, 0.5, 0.5);

        // Tap the overlap to swap the panes.
        Grip("swap", new Point(overlap.Center.X, overlap.Center.Y), GripAxis.Tap, DiagramText.Get("Colour_Grip_Swap"), _ => BlueOnTop = !BlueOnTop);

        // Each pane's opacity: a level beside it.
        var levelH = Math.Min(160, h * 0.24);
        var levelY = scene.Bottom + 40 + levelH;
        var redX = scene.X + 20;
        var blueX = scene.Right - 20;
        var redTop = DrawLevel(context, new Point(redX, levelY), levelH, 14, red, Brush(Panes.Red));
        var blueTop = DrawLevel(context, new Point(blueX, levelY), levelH, 14, blue, Brush(Panes.Blue));
        Grip("red", redTop, GripAxis.Vertical, DiagramText.Get("Colour_Grip_Opacity"),
            at => Red = Math.Round(LevelAt(at, levelY, levelH) * 100) / 100);
        Grip("blue", blueTop, GripAxis.Vertical, DiagramText.Get("Colour_Grip_Opacity"),
            at => Blue = Math.Round(LevelAt(at, levelY, levelH) * 100) / 100);
        DiagramText.Draw(context, Percent(red), new Point(redX + 16, levelY - (levelH / 2)), 12, DiagramText.Ink, 0, 0.5);
        DiagramText.Draw(context, Percent(blue), new Point(blueX - 16, levelY - (levelH / 2)), 12, DiagramText.Ink, 1, 0.5);
        DiagramText.Draw(context, DiagramText.Get("Colour_Panes_Opacity"), new Point(scene.Center.X, levelY - (levelH / 2)), 12,
            DiagramText.Faint, 0.5, 0.5);

        DiagramText.Draw(context, DiagramText.Get("Colour_Panes_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }
}
