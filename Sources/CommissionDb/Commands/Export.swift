import ArgumentParser

/// Exports records as CSV to stdout, optionally filtered by status or type.
///
/// Columns: Date,Type,Client,Reference,Description,Fee,Title,Slot,Status
struct Export: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "export",
        abstract: "Export records to CSV (stdout)."
    )

    @Option(name: .shortAndLong, help: "Filter by status.")
    var status: Status?

    @Option(name: .shortAndLong, help: "Filter by type.")
    var type: RecordType?

    mutating func run() throws {
        let db = try Commissiondb.open()
        let records = try db.fetchRecords(status: status, type: type)

        print("Date,Type,Client,Reference,Description,Fee,Title,Slot,Status")
        for r in records {
            print(CSV.row([
                r.date,
                r.type,
                r.client,
                r.reference,
                r.description,
                Format.fee(r.fee),
                r.title ?? "",
                r.slot.map(String.init) ?? "",
                r.status,
            ]))
        }
    }
}
