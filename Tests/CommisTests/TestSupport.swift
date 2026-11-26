import Foundation
import Lighter
import Testing
@testable import Commis

/// Creates a throwaway database file and removes it when the test finishes.
@discardableResult
func withTemporaryDatabase<T>(
    _ body: (Commis) throws -> T
) throws -> T {
    let url = temporaryDatabaseURL()
    defer { try? FileManager.default.removeItem(at: url) }
    return try body(try Commis.open(at: url))
}

/// A unique path under the temporary directory, not yet created.
func temporaryDatabaseURL() -> URL {
    FileManager.default.temporaryDirectory
        .appendingPathComponent("commis-test-\(UUID().uuidString)")
        .appendingPathExtension("sqlite3")
}

/// A commission record with sensible defaults for storage tests.
func makeRecord(
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
        title: type == .ych ? "Synthesize" : nil,
        slot: type == .ych ? 4 : nil,
        status: status.rawValue
    )
}

/// An open raffle with sensible defaults for storage tests.
func makeRaffle(
    title: String = "Synthesize",
    prize: String = "full-body YCH",
    status: RaffleStatus = .open
) -> Raffles {
    Raffles(
        date: "2026-01-01T00:00:00Z",
        title: title,
        prize: prize,
        ticketPrice: 5,
        opens: nil,
        ends: nil,
        status: status.rawValue
    )
}

/// An entry in raffle `raffleId`, for picker tests that do not touch SQLite.
func makeEntry(
    _ name: String,
    tickets: Int = 1,
    id: Int = -1,
    raffleId: Int = 1
) -> RaffleEntries {
    RaffleEntries(
        id: id,
        raffleId: raffleId,
        name: name,
        reference: "",
        tickets: tickets,
        entered: "2026-01-01T00:00:00Z"
    )
}
