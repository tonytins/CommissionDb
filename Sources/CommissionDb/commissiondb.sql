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
