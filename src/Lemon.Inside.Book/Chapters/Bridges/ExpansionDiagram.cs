using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;
using static Lemon.Inside.Book.Chapters.Bridges.BridgesParts;
using static Lemon.Inside.Book.Chapters.Lifting.LiftingParts;

namespace Lemon.Inside.Book.Chapters.Bridges;

/// <summary>
/// Spread 6: above, the whole 400 m deck on its piers, pinned at the left abutment and on rollers elsewhere, its growth
/// drawn a hundred times bigger; below, the joint at its right end seen from above, to scale in centimetres: the
/// deck's steel fingers sliding between the abutment's. Held fast with no joint, the deck bows in the heat and pushes on
/// the abutment. The weather changes gradually: the thermometer climbs or falls and the deck grows or shrinks with it,
/// its fingers sliding in the joint. The reader drags the thermometer and taps the joint to weld it shut or cut it again.
/// </summary>
public sealed class ExpansionDiagram : GearPicture
{
    public static readonly StyledProperty<double> TemperatureProperty =
        AvaloniaProperty.Register<ExpansionDiagram, double>(nameof(Temperature), Deck.Mild, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> JointProperty =
        AvaloniaProperty.Register<ExpansionDiagram, bool>(nameof(Joint), true, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>How much bigger the deck's growth is drawn in the view of the whole bridge.</summary>
    private const double Exaggeration = 100;

    /// <summary>The fingers' length and width in the joint, cm.</summary>
    private const double Finger = 26;
    private const double Tooth = 6;

    /// <summary>The temperature drawn, °C, warming or cooling toward the air's; NaN until the first frame.</summary>
    private double _temperature = double.NaN;

    /// <summary>How far the held deck has bowed, 0 to 1 of its bow (it bows up gradually, not at once).</summary>
    private double _held = double.NaN;

    static ExpansionDiagram()
    {
        AffectsRender<ExpansionDiagram>(TemperatureProperty, JointProperty);
    }

    /// <summary>The air's temperature, °C.</summary>
    public double Temperature
    {
        get => GetValue(TemperatureProperty);
        set => SetValue(TemperatureProperty, value);
    }

    public bool Joint
    {
        get => GetValue(JointProperty);
        set => SetValue(JointProperty, value);
    }

    /// <summary>The steel warms or cools toward the air at about thirty degrees a second, so the deck's growth can be watched.</summary>
    protected override void Advance(double seconds)
    {
        Start();
        _temperature = Lifting.LiftingParts.Ease(_temperature, Math.Clamp(Temperature, -20, 45), 30, seconds);
        _held = Glide(_held, Joint ? 0 : 1, seconds, 0.2);
    }

    private void Start()
    {
        if (double.IsNaN(_temperature)) _temperature = Math.Clamp(Temperature, -20, 45);
        if (double.IsNaN(_held)) _held = Joint ? 0 : 1;
    }

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        Start();
        var temperature = _temperature;
        var joint = Joint;
        var change = Deck.Change(temperature);
        DrawWhole(context, bounds, temperature, joint, change);
        DrawJoint(context, bounds, temperature, joint, change);
        DiagramText.Draw(context, DiagramText.Get("Bridges_Expansion_Rule"), new Point(bounds.X + 16, bounds.Y + 16), 12, DiagramText.Faint);
    }

    /// <summary>The whole bridge, its growth drawn a hundred times bigger.</summary>
    private void DrawWhole(DrawingContext context, Rect bounds, double temperature, bool joint, double change)
    {
        var scale = (bounds.Width - 120) / 430;
        var v = new View(new Point(bounds.X + 70, bounds.Y + (bounds.Height * 0.36)), scale);
        var held = _held;
        var grown = (1 - held) * change / 100 * Exaggeration;
        var hot = temperature > Deck.Mild;
        var bow = held * Math.Clamp(Math.Abs(temperature - Deck.Mild) * 0.9, 0, 26) * (hot ? 1 : 0);

        context.FillRectangle(River, new Rect(bounds.X + 12, v.At(0, -26).Y, bounds.Width - 24, 5 * scale));
        var left = v.Box(-14, -30, 0, 0);
        var right = v.Box(Deck.Length + 4, -30, Deck.Length + 18, 0);
        context.DrawRectangle(StoneDark, Edge, left);
        context.DrawRectangle(StoneDark, Edge, right);
        Mark("abutment", right);
        for (var x = 50.0; x < Deck.Length; x += 50) context.DrawRectangle(Concrete, Edge, v.Box(x - 2, -26, x + 2, -5));

        // The deck: pinned at the left, free to slide at the right (or held fast and bowing).
        var end = Deck.Length + grown;
        var deck = new List<Point>();
        for (var i = 0; i <= 40; i++)
        {
            var x = end * i / 40;
            deck.Add(v.At(x, -2 + (bow * Math.Sin(Math.PI * i / 40))));
        }

        var brush = joint ? Idle : hot ? SqueezeBrush : temperature < Deck.Mild ? StretchBrush : Idle;
        context.DrawGeometry(null, new ImmutablePen(brush, Math.Max(5, 4 * scale), null, PenLineCap.Flat, PenLineJoin.Round), Plumbing.PlumbingParts.Polyline(deck));
        Mark("deck", deck, 6);
        DrawPolygon(context, Concrete, Edge, [v.At(0, -4), v.At(-4, -9), v.At(4, -9)]);
        var roller = v.At(Deck.Length + grown, -6.5);
        context.DrawEllipse(Concrete, Edge, roller, Math.Max(4, 2.4 * scale), Math.Max(4, 2.4 * scale));
        Mark("bearing", roller, 12);
        DiagramText.Draw(context, DiagramText.Get("Bridges_Expansion_Pinned"), v.At(-7, 4), 11, DiagramText.Faint, 0.5, 1);
        DiagramText.Draw(context, DiagramText.Get(joint ? "Bridges_Expansion_Drawn" : hot ? "Bridges_Expansion_Bows" : "Bridges_Expansion_HeldLabel"),
            v.At(Deck.Length / 2, 6 + bow), 12, joint ? DiagramText.Faint : brush, 0.5, 1, bold: !joint);

        // The thermometer.
        var tube = new Rect(bounds.X + 22, bounds.Y + 48, 10, bounds.Height * 0.2);
        context.DrawRectangle(AirFill, Edge, tube, 5, 5);
        var share = (temperature + 20) / 65;
        var fill = new Rect(tube.X + 2, tube.Bottom - ((tube.Height - 4) * share) - 2, tube.Width - 4, (tube.Height - 4) * share);
        context.FillRectangle(hot ? SqueezeBrush : StretchBrush, fill);
        DiagramText.Draw(context, DiagramText.Format("Bridges_Degrees", Number(temperature, "0")), new Point(tube.Right + 18, fill.Top), 12.5, DiagramText.Ink, 0, 0.5, bold: true);

        // The reader's handle on the thermometer: dragged up for a hotter day, down for a colder one.
        Grip("temperature", new Point(tube.Center.X, fill.Top), GripAxis.Vertical, DiagramText.Get("Bridges_Grip_Temperature"),
            at => Temperature = Math.Clamp(Math.Round(-20 + (65 * (tube.Bottom - 2 - at.Y) / (tube.Height - 4))), -20, 45));
    }

    /// <summary>The joint from above, to scale in centimetres.</summary>
    private void DrawJoint(DrawingContext context, Rect bounds, double temperature, bool joint, double change)
    {
        var box = new Rect(bounds.X + 30, bounds.Y + (bounds.Height * 0.52), bounds.Width - 60, (bounds.Height * 0.48) - 70);
        context.DrawRectangle(Dark, new ImmutablePen(DiagramText.Faint, 1), box, 8, 8);
        Mark("joint", box);
        DiagramText.Draw(context, DiagramText.Get("Bridges_Expansion_FromAbove"), new Point(box.X + 10, box.Y + 8), 11.5, DiagramText.Faint);

        var gap = joint ? ExpansionSpread.MildGap - change : 0;
        var scale = Math.Min((box.Width - 30) / 110, (box.Height - 50) / 44);
        var face = box.Center.X + (25 * scale);
        var top = box.Y + 34;
        var width = 42 * scale;
        var deckEnd = face - (gap * scale);
        var steel = new ImmutableSolidColorBrush(Color.FromRgb(0x9A, 0xA4, 0xB2));

        // The deck on the left, the abutment on the right, and between them their fingers (or nothing, held fast).
        context.FillRectangle(Concrete, new Rect(box.X + 10, top, deckEnd - box.X - 10, width));
        context.FillRectangle(StoneDark, new Rect(face, top, box.Right - 10 - face, width));
        if (joint)
        {
            var slots = (int)(42 / Tooth);
            for (var k = 0; k < slots; k++)
            {
                var y = top + (k * Tooth * scale);
                var tooth = k % 2 == 0
                    ? new Rect(deckEnd, y + 1, Finger * scale, (Tooth * scale) - 2)
                    : new Rect(face - (Finger * scale), y + 1, Finger * scale, (Tooth * scale) - 2);
                context.DrawRectangle(steel, Edge, tooth);
            }

            var dimensionY = top + width + 16;
            context.DrawLine(new ImmutablePen(DiagramText.Gold, 1.5), new Point(deckEnd, dimensionY), new Point(face, dimensionY));
            DiagramText.DrawArrowAlong(context, new Point(face, dimensionY), new Point(deckEnd, dimensionY), DiagramText.Gold, 7);
            DiagramText.DrawArrowAlong(context, new Point(deckEnd, dimensionY), new Point(face, dimensionY), DiagramText.Gold, 7);
            DiagramText.Draw(context, DiagramText.Format("Bridges_Expansion_GapLabel", Number(gap, "0.#")), new Point((deckEnd + face) / 2, dimensionY + 6), 12.5,
                DiagramText.Gold, 0.5, 0, bold: true);
        }
        else if (Math.Abs(temperature - Deck.Mild) > 0.5)
        {
            var hot = temperature > Deck.Mild;
            var y = top + (width / 2);
            var from = new Point(face - (40 * scale), y);
            var to = new Point(face - 4, y);
            var brush = hot ? SqueezeBrush : StretchBrush;
            DrawPush(context, hot ? from : to, hot ? to : from, brush, string.Empty, 0.5, 1);
            DiagramText.Draw(context, DiagramText.Format(hot ? "Bridges_Expansion_PushLabel" : "Bridges_Expansion_PullLabel", Tonnes(Deck.Push(temperature))),
                new Point(face - (20 * scale), top - 4), 12.5, brush, 0.5, 1, bold: true);
        }

        // The reader's handle on the joint: tapped to weld the deck to the abutment, or to cut the joint again.
        Grip("joint", new Point((deckEnd + face) / 2, top + (width * 0.8)), GripAxis.Tap,
            DiagramText.Get(joint ? "Bridges_Grip_JointWeld" : "Bridges_Grip_JointCut"), _ => Joint = !Joint);

        DiagramText.Draw(context, DiagramText.Get("Bridges_Expansion_DeckLabel"), new Point(box.X + 18, top + width + 6), 11.5, DiagramText.Ink, 0, 0);
        DiagramText.Draw(context, DiagramText.Get("Bridges_Expansion_AbutmentLabel"), new Point(box.Right - 18, top + width + 6), 11.5, DiagramText.Ink, 1, 0);
    }

    private static readonly IImmutableBrush AirFill = new ImmutableSolidColorBrush(Color.FromRgb(0x1C, 0x22, 0x30));
}
