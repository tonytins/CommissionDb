import Testing
@testable import CommissionDb

// MARK: - Status

@Test("Status raw values match SQL CHECK constraint values")
func statusRawValues() {
    #expect(Status.pending.rawValue   == "pending")
    #expect(Status.paid.rawValue      == "paid")
    #expect(Status.cancelled.rawValue == "cancelled")
    #expect(Status.refunded.rawValue  == "refunded")
}

@Test("Status round-trips through rawValue")
func statusRoundTrip() {
    for status in Status.allCases {
        #expect(Status(rawValue: status.rawValue) == status)
    }
}

@Test("Unknown status string returns nil")
func statusUnknownReturnsNil() {
    #expect(Status(rawValue: "unknown") == nil)
    #expect(Status(rawValue: "") == nil)
}

// MARK: - RecordType

@Test("RecordType raw values match SQL CHECK constraint values")
func recordTypeRawValues() {
    #expect(RecordType.commission.rawValue == "commission")
    #expect(RecordType.ych.rawValue        == "ych")
}

@Test("RecordType round-trips through rawValue")
func recordTypeRoundTrip() {
    for type_ in RecordType.allCases {
        #expect(RecordType(rawValue: type_.rawValue) == type_)
    }
}

// MARK: - CSV

@Test("CSV field wraps plain strings in double-quotes")
func csvFieldPlain() {
    #expect(CSV.field("hello") == "\"hello\"")
}

@Test("CSV field escapes internal double-quotes")
func csvFieldEscapesQuotes() {
    #expect(CSV.field("say \"hi\"") == "\"say \"\"hi\"\"\"")
}

@Test("CSV field handles empty strings")
func csvFieldEmpty() {
    #expect(CSV.field("") == "\"\"")
}

@Test("CSV field preserves commas and newlines without extra escaping")
func csvFieldSpecialChars() {
    #expect(CSV.field("a,b") == "\"a,b\"")
    #expect(CSV.field("line1\nline2") == "\"line1\nline2\"")
}

@Test("CSV row joins quoted fields with commas")
func csvRowJoinsFields() {
    #expect(CSV.row(["a", "b,c", ""]) == "\"a\",\"b,c\",\"\"")
}
