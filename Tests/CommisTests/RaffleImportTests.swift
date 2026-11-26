import Testing
@testable import Commis

// MARK: - Plain text

@Test("Text import reads one name per line")
func textImportReadsNames() {
    let drafts = RaffleEntryImport.parseText("Bessie\nAda\n")

    #expect(drafts.map(\.name) == ["Bessie", "Ada"])
    #expect(drafts.allSatisfy { $0.tickets == 1 })
    #expect(drafts.allSatisfy { $0.reference.isEmpty })
}

@Test("Text import trims whitespace and skips blank lines")
func textImportSkipsBlankLines() {
    let drafts = RaffleEntryImport.parseText("  Bessie  \n\n\t\nAda")

    #expect(drafts.map(\.name) == ["Bessie", "Ada"])
}

// MARK: - CSV

@Test("CSV import reads name, reference, and tickets")
func csvImportReadsAllColumns() throws {
    let csv = """
    name,reference,tickets
    Bessie,https://example.com/bessie,3
    Ada,,1
    """

    let drafts = try RaffleEntryImport.parseCSV(csv)

    #expect(drafts.count == 2)
    #expect(drafts[0].name == "Bessie")
    #expect(drafts[0].reference == "https://example.com/bessie")
    #expect(drafts[0].tickets == 3)
    #expect(drafts[1].name == "Ada")
    #expect(drafts[1].reference.isEmpty)
    #expect(drafts[1].tickets == 1)
}

@Test("CSV import accepts headers in any order and case")
func csvImportAcceptsHeaderOrder() throws {
    let csv = """
    Tickets,NAME
    2,Bessie
    """

    let drafts = try RaffleEntryImport.parseCSV(csv)

    #expect(drafts == [RaffleEntryDraft(name: "Bessie", reference: "", tickets: 2)])
}

@Test("CSV import defaults optional columns when the header omits them")
func csvImportOptionalColumns() throws {
    let drafts = try RaffleEntryImport.parseCSV("name\nBessie\nAda\n")

    #expect(drafts.map(\.tickets) == [1, 1])
    #expect(drafts.allSatisfy { $0.reference.isEmpty })
}

@Test("CSV import strips a UTF-8 byte order mark")
func csvImportStripsBOM() throws {
    let csv = "\u{FEFF}name\nBessie\n"

    #expect(try RaffleEntryImport.parseCSV(csv).map(\.name) == ["Bessie"])
}

@Test("CSV import skips blank lines")
func csvImportSkipsBlankLines() throws {
    let csv = "name\nBessie\n\nAda\n"

    #expect(try RaffleEntryImport.parseCSV(csv).map(\.name) == ["Bessie", "Ada"])
}

@Test("Quoted CSV fields may contain commas")
func csvImportHandlesQuotedCommas() throws {
    let csv = """
    name,reference
    "Mann, Alberta",https://example.com/ref
    """

    let drafts = try RaffleEntryImport.parseCSV(csv)

    #expect(drafts.map(\.name) == ["Mann, Alberta"])
}

@Test("A CSV without a name column is rejected")
func csvImportRequiresNameColumn() {
    #expect(throws: RaffleImportError.missingNameColumn) {
        try RaffleEntryImport.parseCSV("reference,tickets\nhttps://example.com,1\n")
    }
}

@Test("An empty name is reported with its line number")
func csvImportReportsEmptyName() {
    #expect(throws: RaffleImportError.emptyName(line: 3)) {
        try RaffleEntryImport.parseCSV(
            "name,reference\nBessie,https://example.com/bessie\n,https://example.com/ada\n"
        )
    }
}

@Test("Invalid tickets are reported with their line number")
func csvImportReportsInvalidTickets() {
    #expect(throws: RaffleImportError.invalidTickets(line: 3, value: "many")) {
        try RaffleEntryImport.parseCSV("name,tickets\nBessie,1\nAda,many\n")
    }
    #expect(throws: RaffleImportError.invalidTickets(line: 2, value: "0")) {
        try RaffleEntryImport.parseCSV("name,tickets\nBessie,0\n")
    }
}
