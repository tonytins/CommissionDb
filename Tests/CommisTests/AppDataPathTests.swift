import Foundation
import Testing
@testable import Commis

/// A throwaway application-support directory.
private func withTemporaryBase<T>(_ body: (URL) throws -> T) throws -> T {
    let base = FileManager.default.temporaryDirectory
        .appendingPathComponent("commis-path-\(UUID().uuidString)", isDirectory: true)
    defer { try? FileManager.default.removeItem(at: base) }
    return try body(base)
}

/// Creates an empty file at `url`, including parent directories.
private func touch(_ url: URL) throws {
    try FileManager.default.createDirectory(
        at: url.deletingLastPathComponent(),
        withIntermediateDirectories: true
    )
    guard FileManager.default.createFile(atPath: url.path, contents: nil) else {
        throw CocoaError(.fileWriteUnknown)
    }
}

@Test("A new database lives in the commis directory")
func newDatabasePathUsesCommisDirectory() throws {
    try withTemporaryBase { base in
        let url = try AppDataPath.databaseURL(in: base)

        #expect(url.lastPathComponent == "commis.sqlite3")
        #expect(url.deletingLastPathComponent().lastPathComponent == "commis")
        #expect(FileManager.default.fileExists(
            atPath: url.deletingLastPathComponent().path
        ))
    }
}

@Test("A pre-rename database is used when the new file is absent")
func legacyDatabasePathIsUsed() throws {
    try withTemporaryBase { base in
        let legacy = base
            .appendingPathComponent("cdb", isDirectory: true)
            .appendingPathComponent("commissiondb.sqlite3")
        try touch(legacy)

        #expect(try AppDataPath.databaseURL(in: base) == legacy)
        #expect(!FileManager.default.fileExists(
            atPath: base.appendingPathComponent("commis").path
        ))
    }
}

@Test("The new database wins when both paths exist")
func newDatabasePathWinsOverLegacy() throws {
    try withTemporaryBase { base in
        let url = base
            .appendingPathComponent("commis", isDirectory: true)
            .appendingPathComponent("commis.sqlite3")
        let legacy = base
            .appendingPathComponent("cdb", isDirectory: true)
            .appendingPathComponent("commissiondb.sqlite3")
        try touch(url)
        try touch(legacy)

        #expect(try AppDataPath.databaseURL(in: base) == url)
    }
}
