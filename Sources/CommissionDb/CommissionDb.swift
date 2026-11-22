import ArgumentParser
import GitVersion

@main
struct CommissionDb: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "cdb",
        abstract: "Commission Database — track commissions and YCH transactions.",
        version: GitVersion.version,
        subcommands: [
            Add.self, List.self, Export.self, UpdateStatus.self, Raffle.self,
        ],
        defaultSubcommand: List.self
    )
}
