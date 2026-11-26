import ArgumentParser
import Foundation
import GitVersion
import Lighter

/// Reports where the database is, which schema it uses, and how many records
/// it holds.
///
/// The desktop app calls this first, to learn what it is looking at before it
/// asks for records.
struct Info: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "info",
        abstract: "Show the database's location, schema version, and record count."
    )

    @Flag(name: .long, help: "Print JSON for other programs.")
    var json = false

    mutating func run() throws {
        let url = try AppDataPath.databaseURL
        let db = try Commis.open(at: url)
        let payload = try Self.payload(database: url, db: db)

        if json {
            print(try JSON.info(payload))
            return
        }

        print("Database: \(payload.database)")
        print("Schema:   \(payload.schema)")
        print("Records:  \(payload.records)")
    }

    /// The values `run` reports, read from `db` at `url`.
    static func payload(database url: URL, db: Commis) throws -> JSON.Info {
        JSON.Info(
            version: JSON.version,
            commis: GitVersion.version,
            database: url.path,
            schema: try db.get(pragma: "user_version", as: Int.self),
            records: try db.fetchRecords().count
        )
    }
}
