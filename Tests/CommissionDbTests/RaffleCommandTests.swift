import Lighter
import Testing
@testable import CommissionDb

// MARK: - Entering

@Test("Entries can be added to an open raffle")
func enterAddsEntry() throws {
    try withTemporaryDatabase { db in
        let raffle = try db.insert(makeRaffle())

        let entry = try db.enter(
            raffle.id,
            draft: RaffleEntryDraft(name: "Bessie", reference: "https://example.com", tickets: 3)
        )

        #expect(entry.id > 0)
        #expect(entry.raffleId == raffle.id)
        #expect(entry.tickets == 3)
        #expect(entry.wonAt == nil)
    }
}

@Test("Batch entry inserts every draft")
func enterAddsBatch() throws {
    try withTemporaryDatabase { db in
        let raffle = try db.insert(makeRaffle())
        let drafts = [
            RaffleEntryDraft(name: "Bessie", reference: "", tickets: 1),
            RaffleEntryDraft(name: "Ada", reference: "", tickets: 2),
        ]

        let inserted = try db.enter(raffle.id, drafts: drafts)

        #expect(inserted.map(\.name) == ["Bessie", "Ada"])
        #expect(try db.fetchEntries(raffleId: raffle.id).count == 2)
    }
}

@Test("A closed raffle rejects new entries")
func enterRejectsClosedRaffle() throws {
    try withTemporaryDatabase { db in
        let raffle = try db.insert(makeRaffle(status: .drawn))

        #expect(throws: RaffleError.closedForEntries(id: raffle.id, status: "drawn")) {
            try db.enter(
                raffle.id,
                draft: RaffleEntryDraft(name: "Bessie", reference: "", tickets: 1)
            )
        }
    }
}

@Test("Entering a missing raffle fails")
func enterMissingRaffle() throws {
    try withTemporaryDatabase { db in
        #expect(throws: RaffleError.notFound(99)) {
            try db.enter(
                99,
                draft: RaffleEntryDraft(name: "Bessie", reference: "", tickets: 1)
            )
        }
    }
}

// MARK: - Drawing

@Test("Drawing picks a winner, records it, and moves the raffle to drawn")
func drawMarksWinner() throws {
    try withTemporaryDatabase { db in
        let raffle = try db.insert(makeRaffle())
        _ = try db.enter(raffle.id, draft: RaffleEntryDraft(name: "Bessie", reference: "", tickets: 1))
        _ = try db.enter(raffle.id, draft: RaffleEntryDraft(name: "Ada", reference: "", tickets: 1))

        let winners = try db.draw(raffle.id, winners: 1)

        #expect(winners.count == 1)
        #expect(try db.raffle(raffle.id).status == RaffleStatus.drawn.rawValue)
        let entries = try db.fetchEntries(raffleId: raffle.id)
        let marked = entries.filter { $0.wonAt != nil }
        #expect(marked.map(\.id) == winners.map(\.id))
    }
}

@Test("An open raffle cannot be drawn twice")
func drawTwiceFails() throws {
    try withTemporaryDatabase { db in
        let raffle = try db.insert(makeRaffle())
        _ = try db.enter(raffle.id, draft: RaffleEntryDraft(name: "Bessie", reference: "", tickets: 1))
        _ = try db.draw(raffle.id, winners: 1)

        #expect(throws: RaffleError.notOpenForDraw(id: raffle.id, status: "drawn")) {
            try db.draw(raffle.id, winners: 1)
        }
    }
}

@Test("A raffle with no entries cannot be drawn")
func drawWithoutEntriesFails() throws {
    try withTemporaryDatabase { db in
        let raffle = try db.insert(makeRaffle())

        #expect(throws: RaffleError.noEntries(raffle.id)) {
            try db.draw(raffle.id, winners: 1)
        }
    }
}

@Test("Drawing more winners than entries fails")
func drawTooManyWinnersFails() throws {
    try withTemporaryDatabase { db in
        let raffle = try db.insert(makeRaffle())
        _ = try db.enter(raffle.id, draft: RaffleEntryDraft(name: "Bessie", reference: "", tickets: 1))

        #expect(throws: RaffleError.notEnoughEntries(id: raffle.id, entries: 1, winners: 2)) {
            try db.draw(raffle.id, winners: 2)
        }
    }
}

// MARK: - Fulfilling

@Test("Fulfilling creates a fee-0 record per winner and marks the raffle fulfilled")
func fulfillCreatesRecords() throws {
    try withTemporaryDatabase { db in
        let raffle = try db.insert(makeRaffle(title: "Synthesize", prize: "full-body YCH"))
        _ = try db.enter(
            raffle.id,
            draft: RaffleEntryDraft(name: "Bessie", reference: "https://example.com", tickets: 1)
        )
        let winners = try db.draw(raffle.id, winners: 1)

        let created = try db.fulfill(raffle.id, type: .ych)

        #expect(created.count == 1)
        #expect(created[0].client == winners[0].name)
        #expect(created[0].reference == "https://example.com")
        #expect(created[0].type == RecordType.ych.rawValue)
        #expect(created[0].fee == 0)
        #expect(created[0].title == "Synthesize")
        #expect(created[0].description == "full-body YCH")
        #expect(created[0].status == Status.pending.rawValue)
        #expect(try db.raffle(raffle.id).status == RaffleStatus.fulfilled.rawValue)
        #expect(try db.fetchRecords().count == 1)
    }
}

@Test("Fulfilling falls back to the raffle title when no prize is set")
func fulfillUsesTitleWithoutPrize() throws {
    try withTemporaryDatabase { db in
        let raffle = try db.insert(makeRaffle(prize: ""))
        _ = try db.enter(raffle.id, draft: RaffleEntryDraft(name: "Bessie", reference: "", tickets: 1))
        _ = try db.draw(raffle.id, winners: 1)

        #expect(try db.fulfill(raffle.id, type: .commission)[0].description == "Synthesize")
    }
}

@Test("An open raffle must be drawn before it can be fulfilled")
func fulfillRequiresDraw() throws {
    try withTemporaryDatabase { db in
        let raffle = try db.insert(makeRaffle())
        _ = try db.enter(raffle.id, draft: RaffleEntryDraft(name: "Bessie", reference: "", tickets: 1))

        #expect(throws: RaffleError.notDrawn(id: raffle.id, status: "open")) {
            try db.fulfill(raffle.id, type: .commission)
        }
    }
}

@Test("Fulfilling twice fails")
func fulfillTwiceFails() throws {
    try withTemporaryDatabase { db in
        let raffle = try db.insert(makeRaffle())
        _ = try db.enter(raffle.id, draft: RaffleEntryDraft(name: "Bessie", reference: "", tickets: 1))
        _ = try db.draw(raffle.id, winners: 1)
        _ = try db.fulfill(raffle.id, type: .commission)

        #expect(throws: RaffleError.notDrawn(id: raffle.id, status: "fulfilled")) {
            try db.fulfill(raffle.id, type: .commission)
        }
    }
}

// MARK: - Cancelling

@Test("An open raffle can be cancelled without deleting its entries")
func cancelOpenRaffle() throws {
    try withTemporaryDatabase { db in
        let raffle = try db.insert(makeRaffle())
        _ = try db.enter(raffle.id, draft: RaffleEntryDraft(name: "Bessie", reference: "", tickets: 1))

        try db.cancel(raffle.id)

        #expect(try db.raffle(raffle.id).status == RaffleStatus.cancelled.rawValue)
        #expect(try db.fetchEntries(raffleId: raffle.id).count == 1)
    }
}

@Test("A fulfilled raffle cannot be cancelled")
func cancelFulfilledFails() throws {
    try withTemporaryDatabase { db in
        let raffle = try db.insert(makeRaffle())
        _ = try db.enter(raffle.id, draft: RaffleEntryDraft(name: "Bessie", reference: "", tickets: 1))
        _ = try db.draw(raffle.id, winners: 1)
        _ = try db.fulfill(raffle.id, type: .commission)

        #expect(throws: RaffleError.notCancellable(id: raffle.id, status: "fulfilled")) {
            try db.cancel(raffle.id)
        }
    }
}

// MARK: - Listing

@Test("Raffles can be filtered by status")
func filterRafflesByStatus() throws {
    try withTemporaryDatabase { db in
        _ = try db.insert(makeRaffle(title: "Open"))
        _ = try db.insert(makeRaffle(title: "Drawn", status: .drawn))

        #expect(try db.fetchRaffles().count == 2)
        #expect(try db.fetchRaffles(status: .drawn).map(\.title) == ["Drawn"])
    }
}
