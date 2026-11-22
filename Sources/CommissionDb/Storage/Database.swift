import Lighter

extension Commissiondb {
    /// Opens the database, creating the file and schema on first use.
    static func open() throws -> Commissiondb {
        try bootstrap(at: AppDataPath.databaseURL)
    }

    /// Fetches records, optionally filtered by status and type.
    func fetchRecords(status: Status? = nil, type: RecordType? = nil) throws -> [Records] {
        let statuses = status.map { [$0.rawValue] } ?? Status.allCases.map(\.rawValue)
        let types    = type.map   { [$0.rawValue] } ?? RecordType.allCases.map(\.rawValue)
        return try self.records.fetch {
            $0.status.in(statuses) && $0.type.in(types)
        }
    }
}
