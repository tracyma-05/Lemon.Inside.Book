using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Colour.ColourParts;

namespace Lemon.Inside.Book.Chapters.Colour;

/// <summary>
/// Spread 6: a red sticker eight pixels across, its see-through pixels shown as a checkerboard, and a look inside one of
/// them: it still holds a colour. Beside it the sticker made five times bigger and laid on a background, every new
/// pixel mixed from the four stored ones nearest it. Kept as it is, the hidden colour leaks into the edge as a ring;
/// kept premultiplied, it cannot. The reader taps the switch and drags the hidden colour and the background.
/// </summary>
public sealed class HaloDiagram : GearPicture
{
    public static readonly StyledProperty<double> HiddenProperty =
        AvaloniaProperty.Register<HaloDiagram, double>(nameof(Hidden), 1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> BackdropProperty =
        AvaloniaProperty.Register<HaloDiagram, double>(nameof(Backdrop), 0.15, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> PremultipliedProperty =
        AvaloniaProperty.Register<HaloDiagram, bool>(nameof(Premultiplied), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>How many times bigger the sticker is made.</summary>
    public const int Scale = 5;

    static HaloDiagram()
    {
        AffectsRender<HaloDiagram>(HiddenProperty, BackdropProperty, PremultipliedProperty);
    }

    /// <summary>The grey stored in the see-through pixels, 0 black to 1 white.</summary>
    public double Hidden
    {
        get => GetValue(HiddenProperty);
        set => SetValue(HiddenProperty, value);
    }

    /// <summary>The background's grey, 0 to 1.</summary>
    public double Backdrop
    {
        get => GetValue(BackdropProperty);
        set => SetValue(BackdropProperty, value);
    }

    public bool Premultiplied
    {
        get => GetValue(PremultipliedProperty);
        set => SetValue(PremultipliedProperty, value);
    }

    /// <summary>Some 1600 little squares: drawn again only when a setting changes.</summary>
    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var hidden = Rgb.Grey(Math.Clamp(Hidden, 0, 1));
        var backdrop = Rgb.Grey(Math.Clamp(Backdrop, 0, 1));
        var w = bounds.Width;
        var n = Halo.Size;
        var big = n * Scale;

        // The enlarged sticker, top right, on its background.
        var cellBig = Math.Floor(Math.Clamp((w * 0.5) / big, 3, 8));
        var bigBox = new Rect(bounds.Right - 30 - (cellBig * big), bounds.Y + 70, cellBig * big, cellBig * big);
        for (var v = 0; v < big; v++)
        {
            for (var u = 0; u < big; u++)
            {
                var shown = Halo.Shown(Halo.Enlarged(u, v, Scale, hidden, Premultiplied), backdrop, Premultiplied);
                context.FillRectangle(Brush(shown), new Rect(bigBox.X + (u * cellBig), bigBox.Y + (v * cellBig), cellBig + 0.4, cellBig + 0.4));
            }
        }

        context.DrawRectangle(null, Frame, bigBox);
        Mark("edge", bigBox.Deflate(bigBox.Width * 0.08));
        DiagramText.Draw(context, DiagramText.Format("Colour_Halo_Big", Scale), new Point(bigBox.Center.X, bigBox.Y - 8), 11.5, DiagramText.Faint, 0.5, 1);

        // The sticker as stored, top left: red pixels and see-through ones.
        var cell = Math.Floor(Math.Min(20, (bigBox.X - bounds.X - 60) / n));
        var small = new Rect(bounds.X + 30, bigBox.Y, cell * n, cell * n);
        DrawCheckers(context, small, cell / 2);
        for (var row = 0; row < n; row++)
        {
            for (var column = 0; column < n; column++)
            {
                if (Halo.Inside(column, row))
                {
                    context.FillRectangle(Brush(Halo.Sticker), new Rect(small.X + (column * cell), small.Y + (row * cell), cell, cell));
                }
            }
        }

        context.DrawRectangle(null, Frame, small);
        Mark("small", small);
        DiagramText.Draw(context, DiagramText.Format("Colour_Halo_Small", n), new Point(small.Center.X, small.Y - 8), 11.5, DiagramText.Faint, 0.5, 1);

        // Inside one see-through pixel: the colour it keeps, and its opacity of nothing.
        var corner = new Point(small.X + (cell / 2), small.Y + (cell / 2));
        var look = new Rect(small.X, small.Bottom + 40, cell * n, 64);
        context.DrawLine(Frame, corner, new Point(look.X + 20, look.Y));
        context.DrawRectangle(Track, Frame, look, 6, 6);
        var stored = Premultiplied ? Rgb.Black : hidden;
        var swatch = new Rect(look.X + 10, look.Y + 12, 40, 40);
        context.DrawRectangle(Brush(stored), Frame, swatch, 4, 4);
        DiagramText.Draw(context, DiagramText.Get(Premultiplied ? "Colour_Halo_Nothing" : "Colour_Halo_Holds"), new Point(swatch.Right + 10, look.Y + 14),
            11.5, DiagramText.Ink);
        DiagramText.Draw(context, DiagramText.Get("Colour_Halo_Opacity"), new Point(swatch.Right + 10, look.Y + 34), 11.5, DiagramText.Faint);
        Mark("invisible", look);

        // How the colour is kept.
        var switchAt = new Point(look.Center.X, look.Bottom + 36);
        var box = DrawSwitch(context, switchAt, DiagramText.Get(Premultiplied ? "Colour_Halo_Multiplied" : "Colour_Halo_Straight"), Premultiplied);
        Mark("switch", box);
        Grip("switch", new Point(box.Left - 2, switchAt.Y), GripAxis.Tap, DiagramText.Get("Colour_Grip_Premultiplied"), _ => Premultiplied = !Premultiplied);

        // The hidden colour and the background, each a strip from black to white.
        var stripLeft = bigBox.X;
        var stripWidth = bigBox.Width;
        var y1 = bigBox.Bottom + 48;
        var y2 = y1 + 56;
        DrawGreyStrip(context, stripLeft, y1, stripWidth);
        DrawGreyStrip(context, stripLeft, y2, stripWidth);
        DiagramText.Draw(context, DiagramText.Get("Colour_Halo_Hidden"), new Point(stripLeft, y1 - 14), 11.5, DiagramText.Faint, 0, 1);
        DiagramText.Draw(context, DiagramText.Get("Colour_Halo_Backdrop"), new Point(stripLeft, y2 - 14), 11.5, DiagramText.Faint, 0, 1);
        Grip("hidden", new Point(stripLeft + (stripWidth * Math.Clamp(Hidden, 0, 1)), y1), GripAxis.Horizontal, DiagramText.Get("Colour_Grip_Hidden"),
            at => Hidden = Math.Round(Math.Clamp((at.X - stripLeft) / stripWidth, 0, 1) * 100) / 100);
        Grip("backdrop", new Point(stripLeft + (stripWidth * Math.Clamp(Backdrop, 0, 1)), y2), GripAxis.Horizontal, DiagramText.Get("Colour_Grip_Backdrop"),
            at => Backdrop = Math.Round(Math.Clamp((at.X - stripLeft) / stripWidth, 0, 1) * 100) / 100);

        var verdict = Halo.Verdict(hidden, backdrop, Premultiplied);
        DiagramText.Draw(context, DiagramText.Get(verdict switch { 1 => "Colour_Halo_ShowLight", -1 => "Colour_Halo_ShowDark", _ => "Colour_Halo_ShowClean" }),
            new Point(bigBox.Center.X, y2 + 34), 13, verdict == 0 ? DiagramText.Gold : DiagramText.Ink, 0.5, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Colour_Halo_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }

    private static void DrawGreyStrip(DrawingContext context, double left, double y, double width)
    {
        const int Steps = 24;
        for (var i = 0; i < Steps; i++)
        {
            context.FillRectangle(Brush(Rgb.Grey((i + 0.5) / Steps)), new Rect(left + (width * i / Steps), y - 6, (width / Steps) + 0.5, 12));
        }

        context.DrawRectangle(null, Frame, new Rect(left, y - 6, width, 12));
    }
}
