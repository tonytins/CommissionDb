import ArgumentParser

/// Distinguishes a commission from a YCH entry.
/// Stored as a raw string in SQLite so it's readable without the app.
enum RecordType: String, CaseIterable, Sendable {
    case commission
    case ych

    /// Human-readable label shown in list output.
    var label: String {
        switch self {
        case .commission: "Commission"
        case .ych:        "YCH"
        }
    }
}

extension RecordType: ExpressibleByArgument {}
