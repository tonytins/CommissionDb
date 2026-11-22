import ArgumentParser

/// Groups the raffle subcommands.
struct Raffle: ParsableCommand {
    static let configuration = CommandConfiguration(
        commandName: "raffle",
        abstract: "Manage art raffles.",
        subcommands: [
            RaffleAdd.self, RaffleEnter.self, RaffleImport.self,
            RaffleList.self, RaffleShow.self, RaffleDraw.self,
            RaffleFulfill.self, RaffleCancel.self,
        ],
        defaultSubcommand: RaffleList.self
    )
}
