using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Lemon.Inside.Book.Chapters.Colour;
using Lemon.Inside.Book.Contents;
using Lemon.Inside.Book.Controls;
using Xunit;
using static Lemon.Inside.Book.Tests.ChapterTestKit;

namespace Lemon.Inside.Book.Tests;

/// <summary>The Colour chapter: the models behind its pictures, its words, and every spread drawn.</summary>
public sealed class ColourTests
{
    [Fact]
    public void ThreeLamps_AddUp_AndAScreenStoresNearlySeventeenMillionMixes()
    {
        Assert.Equal(16_777_216, Lamps.Colours, 9);
        Assert.Equal("Colour_Name_Yellow", ColourNames.Key(Lamps.Mix(1, 1, 0)));
        Assert.Equal("Colour_Name_White", ColourNames.Key(Lamps.Mix(1, 1, 1)));
        Assert.Equal("Colour_Name_Cyan", ColourNames.Key(Lamps.Mix(0, 1, 1)));
        Assert.Equal("Colour_Name_Magenta", ColourNames.Key(Lamps.Mix(1, 0, 1)));
        Assert.Equal("Colour_Name_Orange", ColourNames.Key(Lamps.Mix(1, 0.5, 0))); // what the walkthrough asks for
        Assert.Equal("Colour_Name_Black", ColourNames.Key(Lamps.Mix(0, 0, 0)));
        Assert.Equal(128, Lamps.Number(0.5));
    }

    [Fact]
    public void Inks_TakeLightAway_AndRealOnesLeaveAMuddyBrown()
    {
        Assert.True(Inks.Back(1, 0, 1, false).Distance(new Rgb(0, 1, 0)) < 1e-9); // cyan and yellow print green
        Assert.True(Inks.Back(0, 1, 1, false).Distance(new Rgb(1, 0, 0)) < 1e-9); // magenta and yellow print red
        Assert.Equal(Rgb.Black, Inks.Back(1, 1, 1, false));
        Assert.Equal(Rgb.White, Inks.Back(0, 0, 0, true));
        var real = Inks.Back(1, 1, 1, true);
        Assert.Equal("Colour_Name_Brown", ColourNames.Key(real));
        Assert.InRange(Inks.Brightness(real), 0.1, 0.14); // "about an eighth of the light"
    }

    [Fact]
    public void Number128_GivesAboutAFifthOfTheLight_ButLooksAboutHalf()
    {
        Assert.Equal(0, Numbers.Light(0), 9);
        Assert.Equal(1, Numbers.Light(255), 9);
        Assert.InRange(Numbers.Light(128), 0.2, 0.23); // "about a fifth"
        Assert.InRange(Numbers.Looks(Numbers.Light(128)), 0.5, 0.56); // "roughly half-way"
        Assert.InRange(Numbers.Number(0.5), 186, 190); // "near 188"
        Assert.Equal(128, Numbers.Number(Numbers.Light(128)), 6);
        Assert.Equal(3, Numbers.DarkNumbers(false)); // "only 3 would be left"
        Assert.InRange(Numbers.DarkNumbers(true), 25, 30);
    }

    [Fact]
    public void MixingNumbers_ComesOutTooDark_MixingLightDoesNot()
    {
        Assert.InRange(Blend.StripesMatch, 186, 190);
        var byNumbers = Blend.Mix(Blend.From, Blend.To, 0.5, false);
        var inLight = Blend.Mix(Blend.From, Blend.To, 0.5, true);
        Assert.Equal("Colour_Name_Olive", ColourNames.Key(byNumbers));
        Assert.Equal("Colour_Name_Yellow", ColourNames.Key(inLight));
        Assert.True(inLight.R > byNumbers.R + 0.2);
        Assert.True(Blend.Mix(Blend.From, Blend.To, 0, true).Distance(Blend.From) < 1e-9);
    }

    [Fact]
    public void TwoHalfClearPanes_HideThreeQuarters_WhicheverIsInFront()
    {
        Assert.Equal(0.75, Panes.Hidden(0.5, 0.5), 9);
        Assert.Equal(Panes.Hidden(0.3, 0.8), Panes.Hidden(0.8, 0.3), 9);
        Assert.Equal(1, Panes.Hidden(1, 0.2), 9);
        Assert.True(Panes.Overlap(Rgb.White, 0.5, 0.5, true).Distance(Panes.Overlap(Rgb.White, 0.5, 0.5, false)) > 0.1); // the colour does depend on order
        Assert.Equal(Panes.Red, Panes.Over(Panes.Red, 1, Rgb.White));
    }

    [Fact]
    public void TheHiddenColour_LeaksIntoTheEdge_UnlessColoursArePremultiplied()
    {
        var dark = Rgb.Grey(0.15);
        Assert.Equal(1, Halo.Verdict(Rgb.White, dark, false)); // a pale ring
        Assert.Equal(-1, Halo.Verdict(Rgb.Black, Rgb.Grey(0.9), false)); // a dark ring on a light background
        Assert.InRange(Halo.Fringe(Rgb.White, dark, false), 0.2, 0.25);
        Assert.Equal(0, Halo.Fringe(Rgb.White, dark, true), 9);
        Assert.Equal(0, Halo.Fringe(Rgb.Black, Rgb.Grey(0.9), true), 9);
        Assert.Equal(0, Halo.Fringe(Halo.Sticker, dark, false), 9); // filled with the edge colour, no ring either
        Assert.True(Halo.Edge(0, Rgb.White, dark, false).Distance(Halo.Sticker) < 1e-9);
        Assert.True(Halo.Edge(1, Rgb.White, dark, true).Distance(dark) < 1e-9);

        // The middle of the enlarged sticker is solid red, its corner is the background.
        var middle = Halo.Enlarged(Halo.Size * HaloDiagram.Scale / 2, Halo.Size * HaloDiagram.Scale / 2, HaloDiagram.Scale, Rgb.White, false);
        Assert.Equal(1, middle.Opacity, 9);
        Assert.Equal(0, Halo.Enlarged(0, 0, HaloDiagram.Scale, Rgb.White, false).Opacity, 9);
    }

    [Fact]
    public void TheSpreads_SayWhatTheirPicturesShow()
    {
        var colour = new ColourViewModel();
        Assert.Equal(BookContents.Chapters.Single(c => c.Slug == "Colour").Spreads, colour.Spreads.Count);
        foreach (var spread in colour.Spreads)
        {
            Assert.False(string.IsNullOrWhiteSpace((string?)spread.GetType().GetProperty("ReadoutText")!.GetValue(spread)));
        }

        colour.Blend.Grey = 188;
        Assert.True(colour.Blend.Matches);
        colour.Halo.Premultiplied = true;
        Assert.True(colour.Halo.Fringe < 0.01);
        colour.Inks.Real = true;
        Assert.Contains(LocalizedName("Colour_Name_Brown"), colour.Inks.ReadoutText);
    }

    [Fact]
    public void EverySpreadsWords_AndColourNames_AreInBothLanguages()
    {
        foreach (var keys in BothLanguages())
        {
            foreach (var key in new ColourViewModel().Spreads.SelectMany(s => s.TextKeys).Concat(ColourNames.All))
            {
                Assert.Contains(key, keys);
            }
        }
    }

    /// <summary>Every picture has grips, and dragging or tapping them moves the spread's own values (the sliders follow).</summary>
    [AvaloniaFact]
    public void ThePictures_AreWorkedByTheirGrips()
    {
        var (window, model) = OpenAt("Colour");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Colour");
        var colour = Assert.IsType<ColourTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Colour;
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

        var lamps = Open(0);
        lamps.DragGrip("blue", Far(lamps, "blue", 0, 2000));
        Assert.Equal(0, colour.Lamps.Blue, 9);
        Frames(2);
        lamps.DragGrip("green", Far(lamps, "green", 0, 2000));
        Assert.Equal(0, colour.Lamps.Green, 9);

        var inks = Open(1);
        inks.DragGrip("real", default);
        Assert.True(colour.Inks.Real);
        inks.DragGrip("magenta", Far(inks, "magenta", 0, 2000));
        Assert.Equal(0, colour.Inks.Magenta, 9);

        var steps = Open(2);
        steps.DragGrip("number", Far(steps, "number", 2000, 0));
        Assert.Equal(255, colour.Steps.Value, 9);

        var blend = Open(3);
        blend.DragGrip("grey", Far(blend, "grey", 0, -2000));
        Assert.Equal(255, colour.Blend.Grey, 9);
        blend.DragGrip("position", Far(blend, "position", -2000, 0));
        Assert.Equal(0, colour.Blend.Position, 9);

        var panes = Open(4);
        panes.DragGrip("swap", default);
        Assert.False(colour.Panes.BlueOnTop);
        panes.DragGrip("red", Far(panes, "red", 0, -2000));
        Assert.Equal(1, colour.Panes.Red, 9);

        var halo = Open(5);
        halo.DragGrip("switch", default);
        Assert.True(colour.Halo.Premultiplied);
        halo.DragGrip("hidden", Far(halo, "hidden", -2000, 0));
        Assert.Equal(0, colour.Halo.Hidden, 9);
        Frames(2);
        halo.DragGrip("backdrop", Far(halo, "backdrop", 2000, 0));
        Assert.Equal(1, colour.Halo.Backdrop, 9);

        window.Close();
    }

    [AvaloniaFact]
    public void EverySpread_DrawsItsPicture()
    {
        var (window, model) = OpenAt("Colour");
        var chapter = BookContents.Chapters.ToList().FindIndex(c => c.Slug == "Colour");
        Type[] pictures =
        [
            typeof(LampsDiagram), typeof(InksDiagram), typeof(StepsDiagram), typeof(BlendDiagram), typeof(PanesDiagram), typeof(HaloDiagram),
        ];
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(5);
            var picture = window.GetVisualDescendants().OfType<Control>().First(c => c.GetType() == pictures[s] && c.IsEffectivelyVisible);
            Assert.True(picture.Bounds.Width > 100, pictures[s].Name);
            Snapshot(window, $"colour-{s + 1}");
        }

        var colour = Assert.IsType<ColourTextPageViewModel>(model.Pages[BookContents.SpreadPage(chapter, 0)]).Colour;
        (colour.Lamps.Red, colour.Lamps.Green, colour.Lamps.Blue) = (1, 0.5, 0);
        colour.Inks.Real = true;
        colour.Steps.Value = 188;
        (colour.Blend.Grey, colour.Blend.Position) = (188, 0.8);
        (colour.Panes.BlueOnTop, colour.Panes.Red) = (false, 0.8);
        (colour.Halo.Hidden, colour.Halo.Backdrop) = (0, 0.9);
        for (var s = 0; s < pictures.Length; s++)
        {
            model.GoToPage(BookContents.SpreadPage(chapter, s));
            Frames(10);
            Snapshot(window, $"colour-{s + 1}-changed");
        }

        window.Close();
    }

    private static string LocalizedName(string key) => Lemon.Book.Shell.Infrastructures.Localization.LocalizationService.Instance.GetString(key);
}
