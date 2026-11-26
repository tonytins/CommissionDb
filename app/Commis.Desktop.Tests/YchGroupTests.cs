using Commis.Desktop.Records;

namespace Commis.Desktop.Tests;

public sealed class YchGroupTests
{
    [Fact]
    public void EntriesGatherUnderTheirRun()
    {
        var groups = Browser.Groups(
            [Ych(1, "Synthesize", 1), Ych(2, "Rooftop", 1), Ych(3, "Synthesize", 4)], null, Shipped.English);

        Assert.Equal(["Synthesize", "Rooftop"], groups.Select(group => group.Header.Title));
        Assert.Equal([1, 3], groups[0].Rows.Select(row => row.Id));
    }

    [Fact]
    public void RunsSortByTheirNewestEntry()
    {
        var groups = Browser.Groups(
        [
            Ych(1, "Old", 1, date: "2026-11-01T00:00:00Z"),
            Ych(2, "New", 1, date: "2026-11-10T00:00:00Z"),
            Ych(3, "Old", 2, date: "2026-11-20T00:00:00Z"),
        ], null, Shipped.English);

        Assert.Equal(["Old", "New"], groups.Select(group => group.Header.Title));
    }

    [Fact]
    public void SlotsComeInOrderAndSlotlessEntriesLast()
    {
        var groups = Browser.Groups(
            [Ych(1, "Run", null), Ych(2, "Run", 7), Ych(3, "Run", 2), Ych(4, "Run", null)], null, Shipped.English);

        Assert.Equal([3, 2, 1, 4], groups.Single().Rows.Select(row => row.Id));
        Assert.Equal(["#2", "#7", RecordRow.None, RecordRow.None], groups.Single().Rows.Select(row => row.SlotLabel));
    }

    [Fact]
    public void EntriesWithNoTitleStandUnderUntitled()
    {
        var groups = Browser.Groups([Ych(1, null, 1), Ych(2, "", 2)], null, Shipped.English);

        Assert.Equal("Untitled", groups.Single().Header.Title);
        Assert.Equal(2, groups.Single().Rows.Count);
    }

    [Fact]
    public void TheHeadingCountsItsEntries()
    {
        var groups = Browser.Groups([Ych(1, "A", 1), Ych(2, "B", 1), Ych(3, "B", 2)], null, Shipped.English);

        Assert.Equal(["1 entry", "2 entries"], groups.OrderBy(group => group.Header.Title).Select(group => group.Header.Count));
    }

    [Fact]
    public void TheFilterHidesRunsWithNothingUnderIt()
    {
        var groups = Browser.Groups(
            [Ych(1, "A", 1, "paid"), Ych(2, "B", 1, "pending"), Ych(3, "A", 2, "pending")], "paid", Shipped.English);

        var run = Assert.Single(groups);
        Assert.Equal("A", run.Header.Title);
        Assert.Equal("1 entry", run.Header.Count);
    }

    [Fact]
    public void CommissionsAreLeftOut() =>
        Assert.Empty(Browser.Groups(
            [new Record(1, "2026-11-01T00:00:00Z", "commission", "C", "", "", 1, "Run", null, "paid")],
            null,
            Shipped.English));

    private static Record Ych(int id, string? title, int? slot, string status = "pending", string date = "2026-11-01T00:00:00Z") =>
        new(id, date, "ych", $"Client {id}", "", "", 25, title, slot, status);
}
