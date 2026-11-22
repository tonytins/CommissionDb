/// Lifecycle status of a commission or YCH entry.
/// Stored as a raw string in SQLite so it's readable without the app.
enum Status: String, CaseIterable, Sendable {
    case pending
    case paid
    case cancelled
    case refunded

    /// Human-readable label shown in list output.
    var label: String { rawValue.capitalized }
}
