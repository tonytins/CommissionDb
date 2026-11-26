# Commis (Working Title)

> This is currently being rewritten in Swift. For the original Rust versions, see the `rust/alpha2` and `rust/alpha1` branches. 

Commis (formerly Commission Database, `cdb`) is a command line application for keeping track of commissions, YCH transactions, and art raffles. Records are stored in a local SQLite database and can be exported to CSV for further use in a spreadsheet program.

There's also a desktop window for commissions and YCH, in [`app/`](app/). It works like a Git client: it runs the `commis` command for everything it shows or changes, and never opens the database itself.

## Building

Requires Swift 6.3 or later.

```sh
swift build -c release
.build/release/commis --help
```

Run the test suite with `swift test`.

### The desktop window

Requires the .NET 10 SDK, and a built `commis`. The window is written in C# with [Avalonia](https://avaloniaui.net/).

```sh
swift build                       # the window finds this build on its own
dotnet run --project app/Commis.Desktop
```

Run its tests with `dotnet test --solution app/Commis.slnx`.

The window looks for `commis` in this order: the `COMMIS` environment variable, beside the window's own executable, a `.build/release` or `.build/debug` build in the checkout it runs from (whichever is newer), then your `PATH`.

## Usage

```sh
commis add -c "Alberta Mann" -r "https://www.furaffinity.net/view/64746038/" -f 43
commis raffle add --title "Synthesize" --prize "full-body YCH"
commis list
commis export > commissions.csv
```

See [Usage.md](Usage.md) for the full command reference, storage location, and CSV format, and for what the desktop window can do.

## To-Do

- [x] YCH support
- [x] Export to CSV
- [x] Write to Database
- [x] Raffles ([#1](https://github.com/tonytins/CommissionDb/issues/1))
- [x] Dry runs for commands that change the database
- [x] Desktop window for commissions and YCH
- [ ] Raffles in the desktop window (needs a broader discussion first)

## License

I hereby waive this project into the public domain - see [UNLICENSE](LICENSE) for details.
