# Usage

`commis` stores commissions, YCH entries, and art raffles in a local SQLite
database and can export records to CSV. Running `commis` with no subcommand lists
every record.

```
commis <subcommand>
```

| Subcommand | Purpose |
| ---------- | ------- |
| `add`      | Add a commission or YCH record |
| `list`     | List records (default subcommand) |
| `export`   | Export records as CSV to stdout |
| `status`   | Change the status of a record |
| `info`     | Show the database's location, schema version, and record count |
| `raffle`   | Create, fill, draw, and fulfill raffles |

## Storage

Everything lives in a single SQLite database in the platform
application-support directory:

| Platform | Path |
| -------- | ---- |
| Linux    | `~/.local/share/commis/commis.sqlite3` (or `$XDG_DATA_HOME/commis/`) |
| macOS    | `~/Library/Application Support/commis/commis.sqlite3` |
| Windows  | `%APPDATA%\commis\commis.sqlite3` |

The database and its tables are created automatically the first time a command
runs. Older databases are migrated in place; the data is kept.

Databases created before the rename live in `cdb/commissiondb.sqlite3`. If the
new file does not exist yet, `commis` uses the old file in place, so existing
data keeps working without a manual move.

## Statuses

Every record has one of four statuses:

`pending` · `paid` · `cancelled` · `refunded`

New records default to `pending`.

Raffles have their own lifecycle:

`open` → `drawn` → `fulfilled`, or `cancelled`

## Commissions

```sh
commis add --client "Alberta Mann" \
        --reference "https://www.furaffinity.net/view/64746038/" \
        --fee 43 \
        --description "Lorem ipsum dolor sit amet."
```

### `add` options

| Option | Description |
| ------ | ----------- |
| `-c, --client <client>` | Client name (required) |
| `-r, --reference <reference>` | Reference URL or identifier (required) |
| `-f, --fee <fee>` | Fee amount, must be zero or greater (required) |
| `-d, --description <description>` | Description of the work |
| `-s, --status <status>` | Initial status (default: `pending`) |
| `--title <title>` | Title of the piece |
| `--slot <n>` | YCH slot number; implies a YCH entry |

## YCH

Passing `--slot` creates a YCH entry instead of a commission; `--title` is
optional for both. There is no longer a separate `--ych-title` flag, and no
rule forcing `--title` and `--slot` to appear together.

```sh
commis add --client "Bessie Hettinger" \
        --reference "https://www.furaffinity.net/view/64746038/" \
        --fee 25 \
        --title "Synthesize" \
        --slot 4
```

## Listing

```sh
commis list                      # everything
commis list --status pending     # only unpaid work
commis list --type ych           # only YCH entries
```

Each record prints its ID, status, type, title, client, reference, description,
fee, and date. IDs are what `commis status` accepts.

## Changing status

```sh
commis status 1 --set paid
```

The ID comes from `commis list` or `commis export`. Unknown IDs exit with a non-zero
status.

## Exporting

```sh
commis export > commissions.csv
commis export --status paid --type commission
```

CSV is written to stdout with a header row and these columns:

```csv
Date,Type,Client,Reference,Description,Fee,Title,Slot,Status
"2026-11-22T18:13:05Z","commission","Alberta Mann","https://www.furaffinity.net/view/64746038/","Lorem ipsum dolor sit amet.","43.00","","","pending"
```

All fields are quoted and internal double-quotes are doubled, per RFC 4180.
`Title` and `Slot` are empty for records without them; fees use two decimal
places.

Raffles are not part of the CSV export; use `commis raffle list` and
`commis raffle show` for raffle data.

## Machine-readable output

`list` and `info` take `--json` and print one line of JSON for other programs,
such as the desktop app. Without the flag their text output is unchanged.

```sh
commis list --json --status pending
commis info --json
```

`list --json` wraps the records in an envelope carrying the contract's version:

```json
{"records":[{"client":"Alberta Mann","date":"2026-11-22T18:13:05Z","description":"","fee":43,"id":1,"reference":"https://www.furaffinity.net/view/64746038/","slot":null,"status":"pending","title":null,"type":"commission"}],"version":1}
```

`info --json` says where the database is, which schema it uses, and how many
records it holds:

```json
{"commis":"0.1.109","database":"/home/you/.local/share/commis/commis.sqlite3","records":16,"schema":1,"version":1}
```

Absent titles and slots are `null`, never left out. Dates are the stored ISO
8601 strings. `version` is the contract's, not the app's, and changes only when
a field changes meaning or goes away. A command that fails exits non-zero.

## Raffles

A raffle is kept separate from commission records until it is fulfilled. When
you fulfill a raffle, each winner becomes a normal fee-0 record, so `list` and
`export` continue to work unchanged.

### Create

```sh
commis raffle add --title "Synthesize" \
               --prize "full-body YCH" \
               --ticket-price 5 \
               --ends 2026-12-01
```

| Option | Description |
| ------ | ----------- |
| `--title <title>` | Raffle title (required) |
| `--prize <prize>` | What the winner receives |
| `--ticket-price <n>` | Informational price per ticket; `0` means free |
| `--opens <date>` | When entries open (ISO 8601 or `yyyy-MM-dd`) |
| `--ends <date>` | When entries close (ISO 8601 or `yyyy-MM-dd`) |

The new raffle ID is printed. `--opens` must not be after `--ends`.

### Collect entries

```sh
commis raffle enter 1 --name "Bessie" --reference "https://example.com/bessie" --tickets 3
```

`--tickets` defaults to 1 and sets the entrant's weight in the draw. Entries
are only accepted while the raffle is `open`.

To import a list:

```sh
commis raffle import 1 entries.txt          # one name per line
commis raffle import 1 entries.csv --csv    # header: name,reference,tickets
```

The CSV header must include `name`; `reference` and `tickets` are optional, and
column order does not matter. Blank lines are skipped, a UTF-8 byte order mark
is ignored, and bad rows are reported with their line numbers. Nothing is
inserted unless the whole file parses.

### Review

```sh
commis raffle list                  # all raffles with entry and ticket counts
commis raffle list --status open
commis raffle show 1                # one raffle and its entries
```

Winners are marked `[winner]` in `raffle show`.

### Draw

```sh
commis raffle draw 1
commis raffle draw 1 --winners 2
```

Only `open` raffles can be drawn, there must be at least as many entries as
winners, and each entry can win at most once. Selection is weighted by ticket
count using the system random number generator. The draw prints a transcript
with the raffle title, entry count, total tickets, and each winner's entry ID
and ticket count.

A raffle can only be drawn once. If entries are still arriving, wait until
they close before drawing.

### Fulfill

```sh
commis raffle fulfill 1 --type ych
```

This creates one pending, fee-0 record per winner, using the raffle title as
the record title and the prize (or the title, if no prize is set) as the
description. The raffle becomes `fulfilled`.

### Cancel

```sh
commis raffle cancel 1
```

Cancels an `open` or `drawn` raffle without deleting its entries. `fulfilled`
raffles cannot be cancelled.
