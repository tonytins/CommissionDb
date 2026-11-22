# Project Guidelines

## Project Structure

Commission Database, aka **cdb**, is a Swift 6 command-line tool for tracking
commissions and YCH transactions and exporting them to CSV. The package name is
`CommissionDb`; the executable is `cdb`. It targets macOS 10.13, Windows 10,
Ubuntu 20.04, Fedora 39, and Debian 12.

```
Sources/CommissionDb/   # executable target
Tests/CommissionDbTests/
Package.swift
Usage.md                # CLI usage reference
```

Approved dependencies: `swift-argument-parser`, `swift-configuration`.
`Lighter` (SQLite ORM) is stubbed out and commented pending a future change.
New dependencies need the owner's approval.

Never commit build output (`.build/`, `DerivedData/`, `*.xcodeproj`,
`xcuserdata/`) or database files (`*.db`, `*.sqlite3`, `*.db-shm`, `*.db-wal`).

## Build and Test

```sh
swift build          # must succeed with no new warnings
swift test           # all tests must pass
```

Follow the [Swift guidelines skill](.agents/skills/swift-guidelines/SKILL.md)
for style and conventions. Where it disagrees with this file, this file wins.

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
- Keep `CommissionDb.swift` focused on the CLI structure. Extract logic (CSV
  formatting, file I/O, record types) into separate files as the codebase grows.
- Validate all user input before using it. Report bad input through
  `ArgumentParser`'s `ValidationError` where possible; use `throw` elsewhere.
- When file I/O lands, use `Foundation`'s `URL`-based APIs and handle errors
  explicitly — no `try!` outside tests.
- Keep `Usage.md` accurate: update the options table and examples when CLI
  flags or output format change.

### Error Handling

- Throw typed errors, not strings. Define `Error` conformances close to where
  they're used.
- Don't swallow errors silently. Surface them through `ArgumentParser`'s
  exit-with-message path or `throw`; let the runtime print the usage line.

## Keeping Docs Current

- [README.md](README.md): update the minimum-requirements table, to-do list,
  and build steps when they change.
- [Usage.md](Usage.md): keep the options table and examples in sync with the
  actual CLI flags and CSV output.
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
