import ArgumentParser
import Foundation
import Lighter

/// Creates a raffle.
struct RaffleAdd: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "add",
        abstract: "Create a raffle."
    )

    @Option(name: .long, help: "Raffle title.")
    var title: String

    @Option(name: .long, help: "What the winner receives.")
    var prize: String = ""

    @Option(name: .long, help: "Price per ticket; 0 for free entry.")
    var ticketPrice: Double = 0

    @Option(name: .long, help: "When entries open (ISO 8601 or yyyy-MM-dd).")
    var opens: String?

    @Option(name: .long, help: "When entries close (ISO 8601 or yyyy-MM-dd).")
    var ends: String?

    @OptionGroup var options: WriteOptions

    private var opensDate: Date?
    private var endsDate: Date?

    mutating func validate() throws {
        guard ticketPrice >= 0 else {
            throw ValidationError("--ticket-price must be a non-negative number.")
        }

        opensDate = try parseDate(opens, name: "--opens")
        endsDate = try parseDate(ends, name: "--ends")

        if let opensDate, let endsDate, opensDate > endsDate {
            throw ValidationError("--opens must not be after --ends.")
        }
    }

    mutating func run() throws {
        try options.withDatabase { db in
            let raffle = Raffles(
                date: Timestamps.now(),
                title: title,
                prize: prize,
                ticketPrice: ticketPrice,
                opens: opensDate.map(Timestamps.isoString(from:)),
                ends: endsDate.map(Timestamps.isoString(from:)),
                status: RaffleStatus.open.rawValue
            )

            let inserted = try db.insert(raffle)
            print("Added raffle #\(inserted.id) \"\(title)\".")
        }
    }

    private func parseDate(_ value: String?, name: String) throws -> Date? {
        guard let value else { return nil }
        guard let date = Timestamps.parse(value) else {
            throw ValidationError(
                "\(name) must be an ISO 8601 timestamp or yyyy-MM-dd date."
            )
        }
        return date
    }
}
