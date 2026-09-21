# Expresso_Sample portable scripts

These scripts recreate the **sample** books schema (`publisher`, `author`, `book`, `book_author`, `award`) and load the same rows as the live SQL Server `Expresso_Sample` dump.

They are **not** used by renderer integration tests (`widget` tables live only in IT fixtures).

Canonical row dump: [seed.json](seed.json) (15 publishers, 50 authors, 305 books, 310 book_author rows, 4 awards). Run `schema.sql` then `seed.sql` in the engine folder. The agent does not apply these scripts to live databases.

SQL Server schema is also copied at [../Expresso.Sample.WebApi/database/schema.sql](../Expresso.Sample.WebApi/database/schema.sql). Seed for SQL Server: [sqlserver/seed.sql](sqlserver/seed.sql).

Oracle and Db2 schemas use **quoted lowercase** identifiers so they match the sample app SQL catalog.

| Folder | Engine |
|---|---|
| `sqlserver/` | SQL Server |
| `postgresql/` | PostgreSQL |
| `mysql/` | MySQL 8 / MariaDB |
| `sqlite/` | SQLite |
| `oracle/` | Oracle |
| `db2/` | IBM DB2 LUW |
