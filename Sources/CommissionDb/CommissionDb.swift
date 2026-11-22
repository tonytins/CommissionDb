import ArgumentParser
import GitVersion

@main
struct CommissionDb: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "cdb",
        abstract: "Commission Database \(GitVersion.version) — track commissions and YCH transactions.",
        subcommands: [Add.self, List.self, Export.self, UpdateStatus.self],
        defaultSubcommand: List.self
    )
}
