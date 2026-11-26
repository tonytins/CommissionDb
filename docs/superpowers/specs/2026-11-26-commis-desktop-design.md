# Commis Desktop — Design

Date: 2026-11-26
Status: draft

## Intent

Commis is a Swift command-line tool. Adding a record with flags is fine, but
reading a list wrapped in text is poor for the question an artist asks most
often — who is still pending — and a YCH run reads as scattered lines rather
than a set of slots.

This design adds a desktop window for Commis, written in C# with Avalonia and
following the Venture Design Language. It is a client of the `commis` binary,
the way a Git client is a client of `git`: it runs the executable and renders
what comes back, and never opens the database itself. The CLI stays the only
writer, so migrations, validation, and the raffle draw have one home.

The owner does YCH work daily, so YCH gets a view built for how a run actually
looks — one title, numbered slots. Raffles are out of scope; that was the
largest piece on the CLI side and it can follow once this contract has proven
itself.

## Goals

- Browse commissions and YCH entries in one window, with the two as sections.
- Show a YCH run as its title with its sold slots beneath it.
- Add a commission or YCH entry from a form that runs `commis add`.
- Change a record's status from the detail pane via `commis status`.
- Export CSV through `commis export` to a file the user chooses.
- Follow the design language's structure, roles, states, keyboard, and voice.
- Pick up changes made in a terminal without a restart.

## Non-goals

- Raffles: add, enter, import, draw, fulfill, cancel. Later.
- Editing or deleting records. The CLI has no command for either; a window must
  not invent operations the executable cannot perform.
- Writing SQLite directly, or any second implementation of the schema.
- Payments, sync, accounts, mobile, or web.
- Showing unsold YCH slots. The database records which slots sold, never how
  many were offered.

## Anti-goals

What would make this a failure even if it works:

- Two writers on one database, so migrations or `user_version` drift apart.
- A window that shows stale rows after a terminal command changed them.
- A window that reports success for a command that failed, or hides stderr.
- A look or structure that is not the design language's.
- A GUI-only feature that the CLI cannot do, so the two frontends disagree.

## Constraints

- C# on .NET, Avalonia 12.1.5 and CSTNet 2.1.200, matching S1PK. New
  dependencies need the owner's approval.
- The Swift CLI remains the only process that reads or writes the database.
- The JSON contract must be stable: fields are added, never renamed or removed
  without a version bump.
- Text reaches 3 to 1 contrast on both palettes, and is checked by a test.
- The repo is a Swift package; the C# solution lives beside it without
  disturbing `Sources/` or `Tests/`.

## Approaches considered

1. **The app opens `commis.sqlite3` itself.** Fast and simple for reading.
   Rejected: the app would need the schema, the migrations, and the validation
   rules to write anything, and two writers on one file eventually disagree.
   The CLI's `PRAGMA user_version` migration chain assumes one writer.
2. **Port the domain to C# and let the app own the database.** Rejected: it
   doubles every rule, and in practice retires the Swift tool by accident
   rather than by decision.
3. **Shell out to `commis`, with a machine-readable output mode.** Chosen. One
   implementation of every rule; the cost is a contract to keep and a process
   per action, both small for a database of this size.

## The contract

The CLI gains `--json` on the read commands and a small `info` command. Human
output stays exactly as it is; JSON is printed to stdout only in that mode, and
errors always go to stderr with a non-zero exit code.

```sh
commis list --json [--status pending] [--type ych]
commis info --json
```

`list --json`:

```json
{
  "version": 1,
  "records": [
    {
      "id": 3,
      "date": "2026-11-22T18:13:05Z",
      "type": "ych",
      "client": "Bessie Hettinger",
      "reference": "https://www.furaffinity.net/view/64746038/",
      "description": "",
      "fee": 25.0,
      "title": "Synthesize",
      "slot": 4,
      "status": "pending"
    }
  ]
}
```

`info --json`:

```json
{ "version": 1, "commis": "0.1.100", "database": "/home/…/commis.sqlite3", "schema": 1, "records": 16 }
```

Rules:

- `version` is the contract's, not the app's; it changes only when a field
  changes meaning or disappears.
- `fee` is a number, `slot` is a number or null, `title` is a string or null.
  Absent values are null, never `""` standing in for "none".
- Dates are the stored ISO 8601 strings, in UTC.
- Empty results are `{"version":1,"records":[]}` with exit code 0, not an error.
- A failure exits non-zero. The window shows, in order: stderr if it isn't
  empty; otherwise stdout if it isn't valid JSON; otherwise `commis exited
  with <code>.` Existing commands already write some failures to stdout
  (`commis status 99` prints `No record with ID 99.` before exiting), and
  changing where they write is not part of this work. When stdout parses as
  the contract, it is data, not an error, whatever the exit code.

## The window

Structure, per the design language: window controls and the toolbar share one
bar at the top; a sidebar of places on the left; content on the right; the
standard menus.

```
┌────────────────────────────────────────────────────┐
│ Commis        [Add] [Status] [Export]   – □ ×      │
├─────────────────┬──────────────────────────────────┤
│ Commissions  12 │ Synthesize            3 entries  │
│ YCH           7 │   #1  Bessie      paid    $25.00 │
│ Raffles       2 │   #4  Alberta     pending $25.00 │
│                 │   #7  Corey       pending $25.00 │
│                 │ Rooftop               2 entries  │
└─────────────────┴──────────────────────────────────┘
```

- **Commissions** is first and is the section a first run opens on. The window
  reopens on the last section used.
- **Commissions** lists one row per record: client, title, fee, status, date.
- **YCH** groups rows under their title, with the slot number leading the row.
  Groups sort by the newest entry in them; rows sort by slot, and rows with no
  slot come last in their group, showing `—`. Entries without a slot are real:
  `commis raffle fulfill --type ych` writes a title and no slot. Entries with a
  slot and no title stand under `Untitled`.
- **Raffles** is present but says it isn't available yet, and why. It does not
  open a half-built view, and shows no count, since nothing in the contract
  returns one.
- **Detail** shows the selected record's client, reference, description, fee,
  date, title, and slot, with status as a control that runs `commis status`.
- **Add** opens a form. In the YCH section it requires a slot, since a slot is
  what makes an entry a YCH and a blank one would quietly write a commission
  that never appears in the section you were standing in. A title stays
  optional there. In Commissions the form has neither field.
- **Filtering** by status is one toolbar control that applies to the section
  showing, is remembered per section while the app runs, and starts at All on
  launch. The sidebar's counts are always totals, never filtered, so a filter
  can't look like missing data; an empty list names the filter it's under.
- **Export** runs `commis export` with the filter showing and writes the result
  to a file the user picks, then reports the path.
- After every command the window refreshes from `list --json`. A file watcher
  on the database and its `-wal` does the same for terminal changes, debounced
  so a burst of writes is one refresh.

Errors follow the design language's voice: the sentence the CLI wrote, in
place, with the window still usable. A failed `status` change leaves the row as
it was.

## Roles and paint

The app defines a value for every color and type role in its theme, and draws
with roles, never with hex values in views.

- Surfaces, text, and lines are neutral, after libadwaita; only the accent
  carries color.
- Both light and dark are supported, and switching reaches the open window.
- The accent is blue: libadwaita's `#3584e4` on light and `#78aeed` on dark as
  the starting values, adjusted if the contrast test says so. Focus, selection,
  and progress marks use it; nothing else does.

## In the repository

```
app/Commis.Desktop/          # Avalonia app
app/Commis.Desktop.Tests/    # contract parsing, grouping, title bar, contrast
app/Commis.slnx
```

`Sources/` and `Tests/` stay Swift's. The README and `AGENTS.md` gain a short
section on building the window.

## Packaging

Self-contained publish per platform, as S1PK does. The app looks for `commis`
on `PATH`, then at a path set in its settings, and says plainly which of the
two failed when it can't find it. Bundling the executable inside the app is a
later step, and would need the Swift runtime or a static build on Linux.

## Testing

- **Swift:** `list --json` and `info --json` shapes against a fixture database,
  filters, empty results, and the exit code and stderr of a failure.
- **C#:** parsing each fixture into records, grouping YCH by title with slots
  in order and slotless rows last, the stderr/stdout/exit-code error fallback,
  `TitleBars` decisions, and a contrast check of both palettes.
- **Manual:** the window against a copy of a real database, including a change
  made in a terminal while the window is open.

## Decisions made here

- Same repository, `app/` beside the Swift sources.
- Window controls: Windows-style — minimize, maximize, close on the right — on
  every platform the app runs on, as the apps book asks. Chosen for an audience
  that mostly uses Windows, over the design language's traffic lights.
- No edit or delete in the first version.
- Export writes a file; it does not open a spreadsheet.
- A YCH with no slot is shown, sorted last in its group, rather than hidden.
- Errors are read from stderr, then stdout, then the exit code; no CLI change.
- The Raffles row shows no count, since no command returns one.
- The status filter belongs to the section, not the window, and is not saved.
- The YCH add form requires a slot, and leaves the title optional.

## Left to the builder

Exact spacing within the 4–10 range, icons and their absence, window size and
minimum, the order of fields in the add form, and the wording of the strings
table.
