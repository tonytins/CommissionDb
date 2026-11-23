import Foundation

/// Errors raised while reading a raffle entrant list.
///
/// Messages are written for the person running the command, so they name the
/// file and line that needs attention.
enum RaffleImportError: Error, Equatable, CustomStringConvertible {
    case unreadableFile(path: String, reason: String)
    case noEntries(path: String)
    case missingNameColumn
    case missingName(line: Int)
    case emptyName(line: Int)
    case invalidTickets(line: Int, value: String)

    var description: String {
        switch self {
        case .unreadableFile(let path, let reason):
            return "Could not read \(path): \(reason)"
        case .noEntries(let path):
            return "No entries found in \(path)."
        case .missingNameColumn:
            return "CSV header must include a 'name' column."
        case .missingName(let line):
            return "Line \(line): missing name."
        case .emptyName(let line):
            return "Line \(line): name is empty."
        case .invalidTickets(let line, let value):
            return "Line \(line): tickets must be a positive integer, got '\(value)'."
        }
    }
}

/// Parses raffle entrant lists from a text or CSV file.
///
/// A plain text file holds one entrant name per line, one ticket each. CSV
/// files need a `name` column and may include `reference` and `tickets`.
enum RaffleEntryImport {
    /// Parses one name per line, ignoring blank lines.
    static func parseText(_ text: String) -> [RaffleEntryDraft] {
        text.split(whereSeparator: \.isNewline)
            .map { $0.trimmingCharacters(in: .whitespaces) }
            .filter { !$0.isEmpty }
            .map { RaffleEntryDraft(name: $0, reference: "", tickets: 1) }
    }

    /// Parses CSV with a header row and a `name` column.
    ///
    /// Throws `RaffleImportError` naming the offending line, so a bad file is
    /// rejected before anything is inserted.
    static func parseCSV(_ text: String) throws -> [RaffleEntryDraft] {
        let cleaned = text.hasPrefix("\u{FEFF}") ? String(text.dropFirst()) : text
        let rows = CSV.parse(cleaned)
        guard let header = rows.first else { return [] }

        let columns = header.map { $0.trimmingCharacters(in: .whitespaces).lowercased() }
        guard let nameColumn = columns.firstIndex(of: "name") else {
            throw RaffleImportError.missingNameColumn
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
                throw RaffleImportError.missingName(line: line)
            }

            let name = row[nameColumn].trimmingCharacters(in: .whitespaces)
            guard !name.isEmpty else {
                throw RaffleImportError.emptyName(line: line)
            }

            let reference = referenceColumn.flatMap { $0 < row.count ? row[$0] : nil } ?? ""

            var tickets = 1
            if let ticketsColumn, ticketsColumn < row.count {
                let raw = row[ticketsColumn].trimmingCharacters(in: .whitespaces)
                if !raw.isEmpty {
                    guard let value = Int(raw), value > 0 else {
                        throw RaffleImportError.invalidTickets(line: line, value: raw)
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
