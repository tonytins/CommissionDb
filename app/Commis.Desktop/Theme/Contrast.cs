using Avalonia.Media;

namespace Commis.Desktop.Theme;

/// <summary>
/// The contrast between two colors, as WCAG measures it, so a test can hold the palettes to the design language's
/// 3 to 1 for text.
/// </summary>
internal static class Contrast
{
    /// <summary>How far apart two colors are, from 1 (the same) to 21 (black on white).</summary>
    internal static double Ratio(Color first, Color second)
    {
        var one = Luminance(first);
        var other = Luminance(second);
        return (Math.Max(one, other) + 0.05) / (Math.Min(one, other) + 0.05);
    }

    // Relative luminance, per WCAG 2.2.
    private static double Luminance(Color color) =>
        0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);

    private static double Channel(byte value)
    {
        var channel = value / 255.0;
        return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
