import ArgumentParser

/// Shows one raffle and its entries.
struct RaffleShow: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "show",
        abstract: "Show a raffle and its entries."
    )

    @Argument(help: "Raffle ID.")
    var raffleId: Int

    mutating func run() throws {
        let db = try Commissiondb.open()

        guard let raffle = try db.raffles.find(raffleId) else {
            print("No raffle with ID \(raffleId).")
            throw ExitCode.failure
        }

        let entries = try db.fetchEntries(raffleId: raffleId)
        let totalTickets = entries.reduce(0) { $0 + $1.tickets }

        print("#\(raffle.id) [\(raffle.status.uppercased())] \(raffle.title)")
        print("  Created: \(raffle.date)")
        if !raffle.prize.isEmpty { print("  Prize:   \(raffle.prize)") }
        print("  Tickets: \(Format.price(raffle.ticketPrice))")
        if let opens = raffle.opens { print("  Opens:   \(opens)") }
        if let ends = raffle.ends { print("  Ends:    \(ends)") }
        print("  Entries: \(entries.count) (\(totalTickets) tickets)")

        guard !entries.isEmpty else { return }
        print()
        for entry in entries {
            let marker = entry.wonAt != nil ? " [winner]" : ""
            print("  #\(entry.id) \(entry.name) (\(entry.tickets) tickets)\(marker)")
            if !entry.reference.isEmpty {
                print("    \(entry.reference)")
            }
        }
    }
}
