import Testing
@testable import Commis

/// Deterministic generator so draw tests are repeatable.
private struct SeededGenerator: RandomNumberGenerator {
    private var state: UInt64

    init(seed: UInt64) {
        state = seed &* 2_862_933_555_777_941_757 &+ 3_037_000_493
    }

    mutating func next() -> UInt64 {
        state = state &* 6_364_136_223_846_793_005 &+ 1_442_695_040_888_963_407
        var value = state
        value = (value ^ (value >> 30)) &* 0xBF58_476D_1CE4_E5B9
        value = (value ^ (value >> 27)) &* 0x94D0_49BB_1331_11EB
        return value ^ (value >> 31)
    }
}

@Test("A draw returns the requested number of distinct winners")
func drawReturnsDistinctWinners() {
    let entries = (1...10).map { makeEntry("Entrant \($0)", id: $0) }
    var rng = SeededGenerator(seed: 42)

    let winners = RafflePicker.winners(from: entries, count: 5, using: &rng)

    #expect(winners.count == 5)
    #expect(Set(winners.map(\.id)).count == 5)
}

@Test("The same seed produces the same winners")
func drawIsDeterministicForASeed() {
    let entries = (1...8).map { makeEntry("Entrant \($0)", id: $0) }
    var first = SeededGenerator(seed: 7)
    var second = SeededGenerator(seed: 7)

    let a = RafflePicker.winners(from: entries, count: 3, using: &first)
    let b = RafflePicker.winners(from: entries, count: 3, using: &second)

    #expect(a.map(\.id) == b.map(\.id))
}

@Test("Drawing every entry returns all of them")
func drawAllEntries() {
    let entries = (1...4).map { makeEntry("Entrant \($0)", id: $0) }
    var rng = SeededGenerator(seed: 1)

    let winners = RafflePicker.winners(from: entries, count: 4, using: &rng)

    #expect(Set(winners.map(\.id)) == Set(entries.map(\.id)))
}

@Test("Asking for more winners than entries returns every entry")
func drawTooManyWinnersReturnsAll() {
    let entries = (1...3).map { makeEntry("Entrant \($0)", id: $0) }
    var rng = SeededGenerator(seed: 1)

    let winners = RafflePicker.winners(from: entries, count: 10, using: &rng)

    #expect(winners.count == 3)
}

@Test("Drawing zero winners returns nothing")
func drawZeroWinners() {
    let entries = [makeEntry("Solo", id: 1)]
    var rng = SeededGenerator(seed: 1)

    #expect(RafflePicker.winners(from: entries, count: 0, using: &rng).isEmpty)
}

@Test("Drawing from no entries returns nothing")
func drawNoEntries() {
    var rng = SeededGenerator(seed: 1)

    #expect(RafflePicker.winners(from: [], count: 1, using: &rng).isEmpty)
}

@Test("More tickets make an entry more likely to win")
func ticketsWeightTheDraw() {
    let entries = [
        makeEntry("Frequent", tickets: 9, id: 1),
        makeEntry("Rare", tickets: 1, id: 2),
    ]

    var frequentWins = 0
    for seed in 0..<200 {
        var rng = SeededGenerator(seed: UInt64(seed))
        let winner = RafflePicker.winners(from: entries, count: 1, using: &rng)[0]
        if winner.id == 1 { frequentWins += 1 }
    }

    // With 9 of 10 tickets, the frequent entrant should win most draws.
    #expect(frequentWins > 150)
    #expect(frequentWins < 200)
}
