import ArgumentParser

/// Lifecycle of a raffle.
enum RaffleStatus: String, CaseIterable, Sendable {
    case open
    case drawn
    case fulfilled
    case cancelled
}

extension RaffleStatus: ExpressibleByArgument {}
