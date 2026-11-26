namespace Commis.Desktop;

/// <summary>
/// Tells the window when the database changed under it, such as after a command in a terminal. Events are gathered
/// for a moment so a burst of writes is one refresh, and a change is only reported when the files actually differ
/// from the last look, so the window's own reads can never set it off.
/// </summary>
internal sealed class DatabaseWatcher : IDisposable
{
    /// <summary>How long the watcher waits for writes to settle before reporting.</summary>
    internal static readonly TimeSpan Settle = TimeSpan.FromMilliseconds(300);

    private readonly string database;
    private readonly FileSystemWatcher watcher;
    private readonly Lock gate = new();
    private Timer? timer;
    private Fingerprint seen;

    /// <summary>Starts watching <paramref name="database"/> and its journal files.</summary>
    /// <param name="database">The database file, as <c>commis info</c> names it.</param>
    /// <param name="changed">Called, off the UI thread, when the files differ from the last look.</param>
    internal DatabaseWatcher(string database, Action changed)
    {
        this.database = database;
        Changed = changed;
        seen = Fingerprint.Of(database);

        watcher = new FileSystemWatcher(Path.GetDirectoryName(database)!, Path.GetFileName(database) + "*")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
        };
        watcher.Changed += OnEvent;
        watcher.Created += OnEvent;
        watcher.Deleted += OnEvent;
        watcher.Renamed += OnEvent;
        watcher.EnableRaisingEvents = true;
    }

    private Action Changed { get; }

    /// <summary>Takes the files as they are now as seen, such as after the window's own command and refresh.</summary>
    internal void MarkSeen()
    {
        lock (gate)
        {
            seen = Fingerprint.Of(database);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        watcher.Dispose();
        lock (gate)
        {
            timer?.Dispose();
            timer = null;
        }
    }

    private void OnEvent(object sender, FileSystemEventArgs e)
    {
        lock (gate)
        {
            timer ??= new Timer(_ => Report());
            timer.Change(Settle, Timeout.InfiniteTimeSpan);
        }
    }

    private void Report()
    {
        lock (gate)
        {
            var now = Fingerprint.Of(database);
            if (now == seen)
            {
                return;
            }

            seen = now;
        }

        Changed();
    }

    /// <summary>The size and write time of the database and its write-ahead log, which change on every write.</summary>
    internal readonly record struct Fingerprint(long Length, DateTime Written, long LogLength, DateTime LogWritten)
    {
        internal static Fingerprint Of(string database)
        {
            var file = new FileInfo(database);
            var log = new FileInfo(database + "-wal");
            return new Fingerprint(
                file.Exists ? file.Length : -1,
                file.Exists ? file.LastWriteTimeUtc : default,
                log.Exists ? log.Length : -1,
                log.Exists ? log.LastWriteTimeUtc : default);
        }
    }
}
