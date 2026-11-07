using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Lemon.Inside.Book.Controls.Mechanics;
using Lemon.Inside.Book.Controls.Scene;

namespace Lemon.Inside.Book.Chapters.Boats;

/// <summary>
/// Spread 1: a block in a tank of water, bobbing until it settles. Its weight pulls down from its middle; the water's
/// push comes up from the middle of the part under water, as big as the weight of that much water.
/// </summary>
public sealed class FloatDiagram : GearPicture
{
    public static readonly StyledProperty<double> DensityProperty =
        AvaloniaProperty.Register<FloatDiagram, double>(nameof(Density), 500);

    private static readonly IImmutableBrush Glass = new ImmutableSolidColorBrush(Color.FromArgb(0x30, 0xE6, 0xEA, 0xF2));
    private static readonly IPen GlassEdge = new ImmutablePen(new ImmutableSolidColorBrush(Color.FromArgb(0x90, 0xE6, 0xEA, 0xF2)), 2);
    private static readonly Color Wood = Color.FromRgb(0xC8, 0x9A, 0x5A);
    private static readonly Color Stone = Color.FromRgb(0x9A, 0x6A, 0x5A);
    private static readonly IImmutableBrush Pushed = new ImmutableSolidColorBrush(Color.FromArgb(0x60, 0x9F, 0xD8, 0xF5));

    private readonly FloatingBlock _block = new();

    static FloatDiagram()
    {
        AffectsRender<FloatDiagram>(DensityProperty);
    }

    /// <summary>The block's density, kilograms per cubic metre.</summary>
    public double Density
    {
        get => GetValue(DensityProperty);
        set => SetValue(DensityProperty, value);
    }

    internal FloatingBlock Block => _block;

    /// <summary>How far the tank's floor is below the surface, in block heights.</summary>
    private const double Floor = 2.2;

    protected override void Advance(double seconds) => _block.Step(seconds, Density, Floor);

    protected override void Draw(DrawingContext context, Rect bounds)
    {
        var tank = new Rect(bounds.Width * 0.12, bounds.Height * 0.16, bounds.Width * 0.76, bounds.Height * 0.62);
        var size = Math.Min(tank.Height / (Floor + 1.2), tank.Width * 0.36);
        var surface = tank.Bottom - (Floor * size);
        context.FillRectangle(Glass, tank);
        BoatsParts.DrawWater(context, new Rect(tank.X, tank.Y, tank.Width, tank.Height), surface);
        context.DrawRectangle(null, GlassEdge, tank);

        // The block, its bottom _block.Depth block heights under the surface.
        var width = size * 1.3;
        var bottom = surface + (_block.Depth * size);
        var block = new Rect(tank.Center.X - (width / 2), bottom - size, width, size);
        var colour = Density > Water.Fresh ? Stone : Wood;
        context.DrawRectangle(new ImmutableSolidColorBrush(colour), new ImmutablePen(new ImmutableSolidColorBrush(Color.FromRgb((byte)(colour.R / 2), (byte)(colour.G / 2), (byte)(colour.B / 2))), 1.5), block, 3, 3);

        // The water it has pushed aside, outlined: that much water weighs what the water's push is.
        var under = _block.Submerged;
        if (under > 0.01)
        {
            var wet = new Rect(block.X, Math.Max(block.Y, surface), block.Width, block.Bottom - Math.Max(block.Y, surface));
            context.FillRectangle(Pushed, wet);
            context.DrawRectangle(null, new ImmutablePen(BoatsParts.Surface, 1.2, new ImmutableDashStyle([4, 3], 0)), wet);
        }

        Mark("block", block);
        var line = new Point(block.Right + 6, surface);
        context.DrawLine(new ImmutablePen(BoatsParts.Surface, 2), new Point(block.X - 10, surface), new Point(block.Right + 10, surface));
        Mark("waterline", [new Point(block.X - 10, surface), line + new Point(4, 0)], 4);

        // Weight down from the block's middle; the water's push up from the middle of the wet part.
        var perUnit = size * 0.9;
        var heaviness = Density / Water.Fresh;
        var middle = block.Center;
        var weightEnd = middle + new Point(-10, heaviness * perUnit);
        BoatsParts.DrawArrow(context, middle + new Point(-10, 0), weightEnd, BoatsParts.Weight, 5);
        Mark("weight", [middle + new Point(-10, 0), weightEnd], 6);
        var wetMiddle = new Point(middle.X + 10, block.Bottom - (under * size / 2));
        var liftEnd = wetMiddle - new Point(0, under * perUnit);
        BoatsParts.DrawArrow(context, wetMiddle, liftEnd, BoatsParts.Lift, 5);
        Mark("lift", [wetMiddle, liftEnd + new Point(0, -4)], 6);

        DiagramText.Draw(context, DiagramText.Get("Boats_Float_WeightLabel"), weightEnd + new Point(-16, 0), 12, BoatsParts.Weight, 1, 1);
        DiagramText.Draw(context, DiagramText.Get("Boats_Float_LiftLabel"), liftEnd + new Point(16, 0), 12, BoatsParts.Lift, 0, 0);

        // How much of it is under water now.
        var info = new Point(bounds.X + 18, tank.Bottom + 18);
        var first = DiagramText.Draw(context, DiagramText.Format("Boats_Float_Under", (under * 100).ToString("0")), info, 15, DiagramText.Ink, bold: true);
        if (_block.Depth >= Floor - 1e-6)
        {
            DiagramText.Draw(context, DiagramText.Get("Boats_Float_OnFloor"), new Point(info.X, first.Bottom + 4), 12.5, BoatsParts.Danger);
        }
    }
}
