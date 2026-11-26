# Review Guide

Use this checklist when reviewing a change to commis.

## Before Reviewing

- Read the change description and the relevant code and tests.
- Identify the user-visible behavior or problem the change addresses. If it
  refers to an issue, check that the change addresses that context.
- Run `swift build` and `swift test`; report any checks that could not be run.

## Review Checklist

- Does the behavior match the stated intent, including edge cases such as
  missing options, empty strings, invalid fees or slots, and raffle lifecycle
  mistakes (entering a closed raffle, drawing twice, fulfilling before a draw)?
- Are CLI flag names, defaults, and help text clear and consistent with the
  existing command surface? Could the change break existing callers or scripts?
- Is CSV output format-correct (quoting, field order, header presence) for both
  commission and YCH paths?
- If the schema or an enum changed, do `commis.sql` and the Swift enum
  raw values still agree? Does `PRAGMA user_version` change, and is there a
  migration step that preserves existing rows? Was an already-shipped migration
  step edited?
- Do raffle operations leave the database consistent on failure? Entry batches
  should be all-or-nothing, and a draw or fulfill should either complete or
  change nothing.
- Do tests use a temporary database rather than the user's real one? Is the
  weighted draw tested with a seeded generator rather than by chance?
- Does a migration test start from the previous schema and check that old data
  survives?
- Are errors reported through `ArgumentParser`'s `ValidationError` or typed
  `throw`s — not silent defaults or `try!`?
- Do tests cover the changed behavior? Are there cases that need a live
  invocation check in addition to unit tests?
- Are [Usage.md](Usage.md) and [README.md](README.md) updated when the CLI
  surface or output format changes?

## Writing Review Comments

- Report concrete correctness, compatibility, reliability, or missing-test
  concerns; do not block changes on personal style preferences.
- Make each comment specific, constructive, and actionable. Point to the
  smallest relevant location and explain the impact and a useful next step.
- Prioritize findings by impact:
  - **Blocking**: prevents the package from building or causes data loss or
    severe failure in normal use.
  - **High**: breaks an existing flag, output format, or a common workflow.
  - **Medium**: causes a limited but meaningful behavior or reliability issue.
  - **Low**: a small edge case or non-blocking improvement.
- If no actionable issues remain, say so and note any checks that were not run.
