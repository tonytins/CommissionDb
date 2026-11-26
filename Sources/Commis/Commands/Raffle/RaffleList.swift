import ArgumentParser

/// Lists raffles with entry and ticket counts.
struct RaffleList: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "list",
        abstract: "List raffles."
    )

    @Option(name: .shortAndLong, help: "Filter by status.")
    var status: RaffleStatus?

    mutating func run() throws {
        let db = try Commis.open()
        let raffles = try db.fetchRaffles(status: status)

        guard !raffles.isEmpty else {
            print("No raffles found.")
            return
        }

        for raffle in raffles {
            let entries = try db.fetchEntries(raffleId: raffle.id)
            let totalTickets = entries.reduce(0) { $0 + $1.tickets }
            let winners = entries.filter { $0.wonAt != nil }.map(\.name)

            print("#\(raffle.id) [\(raffle.status.uppercased())] \(raffle.title)")
            if !raffle.prize.isEmpty { print("  Prize:   \(raffle.prize)") }
            print("  Price:   \(Format.pricePerTicket(raffle.ticketPrice))")
            print("  Entries: \(Format.count(entries.count, "entry", "entries"))"
                  + " (\(Format.count(totalTickets, "ticket")))")
            if !winners.isEmpty {
                print("  Winners: \(winners.joined(separator: ", "))")
            }
            print()
        }
    }
}
