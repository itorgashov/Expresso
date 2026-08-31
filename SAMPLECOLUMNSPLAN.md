# Sample columns aligned to live Expresso_Sample

Status: **complete**. No package version bump (sample-only).

TIME demo columns: `publisher.opens_at` / `closes_at` only (query fields `opens` / `closes`). Guid demo: `book.external_id` (filter only). DATE demo: `author.date_of_birth`. Field catalogs are per host: [SAMPLEFIELDPROVIDERPLAN.md](SAMPLEFIELDPROVIDERPLAN.md).

Dropped (live DB: run [samples/Expresso.Sample.WebApi/database/drop_excess_columns.sql](samples/Expresso.Sample.WebApi/database/drop_excess_columns.sql) yourself): `author.preferred_writing_time`, `author.date_of_death`, `book.publication_date`, `publisher.sync_id`.
