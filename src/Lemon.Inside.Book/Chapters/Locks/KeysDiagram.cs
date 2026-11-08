using System.Globalization;
using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Locks;

/// <summary>
/// Spread 2: a key's blade drawn large, with a dashed line for every depth a cut may take. The steepest step between
/// neighbouring cuts is picked out; below, how many different keys the design allows. The reader drags the key's tip
/// out for more cuts, the deepest dashed line down for more depths, and the steep step's lower cut for a steeper or
/// gentler limit; the blade grows or shrinks and each cut is ground to its new depth rather than jumping there.
/// </summary>
public sealed class KeysDiagram : GearPicture
{
    public static readonly StyledProperty<int> CutsProperty =
        AvaloniaProperty.Register<KeysDiagram, int>(nameof(Cuts), 5, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> DepthsProperty =
        AvaloniaProperty.Register<KeysDiagram, int>(nameof(Depths), 10, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> SteepestProperty =
        AvaloniaProperty.Register<KeysDiagram, int>(nameof(Steepest), 9, defaultBindingMode: BindingMode.TwoWay);

    private static readonly IPen DepthPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x60, 0xE6, 0xEA, 0xF2)), 1, new ImmutableDashStyle([3, 4], 0));

    /// <summary>The sliders' ranges: cuts and depths.</summary>
    private const int FewestCuts = 3;
    private const int MostCuts = 7;
    private const int FewestDepths = 4;
    private const int MostDepths = 10;

    /// <summary>Each cut's depth as drawn (in steps), and the cuts as drawn, easing toward the sample key's.</summary>
    private readonly double[] _depth = new double[MostCuts];
    private double _cuts = double.NaN;

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

    private (int Cuts, int Depths, int Steepest, int[] Key) Design()
    {
        var cuts = Math.Clamp(Cuts, 1, MostCuts);
        var depths = Math.Clamp(Depths, 2, MostDepths);
        var steepest = Math.Clamp(Steepest, 1, depths - 1);
        return (cuts, depths, steepest, KeyCount.Sample(cuts, depths, steepest));
    }

    protected override void Advance(double seconds)
    {
        var (cuts, _, _, key) = Design();
        if (double.IsNaN(_cuts))
        {
            _cuts = cuts;
            for (var i = 0; i < cuts; i++) _depth[i] = key[i];
            return;
        }

        _cuts = Glide(_cuts, cuts, seconds, 0.08);
        for (var i = 0; i < MostCuts; i++)
        {
            // A new cut starts uncut (depth 0) and is ground down to its depth.
            _depth[i] = i < cuts ? Glide(_depth[i], key[i], seconds, 0.08) : 0;
        }
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var (cuts, depths, steepest, key) = Design();
        var settled = double.IsNaN(_cuts);
        var shownCuts = settled ? cuts : _cuts;
        double Depth(int i) => settled ? key[i] : _depth[i];

        // The blade: shoulder on the left, tip on the right, a cut for every pitch along it, so more cuts make a longer key.
        var area = new Rect(bounds.X + (bounds.Width * 0.22), bounds.Y + (bounds.Height * 0.2), bounds.Width * 0.72, bounds.Height * 0.34);
        var tip = area.Height * 0.4;
        var pitch = (area.Width - tip) / (MostCuts + 0.3);
        var blade = new Rect(area.X, area.Y, (pitch * (shownCuts + 0.3)) + tip, area.Height);
        var end = blade.Right - tip;
        var shallow = blade.Y + (blade.Height * 0.12);
        var step = blade.Height * 0.6 / (MostDepths - 1);
        var deep = shallow + (step * (depths - 1));
        double CutY(double d) => shallow + (step * d);
        double CutX(int i) => blade.X + (pitch * (i + 0.75));

        // The top edge: at every x, the deepest of the cuts' Vs, each with a small flat bottom and sides steep enough
        // that even the deepest cut is back at the top halfway to its neighbour (so every cut shows its depth).
        var flat = pitch * 0.12;
        var slope = (shallow + (step * (MostDepths - 1)) - blade.Y) / ((pitch * 0.5) - flat);
        var profile = new List<Point> { new(blade.X, blade.Bottom), new(blade.X, blade.Y) };
        for (var x = blade.X; x <= end; x += 1)
        {
            var y = blade.Y;
            for (var i = 0; i < cuts; i++)
            {
                y = Math.Max(y, CutY(Depth(i)) - (slope * Math.Max(0, Math.Abs(x - CutX(i)) - flat)));
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
            context.DrawEllipse(DiagramText.Gold, null, new Point(CutX(i), CutY(Depth(i))), 3.5, 3.5);
            DiagramText.Draw(context, key[i].ToString(CultureInfo.CurrentCulture), new Point(CutX(i), blade.Bottom + 6), 14, DiagramText.Gold, 0.5, 0, bold: true);
        }

        var worst = 0;
        if (cuts > 1)
        {
            worst = Enumerable.Range(1, cuts - 1).MaxBy(i => Math.Abs(key[i] - key[i - 1]));
            var a = new Point(CutX(worst - 1), CutY(Depth(worst - 1)));
            var b = new Point(CutX(worst), CutY(Depth(worst)));
            context.DrawLine(new ImmutablePen(DiagramText.Cyan, 3, lineCap: PenLineCap.Round), a, b);
            DiagramText.Draw(context, DiagramText.Format("Locks_Keys_SteepLabel", Math.Abs(key[worst] - key[worst - 1]), steepest),
                new Point(Math.Clamp(a.X - 10, area.X + 4, area.Right - 220), blade.Y - 10), 12, DiagramText.Cyan, 0, 1);
            Mark("steep", [a, b], 10);
        }

        // How many keys.
        var count = new Point(blade.X, blade.Bottom + 72);
        var big = DiagramText.Draw(context, DiagramText.Format("Locks_Keys_All", KeyCount.All(cuts, depths).ToString("N0", CultureInfo.CurrentCulture)), count, 26, DiagramText.Gold, bold: true);
        var formula = DiagramText.Draw(context, DiagramText.Format("Locks_Keys_Formula", depths, cuts), new Point(count.X, big.Bottom + 2), 12.5, DiagramText.Faint);
        var allowed = DiagramText.Draw(context, DiagramText.Format("Locks_Keys_Allowed", KeyCount.Allowed(cuts, depths, steepest).ToString("N0", CultureInfo.CurrentCulture), steepest),
            new Point(count.X, formula.Bottom + 8), 14, DiagramText.Cyan, bold: true);
        Mark("count", big.Union(allowed));

        // The reader's handles: the tip pulled out for a longer key with more cuts, the deepest line pulled down for
        // more depths, and the steep step's lower cut pulled away from its neighbour for a steeper limit.
        var bladeX = blade.X;
        var depthsAt = new Point(Math.Min(area.Right + 6, bounds.Right - 14), deep);
        context.DrawLine(DepthPen, new Point(end, deep), depthsAt);
        Grip("cuts", new Point(blade.Right, blade.Y + (blade.Height * 0.45)), GripAxis.Horizontal, DiagramText.Get("Locks_Grip_Cuts"),
            at => Cuts = Math.Clamp((int)Math.Round(((at.X - bladeX - tip) / pitch) - 0.3), FewestCuts, MostCuts));
        Grip("depths", depthsAt, GripAxis.Vertical,
            DiagramText.Get("Locks_Grip_Depths"),
            at => Depths = Math.Clamp((int)Math.Round((at.Y - shallow) / step) + 1, FewestDepths, MostDepths));
        if (cuts > 1)
        {
            var from = CutY(key[worst - 1]);
            var most = depths - 1;
            Grip("steepest", new Point(CutX(worst), CutY(Depth(worst))), GripAxis.Vertical, DiagramText.Get("Locks_Grip_Steepest"),
                at => Steepest = Math.Clamp((int)Math.Round(Math.Abs(at.Y - from) / step), 1, most));
        }
    }
}
