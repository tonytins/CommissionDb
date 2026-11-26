using System.Globalization;

namespace Commis.Desktop.Records;

/// <summary>
/// Decides what the window shows from one <c>commis list --json</c>: the sidebar's counts, a section's rows under a
/// status filter, and what an empty list says. The window filters here rather than asking commis again, so a refresh
/// is always one command.
/// </summary>
internal static class Browser
{
    /// <summary>How many records a section holds, whatever the filter, or null for a section with no count.</summary>
    internal static int? Count(IEnumerable<Record> records, Section section) =>
        section.RecordType() is { } type ? records.Count(record => record.Type == type) : null;

    /// <summary>
    /// The section's records with <paramref name="status"/>, or all of them when it's null, newest first.
    /// </summary>
    internal static IReadOnlyList<RecordRow> Rows(
        IEnumerable<Record> records,
        Section section,
        string? status,
        InterfaceText text)
    {
        if (section.RecordType() is not { } type)
        {
            return [];
        }

        return records
            .Where(record => record.Type == type && (status is null || record.Status == status))
            .OrderByDescending(record => record.Date, StringComparer.Ordinal)
            .ThenByDescending(record => record.Id)
            .Select(record => new RecordRow(record, text))
            .ToList();
    }

    /// <summary>
    /// The YCH entries with <paramref name="status"/>, or all of them when it's null, gathered into runs by title.
    /// Runs sort by their newest entry; within a run, entries sort by slot, and entries with no slot come last. Entries
    /// with no title stand together under "Untitled". A run with nothing under the filter isn't shown.
    /// </summary>
    internal static IReadOnlyList<YchGroup> Groups(IEnumerable<Record> records, string? status, InterfaceText text) =>
        records
            .Where(record => record.Type == "ych" && (status is null || record.Status == status))
            .GroupBy(record => string.IsNullOrEmpty(record.Title) ? null : record.Title)
            .OrderByDescending(run => run.Max(record => record.Date), StringComparer.Ordinal)
            .ThenByDescending(run => run.Max(record => record.Id))
            .Select(run =>
            {
                var rows = run
                    .OrderBy(record => record.Slot is null)
                    .ThenBy(record => record.Slot)
                    .ThenBy(record => record.Id)
                    .Select(record => new RecordRow(record, text))
                    .ToList();
                var title = run.Key ?? text.Get(TextKeys.YchUntitled);
                var count = rows.Count == 1 ? text.Get(TextKeys.YchOneEntry) : text.Get(TextKeys.YchEntries, rows.Count);
                return new YchGroup(new YchHeader(title, count), rows);
            })
            .ToList();

    /// <summary>What an empty list says, naming the filter it's under.</summary>
    internal static string Empty(Section section, string? status, InterfaceText text)
    {
        var ych = section == Section.Ych;
        if (status is null)
        {
            return text.Get(ych ? TextKeys.EmptyYch : TextKeys.EmptyCommissions);
        }

        // The status reads mid-sentence, so in lower case: "No paid commissions."
        var name = Statuses.Display(status, text).ToLower(CultureInfo.CurrentCulture);
        return text.Get(ych ? TextKeys.EmptyYchFiltered : TextKeys.EmptyCommissionsFiltered, name);
    }
}
