namespace Commis.Desktop.Records;

/// <summary>The places in the sidebar, in the order they're shown.</summary>
internal enum Section
{
    Commissions,
    Ych,
    Raffles,
}

/// <summary>What each section is called and which records it holds.</summary>
internal static class Sections
{
    /// <summary>Every section, in sidebar order. Commissions comes first and is where a first run opens.</summary>
    internal static IReadOnlyList<Section> All { get; } = [Section.Commissions, Section.Ych, Section.Raffles];

    /// <summary>The section's name in the sidebar.</summary>
    internal static string Name(this Section section) => section switch
    {
        Section.Commissions => "Commissions",
        Section.Ych => "YCH",
        Section.Raffles => "Raffles",
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, null),
    };

    /// <summary>The record type the section shows, as the contract spells it, or null for one that shows none.</summary>
    internal static string? RecordType(this Section section) => section switch
    {
        Section.Commissions => "commission",
        Section.Ych => "ych",
        _ => null,
    };
}
