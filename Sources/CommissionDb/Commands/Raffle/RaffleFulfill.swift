import ArgumentParser

/// Turns drawn winners into pending commission or YCH records.
struct RaffleFulfill: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "fulfill",
        abstract: "Create records for the winners of a drawn raffle."
    )

    @Argument(help: "Raffle ID.")
    var raffleId: Int

    @Option(name: .long, help: "Record type for the prize.")
    var type: RecordType

    mutating func run() throws {
        let db = try Commissiondb.open()
        let created = try db.fulfill(raffleId, type: type)

        for record in created {
            print("Created \(type.label) #\(record.id) for \(record.client).")
        }
    }
}
