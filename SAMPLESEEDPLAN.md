# Sample seed and multi-engine sample app

Dump live `Expresso_Sample` rows (SELECT only) into portable seed scripts, then run the sample APIs against any supported engine.

- Canonical data: [samples/database/seed.json](samples/database/seed.json)
- Per-engine `schema.sql` + `seed.sql` under [samples/database](samples/database)
- One `BookRepository` / `AuthorRepository` / `PublisherRepository`; dialect SQL fragments live in `ISampleSql` (`Table`, `TableAs`, `Param`) plus `ISampleDb` bind/open
- Hosts: switch `ExpressoSample:Engine` in appsettings; connection strings are `ConnectionStrings:{Engine}` in user secrets (shared `UserSecretsId` across sample hosts)

Oracle/Db2 sample schemas use quoted lowercase identifiers. The net48 host does not register Db2. DB2 cannot `ORDER BY` a correlated collection aggregate (`count(authors)`).
