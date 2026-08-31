-- Seed sample awards for Expresso_Sample (run manually after add_awards_table.sql).
-- Matches authors by display_name; some authors intentionally have no awards.

USE Expresso_Sample;
GO

INSERT INTO dbo.award (author_id, title, year)
SELECT a.id, v.title, v.year
FROM (VALUES
    (N'Leo Tolstoy', N'Nobel Prize in Literature', 1901),
    (N'Leo Tolstoy', N'Pushkin Medal', 1884),
    (N'Fyodor Dostoevsky', N'Pushkin Prize', 1860),
    (N'Jane Austen', N'Posthumous literary acclaim', 1817)
) AS v(display_name, title, year)
INNER JOIN dbo.author AS a ON a.display_name = v.display_name;
GO
