using Commis.Desktop.Records;

namespace Commis.Desktop.Tests;

public sealed class WindowSettingsTests : IDisposable
{
    private readonly DirectoryInfo root = Directory.CreateTempSubdirectory("commis-settings-");

    public void Dispose() => root.Delete(recursive: true);

    private WindowSettings Settings => new(Path.Combine(root.FullName, "Commis", "window.json"));

    [Fact]
    public void AFirstRunOpensOnCommissions() => Assert.Equal(Section.Commissions, Settings.LoadSection());

    [Fact]
    public void TheLastSectionComesBack()
    {
        Settings.SaveSection(Section.Ych);

        Assert.Equal(Section.Ych, Settings.LoadSection());
        Assert.Equal("{\"section\":\"ych\"}", File.ReadAllText(Settings.FilePath));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"section\":\"auctions\"}")]
    [InlineData("{\"section\":\"42\"}")]
    [InlineData("{}")]
    public void AnythingUnreadableOpensOnCommissions(string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Settings.FilePath)!);
        File.WriteAllText(Settings.FilePath, text);

        Assert.Equal(Section.Commissions, Settings.LoadSection());
    }

    [Fact]
    public void FieldsFromALaterVersionAreIgnored()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Settings.FilePath)!);
        File.WriteAllText(Settings.FilePath, "{\"section\":\"raffles\",\"width\":900}");

        Assert.Equal(Section.Raffles, Settings.LoadSection());
    }
}
