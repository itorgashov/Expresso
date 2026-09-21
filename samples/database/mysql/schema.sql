-- Expresso_Sample schema for MySQL 8 / MariaDB.
CREATE DATABASE IF NOT EXISTS Expresso_Sample;
USE Expresso_Sample;

CREATE TABLE publisher (
    id         INT NOT NULL PRIMARY KEY,
    name       VARCHAR(300) NOT NULL,
    country    VARCHAR(50) NOT NULL,
    location   VARCHAR(100) NULL,
    opens_at   TIME NOT NULL DEFAULT '09:00:00',
    closes_at  TIME NOT NULL DEFAULT '17:00:00'
);

CREATE TABLE author (
    id             INT NOT NULL PRIMARY KEY,
    first_name     VARCHAR(100) NOT NULL,
    last_name      VARCHAR(100) NOT NULL,
    display_name   VARCHAR(100) NOT NULL,
    date_of_birth  DATE NULL,
    created_at     DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
);

CREATE TABLE book (
    id            INT NOT NULL PRIMARY KEY,
    title         VARCHAR(500) NOT NULL,
    year          SMALLINT NOT NULL,
    isbn          VARCHAR(20) NULL,
    publisher_id  INT NOT NULL,
    rating        DOUBLE NOT NULL,
    price         DECIMAL(8, 2) NOT NULL,
    created_at    DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    external_id   CHAR(36) NOT NULL,
    CONSTRAINT FK_book_publisher FOREIGN KEY (publisher_id) REFERENCES publisher (id)
);

CREATE INDEX IX_book_publisher_id ON book (publisher_id);

CREATE TABLE book_author (
    book_id    INT NOT NULL,
    author_id  INT NOT NULL,
    PRIMARY KEY (book_id, author_id),
    CONSTRAINT FK_book_author_book FOREIGN KEY (book_id) REFERENCES book (id),
    CONSTRAINT FK_book_author_author FOREIGN KEY (author_id) REFERENCES author (id)
);

CREATE INDEX IX_book_author_author_id ON book_author (author_id);

CREATE TABLE award (
    id         INT NOT NULL PRIMARY KEY,
    author_id  INT NOT NULL,
    title      VARCHAR(300) NOT NULL,
    year       SMALLINT NOT NULL,
    CONSTRAINT FK_award_author FOREIGN KEY (author_id) REFERENCES author (id)
);

CREATE INDEX IX_award_author_id ON award (author_id);
