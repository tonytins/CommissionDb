namespace Commis.Desktop;

/// <summary>
/// Finds the <c>commis</c> executable, which does all the reading and writing. Adapted from Depot's
/// <c>RendererLocation</c>, which looks for <c>depot-render</c> the same way.
/// </summary>
internal static class CommisLocation
{
    /// <summary>The environment variable that names commis, ahead of anywhere the window looks.</summary>
    internal const string Variable = "COMMIS";

    /// <summary>The program's file name on this system.</summary>
    internal static string FileName => OperatingSystem.IsWindows() ? "commis.exe" : "commis";

    // How far up from the app's own folder a development build is looked for:
    // app/Commis.Desktop/bin/Debug/net10.0 is five folders below the repository root.
    private const int FoldersUp = 8;

    /// <summary>
    /// Where commis is: <see cref="Variable"/> when it's set; beside the window, as a package puts it; a SwiftPM build
    /// in a Commis checkout the window runs from, the newer of its release and debug builds; or on the <c>PATH</c>.
    /// </summary>
    /// <returns>The path, or <see langword="null"/> when it's nowhere.</returns>
    internal static string? Find(string? named, string baseDirectory, string? searchPath)
    {
        if (!string.IsNullOrWhiteSpace(named))
        {
            return named;
        }

        var beside = Path.Combine(baseDirectory, FileName);
        if (File.Exists(beside))
        {
            return beside;
        }

        var folder = new DirectoryInfo(baseDirectory);
        for (var up = 0; folder is not null && up < FoldersUp; up++, folder = folder.Parent)
        {
            var built = new[] { "release", "debug" }
                .Select(configuration => new FileInfo(Path.Combine(folder.FullName, ".build", configuration, FileName)))
                .Where(file => file.Exists)
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault();
            if (built is not null)
            {
                return built.FullName;
            }
        }

        foreach (var entry in (searchPath ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var onPath = Path.Combine(entry, FileName);
            if (File.Exists(onPath))
            {
                return onPath;
            }
        }

        return null;
    }

    /// <summary>Where commis is for this process, from its environment.</summary>
    internal static string? Find() =>
        Find(Environment.GetEnvironmentVariable(Variable), AppContext.BaseDirectory, Environment.GetEnvironmentVariable("PATH"));
}
