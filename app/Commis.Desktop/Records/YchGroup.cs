namespace Commis.Desktop.Records;

/// <summary>A YCH run: its title, and the entries sold in it, slots in order.</summary>
/// <param name="Header">What the run's heading row shows.</param>
/// <param name="Rows">Its entries, by slot, with entries that have no slot last.</param>
internal sealed record YchGroup(YchHeader Header, IReadOnlyList<RecordRow> Rows);

/// <summary>The heading row above a YCH run. It can't be selected.</summary>
/// <param name="Title">The run's title, or "Untitled".</param>
/// <param name="Count">How many entries it shows, in words, such as "3 entries".</param>
internal sealed record YchHeader(string Title, string Count);
