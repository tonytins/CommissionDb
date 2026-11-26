import ArgumentParser

/// Draws winners for an open raffle.
struct RaffleDraw: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "draw",
        abstract: "Draw winners for an open raffle."
    )

    @Argument(help: "Raffle ID.")
    var raffleId: Int

    @Option(name: .long, help: "Number of winners.")
    var winners: Int = 1

    @OptionGroup var options: WriteOptions

    mutating func validate() throws {
        guard winners > 0 else {
            throw ValidationError("--winners must be greater than zero.")
        }
    }

    mutating func run() throws {
        try options.withDatabase { db in
            let picked = try db.draw(raffleId, winners: winners)

            let raffle = try db.raffle(raffleId)
            let entries = try db.fetchEntries(raffleId: raffleId)
            let totalTickets = entries.reduce(0) { $0 + $1.tickets }
            let winnerWord = Format.count(picked.count, "winner")

            // Winners drawn on a copy aren't the ones a real draw picks, so a
            // dry run names none of them.
            guard !options.dryRun else {
                print("Would draw \(winnerWord) for raffle #\(raffleId) \"\(raffle.title)\".")
                print("Entries: \(entries.count), tickets: \(totalTickets)")
                print("Winners are picked only when the draw is real.")
                return
            }

            print("Drew \(winnerWord) for raffle #\(raffleId) \"\(raffle.title)\".")
            print("Entries: \(entries.count), tickets: \(totalTickets)")
            for entry in picked {
                let ticketWord = Format.count(entry.tickets, "ticket")
                print("  #\(entry.id) \(entry.name) (\(ticketWord))")
            }
        }
    }
}
