using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Boats;

/// <summary>
/// Spread 3: a small steel boat from the side, loaded with crates, its draft marks on the bow; on the seabed, the same
/// steel made into a lump. Overloaded, the sea comes over the deck and the boat goes down. The reader drags the grip
/// over the crates along the deck to load more (they stack two high, filling from the stern); the boat settles slowly
/// down to its new draft instead of jumping there.
/// </summary>
public sealed class HullDiagram : GearPicture
{
    public static readonly StyledProperty<double> CargoProperty =
        AvaloniaProperty.Register<HullDiagram, double>(nameof(Cargo), 20, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Tonnes in each crate.</summary>
    private const double Crate = 5;

    /// <summary>The slider's most cargo, tonnes: sixteen crates, eight stacks of two along the deck.</summary>
    private const double MostCargo = 80;

    /// <summary>Metres of deck a tonne of cargo takes (a stack of two crates, 10 t, every 1.25 m), and where the first stands.</summary>
    private const double DeckPerTonne = 1.25 / (2 * Crate);
    private const double FirstStack = 1.0;

    private static readonly IImmutableBrush CrateFill = new ImmutableSolidColorBrush(Color.FromRgb(0xD9, 0xB3, 0x5B));
    private static readonly IImmutableBrush SteelFill = new ImmutableSolidColorBrush(Color.FromRgb(0x7A, 0x84, 0x96));
    private static readonly IImmutableBrush Seabed = new ImmutableSolidColorBrush(Color.FromRgb(0x3A, 0x34, 0x2A));
    private static readonly IPen MarkPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0xF2, 0xF2, 0xF2)), 1.2);

    private double _seconds;
    private double _sunk;

    /// <summary>The draft as drawn, metres, settling toward the cargo's.</summary>
    private double _draft = Hull.Draft(20);

    static HullDiagram()
    {
        AffectsRender<HullDiagram>(CargoProperty);
    }

    /// <summary>Tonnes of cargo aboard.</summary>
    public double Cargo
    {
        get => GetValue(CargoProperty);
        set => SetValue(CargoProperty, value);
    }

    protected override void Advance(double seconds)
    {
        _seconds += seconds;
        _sunk = Hull.Sinks(Cargo) ? _sunk + (seconds * 1.2) : 0;
        _draft = Glide(_draft, Math.Min(Hull.Draft(Math.Clamp(Cargo, 0, MostCargo)), Hull.Depth), seconds, 0.25);
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var surface = bounds.Height * 0.4;
        var seabed = bounds.Height * 0.8;
        var scale = Math.Min(bounds.Width * 0.62 / Hull.Length, (seabed - surface) / (Hull.Depth * 2.6));
        var left = bounds.Width * 0.08;

        // The boat, its keel Draft metres down, bobbing a little; sinking, it goes on down to the seabed.
        var draft = _draft + _sunk;
        var keel = Math.Min(seabed, surface + (draft * scale) + (Math.Sin(_seconds * 1.6) * 1.5));
        Point P(double x, double y) => new(left + (x * scale), keel - (y * scale));
        var hull = new[] { P(0, Hull.Depth), P(0, 0.35), P(0.5, 0), P(Hull.Length - 1.6, 0), P(Hull.Length, Hull.Depth) };
        var deckY = keel - (Hull.Depth * scale);

        // The crates on deck, in stacks of two from the stern forwards.
        var crates = (int)Math.Ceiling((Cargo / Crate) - 1e-9);
        var box = 1.2 * scale;
        var crateBoxes = new List<Point>();
        for (var i = 0; i < crates; i++)
        {
            var row = i % 2;
            var column = i / 2;
            var r = new Rect(left + ((FirstStack + (column * 1.25)) * scale), deckY - ((row + 1) * box) - (row * 2), box, box);
            context.DrawRectangle(CrateFill, new ImmutablePen(BoatsParts.Dark, 1), r, 2, 2);
            context.DrawLine(new ImmutablePen(BoatsParts.Dark, 1), r.TopLeft, r.BottomRight);
            crateBoxes.Add(r.TopLeft);
            crateBoxes.Add(r.BottomRight);
        }

        context.DrawGeometry(BoatsParts.HullFill, new ImmutablePen(BoatsParts.Dark, 1.5), BoatsParts.Outline(hull));
        context.FillRectangle(BoatsParts.Deck, new Rect(P(0, Hull.Depth).X, deckY - 3, Hull.Length * scale, 4));

        // Draft marks on the bow: every half metre up from the keel.
        var marks = new List<Point>();
        for (var m = 0.5; m <= Hull.Depth + 1e-9; m += 0.5)
        {
            var bow = Hull.Length - 1.6 + (1.6 * m / Hull.Depth);
            var a = P(bow - 0.5, m);
            context.DrawLine(MarkPen, a, P(bow - 0.15, m));
            DiagramText.Draw(context, m.ToString("0.0"), a - new Point(3, 0), 10, DiagramText.Ink, 1, 0.5);
            marks.Add(a - new Point(24, 0));
            marks.Add(P(bow, m));
        }

        // The same steel, as a lump on the seabed.
        var lump = Hull.LumpSide * scale;
        var lumpBox = new Rect(bounds.Width * 0.84 - (lump / 2), seabed - lump, lump, lump);
        context.DrawRectangle(SteelFill, new ImmutablePen(BoatsParts.Dark, 1.2), lumpBox, 2, 2);

        BoatsParts.DrawWater(context, new Rect(bounds.X, bounds.Y, bounds.Width, seabed), surface, 0.72);
        context.FillRectangle(Seabed, new Rect(bounds.X, seabed, bounds.Width, bounds.Bottom - seabed));

        Mark("hull", hull);
        if (crateBoxes.Count > 0) Mark("cargo", crateBoxes, 2);
        else Mark("cargo", new Rect(P(1, Hull.Depth).X, deckY - box, 8 * 1.25 * scale, box));
        Mark("draft", marks, 4);
        Mark("lump", lumpBox);
        DiagramText.Draw(context, DiagramText.Format("Boats_Hull_Lump", Hull.Steel.ToString("0")), new Point(lumpBox.Center.X, lumpBox.Y - 8), 11.5, DiagramText.Ink, 0.5, 1);

        // The loading grip, above the stacks at the end of the cargo: along the deck for more.
        Grip("cargo", new Point(left + ((FirstStack + (Math.Clamp(Cargo, 0, MostCargo) * DeckPerTonne)) * scale), deckY - (2 * box) - 16),
            GripAxis.Horizontal, DiagramText.Get("Boats_Grip_Cargo"),
            at => Cargo = Math.Round(Math.Clamp((((at.X - left) / scale) - FirstStack) / DeckPerTonne, 0, MostCargo)));

        var info = new Point(bounds.X + 18, seabed + 14);
        if (Hull.Sinks(Cargo))
        {
            DiagramText.Draw(context, DiagramText.Get("Boats_Hull_Sinking"), info, 15, BoatsParts.Danger, bold: true);
        }
        else
        {
            var first = DiagramText.Draw(context, DiagramText.Format("Boats_Hull_Draft", Hull.Draft(Cargo).ToString("0.00")), info, 15, DiagramText.Ink, bold: true);
            DiagramText.Draw(context, DiagramText.Format("Boats_Hull_Weighs", (Hull.Steel + Cargo).ToString("0")), new Point(info.X, first.Bottom + 4), 12, DiagramText.Faint);
        }
    }
}
