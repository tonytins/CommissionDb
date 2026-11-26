using Avalonia.Media;
using Commis.Desktop.Theme;

namespace Commis.Desktop.Tests;

public sealed class PaletteTests
{
    // The design language asks for 3 to 1 between text and what it sits on. Disabled text is exempt, as in WCAG.
    private const double Minimum = 3.0;

    public static TheoryData<string> Palettes => ["light", "dark"];

    [Theory]
    [MemberData(nameof(Palettes))]
    public void TextReadsOnEverySurface(string name)
    {
        var palette = Named(name);
        foreach (var surface in Surfaces(palette))
        {
            AssertReads(palette.Text, surface);
            AssertReads(palette.SecondaryText, surface);
        }
    }

    [Theory]
    [MemberData(nameof(Palettes))]
    public void HighlightedTextReadsOnTheAccent(string name)
    {
        var palette = Named(name);

        AssertReads(palette.HighlightedText, palette.Accent);
    }

    [Theory]
    [MemberData(nameof(Palettes))]
    public void ErrorsReadOnThePanel(string name)
    {
        var palette = Named(name);

        AssertReads(palette.Error, palette.Panel);
    }

    [Theory]
    [MemberData(nameof(Palettes))]
    public void TextReadsOnASelection(string name)
    {
        var palette = Named(name);

        AssertReads(palette.Text, palette.Selection);
    }

    [Fact]
    public void BlackOnWhiteIsTwentyOne() =>
        Assert.Equal(21.0, Contrast.Ratio(Colors.Black, Colors.White), precision: 6);

    private static Palette Named(string name) => name == "light" ? Palette.Light : Palette.Dark;

    private static Color[] Surfaces(Palette palette) =>
        [palette.Panel, palette.TitleBar, palette.Sidebar, palette.InputBackground];

    private static void AssertReads(Color text, Color background)
    {
        var ratio = Contrast.Ratio(text, background);
        Assert.True(ratio >= Minimum, $"{text} on {background} is {ratio:0.00} to 1, under {Minimum} to 1.");
    }
}
