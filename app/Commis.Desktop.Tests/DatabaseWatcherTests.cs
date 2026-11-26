namespace Commis.Desktop.Tests;

public sealed class DatabaseWatcherTests : IDisposable
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(5);

    private readonly DirectoryInfo root = Directory.CreateTempSubdirectory("commis-watcher-");

    public void Dispose() => root.Delete(recursive: true);

    [Fact]
    public async Task AWriteIsReported()
    {
        var database = Database();
        var changed = new TaskCompletionSource();
        using var watcher = new DatabaseWatcher(database, () => changed.TrySetResult());

        await File.AppendAllTextAsync(database, "more", TestContext.Current.CancellationToken);

        await changed.Task.WaitAsync(Patience, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ABurstOfWritesIsOneReport()
    {
        var database = Database();
        var reports = 0;
        using var watcher = new DatabaseWatcher(database, () => Interlocked.Increment(ref reports));

        for (var write = 0; write < 5; write++)
        {
            await File.AppendAllTextAsync(database, "more", TestContext.Current.CancellationToken);
        }

        await Task.Delay(DatabaseWatcher.Settle * 4, TestContext.Current.CancellationToken);
        Assert.Equal(1, reports);
    }

    [Fact]
    public async Task ChangesAlreadySeenAreNotReported()
    {
        var database = Database();
        var reports = 0;
        using var watcher = new DatabaseWatcher(database, () => Interlocked.Increment(ref reports));

        // As when the window ran a command itself and refreshed.
        await File.AppendAllTextAsync(database, "more", TestContext.Current.CancellationToken);
        watcher.MarkSeen();

        await Task.Delay(DatabaseWatcher.Settle * 4, TestContext.Current.CancellationToken);
        Assert.Equal(0, reports);
    }

    [Fact]
    public async Task OtherFilesInTheFolderAreIgnored()
    {
        var database = Database();
        var reports = 0;
        using var watcher = new DatabaseWatcher(database, () => Interlocked.Increment(ref reports));

        await File.WriteAllTextAsync(Path.Combine(root.FullName, "notes.txt"), "hi", TestContext.Current.CancellationToken);

        await Task.Delay(DatabaseWatcher.Settle * 4, TestContext.Current.CancellationToken);
        Assert.Equal(0, reports);
    }

    private string Database()
    {
        var path = Path.Combine(root.FullName, "commis.sqlite3");
        File.WriteAllText(path, "start");
        return path;
    }
}
