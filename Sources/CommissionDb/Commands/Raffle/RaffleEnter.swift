import ArgumentParser

/// Adds a single entry to an open raffle.
struct RaffleEnter: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "enter",
        abstract: "Add an entry to a raffle."
    )

    @Argument(help: "Raffle ID.")
    var raffleId: Int

    @Option(name: .long, help: "Entrant name or handle.")
    var name: String

    @Option(name: .long, help: "Entrant reference URL or handle.")
    var reference: String = ""

    @Option(name: .long, help: "Number of tickets.")
    var tickets: Int = 1

    mutating func validate() throws {
        guard tickets > 0 else {
            throw ValidationError("--tickets must be greater than zero.")
        }
    }

    mutating func run() throws {
        let db = try Commissiondb.open()
        let entry = try db.enter(
            raffleId,
            draft: RaffleEntryDraft(name: name, reference: reference, tickets: tickets)
        )
        print("Entry #\(entry.id) for \(name) in raffle #\(raffleId).")
    }
}
