import Foundation

/// The machine-readable output printed by `--json`.
///
/// The desktop app in `app/` reads this, so it is a contract: fields are added,
/// never renamed or removed, without a change to ``version``. The design is in
/// `docs/superpowers/specs/2026-11-26-commis-desktop-design.md`.
enum JSON {
    /// The contract's version, which is not the app's.
    static let version = 1

    /// `{"version":1,"records":[…]}`, on one line, with keys sorted.
    static func records(_ records: [Records]) throws -> String {
        try encode(RecordsEnvelope(version: version, records: records.map(Record.init)))
    }

    /// What `info --json` prints.
    static func info(_ info: Info) throws -> String {
        try encode(info)
    }

    private static func encode<T: Encodable>(_ value: T) throws -> String {
        let encoder = JSONEncoder()
        encoder.outputFormatting = .sortedKeys
        return String(decoding: try encoder.encode(value), as: UTF8.self)
    }

    /// The envelope every list response shares.
    private struct RecordsEnvelope: Encodable {
        let version: Int
        let records: [Record]
    }

    /// A record as the contract describes it.
    struct Record: Encodable {
        let id: Int
        let date: String
        let type: String
        let client: String
        let reference: String
        let description: String
        let fee: Double
        let title: String?
        let slot: Int?
        let status: String

        init(_ record: Records) {
            id = record.id
            date = record.date
            type = record.type
            client = record.client
            reference = record.reference
            description = record.description
            fee = record.fee
            title = record.title
            slot = record.slot
            status = record.status
        }

        /// Writes an absent title or slot as null, rather than leaving the key
        /// out, so a reader never has to tell "missing" from "none".
        func encode(to encoder: Encoder) throws {
            var container = encoder.container(keyedBy: CodingKeys.self)
            try container.encode(id, forKey: .id)
            try container.encode(date, forKey: .date)
            try container.encode(type, forKey: .type)
            try container.encode(client, forKey: .client)
            try container.encode(reference, forKey: .reference)
            try container.encode(description, forKey: .description)
            try container.encode(fee, forKey: .fee)
            try container.encode(title, forKey: .title)
            try container.encode(slot, forKey: .slot)
            try container.encode(status, forKey: .status)
        }

        private enum CodingKeys: String, CodingKey {
            case id, date, type, client, reference, description, fee, title, slot, status
        }
    }

    /// What `info --json` says about the database.
    struct Info: Encodable {
        let version: Int
        let commis: String
        let database: String
        let schema: Int
        let records: Int
    }
}
