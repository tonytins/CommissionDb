import ArgumentParser
import Foundation

func csvExport(client: String, reference: String, desc: String = "",
               payment: String, fee: String,
               isYch: Bool = false, title: String = "", slot: Int = 0)
{
    let commHeader = "Date,Client,Reference,Payment,Description,Fee"
    let ychHeader = "Date,Client,Reference,Slot,Payment,Fee"
    let date = Date()
    var transiactions = [String]() // Placeholder until we read and write to a file
    
    switch isYch {
    case true:
        for transaction in transiactions {
            if !transaction.contains(ychHeader)
            {
                transiactions.append(ychHeader)
            }
        }
        let ychCsv = """
        "\(date)","\(title)","\(client)","\(reference)","\(payment)","\(slot)","\(fee)"
        """
        transiactions.append(ychCsv)
        print(transiactions)
    default:
        for transaction in transiactions {
            if !transaction.contains(commHeader)
            {
                transiactions.append(commHeader)
            }
        }
        let commCsv = """
        "\(date)","\(client)","\(reference)","\(payment)","\(desc)","\(fee)"
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
        var client: String

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
            csvExport(client: client, reference: reference,
                      payment: payment, fee: bid,
                      isYch: true, title: title, slot: slot)
        }
    }
}

@main
struct CommissionDb: ParsableCommand {
    @Option(name: .shortAndLong)
    var client: String

    @Option(name: .shortAndLong)
    var reference: String

    @Option(name: .shortAndLong)
    var payment: String

    @Option(name: .shortAndLong)
    var description: String

    @Option(name: .shortAndLong)
    var fee: String

    mutating func run() throws {
        csvExport(client: client, reference: reference, desc: description,
                  payment: payment, fee: fee)
    }
}
