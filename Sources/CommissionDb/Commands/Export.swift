import ArgumentParser
import Lighter

/// Exports records as CSV to stdout, optionally filtered by status or type.
///
/// Commission CSV columns: Date,Type,Client,Reference,Description,Fee,Status
/// YCH CSV columns add:    YCH,Slot
struct Export: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "export",
        abstract: "Export records to CSV (stdout)."
    )

    @Option(name: .shortAndLong, help: "Filter by status: pending, paid, cancelled, refunded.")
    var status: String?

    @Option(name: .shortAndLong, help: "Filter by type: commission, ych.")
    var type: String?

    mutating func validate() throws {
        if let s = status, Status(rawValue: s) == nil {
            let valid = Status.allCases.map(\.rawValue).joined(separator: ", ")
            throw ValidationError("Invalid status '\(s)'. Valid values: \(valid).")
        }
        if let t = type, RecordType(rawValue: t) == nil {
            let valid = RecordType.allCases.map(\.rawValue).joined(separator: ", ")
            throw ValidationError("Invalid type '\(t)'. Valid values: \(valid).")
        }
    }

    mutating func run() throws {
        let dbURL = try AppDataPath.databaseURL
        let db = Commissiondb(url: dbURL)

        let records = try db.records.fetch { (record: Records) in
            let statusMatch = status.map { record.status == $0 } ?? true
            let typeMatch   = type.map   { record.type   == $0 } ?? true
            return statusMatch && typeMatch
        }

        print("Date,Type,Client,Reference,Description,Fee,YCH,Slot,Status")
        for r in records {
            let fields: [String] = [
                r.date,
                r.type,
                r.client,
                r.reference,
                r.description,
                String(r.fee),
                r.ychTitle ?? "",
                r.slot.map(String.init) ?? "",
                r.status,
            ]
            print(fields.map(csvQuote).joined(separator: ","))
        }
    }

    /// Wraps a field in double-quotes, escaping any internal double-quotes.
    private func csvQuote(_ value: String) -> String {
        let escaped = value.replacingOccurrences(of: "\"", with: "\"\"")
        return "\"\(escaped)\""
    }
}
