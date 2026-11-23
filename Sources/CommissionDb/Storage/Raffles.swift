import Lighter

/// Errors raised while operating on raffles.
///
/// Messages are written for the person running the command, so they name the
/// raffle, its current state, and what was expected.
enum RaffleError: Error, Equatable, CustomStringConvertible {
    case notFound(Int)
    case closedForEntries(id: Int, status: String)
    case notOpenForDraw(id: Int, status: String)
    case notDrawn(id: Int, status: String)
    case notCancellable(id: Int, status: String)
    case noEntries(Int)
    case notEnoughEntries(id: Int, entries: Int, winners: Int)
    case noWinners(Int)

    var description: String {
        switch self {
        case .notFound(let id):
            return "No raffle with ID \(id)."
        case .closedForEntries(let id, let status):
            return "Raffle #\(id) is \(status); only open raffles accept entries."
        case .notOpenForDraw(let id, let status):
            return "Raffle #\(id) is \(status); only open raffles can be drawn."
        case .notDrawn(let id, let status):
            return "Raffle #\(id) is \(status); draw it before fulfilling."
        case .notCancellable(let id, let status):
            return "Raffle #\(id) is \(status); only open or drawn raffles can be cancelled."
        case .noEntries(let id):
            return "Raffle #\(id) has no entries."
        case .notEnoughEntries(let id, let entries, let winners):
            return "Raffle #\(id) has \(entries) entries; cannot draw \(winners) winners."
        case .noWinners(let id):
            return "Raffle #\(id) has no winners."
        }
    }
}

extension Commissiondb {
    /// Fetches a raffle, or throws `RaffleError.notFound`.
    func raffle(_ id: Int) throws -> Raffles {
        guard let raffle = try self.raffles.find(id) else {
            throw RaffleError.notFound(id)
        }
        return raffle
    }

    /// Adds one entry to an open raffle.
    @discardableResult
    func enter(_ raffleId: Int, draft: RaffleEntryDraft) throws -> RaffleEntries {
        try requireOpenForEntries(raffleId)
        return try insert(
            RaffleEntries(
                raffleId: raffleId,
                name: draft.name,
                reference: draft.reference,
                tickets: draft.tickets,
                entered: Timestamps.now()
            )
        )
    }

    /// Adds several entries to an open raffle as one atomic batch.
    @discardableResult
    func enter(_ raffleId: Int, drafts: [RaffleEntryDraft]) throws -> [RaffleEntries] {
        guard !drafts.isEmpty else { return [] }
        try requireOpenForEntries(raffleId)

        let entered = Timestamps.now()
        return try transaction { tx in
            try drafts.map { draft in
                try tx.insert(
                    RaffleEntries(
                        raffleId: raffleId,
                        name: draft.name,
                        reference: draft.reference,
                        tickets: draft.tickets,
                        entered: entered
                    )
                )
            }
        }
    }

    /// Draws `count` distinct winners from an open raffle, weighted by tickets.
    ///
    /// Marks every winner with the draw time and moves the raffle to `drawn`.
    /// Returns the winners in the order they were picked.
    func draw(
        _ raffleId: Int,
        winners count: Int,
        using rng: inout some RandomNumberGenerator
    ) throws -> [RaffleEntries] {
        var raffle = try raffle(raffleId)
        guard raffle.status == RaffleStatus.open.rawValue else {
            throw RaffleError.notOpenForDraw(id: raffleId, status: raffle.status)
        }

        let entries = try fetchEntries(raffleId: raffleId)
        guard !entries.isEmpty else {
            throw RaffleError.noEntries(raffleId)
        }
        guard count <= entries.count else {
            throw RaffleError.notEnoughEntries(
                id: raffleId, entries: entries.count, winners: count
            )
        }

        let picked = RafflePicker.winners(from: entries, count: count, using: &rng)
        let drawnAt = Timestamps.now()

        try transaction { tx in
            for entry in picked {
                var updated = entry
                updated.wonAt = drawnAt
                try tx.update(updated)
            }
            raffle.status = RaffleStatus.drawn.rawValue
            try tx.update(raffle)
        }

        return picked
    }

    /// Draws winners using the system random number generator.
    func draw(_ raffleId: Int, winners count: Int) throws -> [RaffleEntries] {
        var rng = SystemRandomNumberGenerator()
        return try draw(raffleId, winners: count, using: &rng)
    }

    /// Turns every winner of a drawn raffle into a pending fee-0 record.
    ///
    /// Returns the created records in entry order and moves the raffle to
    /// `fulfilled`.
    @discardableResult
    func fulfill(_ raffleId: Int, type: RecordType) throws -> [Records] {
        var raffle = try raffle(raffleId)
        guard raffle.status == RaffleStatus.drawn.rawValue else {
            throw RaffleError.notDrawn(id: raffleId, status: raffle.status)
        }

        let winners = try fetchEntries(raffleId: raffleId).filter { $0.wonAt != nil }
        guard !winners.isEmpty else {
            throw RaffleError.noWinners(raffleId)
        }

        let description = raffle.prize.isEmpty ? raffle.title : raffle.prize
        let date = Timestamps.now()

        return try transaction { tx in
            let created = try winners.map { winner in
                try tx.insert(
                    Records(
                        date: date,
                        type: type.rawValue,
                        client: winner.name,
                        reference: winner.reference,
                        description: description,
                        fee: 0,
                        title: raffle.title,
                        slot: nil,
                        status: Status.pending.rawValue
                    )
                )
            }
            raffle.status = RaffleStatus.fulfilled.rawValue
            try tx.update(raffle)
            return created
        }
    }

    /// Cancels an open or drawn raffle. Data is kept.
    func cancel(_ raffleId: Int) throws {
        var raffle = try raffle(raffleId)
        guard raffle.status == RaffleStatus.open.rawValue
           || raffle.status == RaffleStatus.drawn.rawValue
        else {
            throw RaffleError.notCancellable(id: raffleId, status: raffle.status)
        }

        raffle.status = RaffleStatus.cancelled.rawValue
        try update(raffle)
    }

    private func requireOpenForEntries(_ raffleId: Int) throws {
        let raffle = try raffle(raffleId)
        guard raffle.status == RaffleStatus.open.rawValue else {
            throw RaffleError.closedForEntries(id: raffleId, status: raffle.status)
        }
    }
}
