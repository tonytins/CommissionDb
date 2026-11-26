import Lighter

/// Errors raised while bringing a database file up to date.
enum MigrationError: Error, Equatable, CustomStringConvertible {
    case futureVersion(found: Int, supported: Int)

    var description: String {
        switch self {
        case .futureVersion(let found, let supported):
            return """
            This database uses schema version \(found), but this build \
            supports \(supported). Upgrade commis before opening it.
            """
        }
    }
}

/// In-place schema migrations for existing database files.
///
/// Migrations are historical: once shipped, never edit them. Add a new step
/// for the next schema version instead.
enum Migrations {
    /// Brings `db` up to `Commis.userVersion`.
    static func apply(to db: Commis) throws {
        let version = try db.get(pragma: "user_version", as: Int.self)
        if version > Commis.userVersion {
            throw MigrationError.futureVersion(
                found: version, supported: Commis.userVersion
            )
        }
        guard version < Commis.userVersion else { return }

        try db.transaction { tx in
            if version < 1 {
                try migrateToV1(tx)
            }
        }
    }

    /// v0 → v1.
    ///
    /// v0 had `records.ych_title` and no raffle tables.
    /// v1 renames the column, adds `raffles` and `raffle_entries`, and indexes
    /// the foreign key column.
    private static func migrateToV1(
        _ tx: SQLChangeTransaction<Commis>
    ) throws {
        try tx.execute("ALTER TABLE records RENAME COLUMN ych_title TO title")

        try tx.execute("""
            CREATE TABLE raffles (
                id           INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                date         TEXT    NOT NULL,
                title        TEXT    NOT NULL,
                prize        TEXT    NOT NULL DEFAULT '',
                ticket_price REAL    NOT NULL DEFAULT 0,
                opens        TEXT,
                ends         TEXT,
                status       TEXT    NOT NULL DEFAULT 'open'
                                     CHECK(status IN ('open','drawn','fulfilled','cancelled'))
            )
            """)

        try tx.execute("""
            CREATE TABLE raffle_entries (
                id        INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                raffle_id INTEGER NOT NULL,
                name      TEXT    NOT NULL,
                reference TEXT    NOT NULL DEFAULT '',
                tickets   INTEGER NOT NULL DEFAULT 1 CHECK(tickets > 0),
                entered   TEXT    NOT NULL,
                won_at    TEXT
            )
            """)

        try tx.execute("CREATE INDEX raffle_entries_raffle_id ON raffle_entries (raffle_id)")
        try tx.execute("PRAGMA user_version = 1")
    }
}
