using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Locks;

/// <summary>
/// Spread 2: a key's blade drawn large, with a dashed line for every depth a cut may take. The steepest step between
/// neighbouring cuts is picked out; below, how many different keys the design allows.
/// </summary>
public sealed class KeysDiagram : GearPicture
{
    public static readonly StyledProperty<int> CutsProperty =
        AvaloniaProperty.Register<KeysDiagram, int>(nameof(Cuts), 5);

    public static readonly StyledProperty<int> DepthsProperty =
        AvaloniaProperty.Register<KeysDiagram, int>(nameof(Depths), 10);

    public static readonly StyledProperty<int> SteepestProperty =
        AvaloniaProperty.Register<KeysDiagram, int>(nameof(Steepest), 9);

    private static readonly IPen DepthPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x60, 0xE6, 0xEA, 0xF2)), 1, new ImmutableDashStyle([3, 4], 0));

    static KeysDiagram()
    {
        AffectsRender<KeysDiagram>(CutsProperty, DepthsProperty, SteepestProperty);
    }

    public int Cuts
    {
        get => GetValue(CutsProperty);
        set => SetValue(CutsProperty, value);
    }

    public int Depths
    {
        get => GetValue(DepthsProperty);
        set => SetValue(DepthsProperty, value);
    }

    public int Steepest
    {
        get => GetValue(SteepestProperty);
        set => SetValue(SteepestProperty, value);
    }

    protected override bool Animates => false;

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var cuts = Math.Clamp(Cuts, 1, 9);
        var depths = Math.Clamp(Depths, 2, 12);
        var steepest = Math.Clamp(Steepest, 1, depths - 1);
        var key = KeyCount.Sample(cuts, depths, steepest);

        // The blade: shoulder on the left, tip on the right, cuts evenly along it.
        var blade = new Rect(bounds.X + (bounds.Width * 0.22), bounds.Y + (bounds.Height * 0.2), bounds.Width * 0.72, bounds.Height * 0.34);
        var end = blade.Right - (blade.Height * 0.4);
        var pitch = (end - blade.X) / (cuts + 0.3);
        var shallow = blade.Y + (blade.Height * 0.12);
        var deep = blade.Y + (blade.Height * 0.72);
        double CutY(int d) => shallow + ((deep - shallow) * d / (depths - 1));
        double CutX(int i) => blade.X + (pitch * (i + 0.75));

        // The top edge: at every x, the deepest of the cuts' Vs, each with a small flat bottom and sides steep enough
        // that even the deepest cut is back at the top halfway to its neighbour (so every cut shows its depth).
        var flat = pitch * 0.12;
        var slope = (deep - blade.Y) / ((pitch * 0.5) - flat);
        var profile = new List<Point> { new(blade.X, blade.Bottom), new(blade.X, blade.Y) };
        for (var x = blade.X; x <= end; x += 1)
        {
            var y = blade.Y;
            for (var i = 0; i < cuts; i++)
            {
                y = Math.Max(y, CutY(key[i]) - (slope * Math.Max(0, Math.Abs(x - CutX(i)) - flat)));
            }

            profile.Add(new Point(x, y));
        }

        profile.Add(new Point(blade.Right, blade.Y + (blade.Height * 0.45)));
        profile.Add(new Point(blade.Right - (blade.Height * 0.25), blade.Bottom));
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(profile[0], true);
            foreach (var p in profile.Skip(1)) g.LineTo(p);
            g.EndFigure(true);
        }

        context.DrawGeometry(LocksParts.KeyBrush, LocksParts.Edge, geometry);
        var bow = new Rect(bounds.X + 16, blade.Y - (blade.Height * 0.2), blade.X - bounds.X - 22, blade.Height * 1.4);
        context.DrawRectangle(LocksParts.KeyBrush, LocksParts.Edge, bow, bow.Width * 0.3, bow.Width * 0.3);
        context.DrawRectangle(LocksParts.KeyBrush, LocksParts.Edge, new Rect(bow.Right - 1, blade.Y - 6, 7, blade.Height + 12));
        context.DrawEllipse(LocksParts.Dark, null, new Point(bow.X + (bow.Width * 0.4), bow.Center.Y), bow.Width * 0.16, bow.Width * 0.16);
        Mark("key", blade);

        // Every depth a cut may take, numbered at the left.
        for (var d = 0; d < depths; d++)
        {
            var y = CutY(d);
            context.DrawLine(DepthPen, new Point(blade.X + 4, y), new Point(end, y));
            DiagramText.Draw(context, d.ToString(CultureInfo.CurrentCulture), new Point(blade.X + 6, y), 9.5, DiagramText.Faint, 0, 0.5);
        }

        Mark("depths", new Rect(blade.X, shallow - 4, blade.Width * 0.9, deep - shallow + 8));

        // Each cut's depth, under the blade, and the steepest step between neighbours.
        for (var i = 0; i < cuts; i++)
        {
            context.DrawEllipse(DiagramText.Gold, null, new Point(CutX(i), CutY(key[i])), 3.5, 3.5);
            DiagramText.Draw(context, key[i].ToString(CultureInfo.CurrentCulture), new Point(CutX(i), blade.Bottom + 6), 14, DiagramText.Gold, 0.5, 0, bold: true);
        }

        if (cuts > 1)
        {
            var worst = Enumerable.Range(1, cuts - 1).MaxBy(i => Math.Abs(key[i] - key[i - 1]));
            var a = new Point(CutX(worst - 1), CutY(key[worst - 1]));
            var b = new Point(CutX(worst), CutY(key[worst]));
            context.DrawLine(new ImmutablePen(DiagramText.Cyan, 3, lineCap: PenLineCap.Round), a, b);
            DiagramText.Draw(context, DiagramText.Format("Locks_Keys_SteepLabel", Math.Abs(key[worst] - key[worst - 1]), steepest),
                new Point(Math.Clamp(a.X - 10, blade.X + 4, blade.Right - 220), blade.Y - 10), 12, DiagramText.Cyan, 0, 1);
            Mark("steep", [a, b], 10);
        }

        // How many keys.
        var count = new Point(blade.X, blade.Bottom + 72);
        var big = DiagramText.Draw(context, DiagramText.Format("Locks_Keys_All", KeyCount.All(cuts, depths).ToString("N0", CultureInfo.CurrentCulture)), count, 26, DiagramText.Gold, bold: true);
        var formula = DiagramText.Draw(context, DiagramText.Format("Locks_Keys_Formula", depths, cuts), new Point(count.X, big.Bottom + 2), 12.5, DiagramText.Faint);
        var allowed = DiagramText.Draw(context, DiagramText.Format("Locks_Keys_Allowed", KeyCount.Allowed(cuts, depths, steepest).ToString("N0", CultureInfo.CurrentCulture), steepest),
            new Point(count.X, formula.Bottom + 8), 14, DiagramText.Cyan, bold: true);
        Mark("count", big.Union(allowed));
    }
}
