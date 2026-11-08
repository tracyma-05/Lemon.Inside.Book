using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Canals.CanalsParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Canals;

/// <summary>
/// Spread 6: a horse on the towpath pulling a loaded barge, seen from above. The barge's wake spreads wider and
/// brighter the faster it goes; below, the pull the barge needs against the steady pull of one horse. The bank and
/// the water stream past at the barge's speed and the horse plods. The reader drags the crates' grip up to load more,
/// and the horse along the towpath to walk faster or slower.
/// </summary>
public sealed class TowingDiagram : GearPicture
{
    public static readonly StyledProperty<double> LoadProperty =
        AvaloniaProperty.Register<TowingDiagram, double>(nameof(Load), 30, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> SpeedProperty =
        AvaloniaProperty.Register<TowingDiagram, double>(nameof(Speed), 4, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The pull bar's widest value, kg-force.</summary>
    private const double MostPull = 300;

    /// <summary>How far the bank has streamed past, pixels: on by the speed every frame.</summary>
    private double _passed;

    static TowingDiagram()
    {
        AffectsRender<TowingDiagram>(LoadProperty, SpeedProperty);
    }

    /// <summary>The barge's load, tonnes.</summary>
    public double Load
    {
        get => GetValue(LoadProperty);
        set => SetValue(LoadProperty, value);
    }

    /// <summary>Its speed, km/h.</summary>
    public double Speed
    {
        get => GetValue(SpeedProperty);
        set => SetValue(SpeedProperty, value);
    }

    protected override void Advance(double seconds) => _passed += seconds * Math.Clamp(Speed, 1, 8) * 14;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var load = Math.Clamp(Load, 10, 60);
        var speed = Math.Clamp(Speed, 1, 8);
        var drag = Towing.Drag(load, speed);
        var manages = Towing.Manages(load, speed);

        // The towpath above, the canal below it.
        var path = new Rect(bounds.X, bounds.Y + 60, bounds.Width, 70);
        var canal = new Rect(bounds.X, path.Bottom, bounds.Width, bounds.Height * 0.32);
        context.FillRectangle(Grass, new Rect(bounds.X, bounds.Y + 40, bounds.Width, 20));
        context.FillRectangle(new ImmutableSolidColorBrush(Color.FromRgb(0x8A, 0x76, 0x58)), path);
        context.FillRectangle(Water, canal);
        context.FillRectangle(Masonry, new Rect(canal.X, canal.Y - 4, canal.Width, 6));

        // The bank and the water streaming past: tufts on the towpath, streaks on the water.
        var tuft = new ImmutableSolidColorBrush(Color.FromRgb(0x5A, 0x7A, 0x46));
        var streak = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x70, 0xD8, 0xEE, 0xFF)), 1.5, lineCap: PenLineCap.Round);
        const double gap = 90;
        var shift = _passed % gap;
        for (var x = bounds.X - shift; x < bounds.Right + gap; x += gap)
        {
            context.DrawEllipse(tuft, null, new Point(x, path.Y + 8), 6, 3);
            context.DrawEllipse(tuft, null, new Point(x + 47, path.Bottom - 7), 5, 2.5);
            context.DrawLine(streak, new Point(x + 20, canal.Y + (canal.Height * 0.15)), new Point(x + 44, canal.Y + (canal.Height * 0.15)));
            context.DrawLine(streak, new Point(x + 63, canal.Y + (canal.Height * 0.85)), new Point(x + 83, canal.Y + (canal.Height * 0.85)));
        }

        // The barge, longer than wide, sitting deeper the more it carries; its wake behind it.
        var barge = new Rect(bounds.X + (bounds.Width * 0.12), canal.Y + (canal.Height * 0.3), bounds.Width * 0.45, canal.Height * 0.32);
        var bow = new Point(barge.Right + (barge.Height * 0.6), barge.Center.Y);
        var spread = 6 + (speed * 5);
        var wakePen = new ImmutablePen(Flow, 1 + (speed * 0.35));
        var wake = new List<Point>();
        foreach (var side in new[] { -1, 1 })
        {
            var wobble = 1 + (0.06 * Math.Sin((Seconds * speed * 1.5) + side));
            var tail = new Point(barge.X - (speed * 18 * wobble), barge.Center.Y + (side * ((barge.Height / 2) + (spread * wobble))));
            context.DrawLine(wakePen, new Point(bow.X - 6, bow.Y + (side * barge.Height * 0.3)), tail);
            wake.AddRange([bow, tail]);
        }

        Mark("wake", wake, 6);
        DrawPolygon(context, Timber, Edge, [barge.TopLeft, barge.TopRight, bow, barge.BottomRight, barge.BottomLeft]);
        var cargo = barge.Deflate(new Thickness(10, barge.Height * 0.2));
        context.DrawRectangle(new ImmutableSolidColorBrush(Color.FromRgb(0x3A, 0x34, 0x2C)), null, cargo, 3, 3);

        // The load as crates, one for every ten tonnes or so.
        var crates = Math.Clamp((int)Math.Round(load / 10), 1, 6);
        var crate = cargo.Width / 6;
        for (var k = 0; k < crates; k++)
        {
            var box = new Rect(cargo.X + (k * crate) + 2, cargo.Y + 3, crate - 4, cargo.Height - 6);
            context.DrawRectangle(new ImmutableSolidColorBrush(Color.FromRgb(0x9A, 0x74, 0x48)), Edge, box, 2, 2);
        }

        DiagramText.Draw(context, DiagramText.Format("Canals_Tonnes", Number(load, "0")), cargo.Center, 13, DiagramText.Ink, 0.5, 0.5, bold: true);
        Mark("barge", barge);

        // The horse ahead on the towpath, the rope back to the barge's bow.
        var horseFrom = bow.X + 40;
        var horseTo = bounds.Right - 100;
        var plod = Math.Abs(Math.Sin(Seconds * (1.5 + (speed * 0.6)))) * 2.5;
        var horse = new Rect(horseFrom + ((horseTo - horseFrom) * (speed - 1) / 7), path.Y + 18 - plod, 70, 30);
        context.DrawRectangle(new ImmutableSolidColorBrush(Color.FromRgb(0x7A, 0x52, 0x38)), Edge, horse, 12, 12);
        context.DrawEllipse(new ImmutableSolidColorBrush(Color.FromRgb(0x7A, 0x52, 0x38)), Edge, new Point(horse.Right + 6, horse.Center.Y), 11, 9);
        Mark("horse", horse);
        var ropeFrom = new Point(barge.X + (barge.Width * 0.2), barge.Top + 4);
        context.DrawLine(new ImmutablePen(manages ? Flow : Warn, 2.2), ropeFrom, new Point(horse.Left, horse.Center.Y));
        Mark("rope", [ropeFrom, new Point(horse.Left, horse.Center.Y)], 6);

        // The bar: what the barge needs against one horse.
        var bar = new Rect(bounds.X + 30, canal.Bottom + 50, bounds.Width - 60, 24);
        context.DrawRectangle(null, new ImmutablePen(DiagramText.Faint, 1), bar, 3, 3);
        context.FillRectangle(manages ? Flow : Warn, new Rect(bar.X, bar.Y, bar.Width * Math.Min(1, drag / MostPull), bar.Height), 3);
        var horseX = bar.X + (bar.Width * Towing.Horse / MostPull);
        context.DrawLine(new ImmutablePen(DiagramText.Gold, 2.5), new Point(horseX, bar.Y - 8), new Point(horseX, bar.Bottom + 8));
        DiagramText.Draw(context, DiagramText.Format("Canals_Towing_NeedsLabel", Text(drag)), new Point(bar.X, bar.Y - 10), 12.5, manages ? DiagramText.Ink : Warn, 0, 1, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Canals_Towing_HorseLabel", Text(Towing.Horse)), new Point(horseX, bar.Bottom + 10), 12, DiagramText.Gold, 0.5, 0, bold: true);
        DiagramText.Draw(context, DiagramText.Format("Canals_Speed", Number(speed)), new Point(bow.X + 12, bow.Y), 12.5, DiagramText.Ink, 0, 0.5, bold: true);
        DiagramText.Draw(context, DiagramText.Get("Canals_Towing_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);

        // The reader's handles: the horse dragged along the towpath for the pace, the crates' grip up for a heavier load.
        Grip("speed", horse.Center, GripAxis.Horizontal, DiagramText.Get("Canals_Grip_Speed"),
            at => Speed = Math.Round(Math.Clamp(1 + (7 * (at.X - (horse.Width / 2) - horseFrom) / (horseTo - horseFrom)), 1, 8), 1));
        Grip("load", new Point(cargo.Right + 2, barge.Bottom - (barge.Height * (load - 10) / 50)), GripAxis.Vertical, DiagramText.Get("Canals_Grip_Load"),
            at => Load = Math.Round(Math.Clamp(10 + ((barge.Bottom - at.Y) / barge.Height * 50), 10, 60)));
    }

    private static string Text(double kgf) => DiagramText.Format("Canals_Kgf", Number(kgf, "0"));

    private static readonly IImmutableBrush Warn = new ImmutableSolidColorBrush(Color.FromRgb(0xF0, 0x6A, 0x5A));
}
