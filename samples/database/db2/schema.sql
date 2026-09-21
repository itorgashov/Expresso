-- Expresso_Sample schema for IBM DB2 LUW.
-- Quoted lowercase identifiers so they match the sample app SQL catalog.

CREATE TABLE "publisher" (
    "id"         INTEGER NOT NULL PRIMARY KEY,
    "name"       VARCHAR(300) NOT NULL,
    "country"    VARCHAR(50) NOT NULL,
    "location"   VARCHAR(100),
    "opens_at"   TIME NOT NULL DEFAULT '09:00:00',
    "closes_at"  TIME NOT NULL DEFAULT '17:00:00'
);

CREATE TABLE "author" (
    "id"             INTEGER NOT NULL PRIMARY KEY,
    "first_name"     VARCHAR(100) NOT NULL,
    "last_name"      VARCHAR(100) NOT NULL,
    "display_name"   VARCHAR(100) NOT NULL,
    "date_of_birth"  DATE,
    "created_at"     TIMESTAMP NOT NULL DEFAULT CURRENT TIMESTAMP
);

CREATE TABLE "book" (
    "id"            INTEGER NOT NULL PRIMARY KEY,
    "title"         VARCHAR(500) NOT NULL,
    "year"          SMALLINT NOT NULL,
    "isbn"          VARCHAR(20),
    "publisher_id"  INTEGER NOT NULL REFERENCES "publisher" ("id"),
    "rating"        DOUBLE NOT NULL,
    "price"         DECIMAL(8, 2) NOT NULL,
    "created_at"    TIMESTAMP NOT NULL DEFAULT CURRENT TIMESTAMP,
    "external_id"   CHAR(36) NOT NULL
);

CREATE INDEX IX_book_publisher_id ON "book" ("publisher_id");

CREATE TABLE "book_author" (
    "book_id"    INTEGER NOT NULL REFERENCES "book" ("id"),
    "author_id"  INTEGER NOT NULL REFERENCES "author" ("id"),
    PRIMARY KEY ("book_id", "author_id")
);

CREATE INDEX IX_book_author_author_id ON "book_author" ("author_id");

CREATE TABLE "award" (
    "id"         INTEGER NOT NULL PRIMARY KEY,
    "author_id"  INTEGER NOT NULL REFERENCES "author" ("id"),
    "title"      VARCHAR(300) NOT NULL,
    "year"       SMALLINT NOT NULL
);

CREATE INDEX IX_award_author_id ON "award" ("author_id");
