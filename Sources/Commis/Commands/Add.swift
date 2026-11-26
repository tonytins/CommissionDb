import ArgumentParser
import Lighter

/// Adds a new commission or YCH record to the database.
///
/// Passing --slot creates a YCH entry; otherwise the record is a commission.
/// --title is optional for both.
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

    @Option(name: .long, help: "Title of the piece.")
    var title: String?

    @Option(name: .long, help: "YCH slot number (implies a YCH entry).")
    var slot: Int?

    @Option(name: .shortAndLong, help: "Initial status.")
    var status: Status = .pending

    @OptionGroup var options: WriteOptions

    mutating func validate() throws {
        guard fee >= 0 else {
            throw ValidationError("Fee must be a non-negative number.")
        }
    }

    mutating func run() throws {
        try options.withDatabase { db in
            let type = slot != nil ? RecordType.ych : .commission

            let record = Records(
                date: Timestamps.now(),
                type: type.rawValue,
                client: client,
                reference: reference,
                description: description,
                fee: fee,
                title: title,
                slot: slot,
                status: status.rawValue
            )

            let inserted = try db.insert(record)
            print("Added \(type.label) #\(inserted.id) for \(client).")
        }
    }
}
