using Commis.Desktop;

namespace Commis.Desktop.Tests;

public sealed class CommisClientTests : IDisposable
{
    private readonly DirectoryInfo root = Directory.CreateTempSubdirectory("commis-client-");

    public void Dispose() => root.Delete(recursive: true);

    [Fact]
    public void StandardErrorExplainsFirst()
    {
        var result = new CommandResult(64, "something else\n", "Error: Missing expected argument '--set <set>'\n");

        Assert.Equal("Error: Missing expected argument '--set <set>'", CommisClient.Explain(result));
    }

    [Fact]
    public void StandardOutputExplainsWhenStandardErrorIsEmpty()
    {
        // commis status prints this to standard output, then exits 1.
        var result = new CommandResult(1, "No record with ID 99.\n", "");

        Assert.Equal("No record with ID 99.", CommisClient.Explain(result));
    }

    [Fact]
    public void JsonIsNeverShownAsTheReason()
    {
        var result = new CommandResult(1, "{\"version\":1}", "");

        Assert.Equal("commis exited with 1.", CommisClient.Explain(result));
    }

    [Fact]
    public void AnUnreadableSuccessSaysSo()
    {
        var result = new CommandResult(0, "", "");

        Assert.Equal("commis printed an answer the window couldn't read.", CommisClient.Explain(result));
    }

    [Fact]
    public async Task InfoIsRead()
    {
        var client = new CommisClient(Script("""
            printf '%s' '{"commis":"0.1.110","database":"\/data\/commis.sqlite3","records":2,"schema":1,"version":1}'
            """));

        var info = await client.ReadInfoAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new Info(1, "0.1.110", "/data/commis.sqlite3", 1, 2), info);
    }

    [Fact]
    public async Task UnknownFieldsAreIgnored()
    {
        var client = new CommisClient(Script("""
            printf '%s' '{"commis":"0.2.100","database":"/d","records":0,"schema":2,"version":1,"later":true}'
            """));

        var info = await client.ReadInfoAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, info.Records);
    }

    [Fact]
    public async Task RecordsAreReadWithNullTitlesAndSlots()
    {
        var client = new CommisClient(Script("""
            printf '%s' '{"version":1,"records":[{"id":1,"date":"2026-11-26T10:00:00Z","type":"commission","client":"Bessie","reference":"r","description":"","fee":25.5,"title":null,"slot":null,"status":"paid"}]}'
            """));

        var records = await client.ListRecordsAsync(cancellationToken: TestContext.Current.CancellationToken);

        var record = Assert.Single(records);
        Assert.Equal("Bessie", record.Client);
        Assert.Equal(25.5, record.Fee);
        Assert.Null(record.Title);
        Assert.Null(record.Slot);
    }

    [Fact]
    public async Task FiltersArePassedAsFlags()
    {
        var client = new CommisClient(Script("""
            printf '%s\n' "$@" >&2
            exit 1
            """));

        var problem = await Assert.ThrowsAsync<CommisException>(() =>
            client.ListRecordsAsync("paid", "ych", TestContext.Current.CancellationToken));

        Assert.Equal("list\n--json\n--status\npaid\n--type\nych", problem.Message);
    }

    [Fact]
    public async Task AFailureCarriesTheSentenceCommisWrote()
    {
        var client = new CommisClient(Script("""
            echo "Couldn't open the database: disk I/O error" >&2
            exit 1
            """));

        var problem = await Assert.ThrowsAsync<CommisException>(() =>
            client.ReadInfoAsync(TestContext.Current.CancellationToken));

        Assert.Equal("Couldn't open the database: disk I/O error", problem.Message);
    }

    [Fact]
    public async Task StatusIsSetWithTheCommandsFlags()
    {
        var log = Path.Combine(root.FullName, "arguments");
        var client = new CommisClient(Script($"""
            printf '%s\n' "$@" > '{log}'
            echo 'Record #7: pending → paid.'
            """));

        await client.SetStatusAsync(7, "paid", TestContext.Current.CancellationToken);

        Assert.Equal("status\n7\n--set\npaid\n", await File.ReadAllTextAsync(log, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ARefusedStatusChangeCarriesCommissSentence()
    {
        // As commis does it: the sentence on standard output, then exit 1.
        var client = new CommisClient(Script("""
            echo 'No record with ID 99.'
            exit 1
            """));

        var problem = await Assert.ThrowsAsync<CommisException>(() =>
            client.SetStatusAsync(99, "paid", TestContext.Current.CancellationToken));

        Assert.Equal("No record with ID 99.", problem.Message);
    }

    [Fact]
    public async Task AMissingExecutableSaysWhere()
    {
        var missing = Path.Combine(root.FullName, "nothing-here");
        var client = new CommisClient(missing);

        var problem = await Assert.ThrowsAsync<CommisException>(() =>
            client.ReadInfoAsync(TestContext.Current.CancellationToken));

        Assert.StartsWith($"Couldn't start commis at {missing}: ", problem.Message);
        Assert.DoesNotContain("An error occurred", problem.Message);
    }

    // A stand-in for commis, written as a shell script, so the tests don't need the Swift build.
    private string Script(string body)
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("The stand-in executable is a shell script.");
            return string.Empty;
        }

        var path = Path.Combine(root.FullName, $"commis-{Guid.NewGuid():N}");
        File.WriteAllText(path, $"#!/bin/sh\n{body}\n");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return path;
    }
}
