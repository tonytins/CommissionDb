# Commission Database (Working Title)

> This is currently being rewritten in Swift. For the original Rust versions, see the `rust/alpha2` and `rust/alpha1` branches. 

Commission Database, aka `cdb`, is a command line application for keeping track of commissions, YCH transactions, and art raffles. Records are stored in a local SQLite database and can be exported to CSV for further use in a spreadsheet program.

## Building

Requires Swift 6.3 or later.

```sh
swift build -c release
.build/release/cdb --help
```

Run the test suite with `swift test`.

## Usage

```sh
cdb add -c "Alberta Mann" -r "https://www.furaffinity.net/view/64746038/" -f 43
cdb raffle add --title "Synthesize" --prize "full-body YCH"
cdb list
cdb export > commissions.csv
```

See [Usage.md](Usage.md) for the full command reference, storage location, and CSV format.

## To-Do

- [x] YCH support
- [x] Export to CSV
- [x] Write to Database
- [x] Raffles ([#1](https://github.com/tonytins/CommissionDb/issues/1))

## License

I hereby waive this project into the public domain - see [UNLICENSE](LICENSE) for details.
