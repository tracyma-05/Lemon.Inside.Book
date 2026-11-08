using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Avalonia;
using Lemon.Inside.Book.Chapters.Gears;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.Controls;
using Lemon.Inside.Book.Controls.Mechanics;
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

    [Fact]
    public void AGearDraggedRound_FollowsTheFinger_WithoutJumpingATurn()
    {
        Assert.Equal((6 * Math.PI) + 0.3, GearsGrips.Follow((6 * Math.PI) + 0.1, 0.3), 9);
        Assert.Equal(Math.PI + 0.1, GearsGrips.Follow(Math.PI - 0.1, -Math.PI + 0.1), 9); // across the left, on round
        Assert.Equal(Math.PI / 2, GearsGrips.AngleOf(new Point(10, 10), new Point(10, 0)), 9); // straight up the screen
        Assert.Equal(17, GearsGrips.Nearest(8, 30, 17 * 3.04, z => z * 3));
        Assert.Equal(8, GearsGrips.Nearest(8, 30, -40, z => z * 3));
    }

    [Fact]
    public void ThePencilsDistance_SaysHowMuchStringIsOff()
    {
        foreach (var degrees in new[] { 0.0, 45, 120, 200 })
        {
            var t = degrees * Math.PI / 180;
            var (end, _) = GearGeometry.Unwind(1, t);
            Assert.Equal(degrees, InvoluteDiagram.UnwoundAt(Length(end)), 6);
        }

        Assert.Equal(0, InvoluteDiagram.UnwoundAt(0.5)); // inside the can
        Assert.Equal(240, InvoluteDiagram.UnwoundAt(50)); // the slider's top
    }

    [Fact]
    public void TheLineOfActionsSlant_IsThePressureAngle_AndATapAddsATrainStep()
    {
        var twenty = 20 * Math.PI / 180;
        Assert.Equal(20, MeshDiagram.PressureToward(new Vector(Math.Sin(twenty), Math.Cos(twenty))), 9);
        Assert.Equal(20, MeshDiagram.PressureToward(new Vector(-Math.Sin(twenty), -Math.Cos(twenty))), 9);
        Assert.Equal(30, MeshDiagram.PressureToward(new Vector(1, 0)));
        Assert.Equal(14, MeshDiagram.PressureToward(new Vector(0, 1)));
        Assert.Equal([2, 3, 4, 1], new[] { 1, 2, 3, 4 }.Select(TrainDiagram.NextSteps));
    }

    [AvaloniaFact]
    public void ThePictures_AreWorkedByTheirGrips()
    {
        var (window, model) = OpenAt("Gears");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Gears");
        var gears = Assert.IsType<GearsTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Gears;
        Point Far(Picture picture, string grip, double dx, double dy) => picture.GripCentre(grip)!.Value + new Vector(dx, dy);

        // A quarter turn clockwise on screen about the part's middle.
        Point Quarter(Picture picture, string grip, string part)
        {
            var c = picture.PartCentre(part)!.Value;
            var d = picture.GripCentre(grip)!.Value - c;
            return c + new Vector(-d.Y, d.X);
        }

        Picture Open(int spread)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, spread));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Picture>().First(p => p.IsEffectivelyVisible);
            Assert.NotEmpty(picture.MarkedGrips);
            return picture;
        }

        var ratio = Open(0);
        ratio.DragGrip("driverTeeth", Far(ratio, "driverTeeth", 0, -2000));
        Assert.Equal(30, gears.Ratio.DriverTeeth);
        Frames(1);
        ratio.DragGrip("drivenTeeth", Far(ratio, "drivenTeeth", 0, 2000));
        Assert.Equal(8, gears.Ratio.DrivenTeeth);

        var torque = Assert.IsType<TorqueDiagram>(Open(1));
        torque.DragGrip("drivenTeeth", Far(torque, "drivenTeeth", 0, 2000));
        Assert.Equal(12, gears.Torque.DrivenTeeth);
        Frames(1);
        var cranked = torque.Turns;
        torque.DragGrip("crank", Quarter(torque, "crank", "crank"));
        Assert.InRange(torque.Turns - cranked, 0.23, 0.27);

        var involute = Open(2);
        involute.DragGrip("pencil", Far(involute, "pencil", 2000, 0));
        Assert.Equal(240, gears.Involute.Unwound, 9);
        Frames(1);
        involute.DragGrip("pencil", involute.PartCentre("can")!.Value);
        Assert.Equal(0, gears.Involute.Unwound, 9);

        var mesh = Assert.IsType<MeshDiagram>(Open(3));
        mesh.DragGrip("pressure", Far(mesh, "pressure", 0, -2000));
        Assert.Equal(14, gears.Mesh.PressureAngle, 9);
        Frames(1);
        mesh.DragGrip("pressure", Far(mesh, "pressure", 2000, 0));
        Assert.Equal(30, gears.Mesh.PressureAngle, 9);
        Frames(1);
        var angle = mesh.Angle;
        mesh.DragGrip("turn", Far(mesh, "turn", 0, 60));
        Assert.Equal(0, gears.Mesh.Speed, 9);
        Assert.NotEqual(angle, mesh.Angle);

        var train = Assert.IsType<TrainDiagram>(Open(4));
        gears.Train.Steps = 2;
        Frames(1);
        train.DragGrip("step", default);
        Assert.Equal(3, gears.Train.Steps);
        Frames(1);
        var turned = train.Turns;
        train.DragGrip("turn", Quarter(train, "turn", "input"));
        Assert.InRange(train.Turns - turned, 0.23, 0.27);

        var planets = Open(5);
        planets.DragGrip("holdSun", default);
        Assert.Equal(PlanetHeld.Sun, gears.Planetary.Held);
        Frames(1);
        planets.DragGrip("holdCarrier", default);
        Assert.Equal(PlanetHeld.Carrier, gears.Planetary.Held);
        Frames(1);
        planets.DragGrip("holdRing", default);
        Assert.True(gears.Planetary.IsRingHeld);
        Frames(1);
        planets.DragGrip("sunTeeth", Far(planets, "sunTeeth", 0, -2000));
        Assert.Equal(4, gears.Planetary.SunStep);
        Frames(1);
        planets.DragGrip("sunTeeth", Far(planets, "sunTeeth", 0, 2000));
        Assert.Equal(0, gears.Planetary.SunStep);

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
