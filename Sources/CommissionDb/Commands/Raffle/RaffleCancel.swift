import ArgumentParser

/// Cancels an open or drawn raffle without deleting its data.
struct RaffleCancel: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "cancel",
        abstract: "Cancel an open or drawn raffle."
    )

    @Argument(help: "Raffle ID.")
    var raffleId: Int

    mutating func run() throws {
        let db = try Commissiondb.open()
        try db.cancel(raffleId)
        print("Cancelled raffle #\(raffleId).")
    }
}
