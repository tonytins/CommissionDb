using System.Text.Json;

namespace Commis.Desktop;

/// <summary>What <c>commis info --json</c> says about the database.</summary>
/// <param name="Version">The contract's version, not the app's.</param>
/// <param name="Commis">The version of the executable that answered.</param>
/// <param name="Database">The database file in use.</param>
/// <param name="Schema">The schema version the file carries.</param>
/// <param name="Records">How many records it holds.</param>
internal sealed record Info(int Version, string Commis, string Database, int Schema, int Records);

/// <summary>A commission or YCH entry, as <c>commis list --json</c> describes it.</summary>
/// <param name="Id">The record's ID, which <c>commis status</c> accepts.</param>
/// <param name="Date">When it was added, as the stored ISO 8601 string.</param>
/// <param name="Type">"commission" or "ych".</param>
/// <param name="Client">Who commissioned it.</param>
/// <param name="Reference">A URL or identifier for the work.</param>
/// <param name="Description">What the work is.</param>
/// <param name="Fee">The amount charged.</param>
/// <param name="Title">The piece's title, or null.</param>
/// <param name="Slot">A YCH slot number, or null.</param>
/// <param name="Status">"pending", "paid", "cancelled", or "refunded".</param>
internal sealed record Record(
    int Id,
    string Date,
    string Type,
    string Client,
    string Reference,
    string Description,
    double Fee,
    string? Title,
    int? Slot,
    string Status);

/// <summary>The envelope <c>commis list --json</c> wraps its records in.</summary>
internal sealed record RecordsEnvelope(int Version, IReadOnlyList<Record> Records);

/// <summary>How the contract's JSON is read.</summary>
internal static class Contract
{
    internal static JsonSerializerOptions Json { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
    };
}
