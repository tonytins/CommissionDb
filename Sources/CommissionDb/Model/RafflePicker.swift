import Lighter

/// Picks raffle winners at random, weighted by ticket count.
enum RafflePicker {
    /// Picks `count` distinct entries from `entries`, weighted by tickets.
    ///
    /// Returns fewer than `count` only when `entries` is smaller than `count`.
    static func winners(
        from entries: [RaffleEntries],
        count: Int,
        using rng: inout some RandomNumberGenerator
    ) -> [RaffleEntries] {
        guard count > 0 else { return [] }

        var pool = entries.filter { $0.tickets > 0 }
        var picked: [RaffleEntries] = []
        picked.reserveCapacity(min(count, pool.count))

        while picked.count < count, !pool.isEmpty {
            let totalTickets = pool.reduce(0) { $0 + $1.tickets }
            guard totalTickets > 0 else { break }

            var ticket = Int.random(in: 0..<totalTickets, using: &rng)
            var index = 0
            for (i, entry) in pool.enumerated() {
                if ticket < entry.tickets {
                    index = i
                    break
                }
                ticket -= entry.tickets
            }
            picked.append(pool.remove(at: index))
        }

        return picked
    }

    /// Picks winners using the system random number generator.
    static func winners(from entries: [RaffleEntries], count: Int) -> [RaffleEntries] {
        var rng = SystemRandomNumberGenerator()
        return winners(from: entries, count: count, using: &rng)
    }
}
