# Project Guidelines

## Project Structure

Commis is a Swift 6 command-line tool for tracking commissions and YCH
transactions in a local SQLite database and exporting them to CSV. The package
name is `Commis`; the executable is `commis`.

`app/` holds a C# desktop window for commissions and YCH, built with Avalonia.
It is a client of `commis`, the way a Git client is a client of `git`: it runs
the executable for every read and change and never opens the database itself.
The design is in `docs/superpowers/specs/2026-11-26-commis-desktop-design.md`.

```
Sources/Commis/
  CommisCommand.swift     # @main, subcommand registration, version
  commis.sql              # schema; Enlighter generates the Commis type from this
  Commands/               # one file per subcommand; Raffle/ groups raffle commands
  Model/                  # enums, timestamps, raffle picking and import parsing
  Storage/                # database bootstrap, migrations, raffle operations
  Output/                 # CSV and display formatting
Tests/CommisTests/
app/
  Commis.slnx
  Commis.Desktop/         # the window; Records/ holds the logic tests reach
    uitext/english.dir/   # interface text, _100_commis.cst
  Commis.Desktop.Tests/
docs/superpowers/specs/   # design documents for larger changes
Package.swift
GitVersion.txt            # version being built
GitVersion.env            # GitVersion configuration
Usage.md                  # CLI usage reference
```

Approved dependencies: `swift-argument-parser`, `Lighter` (SQLite), and
`GitVersion` (build-time versioning). The desktop app may use `Avalonia`,
`Avalonia.Desktop`, and `Avalonia.Themes.Fluent` 12.1.5, and `CSTNet`
2.1.200; its tests use `xunit.v3`. New dependencies need the owner's approval.

Two build plugins generate source into `.build/`, so never edit those files:

- `Enlighter` reads `commis.sql` and generates the `Commis` database type, the
  `Records`, `Raffles`, and `RaffleEntries` records, and their CRUD operations.
- `GitVersionPlugin` reads `GitVersion.txt` / `GitVersion.env` and generates
  the `GitVersion` namespace.

Never commit build output (`.build/`, `DerivedData/`, `*.xcodeproj`,
`xcuserdata/`, `app/**/bin/`, `app/**/obj/`) or database files (`*.db`, `*.sqlite3`, `*.db-shm`, `*.db-wal`).

## Build and Test

```sh
swift build          # must succeed with no new warnings
swift test           # all tests must pass
```

The first build compiles both plugin toolchains and takes noticeably longer
than later builds.

For the desktop app (needs the .NET 10 SDK):

```sh
dotnet build app/Commis.slnx                 # warnings are errors
dotnet test --solution app/Commis.slnx       # all tests must pass
```

Unit tests aren't enough for the window: run it, and check what changed on
screen. With `swift build` done, it finds `.build/debug/commis` on its own.

Follow the [Swift guidelines skill](.agents/skills/swift-guidelines/SKILL.md)
for style and conventions. Where it disagrees with this file, this file wins.

## Database

- The schema lives in `Sources/Commis/commis.sql`. Change it there,
  then let Enlighter regenerate the types; don't hand-write SQL in commands.
- `Commis.open()` (in `Storage/Database.swift`) bootstraps a new database
  and migrates an existing one. `commis.sql` ends with
  `PRAGMA user_version = N`; `Migrations.swift` holds the matching v0→v1 steps.
- Migrations are historical: once a version ships, never edit its step. Add a
  new step, bump `PRAGMA user_version`, and cover it with a test that starts
  from the previous schema.
- `records` holds one row per commission or YCH. Raffles have their own
  `raffles` and `raffle_entries` tables and become `records` only on
  fulfillment.
- The database file lives in the platform application-support directory. Tests
  must use a temporary file, never the user's real database. A database from
  before the rename (`cdb/commissiondb.sqlite3`) is used in place when the new
  `commis/commis.sqlite3` does not exist yet.
- Enums are stored as their raw strings so the database stays readable without
  the app. Keep the enum raw values in sync with the SQL `CHECK` constraints.

## The desktop app

- The window never reads or writes the database. Everything goes through
  `commis`, and what it reads comes from the `--json` contract (`list --json`,
  `info --json`). Fields in that contract are added, never renamed or removed,
  without bumping its `version`.
- Don't parse `commis`'s human-readable output. If the window needs something
  the contract lacks, add it to the contract.
- When a command fails, show commis's own sentence (`CommisClient.Explain`).
- Every string the window shows lives in `uitext/english.dir/_100_commis.cst`,
  with its key in `TextKeys.cs`; a test checks every key is in the table.
- Colours are roles in `Theme/Palette.cs`, never hex values in views; a test
  holds text to 3:1 contrast on both palettes.
- Keep logic that can be tested out of the window's code-behind, in
  `Records/` and the client.

## Versioning

`GitVersion.txt` holds `major.minor.patch`, using .NET-style feature bands: the
hundreds digit of `patch` identifies the feature band (`0.1.100`, `0.1.200`,
and so on). `GitVersion.env` sets `GITVERSION_IGNORE_TAG_VERSION=true` so the
file, not the latest tag, names the version being built.

## Golden Rules

### Design Process

- **Iterate**. A first design or name is a draft. Rebuild clumsy parts as understanding improves.
- **Ask when a decision is open**, and explain the reasoning behind what you suggest.
- **Keep it simple**. The simplest approach that matches what the tool does wins.
- **Say what you're unsure of**: assumptions, unverified behaviour, and what needs testing.

### Code

- Write in Swift 6 strict concurrency mode (`swiftLanguageModes: [.v6]`).
  Avoid `@unchecked Sendable` and `nonisolated(unsafe)` without a clear reason.
- Use `ArgumentParser` types (`ParsableCommand`, `@Option`, `@Argument`,
  `@Flag`) for all CLI surface. Don't parse `CommandLine.arguments` by hand.
- Prefer `ExpressibleByArgument` enums over free-form strings for options with
  a fixed set of values; ArgumentParser then validates and lists them for free.
- Keep `CommisCommand.swift` focused on the CLI structure. Extract logic (CSV
  formatting, file I/O, record types) into separate files.
- Validate all user input before using it. Report bad input through
  `ArgumentParser`'s `ValidationError` where possible; use `throw` elsewhere.
- Use `Foundation`'s `URL`-based APIs and handle errors explicitly — no `try!`
  outside tests.
- Keep `Usage.md` accurate: update the options table and examples when CLI
  flags or output format change.

### Error Handling

- Throw typed errors, not strings. Define `Error` conformances close to where
  they're used.
- Don't swallow errors silently. Surface them through `ArgumentParser`'s
  exit-with-message path or `throw`; let the runtime print the usage line.

## Keeping Docs Current

- [README.md](README.md): update the to-do list, requirements, and build steps
  when they change.
- [Usage.md](Usage.md): keep the options table, storage paths, and examples in
  sync with the actual CLI flags and CSV output, and "The desktop window" in
  sync with what the window runs.
- [changelog.md](changelog.md) (if present): user-visible changes only.

State breaking changes, new dependencies, and flag renames in the commit
message. If you notice a risk outside your task, report it rather than fixing
it quietly.

See [REVIEW.md](REVIEW.md) for the project's review checklist.

## Personal Configuration

Keep personal preferences and machine-specific guidance in git-ignored
`*.local` files or directories (such as `AGENTS.local.md`), rather than in
shared project instructions. Check for relevant local guidance before
environment-specific work. You can create or update these files for details
such as:

- Coding preferences
- Local build and development environment
- Personal shortcuts or workflows
