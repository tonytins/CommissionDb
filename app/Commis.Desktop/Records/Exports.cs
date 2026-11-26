namespace Commis.Desktop.Records;

/// <summary>What an export from the window is called before the person names it.</summary>
internal static class Exports
{
    /// <summary>
    /// The suggested file name for the section under <paramref name="status"/>, such as <c>commissions.csv</c> or
    /// <c>ych-paid.csv</c>.
    /// </summary>
    internal static string FileName(Section section, string? status, InterfaceText text)
    {
        var name = text.Get(section == Section.Ych ? TextKeys.ExportFileYch : TextKeys.ExportFileCommissions);
        return status is null ? $"{name}.csv" : $"{name}-{status}.csv";
    }
}
