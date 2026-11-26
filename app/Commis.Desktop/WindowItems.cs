using System.ComponentModel;
using Commis.Desktop.Records;

namespace Commis.Desktop;

/// <summary>
/// A row in the sidebar: a section and its total, or no count where it has none. The count changes in place so a
/// refresh doesn't disturb the selection.
/// </summary>
internal sealed class SectionItem(Section section, string name) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public Section Section { get; } = section;

    public string Name { get; } = name;

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
    internal static IReadOnlyList<FilterOption> Filters(InterfaceText text) =>
        [new(null, text.Get(TextKeys.FilterAll)), .. Choices(text)];

    /// <summary>The statuses a record can be set to.</summary>
    internal static IReadOnlyList<FilterOption> Choices(InterfaceText text) =>
        Statuses.All.Select(status => new FilterOption(status, Statuses.Display(status, text))).ToList();
}
