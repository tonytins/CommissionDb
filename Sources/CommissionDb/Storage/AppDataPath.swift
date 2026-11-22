import Foundation

/// Resolves the platform-specific directory for the cdb database file.
///
/// Uses the OS application support directory so the database persists across
/// invocations and isn't accidentally deleted with the binary.
enum AppDataPath {
    static var databaseURL: URL {
        get throws {
            let base = try FileManager.default.url(
                for: .applicationSupportDirectory,
                in: .userDomainMask,
                appropriateFor: nil,
                create: true
            )
            let dir = base.appendingPathComponent("cdb", isDirectory: true)
            try FileManager.default.createDirectory(
                at: dir,
                withIntermediateDirectories: true
            )
            return dir.appendingPathComponent("commissiondb.sqlite3")
        }
    }
}
