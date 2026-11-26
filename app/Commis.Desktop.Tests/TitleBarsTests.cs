using Commis.Desktop;

namespace Commis.Desktop.Tests;

public sealed class TitleBarsTests
{
    [Theory]
    [InlineData("GNOME")]
    [InlineData("KDE")]
    [InlineData("ubuntu:GNOME")]
    [InlineData("X-Cinnamon")]
    public void AFloatingDesktopMerges(string desktop) => Assert.True(TitleBars.Merges(desktop, null));

    [Theory]
    [InlineData("sway")]
    [InlineData("Hyprland")]
    [InlineData("niri")]
    [InlineData("GNOME:i3")]
    public void ATilingWindowManagerDoesNot(string desktop) => Assert.False(TitleBars.Merges(desktop, null));

    [Fact]
    public void AnUnnamedDesktopDoesNot() => Assert.False(TitleBars.Merges(null, null));

    [Fact]
    public void WslDoesNot() => Assert.False(TitleBars.Merges("GNOME", "Ubuntu"));
}
