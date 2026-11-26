import Foundation
import Lighter
import Testing
@testable import Commis

@Test("list --json writes the versioned envelope")
func jsonEnvelope() throws {
    try withTemporaryDatabase { db in
        let commission = try db.insert(makeRecord(client: "Alberta Mann"))
        let ych = try db.insert(makeRecord(client: "Bessie", type: .ych))

        let object = try jsonObject(try JSON.records(try db.fetchRecords()))

        #expect(object["version"] as? Int == 1)
        let records = try #require(object["records"] as? [[String: Any]])
        #expect(records.count == 2)

        let commissionRow = try #require(records.first { $0["id"] as? Int == commission.id })
        #expect(commissionRow["client"] as? String == "Alberta Mann")
        #expect(commissionRow["type"] as? String == "commission")
        #expect(commissionRow["reference"] as? String == "https://example.com/ref")
        #expect(commissionRow["description"] as? String == "Test description")
        #expect(commissionRow["fee"] as? Double == 12.5)
        #expect(commissionRow["status"] as? String == "pending")
        #expect(commissionRow["date"] as? String == "2026-01-01T00:00:00Z")

        // Absent values are null, and the key is still there.
        #expect(commissionRow["title"] is NSNull)
        #expect(commissionRow["slot"] is NSNull)

        let ychRow = try #require(records.first { $0["id"] as? Int == ych.id })
        #expect(ychRow["type"] as? String == "ych")
        #expect(ychRow["title"] as? String == "Synthesize")
        #expect(ychRow["slot"] as? Int == 4)
    }
}

@Test("An empty database writes an empty record list")
func jsonEmptyList() throws {
    try withTemporaryDatabase { db in
        let object = try jsonObject(try JSON.records(try db.fetchRecords()))

        #expect(object["version"] as? Int == 1)
        #expect((object["records"] as? [Any])?.isEmpty == true)
    }
}

@Test("info reports the database, its schema, and its record count")
func jsonInfo() throws {
    let url = temporaryDatabaseURL()
    defer { try? FileManager.default.removeItem(at: url) }

    let db = try Commis.open(at: url)
    _ = try db.insert(makeRecord())
    _ = try db.insert(makeRecord(client: "Bessie", type: .ych))

    let payload = try Info.payload(database: url, db: db)
    #expect(payload.database == url.path)
    #expect(payload.schema == Commis.userVersion)
    #expect(payload.records == 2)
    #expect(payload.version == 1)
    #expect(!payload.commis.isEmpty)

    let object = try jsonObject(try JSON.info(payload))
    #expect(object["version"] as? Int == 1)
    #expect(object["database"] as? String == url.path)
    #expect(object["schema"] as? Int == Commis.userVersion)
    #expect(object["records"] as? Int == 2)
    #expect(object["commis"] as? String == payload.commis)
}

/// Parses JSON text into an object, failing the test when it isn't one.
private func jsonObject(_ text: String) throws -> [String: Any] {
    try #require(JSONSerialization.jsonObject(with: Data(text.utf8)) as? [String: Any])
}
