using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.LightShadow;

/// <summary>
/// Spread 2, seen from the side: a torch shining straight down on a board that tips. The same beam lands on a
/// longer patch, so each bit of it gets less light; below, the patch seen face on. Bits of light rain down the beam
/// evenly, so on a tipped board they land further apart. The reader tips the board by dragging its low arm; it
/// swings to a new angle rather than jumping.
/// </summary>
public sealed class TiltDiagram : GearPicture
{
    public static readonly StyledProperty<double> TiltProperty =
        AvaloniaProperty.Register<TiltDiagram, double>(nameof(Tilt), 30, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The slider's range, in degrees.</summary>
    private const double MostTilt = 85;

    private const int Rays = 7;

    /// <summary>Where the grip sits on the board's low arm, as a share of the arm from the pivot.</summary>
    private const double GripAlong = 0.78;

    /// <summary>How fast the bits of light fall, in beam widths a second, and how far apart they are.</summary>
    private const double FallSpeed = 0.9;
    private const double BitGap = 0.28;

    private static readonly IImmutableBrush TorchBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x5A, 0x63, 0x75));
    private static readonly IImmutableBrush BeamBrush = new ImmutableSolidColorBrush(Color.FromArgb(0x26, 0xF2, 0xC9, 0x4C));
    private static readonly IImmutableBrush BitBrush = new ImmutableSolidColorBrush(Color.FromRgb(0xFF, 0xE8, 0x9A));
    private static readonly IPen RayPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x80, 0xF2, 0xC9, 0x4C)), 1.2);
    private static readonly IPen BoardPen = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb(0x9A, 0x86, 0x6A)), 8, lineCap: PenLineCap.Round);
    private static readonly IPen GuidePen = new ImmutablePen(DiagramText.Faint, 1, new ImmutableDashStyle([4, 4], 0));
    private static readonly IPen OutlinePen = new ImmutablePen(DiagramText.Faint, 1.2, new ImmutableDashStyle([4, 3], 0));

    /// <summary>The angle drawn, easing toward <see cref="Tilt"/> (NaN until the first frame, which starts there).</summary>
    private double _shown = double.NaN;

    static TiltDiagram()
    {
        AffectsRender<TiltDiagram>(TiltProperty);
    }

    /// <summary>How far the board is tipped from flat, in degrees.</summary>
    public double Tilt
    {
        get => GetValue(TiltProperty);
        set => SetValue(TiltProperty, value);
    }

    protected override void Advance(double seconds) =>
        _shown = Gripping ? Tilt : LightGrips.Ease(_shown, Tilt, seconds, 0.12);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        if (double.IsNaN(_shown)) _shown = Tilt;
        var tilt = Math.Clamp(_shown, 0, 89);
        var angle = tilt * Math.PI / 180;
        var cos = Math.Cos(angle);
        var tan = Math.Tan(angle);
        var unit = Math.Min(bounds.Width, bounds.Height * 0.9);
        var beam = unit * 0.26;
        var pivot = new Point(bounds.Width * 0.5, bounds.Height * 0.5);
        var torchTop = bounds.Height * 0.08;
        var torchBottom = torchTop + (unit * 0.1);
        var left = pivot.X - (beam / 2);
        var right = pivot.X + (beam / 2);

        // The board: through the pivot, tipped by the angle (its right side goes up).
        double BoardY(double x) => pivot.Y - ((x - pivot.X) * tan);
        var reach = unit * 0.42;
        var boardA = new Point(pivot.X - (reach * cos), pivot.Y + (reach * Math.Sin(angle)));
        var boardB = new Point(pivot.X + (reach * cos), pivot.Y - (reach * Math.Sin(angle)));

        // The torch and its beam down to the board.
        context.FillRectangle(TorchBrush, new Rect(left - 8, torchTop, beam + 16, torchBottom - torchTop), 6);
        var beamShape = new StreamGeometry();
        using (var g = beamShape.Open())
        {
            g.BeginFigure(new Point(left, torchBottom), true);
            g.LineTo(new Point(right, torchBottom));
            g.LineTo(new Point(right, BoardY(right)));
            g.LineTo(new Point(left, BoardY(left)));
            g.EndFigure(true);
        }

        context.DrawGeometry(BeamBrush, null, beamShape);

        // Each ray, and bits of light falling down it at the same pace: on a tipped board they land further apart.
        var gap = beam * BitGap;
        var fallen = Seconds * FallSpeed * beam;
        for (var i = 0; i < Rays; i++)
        {
            var x = left + (beam * i / (Rays - 1));
            var bottom = BoardY(x);
            context.DrawLine(RayPen, new Point(x, torchBottom), new Point(x, bottom));
            for (var y = torchBottom + ((fallen + (i * gap * 0.37)) % gap); y < bottom - 2; y += gap)
            {
                context.DrawEllipse(BitBrush, null, new Point(x, y), 2.2, 2.2);
            }
        }

        // The flat position, dashed, and the angle between it and the board.
        context.DrawLine(GuidePen, new Point(pivot.X - reach, pivot.Y), new Point(pivot.X + reach, pivot.Y));
        context.DrawLine(BoardPen, boardA, boardB);
        var lit = new Pen(new SolidColorBrush(Color.FromArgb(255, 0xF2, 0xC9, 0x4C), 0.35 + (0.65 * cos)), 8, lineCap: PenLineCap.Flat);
        context.DrawLine(lit, new Point(left, BoardY(left)), new Point(right, BoardY(right)));
        Mark("torch", new Rect(left - 8, torchTop, beam + 16, torchBottom - torchTop));
        Mark("patch", [new Point(left, BoardY(left)), new Point(right, BoardY(right))]);
        Mark("board", [boardA, boardB], pad: 2);
        if (tilt >= 3)
        {
            var arcRadius = reach * 0.8;
            var arc = new StreamGeometry();
            using (var g = arc.Open())
            {
                g.BeginFigure(new Point(pivot.X + arcRadius, pivot.Y), false);
                g.ArcTo(new Point(pivot.X + (arcRadius * cos), pivot.Y - (arcRadius * Math.Sin(angle))),
                    new Size(arcRadius, arcRadius), 0, false, SweepDirection.CounterClockwise);
                g.EndFigure(false);
            }

            context.DrawGeometry(null, new Pen(DiagramText.Faint, 1), arc);
            var mid = angle / 2;
            DiagramText.Draw(context, DiagramText.Format("Light_Degrees", tilt.ToString("0")),
                new Point(pivot.X + ((arcRadius + 8) * Math.Cos(mid)), pivot.Y - ((arcRadius + 8) * Math.Sin(mid))), 13, DiagramText.Ink, 0, 0.5);
        }

        DrawFaceOn(context, bounds, beam, cos);

        // Near the board's low end (in from it, clear of the face-on patch below): pushed down or lifted, the board turns
        // about its middle.
        var handle = new Point(pivot.X - (GripAlong * reach * cos), pivot.Y + (GripAlong * reach * Math.Sin(angle)));
        Grip("board", handle, GripAxis.Free, DiagramText.Get("Light_Grip_Board"), at =>
            Tilt = LightGrips.Snap(Math.Atan2(at.Y - pivot.Y, pivot.X - at.X) * 180 / Math.PI, 0, MostTilt, 1));
    }

    /// <summary>The lit patch seen face on: as wide as the beam, 1/cos as long, and cos as bright.</summary>
    private static void DrawFaceOn(DrawingContext context, Rect bounds, double beam, double cos)
    {
        var size = Math.Min(beam * 0.6, bounds.Height * 0.12);
        var length = Math.Min(size / cos, bounds.Width * 0.4);
        var y = bounds.Bottom - size - 40;
        var x0 = bounds.X + 24;
        DiagramText.Draw(context, DiagramText.Get("Light_Tilt_FaceOn"), new Point(x0, y - 8), 12, DiagramText.Faint, 0, 1);

        var patch = new Rect(x0, y, length, size);
        context.FillRectangle(new SolidColorBrush(Color.FromRgb(0xF2, 0xC9, 0x4C), 0.15 + (0.85 * cos)), patch, 3);
        context.DrawRectangle(null, OutlinePen, new Rect(x0, y, size, size), 3);
        DiagramText.Draw(context,
            DiagramText.Format("Light_Tilt_Readout", (1 / cos).ToString("0.00"), (cos * 100).ToString("0")),
            new Point(x0, patch.Bottom + 8), 12.5, DiagramText.Ink);
    }
}
