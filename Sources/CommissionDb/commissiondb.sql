CREATE TABLE records (
    id          INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    date        TEXT    NOT NULL,
    type        TEXT    NOT NULL CHECK(type IN ('commission', 'ych')),
    client      TEXT    NOT NULL,
    reference   TEXT    NOT NULL,
    description TEXT    NOT NULL DEFAULT '',
    fee         REAL    NOT NULL,
    ych_title   TEXT,
    slot        INTEGER,
    status      TEXT    NOT NULL DEFAULT 'pending'
                        CHECK(status IN ('pending', 'paid', 'cancelled', 'refunded'))
);
