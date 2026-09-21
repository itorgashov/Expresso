-- Expresso_Sample schema for SQLite.
CREATE TABLE publisher (
    id         INTEGER NOT NULL PRIMARY KEY,
    name       TEXT NOT NULL,
    country    TEXT NOT NULL,
    location   TEXT NULL,
    opens_at   TEXT NOT NULL DEFAULT '09:00:00',
    closes_at  TEXT NOT NULL DEFAULT '17:00:00'
);

CREATE TABLE author (
    id             INTEGER NOT NULL PRIMARY KEY,
    first_name     TEXT NOT NULL,
    last_name      TEXT NOT NULL,
    display_name   TEXT NOT NULL,
    date_of_birth  TEXT NULL,
    created_at     TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE book (
    id            INTEGER NOT NULL PRIMARY KEY,
    title         TEXT NOT NULL,
    year          INTEGER NOT NULL,
    isbn          TEXT NULL,
    publisher_id  INTEGER NOT NULL REFERENCES publisher (id),
    rating        REAL NOT NULL,
    price         REAL NOT NULL,
    created_at    TEXT NOT NULL DEFAULT (datetime('now')),
    external_id   TEXT NOT NULL
);

CREATE INDEX IX_book_publisher_id ON book (publisher_id);

CREATE TABLE book_author (
    book_id    INTEGER NOT NULL REFERENCES book (id),
    author_id  INTEGER NOT NULL REFERENCES author (id),
    PRIMARY KEY (book_id, author_id)
);

CREATE INDEX IX_book_author_author_id ON book_author (author_id);

CREATE TABLE award (
    id         INTEGER NOT NULL PRIMARY KEY,
    author_id  INTEGER NOT NULL REFERENCES author (id),
    title      TEXT NOT NULL,
    year       INTEGER NOT NULL
);

CREATE INDEX IX_award_author_id ON award (author_id);
