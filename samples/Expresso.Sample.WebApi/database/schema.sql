-- Expresso sample database schema (Expresso_Sample)
-- Run against SQL Server to create the Books demo database.

IF DB_ID(N'Expresso_Sample') IS NOT NULL
BEGIN
    ALTER DATABASE Expresso_Sample SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE Expresso_Sample;
END
GO

CREATE DATABASE Expresso_Sample;
GO

USE Expresso_Sample;
GO

CREATE TABLE dbo.publisher
(
    id       INT            IDENTITY(1, 1) NOT NULL,
    name     NVARCHAR(300)  NOT NULL,
    country  NVARCHAR(50)   NOT NULL,
    location NVARCHAR(100)  NULL,
    CONSTRAINT PK_publisher PRIMARY KEY CLUSTERED (id)
);
GO

CREATE TABLE dbo.author
(
    id             INT            IDENTITY(1, 1) NOT NULL,
    first_name     NVARCHAR(100)  NOT NULL,
    last_name      NVARCHAR(100)  NOT NULL,
    display_name   NVARCHAR(100)  NOT NULL,
    date_of_birth  DATE           NULL,
    date_of_death  DATE           NULL,
    created_at     DATETIME2(7)   NOT NULL
        CONSTRAINT DF_author_created_at DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_author PRIMARY KEY CLUSTERED (id)
);
GO

CREATE TABLE dbo.book
(
    id           INT            IDENTITY(1, 1) NOT NULL,
    title        NVARCHAR(500)  NOT NULL,
    year         SMALLINT       NOT NULL,
    isbn         NVARCHAR(20)   NULL,
    publisher_id INT            NOT NULL,
    rating       FLOAT          NOT NULL,
    price        DECIMAL(8, 2)  NOT NULL,
    created_at   DATETIME2(7)   NOT NULL
        CONSTRAINT DF_book_created_at DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_book PRIMARY KEY CLUSTERED (id),
    CONSTRAINT FK_book_publisher
        FOREIGN KEY (publisher_id) REFERENCES dbo.publisher (id)
);
GO

CREATE INDEX IX_book_publisher_id ON dbo.book (publisher_id);
GO

CREATE UNIQUE INDEX UX_book_isbn
    ON dbo.book (isbn)
    WHERE isbn IS NOT NULL;
GO

CREATE TABLE dbo.book_author
(
    book_id   INT NOT NULL,
    author_id INT NOT NULL,
    CONSTRAINT PK_book_author PRIMARY KEY CLUSTERED (book_id, author_id),
    CONSTRAINT FK_book_author_book
        FOREIGN KEY (book_id) REFERENCES dbo.book (id),
    CONSTRAINT FK_book_author_author
        FOREIGN KEY (author_id) REFERENCES dbo.author (id)
);
GO

CREATE INDEX IX_book_author_author_id ON dbo.book_author (author_id);
GO
