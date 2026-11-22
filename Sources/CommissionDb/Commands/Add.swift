import ArgumentParser
import Foundation
import Lighter

/// Adds a new commission or YCH record to the database.
///
/// Omitting --ych-title and --slot creates a commission entry.
/// Both flags must be supplied together to create a YCH entry.
struct Add: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "add",
        abstract: "Add a new commission or YCH record."
    )

    @Option(name: .shortAndLong, help: "Client name.")
    var client: String

    @Option(name: .shortAndLong, help: "Reference URL or identifier.")
    var reference: String

    @Option(name: .shortAndLong, help: "Description of the work.")
    var description: String = ""

    @Option(name: .shortAndLong, help: "Fee amount.")
    var fee: Double

    @Option(name: .long, help: "YCH title (required together with --slot for a YCH entry).")
    var ychTitle: String?

    @Option(name: .long, help: "YCH slot number (required together with --ych-title for a YCH entry).")
    var slot: Int?

    @Option(name: .shortAndLong, help: "Initial status: pending, paid, cancelled, refunded.")
    var status: String = "pending"

    mutating func validate() throws {
        // ych-title and slot must both be present or both absent.
        switch (ychTitle, slot) {
        case (.some, .none), (.none, .some):
            throw ValidationError("--ych-title and --slot must be supplied together.")
        default:
            break
        }

        guard fee >= 0 else {
            throw ValidationError("Fee must be a non-negative number.")
        }

        guard Status(rawValue: status) != nil else {
            let valid = Status.allCases.map(\.rawValue).joined(separator: ", ")
            throw ValidationError("Invalid status '\(status)'. Valid values: \(valid).")
        }
    }

    mutating func run() throws {
        let dbURL = try AppDataPath.databaseURL
        let db = Commissiondb(url: dbURL)

        let type_ = ychTitle != nil ? RecordType.ych.rawValue : RecordType.commission.rawValue

        let record = Records(
            date: ISO8601DateFormatter().string(from: Date()),
            type: type_,
            client: client,
            reference: reference,
            description: description,
            fee: fee,
            ychTitle: ychTitle,
            slot: slot,
            status: status
        )

        let inserted = try db.insert(record)
        print("Added \(type_) #\(inserted.id) for \(client).")
    }
}
