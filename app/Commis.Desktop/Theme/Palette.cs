using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Themes.Fluent;

namespace Commis.Desktop.Theme;

/// <summary>
/// The value behind every role, on one palette. The window draws with the roles, never with these colors, so the look
/// can change in one place. <see cref="Light"/> and <see cref="Dark"/> are the two palettes Commis offers.
/// </summary>
/// <remarks>
/// Surfaces, text, and lines are neutral, after libadwaita, and only the accent carries color. The accent is blue:
/// <c>#1C71D8</c> on light and <c>#3584E4</c> on dark, libadwaita's own pair, so white text on either reaches 3 to 1.
/// </remarks>
internal sealed record Palette(
    Color Panel,
    Color TitleBar,
    Color Sidebar,
    Color InputBackground,
    Color Text,
    Color SecondaryText,
    Color DisabledText,
    Color HighlightedText,
    Color Accent,
    Color Border,
    Color Selection,
    Color Error)
{
    /// <summary>The light palette, in the family's neutrals with a blue accent.</summary>
    internal static Palette Light { get; } = new(
        Panel: Color.Parse("#F6F5F4"),
        TitleBar: Color.Parse("#EDECEA"),
        Sidebar: Color.Parse("#F1F0EE"),
        InputBackground: Color.Parse("#FFFFFF"),
        Text: Color.Parse("#2E3436"),
        SecondaryText: Color.Parse("#5E5C64"),
        DisabledText: Color.Parse("#9A9996"),
        HighlightedText: Color.Parse("#FFFFFF"),
        Accent: Color.Parse("#1C71D8"),
        Border: Color.Parse("#D4D2CF"),
        Selection: Color.Parse("#D6E4F7"),
        Error: Color.Parse("#C01C28"));

    /// <summary>The dark palette, the same roles a step down.</summary>
    internal static Palette Dark { get; } = new(
        Panel: Color.Parse("#242424"),
        TitleBar: Color.Parse("#1E1E1E"),
        Sidebar: Color.Parse("#1F1F1F"),
        InputBackground: Color.Parse("#1A1A1A"),
        Text: Color.Parse("#FFFFFF"),
        SecondaryText: Color.Parse("#B4B2AF"),
        DisabledText: Color.Parse("#6E6D6A"),
        HighlightedText: Color.Parse("#FFFFFF"),
        Accent: Color.Parse("#3584E4"),
        Border: Color.Parse("#3A3A3A"),
        Selection: Color.Parse("#1B3A5E"),
        Error: Color.Parse("#FF7B72"));

    /// <summary>The brushes the window's styles draw with.</summary>
    internal ResourceDictionary ToResources() => new()
    {
        ["PanelBrush"] = Brush(Panel),
        ["TitleBarBrush"] = Brush(TitleBar),
        ["SidebarBrush"] = Brush(Sidebar),
        ["InputBackgroundBrush"] = Brush(InputBackground),
        ["TextBrush"] = Brush(Text),
        ["SecondaryTextBrush"] = Brush(SecondaryText),
        ["DisabledTextBrush"] = Brush(DisabledText),
        ["HighlightedTextBrush"] = Brush(HighlightedText),
        ["AccentBrush"] = Brush(Accent),
        ["BorderBrush"] = Brush(Border),
        ["SelectionBrush"] = Brush(Selection),
        ["ErrorBrush"] = Brush(Error),
    };

    /// <summary>Fluent's palette, filled from the same values, so its controls match the roles.</summary>
    internal ColorPaletteResources ToFluent() => new()
    {
        Accent = Accent,
        RegionColor = Panel,
        AltHigh = InputBackground,
        AltMediumHigh = InputBackground,
        AltMedium = InputBackground,
        AltMediumLow = InputBackground,
        AltLow = InputBackground,
        BaseHigh = Text,
        BaseMediumHigh = Text,
        BaseMedium = SecondaryText,
        BaseMediumLow = DisabledText,
        BaseLow = Border,
        ChromeMedium = TitleBar,
        ChromeMediumLow = TitleBar,
        ChromeLow = Panel,
        ChromeHigh = Border,
        ChromeAltLow = Text,
        ListLow = Sidebar,
        ListMedium = Selection,
        ErrorText = Error,
    };

    private static ImmutableSolidColorBrush Brush(Color color) => new(color);
}
