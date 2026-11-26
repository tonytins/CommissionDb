import ArgumentParser
import GitVersion

@main
struct CommisCommand: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "commis",
        abstract: "Track commissions, YCH transactions, and art raffles.",
        version: GitVersion.version,
        subcommands: [
            Add.self, List.self, Export.self, UpdateStatus.self, Info.self, Raffle.self,
        ],
        defaultSubcommand: List.self
    )
}
