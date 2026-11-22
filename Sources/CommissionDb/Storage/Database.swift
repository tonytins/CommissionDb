import Foundation
import Lighter

extension Commissiondb {
    /// Opens the database at the platform data path.
    static func open() throws -> Commissiondb {
        try open(at: AppDataPath.databaseURL)
    }

    /// Opens the database at `url`, migrating an existing file if needed.
    ///
    /// A missing file is created with the latest schema by `bootstrap`, which
    /// also records the schema version. An existing file is migrated in place.
    static func open(at url: URL) throws -> Commissiondb {
        let exists = FileManager.default.fileExists(atPath: url.path)
        let db = try bootstrap(at: url)
        if exists {
            try Migrations.apply(to: db)
        }
        return db
    }

    /// Fetches records, optionally filtered by status and type.
    func fetchRecords(status: Status? = nil, type: RecordType? = nil) throws -> [Records] {
        let statuses = status.map { [$0.rawValue] } ?? Status.allCases.map(\.rawValue)
        let types    = type.map   { [$0.rawValue] } ?? RecordType.allCases.map(\.rawValue)
        return try self.records.fetch {
            $0.status.in(statuses) && $0.type.in(types)
        }
    }

    /// Fetches raffles, optionally filtered by status.
    func fetchRaffles(status: RaffleStatus? = nil) throws -> [Raffles] {
        let statuses = status.map { [$0.rawValue] } ?? RaffleStatus.allCases.map(\.rawValue)
        return try self.raffles.fetch(orderBy: \.id) {
            $0.status.in(statuses)
        }
    }

    /// Fetches the entries of a raffle in entry order.
    func fetchEntries(raffleId: Int) throws -> [RaffleEntries] {
        try self.raffleEntries.fetch(orderBy: \.id) {
            $0.raffleId == raffleId
        }
    }
}
