-- Expresso_Sample schema for Oracle (run as a user that can create tables).
-- Quoted lowercase identifiers so they match the sample app SQL catalog.

CREATE TABLE "publisher" (
    "id"         NUMBER(10) NOT NULL PRIMARY KEY,
    "name"       VARCHAR2(300) NOT NULL,
    "country"    VARCHAR2(50) NOT NULL,
    "location"   VARCHAR2(100) NULL,
    "opens_at"   INTERVAL DAY(0) TO SECOND(0) DEFAULT NUMTODSINTERVAL(9, 'HOUR') NOT NULL,
    "closes_at"  INTERVAL DAY(0) TO SECOND(0) DEFAULT NUMTODSINTERVAL(17, 'HOUR') NOT NULL
);

CREATE TABLE "author" (
    "id"             NUMBER(10) NOT NULL PRIMARY KEY,
    "first_name"     VARCHAR2(100) NOT NULL,
    "last_name"      VARCHAR2(100) NOT NULL,
    "display_name"   VARCHAR2(100) NOT NULL,
    "date_of_birth"  DATE NULL,
    "created_at"     TIMESTAMP DEFAULT SYS_EXTRACT_UTC(SYSTIMESTAMP) NOT NULL
);

CREATE TABLE "book" (
    "id"            NUMBER(10) NOT NULL PRIMARY KEY,
    "title"         VARCHAR2(500) NOT NULL,
    "year"          NUMBER(5) NOT NULL,
    "isbn"          VARCHAR2(20) NULL,
    "publisher_id"  NUMBER(10) NOT NULL REFERENCES "publisher" ("id"),
    "rating"        BINARY_DOUBLE NOT NULL,
    "price"         NUMBER(8, 2) NOT NULL,
    "created_at"    TIMESTAMP DEFAULT SYS_EXTRACT_UTC(SYSTIMESTAMP) NOT NULL,
    "external_id"   RAW(16) NOT NULL
);

CREATE INDEX IX_book_publisher_id ON "book" ("publisher_id");

CREATE TABLE "book_author" (
    "book_id"    NUMBER(10) NOT NULL REFERENCES "book" ("id"),
    "author_id"  NUMBER(10) NOT NULL REFERENCES "author" ("id"),
    PRIMARY KEY ("book_id", "author_id")
);

CREATE INDEX IX_book_author_author_id ON "book_author" ("author_id");

CREATE TABLE "award" (
    "id"         NUMBER(10) NOT NULL PRIMARY KEY,
    "author_id"  NUMBER(10) NOT NULL REFERENCES "author" ("id"),
    "title"      VARCHAR2(300) NOT NULL,
    "year"       NUMBER(5) NOT NULL
);

CREATE INDEX IX_award_author_id ON "award" ("author_id");
