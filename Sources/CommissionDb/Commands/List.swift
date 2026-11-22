import ArgumentParser
import Lighter

/// Lists stored records, optionally filtered by status or type.
struct List: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "list",
        abstract: "List commission and YCH records."
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

        guard !records.isEmpty else {
            print("No records found.")
            return
        }

        for r in records {
            let typeLabel = r.type.capitalized
            let ychInfo   = r.ychTitle.map { " — \($0) slot \(r.slot ?? 0)" } ?? ""
            print("#\(r.id) [\(r.status.uppercased())] \(typeLabel)\(ychInfo)")
            print("  Client:      \(r.client)")
            print("  Reference:   \(r.reference)")
            if !r.description.isEmpty { print("  Description: \(r.description)") }
            print("  Fee:         \(r.fee)")
            print("  Date:        \(r.date)")
            print()
        }
    }
}
