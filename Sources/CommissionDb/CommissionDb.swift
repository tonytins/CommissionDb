import ArgumentParser
import Foundation

func csvExport(buyer: String, reference: String, desc: String = "",
               payment: String, fee: String,
               isYch: Bool = false, title: String = "", slot: Int = 0)
{
    let commHeader = "Date,Buyer,Reference,Payment,Description,Fee"
    let ychHeader = "Date,Buyer,Reference,Slot,Payment,Fee"
    let date = Date()

    switch isYch {
    case true:
        let ychCsv = """
        "\(date)","\(title)","\(buyer)","\(reference)","\(payment)","\(slot)","\(fee)"
        """
        let ych = """
        \(ychHeader)
        \(ychCsv)
        """
        print(ych)
    default:
        let commCsv = """
        "\(date)","\(buyer)","\(reference)","\(payment)","\(desc)","\(fee)"
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
