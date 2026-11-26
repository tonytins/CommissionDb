using System.Globalization;
using Commis.Desktop.Records;

namespace Commis.Desktop.Tests;

public sealed class DraftTests
{
    [Fact]
    public void ACommissionReads()
    {
        var check = Check(Section.Commissions, "  Alberta ", "https://example.com", "43", slot: "9", title: "Ignored");

        Assert.Equal(new NewRecord("Alberta", "https://example.com", "", 43m, null, null), check.Record);
        Assert.Null(check.Problem);
    }

    [Fact]
    public void AYchEntryReadsWithItsSlotAndTitle()
    {
        var check = Check(Section.Ych, "Bessie", "ref", "25.50", slot: "4", title: " Synthesize ");

        Assert.Equal(new NewRecord("Bessie", "ref", "", 25.50m, "Synthesize", 4), check.Record);
    }

    [Fact]
    public void AYchTitleIsOptional() => Assert.Null(Check(Section.Ych, "B", "r", "1", slot: "1").Record!.Title);

    [Fact]
    public void AYchEntryNeedsASlot()
    {
        var check = Check(Section.Ych, "Bessie", "ref", "25", slot: " ");

        Assert.Null(check.Record);
        Assert.Equal("A YCH entry needs a slot number.", check.Problem);
        Assert.Equal(DraftField.Slot, check.Field);
        Assert.True(check.Missing);
    }

    [Theory]
    [InlineData("", "r", "1", "Client")]
    [InlineData("c", "", "1", "Reference")]
    [InlineData("c", "r", "", "Fee")]
    public void BlankRequiredFieldsAreMissing(string client, string reference, string fee, string field)
    {
        var check = Check(Section.Commissions, client, reference, fee);

        Assert.Equal(Enum.Parse<DraftField>(field), check.Field);
        Assert.True(check.Missing);
    }

    [Fact]
    public void AFeeThatIsntANumberSaysSo()
    {
        var check = Check(Section.Commissions, "c", "r", "twenty");

        Assert.Equal("The fee needs to be a number, such as 25 or 25.50.", check.Problem);
        Assert.False(check.Missing);
    }

    [Fact]
    public void ASlotThatIsntAWholeNumberSaysSo()
    {
        var check = Check(Section.Ych, "c", "r", "1", slot: "4.5");

        Assert.Equal(DraftField.Slot, check.Field);
        Assert.False(check.Missing);
    }

    [Fact]
    public void ANegativeFeeIsLeftForCommisToRefuse() =>
        Assert.Equal(-5m, Check(Section.Commissions, "c", "r", "-5").Record!.Fee);

    [Fact]
    public void AFeeReadsTheWayThePersonsLanguageWritesIt()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            Assert.Equal(25.5m, Check(Section.Commissions, "c", "r", "25,50").Record!.Fee);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    private static DraftCheck Check(
        Section section, string client, string reference, string fee, string slot = "", string title = "") =>
        Draft.Check(section, client, reference, fee, slot, title, "", Shipped.English);
}
