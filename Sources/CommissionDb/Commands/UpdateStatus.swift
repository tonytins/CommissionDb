import ArgumentParser
import Lighter

/// Updates the status of an existing record by its ID.
struct UpdateStatus: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "status",
        abstract: "Update the status of a record."
    )

    @Argument(help: "Record ID to update.")
    var id: Int

    @Option(name: .long, help: "New status.")
    var set: Status

    mutating func run() throws {
        let db = try Commissiondb.open()

        guard var record = try db.records.find(id) else {
            print("No record with ID \(id).")
            throw ExitCode.failure
        }

        let previous = record.status
        record.status = set.rawValue
        try db.update(record)
        print("Record #\(id): \(previous) → \(set.rawValue).")
    }
}
