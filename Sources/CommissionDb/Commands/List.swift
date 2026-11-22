import ArgumentParser

/// Lists stored records, optionally filtered by status or type.
struct List: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "list",
        abstract: "List commission and YCH records."
    )

    @Option(name: .shortAndLong, help: "Filter by status.")
    var status: Status?

    @Option(name: .shortAndLong, help: "Filter by type.")
    var type: RecordType?

    mutating func run() throws {
        let db = try Commissiondb.open()
        let records = try db.fetchRecords(status: status, type: type)

        guard !records.isEmpty else {
            print("No records found.")
            return
        }

        for r in records {
            let typeLabel = RecordType(rawValue: r.type)?.label ?? r.type
            let titleText = r.title.map { " — \($0)" } ?? ""
            let slotText  = r.type == RecordType.ych.rawValue
                          ? r.slot.map { " slot \($0)" } ?? ""
                          : ""
            print("#\(r.id) [\(r.status.uppercased())] \(typeLabel)\(titleText)\(slotText)")
            print("  Client:      \(r.client)")
            print("  Reference:   \(r.reference)")
            if !r.description.isEmpty { print("  Description: \(r.description)") }
            print("  Fee:         \(Format.fee(r.fee))")
            print("  Date:        \(r.date)")
            print()
        }
    }
}
