# Project Guidelines

## Project Structure

Commission Database, aka **cdb**, is a Swift 6 command-line tool for tracking
commissions and YCH transactions in a local SQLite database and exporting them
to CSV. The package name is `CommissionDb`; the executable is `cdb`.

```
Sources/CommissionDb/
  CommissionDb.swift      # @main, subcommand registration, version
  commissiondb.sql        # schema; Enlighter generates DB types from this
  Commands/               # one file per subcommand; Raffle/ groups raffle commands
  Model/                  # enums, timestamps, raffle picking and import parsing
  Storage/                # database bootstrap, migrations, raffle operations
  Output/                 # CSV and display formatting
Tests/CommissionDbTests/
docs/superpowers/specs/   # design documents for larger changes
Package.swift
GitVersion.txt            # version being built
GitVersion.env            # GitVersion configuration
Usage.md                  # CLI usage reference
```

Approved dependencies: `swift-argument-parser`, `Lighter` (SQLite), and
`GitVersion` (build-time versioning). New dependencies need the owner's
approval.

Two build plugins generate source into `.build/`, so never edit those files:

- `Enlighter` reads `commissiondb.sql` and generates the `Commissiondb`
  database type, the `Records`, `Raffles`, and `RaffleEntries` records, and
  their CRUD operations.
- `GitVersionPlugin` reads `GitVersion.txt` / `GitVersion.env` and generates
  the `GitVersion` namespace.

Never commit build output (`.build/`, `DerivedData/`, `*.xcodeproj`,
`xcuserdata/`) or database files (`*.db`, `*.sqlite3`, `*.db-shm`, `*.db-wal`).

## Build and Test

```sh
swift build          # must succeed with no new warnings
swift test           # all tests must pass
```

The first build compiles both plugin toolchains and takes noticeably longer
than later builds.

Follow the [Swift guidelines skill](.agents/skills/swift-guidelines/SKILL.md)
for style and conventions. Where it disagrees with this file, this file wins.

## Database

- The schema lives in `Sources/CommissionDb/commissiondb.sql`. Change it there,
  then let Enlighter regenerate the types; don't hand-write SQL in commands.
- `Commissiondb.open()` (in `Storage/Database.swift`) bootstraps a new database
  and migrates an existing one. `commissiondb.sql` ends with
  `PRAGMA user_version = N`; `Migrations.swift` holds the matching v0→v1 steps.
- Migrations are historical: once a version ships, never edit its step. Add a
  new step, bump `PRAGMA user_version`, and cover it with a test that starts
  from the previous schema.
- `records` holds one row per commission or YCH. Raffles have their own
  `raffles` and `raffle_entries` tables and become `records` only on
  fulfillment.
- The database file lives in the platform application-support directory. Tests
  must use a temporary file, never the user's real database.
- Enums are stored as their raw strings so the database stays readable without
  the app. Keep the enum raw values in sync with the SQL `CHECK` constraints.

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
- Keep `CommissionDb.swift` focused on the CLI structure. Extract logic (CSV
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
  sync with the actual CLI flags and CSV output.
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
