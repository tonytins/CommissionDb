import Foundation

/// Display formatting shared by the list and export commands.
enum Format {
    /// Formats a fee with two decimal places, e.g. `43` → `"43.00"`.
    static func fee(_ value: Double) -> String {
        String(format: "%.2f", value)
    }

    /// Formats a ticket price, showing free entries as `"free"`.
    static func price(_ value: Double) -> String {
        value > 0 ? fee(value) : "free"
    }

    /// Formats a ticket price with its unit, e.g. `"5.00 per ticket"`.
    static func pricePerTicket(_ value: Double) -> String {
        value > 0 ? "\(fee(value)) per ticket" : "free"
    }

    /// Pluralizes a noun after a count, e.g. `1 ticket` and `3 tickets`.
    static func count(_ value: Int, _ singular: String, _ plural: String? = nil) -> String {
        "\(value) \(value == 1 ? singular : plural ?? singular + "s")"
    }
}
