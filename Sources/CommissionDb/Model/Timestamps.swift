import Foundation

/// ISO 8601 timestamps used for record and raffle dates.
enum Timestamps {
    /// The current time as an ISO 8601 string.
    static func now() -> String {
        isoString(from: Date())
    }

    /// Formats a date as an ISO 8601 string.
    static func isoString(from date: Date) -> String {
        ISO8601DateFormatter().string(from: date)
    }

    /// Parses an ISO 8601 timestamp or a `yyyy-MM-dd` date.
    ///
    /// Date-only values are interpreted at midnight UTC.
    static func parse(_ value: String) -> Date? {
        if let date = ISO8601DateFormatter().date(from: value) {
            return date
        }

        let formatter = DateFormatter()
        formatter.calendar = Calendar(identifier: .iso8601)
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.timeZone = TimeZone(secondsFromGMT: 0)
        formatter.dateFormat = "yyyy-MM-dd"
        return formatter.date(from: value)
    }
}
