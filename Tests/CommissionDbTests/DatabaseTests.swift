import Foundation
import Lighter
import Testing
@testable import CommissionDb

/// Creates a throwaway database file and removes it when the test finishes.
private func withTemporaryDatabase<T>(
    _ body: (Commissiondb) throws -> T
) throws -> T {
    let url = FileManager.default.temporaryDirectory
        .appendingPathComponent("cdb-test-\(UUID().uuidString)")
        .appendingPathExtension("sqlite3")
    defer { try? FileManager.default.removeItem(at: url) }
    return try body(try Commissiondb.bootstrap(at: url))
}

private func makeRecord(
    client: String = "Test Client",
    type: RecordType = .commission,
    status: Status = .pending
) -> Records {
    Records(
        date: "2026-01-01T00:00:00Z",
        type: type.rawValue,
        client: client,
        reference: "https://example.com/ref",
        description: "Test description",
        fee: 12.5,
        ychTitle: type == .ych ? "Synthesize" : nil,
        slot: type == .ych ? 4 : nil,
        status: status.rawValue
    )
}

@Test("Bootstrap creates the schema and round-trips a record")
func databaseRoundTrip() throws {
    try withTemporaryDatabase { db in
        let inserted = try db.insert(makeRecord(client: "Alberta Mann"))
        #expect(inserted.id > 0)

        let fetched = try db.records.find(inserted.id)
        #expect(fetched?.client == "Alberta Mann")
        #expect(fetched?.fee == 12.5)
        #expect(fetched?.status == "pending")
    }
}

@Test("Status updates persist")
func databaseStatusUpdate() throws {
    try withTemporaryDatabase { db in
        var record = try db.insert(makeRecord())
        record.status = Status.paid.rawValue
        try db.update(record)

        #expect(try db.records.find(record.id)?.status == "paid")
    }
}

@Test("Filtering by status and type matches only requested records")
func databaseFiltering() throws {
    try withTemporaryDatabase { db in
        _ = try db.insert(makeRecord(client: "Pending commission"))
        _ = try db.insert(makeRecord(client: "Paid commission", status: .paid))
        _ = try db.insert(makeRecord(client: "Paid YCH", type: .ych, status: .paid))

        let paid = try db.records.fetch { $0.status.in(["paid"]) }
        #expect(paid.count == 2)

        let paidYch = try db.records.fetch {
            $0.status.in(["paid"]) && $0.type.in(["ych"])
        }
        #expect(paidYch.map(\.client) == ["Paid YCH"])
    }
}
