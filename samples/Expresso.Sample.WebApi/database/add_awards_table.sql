-- Add awards table to an existing Expresso_Sample database (non-destructive).
-- Run against Expresso_Sample when the database already exists without dbo.award.

USE Expresso_Sample;
GO

IF OBJECT_ID(N'dbo.award', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.award
    (
        id         INT            IDENTITY(1, 1) NOT NULL,
        author_id  INT            NOT NULL,
        title      NVARCHAR(300)  NOT NULL,
        year       SMALLINT       NOT NULL,
        CONSTRAINT PK_award PRIMARY KEY CLUSTERED (id),
        CONSTRAINT FK_award_author
            FOREIGN KEY (author_id) REFERENCES dbo.author (id)
    );

    CREATE INDEX IX_award_author_id ON dbo.award (author_id);
END
GO
