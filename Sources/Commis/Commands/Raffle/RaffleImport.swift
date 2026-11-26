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

    @OptionGroup var options: WriteOptions

    mutating func run() throws {
        try options.withDatabase { db in
            let text: String
            do {
                text = try String(contentsOf: URL(fileURLWithPath: file), encoding: .utf8)
            } catch {
                throw RaffleImportError.unreadableFile(
                    path: file, reason: error.localizedDescription
                )
            }

            let drafts = csv
                ? try RaffleEntryImport.parseCSV(text)
                : RaffleEntryImport.parseText(text)
            guard !drafts.isEmpty else {
                throw RaffleImportError.noEntries(path: file)
            }

            let inserted = try db.enter(raffleId, drafts: drafts)
            print("Imported \(inserted.count) entries into raffle #\(raffleId).")
        }
    }
}
