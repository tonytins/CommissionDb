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
}
