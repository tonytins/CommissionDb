import ArgumentParser
import Foundation

/// Imports raffle entries from a text or CSV file.
struct RaffleImport: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "import",
        abstract: "Import raffle entries from a text or CSV file."
    )

    @Argument(help: "Raffle ID.")
    var raffleId: Int

    @Argument(help: "Path to a UTF-8 file.")
    var file: String

    @Flag(name: .long, help: "Read CSV with a header row (name,reference,tickets).")
    var csv = false

    mutating func run() throws {
        let db = try Commissiondb.open()

        // Fail before reading the file if the raffle cannot take entries.
        _ = try db.raffle(raffleId)

        let text: String
        do {
            text = try String(contentsOf: URL(fileURLWithPath: file), encoding: .utf8)
        } catch {
            throw ValidationError("Could not read \(file): \(error.localizedDescription)")
        }

        let drafts = csv ? try parseCSV(text) : parseText(text)
        guard !drafts.isEmpty else {
            throw ValidationError("No entries found in \(file).")
        }

        let inserted = try db.enter(raffleId, drafts: drafts)
        print("Imported \(inserted.count) entries into raffle #\(raffleId).")
    }

    /// One entry per line, one ticket each.
    private func parseText(_ text: String) -> [RaffleEntryDraft] {
        text.split(whereSeparator: \.isNewline)
            .map { $0.trimmingCharacters(in: .whitespaces) }
            .filter { !$0.isEmpty }
            .map { RaffleEntryDraft(name: $0, reference: "", tickets: 1) }
    }

    /// CSV with a `name` column and optional `reference` and `tickets` columns.
    private func parseCSV(_ text: String) throws -> [RaffleEntryDraft] {
        let cleaned = text.hasPrefix("\u{FEFF}") ? String(text.dropFirst()) : text
        let rows = CSV.parse(cleaned)
        guard let header = rows.first else { return [] }

        let columns = header.map { $0.trimmingCharacters(in: .whitespaces).lowercased() }
        guard let nameColumn = columns.firstIndex(of: "name") else {
            throw ValidationError("CSV header must include a 'name' column.")
        }
        let referenceColumn = columns.firstIndex(of: "reference")
        let ticketsColumn = columns.firstIndex(of: "tickets")

        var drafts: [RaffleEntryDraft] = []
        for (offset, row) in rows.dropFirst().enumerated() {
            let line = offset + 2
            if row.allSatisfy({ $0.trimmingCharacters(in: .whitespaces).isEmpty }) {
                continue
            }
            guard nameColumn < row.count else {
                throw ValidationError("Line \(line): missing name.")
            }

            let name = row[nameColumn].trimmingCharacters(in: .whitespaces)
            guard !name.isEmpty else {
                throw ValidationError("Line \(line): name is empty.")
            }

            let reference = referenceColumn.flatMap { $0 < row.count ? row[$0] : nil } ?? ""

            var tickets = 1
            if let ticketsColumn, ticketsColumn < row.count {
                let raw = row[ticketsColumn].trimmingCharacters(in: .whitespaces)
                if !raw.isEmpty {
                    guard let value = Int(raw), value > 0 else {
                        throw ValidationError("Line \(line): tickets must be a positive integer.")
                    }
                    tickets = value
                }
            }

            drafts.append(
                RaffleEntryDraft(name: name, reference: reference, tickets: tickets)
            )
        }
        return drafts
    }
}
