using Commis.Desktop.Records;

namespace Commis.Desktop.Tests;

public sealed class ExportTests
{
    [Theory]
    [InlineData("Commissions", null, "commissions.csv")]
    [InlineData("Commissions", "paid", "commissions-paid.csv")]
    [InlineData("Ych", null, "ych.csv")]
    [InlineData("Ych", "refunded", "ych-refunded.csv")]
    public void TheSuggestedNameSaysWhatsInIt(string section, string? status, string expected) =>
        Assert.Equal(expected, Exports.FileName(Enum.Parse<Section>(section), status, Shipped.English));
}
