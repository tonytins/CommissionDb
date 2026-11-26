namespace Commis.Desktop.Records;

/// <summary>The statuses a record can have, as the contract and <c>commis status --set</c> spell them.</summary>
internal static class Statuses
{
    /// <summary>Every status, in the order the CLI lists them.</summary>
    internal static IReadOnlyList<string> All { get; } = ["pending", "paid", "cancelled", "refunded"];

    /// <summary>
    /// The status as a person reads it, such as <c>Pending</c>. A status from a newer commis that the table doesn't
    /// name shows as commis spells it.
    /// </summary>
    internal static string Display(string status, InterfaceText text) =>
        text.Has(TextKeys.Status(status)) ? text.Get(TextKeys.Status(status)) : status;
}
