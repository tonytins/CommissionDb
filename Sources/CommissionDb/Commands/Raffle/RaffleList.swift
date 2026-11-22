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
        let db = try Commissiondb.open()
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
            print("  Tickets: \(Format.price(raffle.ticketPrice))")
            print("  Entries: \(entries.count) (\(totalTickets) tickets)")
            if !winners.isEmpty {
                print("  Winners: \(winners.joined(separator: ", "))")
            }
            print()
        }
    }
}
