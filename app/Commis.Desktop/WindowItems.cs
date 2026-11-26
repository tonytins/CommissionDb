using System.ComponentModel;
using Commis.Desktop.Records;

namespace Commis.Desktop;

/// <summary>
/// A row in the sidebar: a section and its total, or no count where it has none. The count changes in place so a
/// refresh doesn't disturb the selection.
/// </summary>
internal sealed class SectionItem(Section section) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public Section Section { get; } = section;

    public string Name => Section.Name();

    public string Count
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
            }
        }
    } = string.Empty;
}

/// <summary>A status as a choice in a list; a null <see cref="Value"/> is all statuses.</summary>
internal sealed record FilterOption(string? Value, string Label)
{
    /// <summary>The filter's choices: all statuses, then each one.</summary>
    internal static IReadOnlyList<FilterOption> Filters { get; } =
    [
        new(null, "All statuses"),
        .. Statuses.All.Select(status => new FilterOption(status, Statuses.Display(status))),
    ];

    /// <summary>The statuses a record can be set to.</summary>
    internal static IReadOnlyList<FilterOption> Choices { get; } = Filters.Skip(1).ToList();
}
