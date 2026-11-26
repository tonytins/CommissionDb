using System.Globalization;
using System.Reflection;
using Commis.Desktop.Records;
using CSTNet;

namespace Commis.Desktop.Tests;

public sealed class InterfaceTextTests
{
    public static TheoryData<string> AllKeys => new(
        typeof(TextKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(constant => constant.IsLiteral)
            .Select(constant => (string)constant.GetRawConstantValue()!));

    [Theory]
    [MemberData(nameof(AllKeys))]
    public void EnglishHasEveryKey(string key) => Assert.True(Shipped.English.Has(key), $"English lacks '{key}'.");

    [Fact]
    public void EveryStatusHasAName()
    {
        foreach (var status in Statuses.All)
        {
            Assert.True(Shipped.English.Has(TextKeys.Status(status)), $"English lacks a name for '{status}'.");
        }
    }

    [Fact]
    public void TheEnglishTableReadsWithoutProblems()
    {
        var directory = CSTDirectory.Read(Path.Combine(Shipped.Folder, "english.dir"));

        Assert.Empty(directory.Problems);
        Assert.True(directory.TryGetTable(InterfaceText.TableId, out var table));
        Assert.Empty(table.Problems);
    }

    [Fact]
    public void PlaceholdersAreFilledInOrder() =>
        Assert.Equal(
            "/data/commis.sqlite3 · schema 1 · commis 0.1.110",
            Shipped.English.Get(TextKeys.DatabaseLine, "/data/commis.sqlite3", 1, "0.1.110"));

    [Fact]
    public void AStatusANewerCommisAddsShowsAsSpelled() => Assert.Equal("on hold", Statuses.Display("on hold", Shipped.English));

    [Theory]
    [InlineData("fr-FR", "french")]
    [InlineData("fr", "french")]
    [InlineData("en-US", "english")]
    [InlineData("", "english")]
    public void LanguagesAreNamedAsTheirFoldersAre(string culture, string expected) =>
        Assert.Equal(expected, InterfaceText.LanguageName(new CultureInfo(culture)));

    [Fact]
    public void TranslationsFallBackToEnglish()
    {
        using var folder = new TempTables();
        folder.Write("english", "greeting ^Hello, %s!^\nfarewell ^Goodbye^\nempty ^Not empty in English^");
        folder.Write("french", "greeting ^Bonjour, %s !^\nempty ^^");

        var text = new InterfaceText(folder.Path, new CultureInfo("fr-FR"));

        Assert.Equal("Bonjour, Alberta !", text.Get("greeting", "Alberta"));
        Assert.Equal("Goodbye", text.Get("farewell"));
        Assert.Equal("Not empty in English", text.Get("empty"));
    }

    [Fact]
    public void AMissingStringShowsItsKey()
    {
        using var folder = new TempTables();
        folder.Write("english", "greeting ^Hello^");

        var text = new InterfaceText(folder.Path, CultureInfo.InvariantCulture);

        Assert.Equal("no such key", text.Get("no such key"));
        Assert.False(text.Has("no such key"));
    }

    [Fact]
    public void NoTablesAtAllStillWorks()
    {
        var text = new InterfaceText(Path.Combine(Path.GetTempPath(), "no-such-uitext"), new CultureInfo("de-DE"));

        Assert.Equal(TextKeys.AppTitle, text.Get(TextKeys.AppTitle));
        Assert.Equal("german", text.Language);
    }

    private sealed class TempTables : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("commis-uitext-").FullName;

        public void Write(string language, string table)
        {
            var folder = System.IO.Path.Combine(Path, language + ".dir");
            Directory.CreateDirectory(folder);
            File.WriteAllText(System.IO.Path.Combine(folder, "_100_commis.cst"), table);
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
