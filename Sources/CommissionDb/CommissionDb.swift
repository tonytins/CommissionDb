import ArgumentParser
import Foundation

func receipt(buyer _: String, reference _: String, currency _: String, desc _: String,
             fee _: String, isYch _: Bool = false, slot _: Int = 0) {}

extension CommissionDb {
    struct Ych: ParsableCommand {
        static let configuration = CommandConfiguration(
            commandName: "ych"
        )

        @Option(name: .shortAndLong)
        var buyer: String

        @Option(name: .shortAndLong)
        var reference: String

        @Option(name: .shortAndLong)
        var currency: String

        @Option(name: .shortAndLong)
        var description: String

        @Option(name: .long)
        var bid: String

        @Option(name: .shortAndLong)
        var slot: Int

        mutating func run() {
            receipt(buyer: buyer, reference: reference,
                    currency: currency, desc: description,
                    fee: bid, isYch: true, slot: slot)
        }
    }
}

@main
struct CommissionDb: ParsableCommand {
    @Option(name: .shortAndLong)
    var buyer: String

    @Option(name: .shortAndLong)
    var reference: String

    @Option(name: .shortAndLong)
    var currency: String

    @Option(name: .shortAndLong)
    var description: String

    @Option(name: .shortAndLong)
    var fee: String

    mutating func run() throws {
        receipt(buyer: buyer, reference: reference,
                currency: currency, desc: description, fee: fee)
        print("Hello, world!")
    }
}
