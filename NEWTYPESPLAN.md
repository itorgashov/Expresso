# DateOnly, TimeOnly, and Guid support

Status: **complete**. Package version **0.5.0**.

Package version: **0.5.0** (breaking: `date()` returns `DateOnly` on net6.0).

## Scope

- **Guid:** all TFMs — literals, `eq`/`neq`/`in`/`isnull`.
- **DateOnly / TimeOnly:** net6.0 library TFM only (`#if NET6_0_OR_GREATER`).
- **Widened add*/getters** for DateOnly/TimeOnly on net6.0.
- **`date()` / `time()`:** SQL-only `CAST`; net6.0 `date()` returns `DateOnly`; new `time()` returns `TimeOnly`.
- **netstandard2.0:** Guid + unchanged DateTime-only `date()` (DateTime → DateTime).

See attached implementation plan for full type matrix, file checklist, and validation commands.
