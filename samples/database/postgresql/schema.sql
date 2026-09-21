-- Expresso_Sample schema for PostgreSQL (run as a superuser; creates database if needed).
CREATE DATABASE "Expresso_Sample";
\connect Expresso_Sample

CREATE TABLE publisher (
    id         INTEGER NOT NULL PRIMARY KEY,
    name       VARCHAR(300) NOT NULL,
    country    VARCHAR(50) NOT NULL,
    location   VARCHAR(100) NULL,
    opens_at   TIME NOT NULL DEFAULT TIME '09:00:00',
    closes_at  TIME NOT NULL DEFAULT TIME '17:00:00'
);

CREATE TABLE author (
    id             INTEGER NOT NULL PRIMARY KEY,
    first_name     VARCHAR(100) NOT NULL,
    last_name      VARCHAR(100) NOT NULL,
    display_name   VARCHAR(100) NOT NULL,
    date_of_birth  DATE NULL,
    created_at     TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'UTC')
);

CREATE TABLE book (
    id            INTEGER NOT NULL PRIMARY KEY,
    title         VARCHAR(500) NOT NULL,
    year          SMALLINT NOT NULL,
    isbn          VARCHAR(20) NULL,
    publisher_id  INTEGER NOT NULL REFERENCES publisher (id),
    rating        DOUBLE PRECISION NOT NULL,
    price         NUMERIC(8, 2) NOT NULL,
    created_at    TIMESTAMP NOT NULL DEFAULT (CURRENT_TIMESTAMP AT TIME ZONE 'UTC'),
    external_id   UUID NOT NULL DEFAULT gen_random_uuid()
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
    title      VARCHAR(300) NOT NULL,
    year       SMALLINT NOT NULL
);

CREATE INDEX IX_award_author_id ON award (author_id);
