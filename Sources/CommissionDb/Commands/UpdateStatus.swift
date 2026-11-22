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

    @Option(name: .long, help: "New status: pending, paid, cancelled, refunded.")
    var set: String

    mutating func validate() throws {
        guard Status(rawValue: set) != nil else {
            let valid = Status.allCases.map(\.rawValue).joined(separator: ", ")
            throw ValidationError("Invalid status '\(set)'. Valid values: \(valid).")
        }
    }

    mutating func run() throws {
        let dbURL = try AppDataPath.databaseURL
        let db = Commissiondb(url: dbURL)

        guard var record = try db.records.find(id) else {
            print("No record with ID \(id).")
            throw ExitCode.failure
        }

        let previous = record.status
        record.status = set
        try db.update(record)
        print("Record #\(id): \(previous) → \(set).")
    }
}
