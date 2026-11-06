using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Gears;
using Lemon.Inside.Book.Contents;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The Gears chapter: tooth shapes, meshing without overlap, ratios, the planetary set, and every spread drawn.</summary>
public sealed class GearsTests
{
    private static readonly double Twenty = 20 * Math.PI / 180;

    [Fact]
    public void TheUnwoundString_IsAsLongAsTheArc_AndSquareToTheCurve()
    {
        const double R = 3;
        foreach (var t in new[] { 0.3, 1.0, 2.5 })
        {
            var (end, leaves) = GearGeometry.Unwind(R, t);
            var cord = end - leaves;
            Assert.Equal(R * t, Math.Sqrt((cord.X * cord.X) + (cord.Y * cord.Y)), 9);
            Assert.Equal(0, (cord.X * leaves.X) + (cord.Y * leaves.Y), 9); // the string leaves the can square to its radius

            // ...and is square to the curve it draws (the curve's direction, by a small step).
            var next = GearGeometry.Unwind(R, t + 1e-6).End - end;
            Assert.Equal(0, ((next.X * cord.X) + (next.Y * cord.Y)) / (Length(next) * Length(cord)), 4);
        }
    }

    [Fact]
    public void ATooth_IsHalfAPitchWide_OnTheRollingCircle()
    {
        foreach (var z in new[] { 12, 20, 40 })
        {
            var half = GearGeometry.HalfToothAngle(z, Twenty, GearGeometry.PitchRadius(z), thinning: 0);
            Assert.Equal(Math.PI / (2 * z), half, 9);
            Assert.True(GearGeometry.HalfToothAngle(z, Twenty, GearGeometry.PitchRadius(z) + GearGeometry.Addendum) > 0, $"{z} teeth come to a point");
        }
    }

    [Theory]
    [InlineData(12, 30, 20)]
    [InlineData(16, 26, 14)]
    [InlineData(16, 26, 30)]
    [InlineData(20, 20, 20)]
    [InlineData(12, 36, 20)]
    public void MeshingGears_NeverOverlap(int z1, int z2, double degrees)
    {
        var pressure = degrees * Math.PI / 180;
        var a = GearGeometry.Outline(z1, pressure);
        var b = GearGeometry.Outline(z2, pressure);
        var centre = GearGeometry.PitchRadius(z1) + GearGeometry.PitchRadius(z2);
        for (var step = 0; step < 40; step++)
        {
            var a1 = step * 2 * Math.PI / z1 / 40;
            var a2 = GearGeometry.MeshAngle(z1, a1, z2, 0);
            var first = Place(a, a1, new Point(0, 0));
            var second = Place(b, a2, new Point(centre, 0));
            Assert.True(Depth(first, second) < 0.02, $"{z1}/{z2} at {degrees}°, step {step}: {Depth(first, second):0.000}");
            Assert.True(Depth(second, first) < 0.02, $"{z1}/{z2} at {degrees}°, step {step}: {Depth(second, first):0.000}");
        }
    }

    [Fact]
    public void MeshingGears_TurnByTheRatioOfTheirTeeth()
    {
        var turned = GearGeometry.MeshAngle(12, 2 * Math.PI, 30, 0) - GearGeometry.MeshAngle(12, 0, 30, 0);
        Assert.Equal(-2 * Math.PI * 12 / 30, turned, 9); // the other way, 12/30 of a turn
    }

    [Fact]
    public void ThereIsAlwaysAPairOfTeethTouching_AndMoreWithASmallerPressureAngle()
    {
        foreach (var degrees in new[] { 14.0, 20, 30 })
        {
            var pressure = degrees * Math.PI / 180;
            Assert.True(GearGeometry.ContactRatio(MeshDiagram.DriverTeeth, MeshDiagram.DrivenTeeth, pressure) > 1, $"{degrees}°");
            for (var a = 0.0; a < 2 * Math.PI / MeshDiagram.DriverTeeth; a += 0.01)
            {
                var (contacts, from, to) = GearGeometry.Contacts(MeshDiagram.DriverTeeth, MeshDiagram.DrivenTeeth, pressure, a);
                Assert.InRange(contacts.Count, 1, 2);
                Assert.All(contacts, s => Assert.InRange(s, from, to));
            }
        }

        Assert.True(GearGeometry.ContactRatio(16, 26, 14 * Math.PI / 180) > GearGeometry.ContactRatio(16, 26, 30 * Math.PI / 180));
    }

    [Fact]
    public void TheTouchingPoint_IsOnTheGoldGearsToothSide()
    {
        // At a touching point, the gold gear's outline passes (to within the drawing's play) through that point.
        var outline = Place(GearGeometry.Outline(16, Twenty, thinning: 0), 0.3, new Point(0, 0));
        foreach (var s in GearGeometry.Contacts(16, 26, Twenty, 0.3).Contacts)
        {
            var p = GearGeometry.OnLineOfAction(16, Twenty, s);
            Assert.True(DistanceToOutline(p, outline) < 0.05, $"{s:0.00}: {DistanceToOutline(p, outline):0.000}");
        }
    }

    [Fact]
    public void ATrain_MultipliesItsSteps_AndTheLastGearTurnsThatMuchSlower()
    {
        var gears = new GearsViewModel();
        gears.Train.Steps = 3;
        Assert.Equal(27, gears.Train.Ratio);
        var start = TrainDiagram.Angles(3, 0)[3];
        var after = TrainDiagram.Angles(3, 27)[3];
        Assert.Equal(2 * Math.PI, after - start, 6); // 27 turns in, one turn out; three meshes, so the other way round
    }

    [Theory]
    [InlineData(PlanetHeld.Ring)]
    [InlineData(PlanetHeld.Sun)]
    [InlineData(PlanetHeld.Carrier)]
    public void APlanetarySet_KeepsTheHeldPartStill_AndFollowsItsRule(PlanetHeld held)
    {
        foreach (var sun in PlanetarySpread.SunChoices)
        {
            var (s, r, c) = Planetary.Speeds(sun, held);
            Assert.Equal((sun + Planetary.RingTeeth) * c, (sun * s) + (Planetary.RingTeeth * r), 9);

            var before = Planetary.Pose(sun, held, 0);
            var after = Planetary.Pose(sun, held, 0.37);
            var ringTurn = 2 * Math.PI / Planetary.RingTeeth;
            switch (held)
            {
                case PlanetHeld.Ring:
                    Assert.Equal(0, Wrap(after.Ring - before.Ring, ringTurn), 6);
                    break;
                case PlanetHeld.Sun:
                    Assert.Equal(before.Sun, after.Sun);
                    Assert.Equal(2 * Math.PI * 0.37, after.Ring - before.Ring, 6); // the ring is what is turned
                    break;
                default:
                    Assert.Equal(before.Carrier, after.Carrier);
                    break;
            }

            // Every planet's teeth fit the ring's, not only planet 0's.
            foreach (var (towards, spin) in after.Planets)
            {
                var ring = GearGeometry.RingAngle(Planetary.PlanetTeeth(sun), spin, Planetary.RingTeeth, towards);
                Assert.Equal(0, Wrap(ring - after.Ring, ringTurn), 6);
            }
        }

        Assert.Equal(1 + (60.0 / 24), 1 / Planetary.Ratio(24, PlanetHeld.Ring), 9);
        Assert.Equal(-24.0 / 60, Planetary.Ratio(24, PlanetHeld.Carrier), 9);
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var gears = new GearsViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Gears").Spreads, gears.Spreads.Count);
        gears.Ratio.DriverTeeth = 12;
        gears.Ratio.DrivenTeeth = 30;
        Assert.Equal(0.4, gears.Ratio.Ratio, 9);
        Assert.Contains("0.40", gears.Ratio.ReadoutText);
        gears.Torque.DrivenTeeth = 36;
        Assert.Contains("3.00", gears.Torque.ReadoutText);
        gears.Planetary.Held = PlanetHeld.Carrier;
        Assert.True(gears.Planetary.IsCarrierHeld);
        Assert.Contains("0.40", gears.Planetary.ReadoutText);
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new GearsViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }
        }
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Gears");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Gears");
        Type[] pictures =
        [
            typeof(RatioDiagram), typeof(TorqueDiagram), typeof(InvoluteDiagram), typeof(MeshDiagram), typeof(TrainDiagram),
            typeof(PlanetaryDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"gears-{s + 1}");
        }

        // The train with four steps, and the planets with the carrier held.
        var train = Assert.IsType<GearsTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 4)]);
        train.Gears.Train.Steps = 4;
        train.Gears.Planetary.Held = PlanetHeld.Carrier;
        model.GoToPage(BookContents.SpreadPage(chapter, 4));
        Frames(3);
        Snapshot(window, "gears-5-four-steps");
        model.GoToPage(BookContents.SpreadPage(chapter, 5));
        Frames(3);
        Snapshot(window, "gears-6-carrier-held");
        window.Close();
    }

    private static double Length(Point v) => Math.Sqrt((v.X * v.X) + (v.Y * v.Y));

    private static double Wrap(double angle, double period)
    {
        var r = angle % period;
        if (r < 0) r += period;
        return Math.Min(r, period - r);
    }

    private static Point[] Place(IReadOnlyList<Point> outline, double angle, Point at)
    {
        var (sin, cos) = Math.SinCos(angle);
        return outline.Select(p => new Point(at.X + (p.X * cos) - (p.Y * sin), at.Y + (p.X * sin) + (p.Y * cos))).ToArray();
    }

    /// <summary>How far the deepest point of <paramref name="points"/> reaches inside <paramref name="polygon"/>.</summary>
    private static double Depth(Point[] points, Point[] polygon) =>
        points.Where(p => Inside(p, polygon)).Select(p => DistanceToOutline(p, polygon)).DefaultIfEmpty(0).Max();

    private static bool Inside(Point p, Point[] polygon)
    {
        var inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if ((polygon[i].Y > p.Y) != (polygon[j].Y > p.Y) &&
                p.X < ((polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y)) + polygon[i].X)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static double DistanceToOutline(Point p, Point[] polygon)
    {
        var best = double.MaxValue;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            var a = polygon[j];
            var ab = polygon[i] - a;
            var t = Math.Clamp((((p.X - a.X) * ab.X) + ((p.Y - a.Y) * ab.Y)) / Math.Max(1e-12, (ab.X * ab.X) + (ab.Y * ab.Y)), 0, 1);
            best = Math.Min(best, Length(p - (a + (ab * t))));
        }

        return best;
    }
}
