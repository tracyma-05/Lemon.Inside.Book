using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Boats;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.Controls;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The Boats chapter: the models behind its pictures, its words, and every spread drawn.</summary>
public sealed class BoatsTests
{
    [Theory]
    [InlineData(240)]
    [InlineData(500)]
    [InlineData(920)]
    public void AFloatingBlock_SettlesWithItsDensityShareUnderWater(double density)
    {
        var block = new FloatingBlock();
        for (var i = 0; i < 300; i++) block.Step(0.05, density, floor: 2.2);
        Assert.Equal(density / Water.Fresh, block.Submerged, 3);
        Assert.Equal(FloatingBlock.SubmergedShare(density), block.Submerged, 3);
    }

    [Fact]
    public void ABlockHeavierThanWater_GoesToTheBottom()
    {
        var block = new FloatingBlock();
        for (var i = 0; i < 300; i++) block.Step(0.05, 1900, floor: 2.2);
        Assert.Equal(2.2, block.Depth, 9);
        Assert.True(FloatingBlock.Sinks(1900));

        // Made light again, it comes back up and floats.
        for (var i = 0; i < 400; i++) block.Step(0.05, 500, floor: 2.2);
        Assert.Equal(0.5, block.Submerged, 3);
    }

    [Fact]
    public void TheDifferenceInPressure_IsTheWeightOfTheWaterPushedAside_AtAnyDepth()
    {
        const double Side = PressureSpread.Side;
        var weight = Water.Fresh * Water.Gravity * Side * Side * Side;
        foreach (var depth in new[] { 0.5, 2, 9.5 })
        {
            var lift = (Water.Pressure(depth + Side) - Water.Pressure(depth)) * Side * Side;
            Assert.Equal(weight, lift, 6);
        }

        Assert.InRange(Water.Pressure(10) / 1000, 95, 100); // ten metres: about the air's own pressure again
    }

    [Fact]
    public void TheSteelHull_Floats_WhereItsLumpSinks_UntilOverloaded()
    {
        Assert.True(Hull.SteelDensity > Water.Sea);
        Assert.True(Hull.Draft(0) < 0.2);
        Assert.Equal(1 / (Water.Sea * Hull.Length * Hull.Beam / 1000), Hull.Draft(1) - Hull.Draft(0), 9); // per tonne
        Assert.False(Hull.Sinks(Hull.MostCargo - 0.5));
        Assert.True(Hull.Sinks(Hull.MostCargo + 0.5));
        Assert.InRange(Hull.MostCargo, 60, 75);
    }

    [Fact]
    public void Heeled_TheWaterPushedAsideKeepsItsSize_AndItsCentreMovesToTheLowSide()
    {
        var boat = HeelSpread.Boat;
        var upright = boat.At(0);
        Assert.Equal(boat.Draft, upright.Waterline, 6);
        Assert.Equal(boat.Draft / 2, upright.Buoyancy.Y, 6);
        Assert.Equal(0, boat.RightingLever(0), 9);

        var previous = 0.0;
        foreach (var degrees in new[] { 5.0, 10, 20, 30 })
        {
            var heel = degrees * Math.PI / 180;
            var (_, under, buoyancy, weight) = boat.At(heel);
            Assert.Equal(boat.Beam * boat.Draft, Area(under), 6);
            Assert.True(buoyancy.X > weight.X, $"{degrees}°");
            Assert.True(boat.RightingLever(heel) > previous);
            previous = boat.RightingLever(heel);
        }

        // For small tips the lever is GM × sin(heel).
        var small = 2 * Math.PI / 180;
        Assert.Equal(boat.MetacentricHeight * Math.Sin(small), boat.RightingLever(small), 3);
        Assert.Equal(-boat.Lever(0.3), boat.Lever(-0.3), 9);
    }

    [Fact]
    public void AHighWeight_MakesItTopHeavy_AndAWiderBoatSteadiesIt()
    {
        var steady = new Section(6, 3, 1.5, 1.6);
        var topHeavy = new Section(5, 3, 1.5, 3.5);
        var wider = new Section(8, 3, 1.5, 3.5);
        Assert.True(steady.MetacentricHeight > 0);
        Assert.True(topHeavy.MetacentricHeight < 0);
        Assert.True(wider.MetacentricHeight > 0);
        Assert.True(topHeavy.RightingLever(5 * Math.PI / 180) < 0);

        var roll = new Roll();
        roll.Nudge(0.35);
        roll.Step(20, steady);
        Assert.True(Math.Abs(roll.Angle) < 0.02, $"{roll.Angle}");

        // Top-heavy, a small push leaves it lying over (or over altogether), never back upright.
        var lolls = new Roll();
        lolls.Nudge(0.35);
        lolls.Step(20, topHeavy);
        Assert.True(Math.Abs(lolls.Angle) > 10 * Math.PI / 180, $"{lolls.Angle}");
    }

    [Fact]
    public void TheSubmarine_Floats_Dives_AndHangsWhenBalanced()
    {
        var sub = new Submarine();
        for (var i = 0; i < 600; i++) sub.Step(0.1, 0);
        Assert.Equal(Submarine.DryWeight, sub.Submerged, 2); // floating with a tenth above the water

        for (var i = 0; i < 3000; i++) sub.Step(0.1, 1);
        Assert.True(sub.OnSeabed);

        for (var i = 0; i < 3000; i++) sub.Step(0.1, 0);
        Assert.Equal(Submarine.DryWeight, sub.Submerged, 2); // blown, it comes back up

        Assert.Equal(1, Submarine.Weight(Submarine.Neutral), 9);
        var still = new Submarine();
        for (var i = 0; i < 100; i++) still.Step(0.1, Submarine.Neutral);
        Assert.True(Math.Abs(still.Speed) < 0.05);
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var boats = new BoatsViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Boats").Spreads, boats.Spreads.Count);
        boats.Float.Density = 500;
        Assert.Contains("50", boats.Float.ReadoutText);
        Assert.Equal("Boats_Material_Pine", boats.Float.MaterialKey);
        boats.Hull.Cargo = 80;
        Assert.True(boats.Hull.Sinks);
        boats.Stability.CentreHeight = 3.5;
        boats.Stability.Beam = 5;
        Assert.True(boats.Stability.MetacentricHeight < 0);
        boats.Submarine.Ballast = Submarine.Neutral;
        Assert.True(boats.Submarine.IsNeutral);
    }

    [Fact]
    public void EverySpreadsWords_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new BoatsViewModel().Spreads.SelectMany(s => s.TextKeys))
            {
                Assert.Contains(key, keys);
            }

            foreach (var (_, material) in FloatSpread.Materials)
            {
                Assert.Contains(material, keys);
            }
        }
    }

    [Fact]
    public void ADraggedBallast_SettlesOnTheBalance_WhenItComesClose()
    {
        Assert.Equal(Submarine.Neutral, SubmarineDiagram.Settle(Submarine.Neutral + 0.02), 9);
        Assert.Equal(Submarine.Neutral, SubmarineDiagram.Settle(Submarine.Neutral - 0.02), 9);
        Assert.Equal(0.6, SubmarineDiagram.Settle(0.6), 9);
        Assert.Equal(0.33, SubmarineDiagram.Settle(0.3333), 9);
        Assert.Equal(0, SubmarineDiagram.Settle(-1), 9);
        Assert.Equal(1, SubmarineDiagram.Settle(2), 9);
    }

    /// <summary>Every picture has grips, and dragging them moves the spread's own values (the sliders follow).</summary>
    [AvaloniaFact]
    public void ThePictures_AreWorkedByTheirGrips()
    {
        var (window, model) = OpenAt("Boats");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Boats");
        var boats = Assert.IsType<BoatsTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Boats;
        Avalonia.Point Far(Picture picture, string grip, double dx, double dy) =>
            picture.GripCentre(grip)!.Value + new Avalonia.Vector(dx, dy);

        Picture Open(int spread)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, spread));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Picture>().First(p => p.IsEffectivelyVisible);
            Assert.NotEmpty(picture.MarkedGrips);
            return picture;
        }

        var block = Open(0);
        block.DragGrip("weight", Far(block, "weight", 0, 2000));
        Assert.Equal(2000, boats.Float.Density, 9);
        block.DragGrip("weight", Far(block, "weight", 0, -2000));
        Assert.Equal(100, boats.Float.Density, 9);

        var pressure = Open(1);
        pressure.DragGrip("cube", Far(pressure, "cube", 0, 2000));
        Assert.Equal(10, boats.Pressure.Depth, 9);
        pressure.DragGrip("cube", Far(pressure, "cube", 0, -2000));
        Assert.Equal(0.5, boats.Pressure.Depth, 9);

        var hull = Open(2);
        hull.DragGrip("cargo", Far(hull, "cargo", 2000, 0));
        Assert.Equal(80, boats.Hull.Cargo, 9);
        hull.DragGrip("cargo", Far(hull, "cargo", -2000, 0));
        Assert.Equal(0, boats.Hull.Cargo, 9);

        var heel = Open(3);
        heel.DragGrip("mast", Far(heel, "mast", 2000, 0));
        Assert.Equal(60, boats.Heel.HeelAngle, 9);
        heel.DragGrip("mast", Far(heel, "mast", -2000, 0));
        Assert.Equal(0, boats.Heel.HeelAngle, 9);

        var stability = Open(4);
        stability.DragGrip("weight", Far(stability, "weight", 0, -2000));
        Assert.Equal(4, boats.Stability.CentreHeight, 9);
        stability.DragGrip("weight", Far(stability, "weight", 0, 2000));
        Assert.Equal(1, boats.Stability.CentreHeight, 9);
        stability.DragGrip("beam", Far(stability, "beam", 2000, 0));
        Assert.Equal(8, boats.Stability.Beam, 9);
        stability.DragGrip("beam", Far(stability, "beam", -2000, 0));
        Assert.Equal(4, boats.Stability.Beam, 9);

        var submarine = Open(5);
        submarine.DragGrip("ballast", Far(submarine, "ballast", 0, -2000));
        Assert.Equal(1, boats.Submarine.Ballast, 9);
        submarine.DragGrip("ballast", Far(submarine, "ballast", 0, 2000));
        Assert.Equal(0, boats.Submarine.Ballast, 9);

        window.Close();
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Boats");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Boats");
        Type[] pictures =
        [
            typeof(FloatDiagram), typeof(PressureDiagram), typeof(HullDiagram), typeof(HeelDiagram), typeof(StabilityDiagram),
            typeof(SubmarineDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"boats-{s + 1}");
        }

        // A sinking brick, deep water, an overloaded boat, a steep tip, a top-heavy boat and a diving submarine.
        var boats = Assert.IsType<BoatsTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Boats;
        boats.Float.Density = 1900;
        boats.Pressure.Depth = 10;
        boats.Hull.Cargo = 60;
        boats.Heel.HeelAngle = 45;
        (boats.Stability.CentreHeight, boats.Stability.Beam) = (3.5, 5);
        boats.Submarine.Ballast = 1;
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            if (s is 4 or 5) Thread.Sleep(1500);
            Frames(3);
            Snapshot(window, $"boats-{s + 1}-changed");
        }

        window.Close();
    }

    private static double Area(IReadOnlyList<Avalonia.Point> p)
    {
        var sum = 0.0;
        for (var i = 0; i < p.Count; i++)
        {
            var (a, b) = (p[i], p[(i + 1) % p.Count]);
            sum += (a.X * b.Y) - (b.X * a.Y);
        }

        return Math.Abs(sum / 2);
    }
}
