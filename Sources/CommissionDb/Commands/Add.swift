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

    @Option(name: .shortAndLong, help: "Initial status.")
    var status: Status = .pending

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
    }

    mutating func run() throws {
        let db = try Commissiondb.open()

        let type_ = ychTitle != nil ? RecordType.ych : .commission

        let record = Records(
            date: ISO8601DateFormatter().string(from: Date()),
            type: type_.rawValue,
            client: client,
            reference: reference,
            description: description,
            fee: fee,
            ychTitle: ychTitle,
            slot: slot,
            status: status.rawValue
        )

        let inserted = try db.insert(record)
        print("Added \(type_.label) #\(inserted.id) for \(client).")
    }
}
