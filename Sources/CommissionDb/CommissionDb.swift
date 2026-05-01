import ArgumentParser
import Foundation

func csvExport(buyer: String, reference: String, desc: String = "",
               payment: String, fee: String,
               isYch: Bool = false, title: String = "", slot: Int = 0)
{
    let commHeader = "Buyer,Reference,Payment,Description,Fee"
    let ychHeader = "Buyer,Reference,Slot,Payment,Fee"

    switch isYch {
    case true:
        let ychCsv = """
        "\(title)","\(buyer)","\(reference)","\(payment)","\(slot)","\(fee)"
        """
        let ych = """
        \(ychHeader)
        \(ychCsv)
        """
        print(ych)
    default:
        let commCsv = """
        "\(buyer)","\(reference)","\(payment)","\(desc)","\(fee)"
        """
        let comm = """
        \(commHeader)
        \(commCsv)
        """
        print(comm)
    }
}

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
        var payment: String

        @Option(name: .shortAndLong)
        var title: String

        @Option(name: .long)
        var bid: String

        @Option(name: .shortAndLong)
        var slot: Int

        mutating func run() {
            csvExport(buyer: buyer, reference: reference,
                      payment: payment, fee: bid,
                      isYch: true, title: title, slot: slot)
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
    var payment: String

    @Option(name: .shortAndLong)
    var description: String

    @Option(name: .shortAndLong)
    var fee: String

    mutating func run() throws {
        csvExport(buyer: buyer, reference: reference, desc: description,
                  payment: payment, fee: fee)
    }
}
