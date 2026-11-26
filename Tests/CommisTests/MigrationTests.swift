import Foundation
import Lighter
import Testing
@testable import Commis

/// The schema shipped before raffles (version 0).
private let v0Schema = [
    """
    CREATE TABLE records (
        id          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
        date        TEXT    NOT NULL,
        type        TEXT    NOT NULL CHECK(type IN ('commission', 'ych')),
        client      TEXT    NOT NULL,
        reference   TEXT    NOT NULL,
        description TEXT    NOT NULL DEFAULT '',
        fee         REAL    NOT NULL,
        ych_title   TEXT,
        slot        INTEGER,
        status      TEXT    NOT NULL DEFAULT 'pending'
                            CHECK(status IN ('pending','paid','cancelled','refunded'))
    )
    """,
    """
    INSERT INTO records
        (date, type, client, reference, description, fee, ych_title, slot, status)
    VALUES
        ('2026-01-01T00:00:00Z', 'ych', 'Legacy Client', 'https://example.com',
         'Legacy description', 25, 'Synthesize', 4, 'paid')
    """,
]

@Test("A new database is created at the current schema version")
func freshDatabaseHasCurrentVersion() throws {
    try withTemporaryDatabase { db in
        let version = try db.get(pragma: "user_version", as: Int.self)
        #expect(version == Commis.userVersion)
    }
}

@Test("Opening a version 0 database migrates it in place")
func migratesVersionZero() throws {
    let url = temporaryDatabaseURL()
    defer { try? FileManager.default.removeItem(at: url) }

    // Build a version 0 file: old column name, no raffle tables.
    try createLegacyDatabase(at: url)

    let db = try Commis.open(at: url)

    #expect(try db.get(pragma: "user_version", as: Int.self) == 1)

    let record = try db.records.find(1)
    #expect(record?.client == "Legacy Client")
    #expect(record?.title == "Synthesize")
    #expect(record?.slot == 4)
    #expect(record?.status == "paid")

    // The raffle tables exist and are usable after the migration.
    let raffle = try db.insert(makeRaffle())
    _ = try db.enter(raffle.id, draft: RaffleEntryDraft(name: "Bessie", reference: "", tickets: 1))
    #expect(try db.fetchRaffles().count == 1)
    #expect(try db.fetchEntries(raffleId: raffle.id).count == 1)
}

@Test("Opening an up-to-date database does not migrate again")
func migrationIsIdempotent() throws {
    let url = temporaryDatabaseURL()
    defer { try? FileManager.default.removeItem(at: url) }

    let first = try Commis.open(at: url)
    _ = try first.insert(makeRecord(client: "Existing"))

    let second = try Commis.open(at: url)
    #expect(try second.get(pragma: "user_version", as: Int.self) == 1)
    #expect(try second.fetchRecords().count == 1)
}

@Test("Opening an empty file initializes a new database")
func emptyFileIsInitialized() throws {
    let url = temporaryDatabaseURL()
    defer { try? FileManager.default.removeItem(at: url) }
    guard FileManager.default.createFile(atPath: url.path, contents: nil) else {
        throw CocoaError(.fileWriteUnknown)
    }

    let db = try Commis.open(at: url)

    #expect(try db.get(pragma: "user_version", as: Int.self) == 1)
    #expect(try db.insert(makeRecord()).id > 0)
}

@Test("A database from a newer build is refused instead of misread")
func futureVersionIsRefused() throws {
    let url = temporaryDatabaseURL()
    defer { try? FileManager.default.removeItem(at: url) }

    let db = try Commis.open(at: url)
    try db.execute("PRAGMA user_version = 99")

    #expect(
        throws: MigrationError.futureVersion(found: 99, supported: Commis.userVersion)
    ) {
        try Commis.open(at: url)
    }
}

/// Writes the version 0 schema and one row into an empty file at `url`.
private func createLegacyDatabase(at url: URL) throws {
    guard FileManager.default.createFile(atPath: url.path, contents: nil) else {
        throw CocoaError(.fileWriteUnknown)
    }

    let legacy = Commis(url: url)
    for statement in v0Schema {
        try legacy.execute(statement)
    }
}
