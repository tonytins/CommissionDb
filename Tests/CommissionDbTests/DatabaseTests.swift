import Foundation
import Lighter
import Testing
@testable import CommissionDb

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
