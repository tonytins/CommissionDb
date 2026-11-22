# Usage

`cdb` stores commissions and YCH entries in a local SQLite database and can
export them to CSV. Running `cdb` with no subcommand lists every record.

```
cdb <subcommand>
```

| Subcommand | Purpose |
| ---------- | ------- |
| `add`      | Add a commission or YCH record |
| `list`     | List records (default subcommand) |
| `export`   | Export records as CSV to stdout |
| `status`   | Change the status of a record |

## Storage

Records live in a single SQLite database in the platform application-support
directory:

| Platform | Path |
| -------- | ---- |
| Linux    | `~/.local/share/cdb/commissiondb.sqlite3` (or `$XDG_DATA_HOME/cdb/`) |
| macOS    | `~/Library/Application Support/cdb/commissiondb.sqlite3` |
| Windows  | `%APPDATA%\cdb\commissiondb.sqlite3` |

The database and its table are created automatically the first time a command
runs.

## Statuses

Every record has one of four statuses:

`pending` · `paid` · `cancelled` · `refunded`

New records default to `pending`.

## Commissions

````sh
cdb add --client "Alberta Mann" \
        --reference "https://www.furaffinity.net/view/64746038/" \
        --fee 43 \
        --description "Lorem ipsum dolor sit amet."
````

### `add` options

| Option | Description |
| ------ | ----------- |
| `-c, --client <client>` | Client name (required) |
| `-r, --reference <reference>` | Reference URL or identifier (required) |
| `-f, --fee <fee>` | Fee amount, must be zero or greater (required) |
| `-d, --description <description>` | Description of the work |
| `-s, --status <status>` | Initial status (default: `pending`) |
| `--ych-title <title>` | YCH title (see below) |
| `--slot <n>` | YCH slot number (see below) |

## YCH

Add `--ych-title` and `--slot` together to create a YCH entry instead of a
commission. Supplying only one of them is an error.

````sh
cdb add --client "Bessie Hettinger" \
        --reference "https://www.furaffinity.net/view/64746038/" \
        --fee 25 \
        --ych-title "Synthesize" \
        --slot 4
````

## Listing

````sh
cdb list                      # everything
cdb list --status pending     # only unpaid work
cdb list --type ych           # only YCH entries
````

Each record prints its ID, status, type, client, reference, description, fee,
and date. IDs are what `cdb status` accepts.

## Changing status

````sh
cdb status 1 --set paid
````

The ID comes from `cdb list` or `cdb export`. Unknown IDs exit with a non-zero
status.

## Exporting

````sh
cdb export > commissions.csv
cdb export --status paid --type commission
````

CSV is written to stdout with a header row and these columns:

```csv
Date,Type,Client,Reference,Description,Fee,YCH,Slot,Status
"2026-11-22T18:13:05Z","commission","Alberta Mann","https://www.furaffinity.net/view/64746038/","Lorem ipsum dolor sit amet.","43.00","","","pending"
```

All fields are quoted and internal double-quotes are doubled, per RFC 4180.
`YCH` and `Slot` are empty for commissions; fees use two decimal places.

## Raffle

TBA. See issue [#1](https://github.com/tonytins/CommissionDb/issues/1).
