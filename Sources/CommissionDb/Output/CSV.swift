/// RFC 4180 CSV encoding helpers.
enum CSV {
    /// Wraps a field in double quotes, escaping any internal double-quotes.
    static func field(_ value: String) -> String {
        let escaped = value.replacingOccurrences(of: "\"", with: "\"\"")
        return "\"\(escaped)\""
    }

    /// Joins fields into a single CSV row.
    static func row(_ fields: [String]) -> String {
        fields.map(field).joined(separator: ",")
    }
}
