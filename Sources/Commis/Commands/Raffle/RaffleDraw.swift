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

    mutating func validate() throws {
        guard winners > 0 else {
            throw ValidationError("--winners must be greater than zero.")
        }
    }

    mutating func run() throws {
        let db = try Commis.open()
        let picked = try db.draw(raffleId, winners: winners)

        let raffle = try db.raffle(raffleId)
        let entries = try db.fetchEntries(raffleId: raffleId)
        let totalTickets = entries.reduce(0) { $0 + $1.tickets }
        let winnerWord = Format.count(picked.count, "winner")

        print("Drew \(winnerWord) for raffle #\(raffleId) \"\(raffle.title)\".")
        print("Entries: \(entries.count), tickets: \(totalTickets)")
        for entry in picked {
            let ticketWord = Format.count(entry.tickets, "ticket")
            print("  #\(entry.id) \(entry.name) (\(ticketWord))")
        }
    }
}
