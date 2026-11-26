namespace Commis.Desktop.Records;

/// <summary>The statuses a record can have, as the contract and <c>commis status --set</c> spell them.</summary>
internal static class Statuses
{
    /// <summary>Every status, in the order the CLI lists them.</summary>
    internal static IReadOnlyList<string> All { get; } = ["pending", "paid", "cancelled", "refunded"];

    /// <summary>The status as a person reads it: <c>pending</c> becomes <c>Pending</c>.</summary>
    internal static string Display(string status) =>
        string.IsNullOrEmpty(status) ? status : char.ToUpperInvariant(status[0]) + status[1..];
}
