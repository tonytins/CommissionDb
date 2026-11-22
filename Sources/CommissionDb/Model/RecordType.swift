/// Distinguishes a commission from a YCH entry.
/// Stored as a raw string in SQLite so it's readable without the app.
enum RecordType: String, CaseIterable, Sendable {
    case commission
    case ych
}
