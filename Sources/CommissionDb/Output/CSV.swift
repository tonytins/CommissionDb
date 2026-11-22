/// RFC 4180 CSV encoding and parsing helpers.
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

    /// Parses CSV text into rows of fields.
    ///
    /// Handles quoted fields, doubled quotes, and newlines inside quotes.
    static func parse(_ text: String) -> [[String]] {
        var rows: [[String]] = []
        var row: [String] = []
        var field = ""
        var inQuotes = false
        var pendingField = false
        var index = text.startIndex

        while index < text.endIndex {
            let character = text[index]

            if inQuotes {
                if character == "\"" {
                    let next = text.index(after: index)
                    if next < text.endIndex, text[next] == "\"" {
                        field.append("\"")
                        index = next
                    } else {
                        inQuotes = false
                    }
                } else {
                    field.append(character)
                }
            } else {
                switch character {
                case "\"":
                    inQuotes = true
                    pendingField = true
                case ",":
                    row.append(field)
                    field = ""
                    pendingField = true
                case "\n":
                    row.append(field)
                    rows.append(row)
                    row = []
                    field = ""
                    pendingField = false
                case "\r":
                    break
                default:
                    field.append(character)
                    pendingField = true
                }
            }

            index = text.index(after: index)
        }

        if pendingField {
            row.append(field)
            rows.append(row)
        }
        return rows
    }
}
