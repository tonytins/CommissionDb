using System.Diagnostics;
using System.Text.Json;
using Commis.Desktop.Records;

namespace Commis.Desktop;

/// <summary>
/// What the window remembers between launches: only the section it was last on. Kept apart from the database, which
/// belongs to commis.
/// </summary>
/// <param name="path">The settings file.</param>
internal sealed class WindowSettings(string path)
{
    /// <summary>The settings file for this user: <c>Commis/window.json</c> in the platform's settings folder.</summary>
    internal static string DefaultPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Commis", "window.json");

    internal string FilePath { get; } = path;

    /// <summary>
    /// The section to open on. A missing or unreadable file, or a section this version doesn't know, opens on
    /// Commissions; the window shouldn't refuse to start over a preference.
    /// </summary>
    internal Section LoadSection()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return Section.Commissions;
            }

            var saved = JsonSerializer.Deserialize<Saved>(File.ReadAllText(FilePath), Contract.Json);
            return Enum.TryParse<Section>(saved?.Section, ignoreCase: true, out var section)
                   && Enum.IsDefined(section)
                ? section
                : Section.Commissions;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            Trace.TraceWarning($"Couldn't read {FilePath}, so the window opens on Commissions: {exception.Message}");
            return Section.Commissions;
        }
    }

    /// <summary>Remembers <paramref name="section"/>. A failure is logged, not shown: the window still works.</summary>
    internal void SaveSection(Section section)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(
                FilePath,
                JsonSerializer.Serialize(new Saved(section.ToString().ToLowerInvariant()), JsonSerializerOptions.Web));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"Couldn't save the last section to {FilePath}: {exception.Message}");
        }
    }

    private sealed record Saved(string? Section);
}
