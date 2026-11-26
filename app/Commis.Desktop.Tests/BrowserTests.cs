using Commis.Desktop.Records;

namespace Commis.Desktop.Tests;

public sealed class BrowserTests
{
    private static readonly Record[] Records =
    [
        Make(1, "2026-11-20T10:00:00Z", "commission", "pending"),
        Make(2, "2026-11-22T10:00:00Z", "ych", "paid", title: "Synthesize", slot: 4),
        Make(3, "2026-11-21T10:00:00Z", "commission", "paid"),
        Make(4, "2026-11-21T10:00:00Z", "commission", "pending"),
    ];

    [Fact]
    public void CountsAreTotalsPerSection()
    {
        Assert.Equal(3, Browser.Count(Records, Section.Commissions));
        Assert.Equal(1, Browser.Count(Records, Section.Ych));
    }

    [Fact]
    public void RafflesHaveNoCount() => Assert.Null(Browser.Count(Records, Section.Raffles));

    [Fact]
    public void ASectionShowsOnlyItsTypeNewestFirst()
    {
        var rows = Browser.Rows(Records, Section.Commissions, null, Shipped.English);

        // 3 and 4 share a date, so the later ID comes first.
        Assert.Equal([4, 3, 1], rows.Select(row => row.Id));
    }

    [Fact]
    public void TheFilterNarrowsByStatus()
    {
        var rows = Browser.Rows(Records, Section.Commissions, "pending", Shipped.English);

        Assert.Equal([4, 1], rows.Select(row => row.Id));
    }

    [Fact]
    public void RafflesShowNoRows() => Assert.Empty(Browser.Rows(Records, Section.Raffles, null, Shipped.English));

    [Theory]
    [InlineData("Commissions", null, "No commissions yet.")]
    [InlineData("Commissions", "refunded", "No refunded commissions.")]
    [InlineData("Ych", null, "No YCH entries yet.")]
    [InlineData("Ych", "paid", "No paid YCH entries.")]
    public void AnEmptyListNamesItsFilter(string section, string? status, string expected) =>
        Assert.Equal(expected, Browser.Empty(Enum.Parse<Section>(section), status, Shipped.English));

    [Fact]
    public void AbsentValuesShowAsADash()
    {
        var row = new RecordRow(Make(1, "2026-11-20T10:00:00Z", "commission", "pending"), Shipped.English);

        Assert.Equal(RecordRow.None, row.Title);
        Assert.Equal(RecordRow.None, row.Slot);
        Assert.Equal(RecordRow.None, row.Description);
    }

    [Fact]
    public void StatusesReadAsWords() => Assert.Equal("Cancelled", new RecordRow(Make(1, "x", "ych", "cancelled"), Shipped.English).Status);

    [Fact]
    public void ADateThatIsntOneShowsAsStored() => Assert.Equal("someday", new RecordRow(Make(1, "someday", "ych", "paid"), Shipped.English).Date);

    private static Record Make(int id, string date, string type, string status, string? title = null, int? slot = null) =>
        new(id, date, type, $"Client {id}", "", "", 25, title, slot, status);
}
