import ArgumentParser
import Foundation
import Lighter
import Testing
@testable import Commis

// MARK: - Read-only databases

@Test("A read-only database is refused before any write")
func readOnlyFileIsRefused() throws {
    let url = try databaseWithOneRecord()
    defer { removeDatabase(url) }
    try FileManager.default.setAttributes([.posixPermissions: 0o444], ofItemAtPath: url.path)

    #expect(throws: DatabaseError.readOnly(path: url.path)) {
        try Commis.withDatabase(at: url, dryRun: false) { db in
            try db.insert(makeRecord())
        }
    }
}

@Test("A database in a read-only folder is refused, since SQLite writes its journal there")
func readOnlyFolderIsRefused() throws {
    let folder = FileManager.default.temporaryDirectory
        .appendingPathComponent("commis-test-\(UUID().uuidString)", isDirectory: true)
    try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
    let url = folder.appendingPathComponent("commis.sqlite3")
    _ = try Commis.open(at: url)
    try FileManager.default.setAttributes([.posixPermissions: 0o555], ofItemAtPath: folder.path)
    defer {
        try? FileManager.default.setAttributes([.posixPermissions: 0o755], ofItemAtPath: folder.path)
        try? FileManager.default.removeItem(at: folder)
    }

    #expect(throws: DatabaseError.readOnly(path: url.path)) {
        try Commis.requireWritable(url)
    }
}

@Test("A writable database passes the check")
func writableDatabasePasses() throws {
    let url = try databaseWithOneRecord()
    defer { removeDatabase(url) }

    try Commis.requireWritable(url)
}

@Test("A database that doesn't exist yet passes the check, so a first run can create it")
func missingDatabasePasses() throws {
    try Commis.requireWritable(temporaryDatabaseURL())
}

@Test("The read-only error names the file")
func readOnlyErrorNamesTheFile() {
    #expect(
        DatabaseError.readOnly(path: "/data/commis.sqlite3").description
            == "Can't change the database at /data/commis.sqlite3: it or its folder is read-only."
    )
}

// MARK: - Dry runs

@Test("A dry run sees its own changes and keeps none of them")
func dryRunKeepsNothing() throws {
    let url = try databaseWithOneRecord()
    defer { removeDatabase(url) }

    let seen = try Commis.withDatabase(at: url, dryRun: true) { db in
        let inserted = try db.insert(makeRecord(client: "Dry"))
        #expect(inserted.id == 2)
        return try db.records.fetchCount()
    }

    #expect(seen == 2)
    #expect(try Commis.open(at: url).records.fetchCount() == 1)
}

@Test("A dry run works on a read-only database")
func dryRunWorksReadOnly() throws {
    let url = try databaseWithOneRecord()
    defer { removeDatabase(url) }
    try FileManager.default.setAttributes([.posixPermissions: 0o444], ofItemAtPath: url.path)

    let count = try Commis.withDatabase(at: url, dryRun: true) { db in
        try db.insert(makeRecord())
        return try db.records.fetchCount()
    }

    #expect(count == 2)
}

@Test("A dry run with no database yet creates none")
func dryRunCreatesNothing() throws {
    let url = temporaryDatabaseURL()

    try Commis.withDatabase(at: url, dryRun: true) { db in
        try db.insert(makeRecord())
    }

    #expect(!FileManager.default.fileExists(atPath: url.path))
}

@Test("A dry run that fails reports the command's own error")
func dryRunReportsErrors() throws {
    let url = try databaseWithOneRecord()
    defer { removeDatabase(url) }

    #expect(throws: RaffleError.notFound(99)) {
        try Commis.withDatabase(at: url, dryRun: true) { db in
            try db.cancel(99)
        }
    }
}

@Test("Every command that writes takes --dry-run", arguments: [
    ["add", "--client", "A", "--reference", "r", "--fee", "1", "--dry-run"],
    ["status", "1", "--set", "paid", "--dry-run"],
    ["raffle", "add", "--title", "T", "--dry-run"],
    ["raffle", "enter", "1", "--name", "A", "--dry-run"],
    ["raffle", "import", "1", "entries.txt", "--dry-run"],
    ["raffle", "draw", "1", "--dry-run"],
    ["raffle", "fulfill", "1", "--type", "ych", "--dry-run"],
    ["raffle", "cancel", "1", "--dry-run"],
])
func everyWriteTakesDryRun(arguments: [String]) throws {
    let command = try CommisCommand.parseAsRoot(arguments)
    let options = Mirror(reflecting: command).children
        .compactMap { $0.value as? OptionGroup<WriteOptions> }
        .first
    #expect(options?.wrappedValue.dryRun == true)
}

// MARK: - Helpers

private func databaseWithOneRecord() throws -> URL {
    let url = temporaryDatabaseURL()
    try Commis.open(at: url).insert(makeRecord())
    return url
}

private func removeDatabase(_ url: URL) {
    try? FileManager.default.removeItem(at: url)
}
