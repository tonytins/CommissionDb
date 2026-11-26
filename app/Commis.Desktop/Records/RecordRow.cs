using System.Globalization;

namespace Commis.Desktop.Records;

/// <summary>A record as the list and the detail pane show it.</summary>
internal sealed class RecordRow(Record record)
{
    /// <summary>Shown where a record has no value.</summary>
    internal const string None = "—";

    /// <summary>The record from <c>commis list --json</c>.</summary>
    public Record Record { get; } = record;

    public int Id => Record.Id;

    public string Client => Record.Client;

    public string Title => string.IsNullOrEmpty(Record.Title) ? None : Record.Title;

    public string Slot => Record.Slot?.ToString(CultureInfo.CurrentCulture) ?? None;

    public string Reference => string.IsNullOrEmpty(Record.Reference) ? None : Record.Reference;

    public string Description => string.IsNullOrEmpty(Record.Description) ? None : Record.Description;

    /// <summary>The fee with two decimals and no currency, as the CLI prints it.</summary>
    public string Fee => Record.Fee.ToString("0.00", CultureInfo.CurrentCulture);

    public string Status => Statuses.Display(Record.Status);

    /// <summary>The day it was added, in local time, or the stored text when it isn't a date.</summary>
    public string Date => Dates.Short(Record.Date);

    /// <summary>When it was added, to the minute, in local time.</summary>
    public string DateAndTime => Dates.Long(Record.Date);
}

/// <summary>Reads the contract's ISO 8601 dates for display.</summary>
internal static class Dates
{
    internal static string Short(string stored) =>
        Parse(stored) is { } date ? date.ToLocalTime().ToString("d", CultureInfo.CurrentCulture) : stored;

    internal static string Long(string stored) =>
        Parse(stored) is { } date ? date.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) : stored;

    private static DateTimeOffset? Parse(string stored) =>
        DateTimeOffset.TryParse(stored, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
            ? date
            : null;
}
