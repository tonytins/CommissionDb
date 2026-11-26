import ArgumentParser
import Foundation
import Lighter

/// Errors raised before a command changes the database.
enum DatabaseError: Error, Equatable, CustomStringConvertible {
    /// The file, or the folder SQLite writes its journal into, can't be written.
    case readOnly(path: String)

    var description: String {
        switch self {
        case .readOnly(let path):
            return "Can't change the database at \(path): it or its folder is read-only."
        }
    }
}

/// The options every command that changes the database takes.
struct WriteOptions: ParsableArguments {
    @Flag(
        name: .long,
        help: "Run against a copy of the database and keep nothing, to see what would happen."
    )
    var dryRun = false

    /// Runs `body` against the database, or against a throwaway copy of it in
    /// a dry run. A dry run ends by saying so on standard error, so standard
    /// output reads the same as a real run's.
    func withDatabase<T>(_ body: (Commis) throws -> T) throws -> T {
        let result = try Commis.withDatabase(at: AppDataPath.databaseURL, dryRun: dryRun, body)
        if dryRun {
            // Standard output is buffered when it isn't a terminal; flush it
            // so the notice comes after the command's own lines.
            fflush(nil)
            FileHandle.standardError.write(Data("Dry run: nothing was saved.\n".utf8))
        }
        return result
    }
}

extension Commis {
    /// Opens the database at `url` for a command that changes it, and runs
    /// `body` against it.
    ///
    /// In a dry run, `body` runs against a copy in a temporary folder that is
    /// removed afterwards, so the command's checks, IDs, and messages are the
    /// ones a real run would give, and the database itself is never opened
    /// for writing or migrated.
    static func withDatabase<T>(
        at url: URL,
        dryRun: Bool,
        _ body: (Commis) throws -> T
    ) throws -> T {
        guard dryRun else {
            try requireWritable(url)
            return try body(open(at: url))
        }

        let fileManager = FileManager.default
        let folder = fileManager.temporaryDirectory
            .appendingPathComponent("commis-dry-run-\(UUID().uuidString)", isDirectory: true)
        try fileManager.createDirectory(at: folder, withIntermediateDirectories: true)
        defer { try? fileManager.removeItem(at: folder) }

        // The journal files come along so the copy is in the state SQLite
        // would see, such as a write that was interrupted.
        let copy = folder.appendingPathComponent(url.lastPathComponent)
        for suffix in ["", "-journal", "-wal"] {
            let source = URL(fileURLWithPath: url.path + suffix)
            if fileManager.fileExists(atPath: source.path) {
                try fileManager.copyItem(at: source, to: URL(fileURLWithPath: copy.path + suffix))
            }
        }
        // A copy of a read-only file is read-only too.
        if fileManager.fileExists(atPath: copy.path) {
            try fileManager.setAttributes([.posixPermissions: 0o600], ofItemAtPath: copy.path)
        }

        return try body(open(at: copy))
    }

    /// Throws ``DatabaseError/readOnly(path:)`` when a write to `url` would
    /// fail for lack of permission.
    ///
    /// Lighter asserts on a failed write in debug builds, so this has to be
    /// caught before the write rather than after it. SQLite also creates its
    /// journal beside the file, so the folder must be writable as well.
    static func requireWritable(_ url: URL) throws {
        let fileManager = FileManager.default
        let folder = url.deletingLastPathComponent().path
        let fileIsReadOnly = fileManager.fileExists(atPath: url.path)
            && !fileManager.isWritableFile(atPath: url.path)
        let folderIsReadOnly = fileManager.fileExists(atPath: folder)
            && !fileManager.isWritableFile(atPath: folder)
        if fileIsReadOnly || folderIsReadOnly {
            throw DatabaseError.readOnly(path: url.path)
        }
    }
}
