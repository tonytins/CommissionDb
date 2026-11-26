import ArgumentParser

/// Cancels an open or drawn raffle without deleting its data.
struct RaffleCancel: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "cancel",
        abstract: "Cancel an open or drawn raffle."
    )

    @Argument(help: "Raffle ID.")
    var raffleId: Int

    @OptionGroup var options: WriteOptions

    mutating func run() throws {
        try options.withDatabase { db in
            try db.cancel(raffleId)
            print("Cancelled raffle #\(raffleId).")
        }
    }
}
