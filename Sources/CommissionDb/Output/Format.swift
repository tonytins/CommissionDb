import Foundation

/// Display formatting shared by the list and export commands.
enum Format {
    /// Formats a fee with two decimal places, e.g. `43` → `"43.00"`.
    static func fee(_ value: Double) -> String {
        String(format: "%.2f", value)
    }
}
