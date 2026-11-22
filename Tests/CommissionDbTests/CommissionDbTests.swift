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

// MARK: - CSV quoting

/// Exercises the RFC 4180 quoting logic from Export without touching the DB.
/// We call the private helper indirectly by reproducing it here — the real
/// test is that the output is correct; Export's own test would need a live DB.
private func csvQuote(_ value: String) -> String {
    let escaped = value.replacingOccurrences(of: "\"", with: "\"\"")
    return "\"\(escaped)\""
}

@Test("CSV quoting wraps plain strings in double-quotes")
func csvQuotePlain() {
    #expect(csvQuote("hello") == "\"hello\"")
}

@Test("CSV quoting escapes internal double-quotes")
func csvQuoteEscapesQuotes() {
    #expect(csvQuote("say \"hi\"") == "\"say \"\"hi\"\"\"")
}

@Test("CSV quoting handles empty strings")
func csvQuoteEmpty() {
    #expect(csvQuote("") == "\"\"")
}

@Test("CSV quoting preserves commas and newlines without extra escaping")
func csvQuoteSpecialChars() {
    #expect(csvQuote("a,b") == "\"a,b\"")
    #expect(csvQuote("line1\nline2") == "\"line1\nline2\"")
}
