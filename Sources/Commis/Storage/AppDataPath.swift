import Foundation

/// Resolves the platform-specific location of the commis database.
///
/// Uses the OS application support directory so the database persists across
/// invocations and isn't accidentally deleted with the binary.
enum AppDataPath {
    /// The database file to use.
    ///
    /// New databases live in `commis/commis.sqlite3`. Databases created before
    /// the rename live in `cdb/commissiondb.sqlite3`; when the new file does
    /// not exist yet and the old one does, the old file is used in place so
    /// existing data keeps working without a manual move.
    static var databaseURL: URL {
        get throws {
            let base = try FileManager.default.url(
                for: .applicationSupportDirectory,
                in: .userDomainMask,
                appropriateFor: nil,
                create: true
            )
            return try databaseURL(in: base)
        }
    }

    /// Resolves the database file inside `base`, creating the `commis`
    /// directory only when no database exists yet.
    static func databaseURL(in base: URL) throws -> URL {
        let directory = base.appendingPathComponent("commis", isDirectory: true)
        let url = directory.appendingPathComponent("commis.sqlite3")
        if FileManager.default.fileExists(atPath: url.path) {
            return url
        }

        let legacyURL = base
            .appendingPathComponent("cdb", isDirectory: true)
            .appendingPathComponent("commissiondb.sqlite3")
        if FileManager.default.fileExists(atPath: legacyURL.path) {
            return legacyURL
        }

        try FileManager.default.createDirectory(
            at: directory,
            withIntermediateDirectories: true
        )
        return url
    }
}
