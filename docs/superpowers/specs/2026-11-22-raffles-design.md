# Art Raffles — Design

Date: 2026-11-22
Status: implemented (2026-11-23)

## Intent

cdb tracks art commissions and YCH work. Artists also run art raffles: they
offer a prize, collect entries for a limited window, draw a winner at random,
and then owe that winner a piece of art. Today cdb cannot help with any of
that, so the artist keeps entries in a spreadsheet or comment thread and draws
with a separate tool.

This design adds raffles to cdb so the artist can keep entries in one place,
draw fairly enough to show the result, and turn the winner into a normal fee-0
record so the prize is tracked alongside commissions and YCH.

The goal is to remove work from the artist, not to become a payment processor
or a social-media scraper.

## Goals

- Create a raffle with a title, prize description, optional ticket price, and
  optional open/end dates.
- Add entrants one at a time or import a list.
- Draw one or more winners at random, weighted by tickets, and record who won.
- Turn drawn winners into pending commission/YCH records with fee 0.
- List, show, and export raffle data.
- Rename `--ych-title` to `--title`, with `--slot` alone meaning YCH.

## Non-goals

- Taking or verifying payments, or recording donation amounts.
- Fetching entries from FurAffinity, Twitter/X, Discord, or forms.
- Legal compliance for paid raffles (gambling rules vary by jurisdiction).
- Multiple distinct prizes in one raffle. One prize per raffle; multiple
  winners may share it.
- Currency handling beyond the artist's own unit (matches current fees).
- A provably fair draw with a published seed. v1 uses the system RNG and
  prints a transcript; a seed option can come later.
- The `commis` rename (planned separately).

## Constraints

- Swift 6, ArgumentParser, Lighter/Enlighter.
- `records` stays one row per commission/YCH transaction. Raffles get their own
  tables because they are one-to-many and have different statuses.
- Existing databases must be migrated in place, never dropped.
- Tests must not touch the real database.
- The executable and package will be renamed to `commis` later, so keep the
  app name and data path in one place.

## Approaches considered

1. **Raffle as a record type.** Add `raffle` to `records.type`. Rejected: no
   place for entrants, and the commission statuses do not describe a raffle.
2. **Draw-only helper.** `cdb draw entries.txt`. Rejected as the whole
   feature: it solves fairness but leaves the bookkeeping (the actual burden)
   untouched. It remains a possible future flag.
3. **Raffle and entrants as their own tables, with draw and fulfill.** Chosen.

## Design

### Schema (version 1)

Final schema in `commissiondb.sql`:

```sql
CREATE TABLE records (
    id          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    date        TEXT    NOT NULL,
    type        TEXT    NOT NULL CHECK(type IN ('commission', 'ych')),
    client      TEXT    NOT NULL,
    reference   TEXT    NOT NULL,
    description TEXT    NOT NULL DEFAULT '',
    fee         REAL    NOT NULL,
    title       TEXT,
    slot        INTEGER,
    status      TEXT    NOT NULL DEFAULT 'pending'
                        CHECK(status IN ('pending','paid','cancelled','refunded'))
);

CREATE TABLE raffles (
    id           INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    date         TEXT    NOT NULL,
    title        TEXT    NOT NULL,
    prize        TEXT    NOT NULL DEFAULT '',
    ticket_price REAL    NOT NULL DEFAULT 0,
    opens        TEXT,
    ends         TEXT,
    status       TEXT    NOT NULL DEFAULT 'open'
                         CHECK(status IN ('open','drawn','fulfilled','cancelled'))
);

CREATE TABLE raffle_entries (
    id        INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    raffle_id INTEGER NOT NULL,
    name      TEXT    NOT NULL,
    reference TEXT    NOT NULL DEFAULT '',
    tickets   INTEGER NOT NULL DEFAULT 1 CHECK(tickets > 0),
    entered   TEXT    NOT NULL,
    won_at    TEXT
);

CREATE INDEX raffle_entries_raffle_id ON raffle_entries (raffle_id);

PRAGMA user_version = 1;
```

Notes:

- `ticket_price` is informational; 0 means free entry. No donation amount is
  stored.
- `tickets` is the entrant's weight in the draw.
- `won_at` is null until a draw; set for every winner.
- No foreign-key constraint in v1; `raffle_id` is validated in code and
  indexed. SQLite foreign keys need a per-connection pragma, which is not worth
  the complexity yet.
- `raffles.date` is creation time, ISO 8601.

### Migration

`open()` becomes `static func open(at url: URL) throws -> Commissiondb`:

- File does not exist: `bootstrap(at:)` creates the latest schema, then set
  `user_version` to `Commissiondb.userVersion`.
- File exists and `user_version < 1`: run the v0→v1 migration in a
  transaction, then set the version.
- File exists and is current: open.

v0→v1 SQL:

```sql
ALTER TABLE records RENAME COLUMN ych_title TO title;
CREATE TABLE raffles ( ... );
CREATE TABLE raffle_entries ( ... );
CREATE INDEX raffle_entries_raffle_id ON raffle_entries (raffle_id);
```

The migration is a Swift constant in `Storage/Migrations.swift`, immutable once
shipped. Commands call `open()` (which uses `AppDataPath.databaseURL`); tests
call `open(at:)` with a temporary URL.

### Commands

```
cdb raffle add    --title <title> [--prize <text>] [--ticket-price <n>]
                  [--opens <date>] [--ends <date>]
cdb raffle enter  <raffle-id> --name <name> [--reference <url>] [--tickets <n>]
cdb raffle import <raffle-id> <file> [--csv]
cdb raffle list   [--status <status>]
cdb raffle show   <raffle-id>
cdb raffle draw   <raffle-id> [--winners <n>]
cdb raffle fulfill <raffle-id> --type commission|ych
cdb raffle cancel <raffle-id>
```

- `add` prints the new raffle ID.
- `enter` appends one entry; `tickets` defaults to 1.
- `import` reads UTF-8. Default: one name per line (tickets 1). With `--csv`:
  header `name,reference,tickets`; missing optional columns are allowed, blank
  lines skipped. Invalid rows are reported with line numbers, and nothing is
  inserted unless the whole file parses.
- `list` shows id, status, title, prize, entry count, total tickets, and
  winners.
- `show` prints the raffle plus its entries, marking winners.
- `draw` requires `open`; picks `--winners` (default 1) distinct entries,
  weighted by `tickets`, sets `won_at`, sets status `drawn`, and prints a
  transcript: raffle title, entry count, total tickets, winners with entry IDs
  and ticket counts. Refuses if there are fewer entries than winners.
  Re-drawing is not allowed in v1.
- `fulfill` requires `drawn`; for each winner inserts a `records` row (date
  now, type from `--type`, client = entry name, reference = entry reference,
  description = raffle prize or title, fee 0, title = raffle title, status
  `pending`), then sets status `fulfilled`. Prints the created record IDs.
- `cancel` sets `cancelled` from `open` or `drawn`; it does not delete data.
  Fulfilled raffles cannot be cancelled.

Randomness: production uses `SystemRandomNumberGenerator`, but the weighted
selection lives in a free function that accepts any `RandomNumberGenerator`, so
tests can inject a seeded one.

### `--title` rename

- CLI: `--title` replaces `--ych-title`.
- `--slot` alone creates a YCH; `--title` alone is allowed on a commission;
  both together is a YCH with a title.
- The old "both or neither" validation is removed.
- DB column `ych_title` becomes `title` in the v1 migration.
- The CSV header `YCH` becomes `Title`, since the field is no longer
  YCH-specific. This is a breaking CSV change; acceptable pre-1.0.

### Files

```
Sources/CommissionDb/
  commissiondb.sql              # updated schema + PRAGMA user_version
  Commands/
    Raffle/                     # RaffleAdd, RaffleEnter, RaffleImport,
                                # RaffleList, RaffleShow, RaffleDraw,
                                # RaffleFulfill, RaffleCancel
  Model/
    RaffleStatus.swift          # open, drawn, fulfilled, cancelled
    RafflePicker.swift          # weighted selection, RNG-injectable
    RaffleEntryImport.swift     # text and CSV parsing
    RaffleEntryDraft.swift      # one parsed entry
    Timestamps.swift            # ISO 8601 now/parse/format
  Storage/
    Database.swift              # open(at:), fetchRecords, fetchRaffles
    Migrations.swift            # v0 -> v1
    Raffles.swift               # enter, draw, fulfill, cancel
Tests/CommissionDbTests/
  RafflePickerTests.swift
  RaffleImportTests.swift
  RaffleCommandTests.swift      # temp-DB integration
  MigrationTests.swift
```

Subcommand type names are prefixed (`RaffleAdd`, not `Add`) to avoid clashing
with the existing top-level commands. Import parsing lives in `Model/` rather
than in the command, so it can be tested without touching a database.

### Testing

- Weighted draw: deterministic seeded RNG, ticket weighting, distinct winners,
  empty and too-few-entries errors.
- Import: text and CSV parsing, bad rows, blank lines.
- Integration: temp DB; create raffle, enter, draw, fulfill; verify records and
  statuses.
- Migration: build a v0 DB with the old `records` schema and a row; run
  `open(at:)`; verify the row survives, `title` exists, raffle tables exist,
  and `user_version == 1`.
- `swift build` with no warnings; `swift test` green.

## Later: rename to `commis`

Not part of this change. When it happens:

- `Package.swift`: package, product, executable, and target names.
- `AppDataPath`: directory `cdb` → `commis`, filename
  `commissiondb.sqlite3` → `commis.sqlite3`.
- Docs and the GitVersion repository URL.
- Existing databases need a one-time move from the old path, or the app should
  fall back to the old path when the new one is absent.

Keeping the directory name and DB filename in `AppDataPath` makes this a small
change.

## What's left to the builder

- Exact ArgumentParser help text and option short names (avoid collisions with
  `-s`, `-t`, and the existing commands).
- Whether `raffle show` prints everything (v1) or paginates.
- Whether `import` should accept headerless CSV.
- The transcript wording for `draw`.
- Any indexes beyond `raffle_entries.raffle_id`.
