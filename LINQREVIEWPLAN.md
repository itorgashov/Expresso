
# LINQ rendering review: remediation plan

Once you approve it, this plan is saved as `LINQREVIEWPLAN.md` in the repo root and kept up to date as work proceeds.

## Are the findings real?

I checked each finding against the code. All 11 are real defects.

- **R1 (P1), real.**
  - EF Core [`AuthorRepository.cs`](samples/Expresso.Sample.WebApi.EfCore/DataAccess/AuthorRepository.cs) materializes the authors, then calls `author.Awards.OrderByNested(_transformer, ...)`, using the `IEnumerable` overload in [`LinqQueryExtensions.cs`](src/Rendering/Expresso.Rendering.Linq/LinqQueryExtensions.cs).
  - That overload compiles the lambda and runs it in memory, where the EF Core `ExpressoDbFunctions` markers throw and EF6 `DbFunctions` behave differently.
  - The EF6 `BookRepository.ApplyNestedSort` and `AuthorRepository` have the same problem.
- **R2 (P1), real.** Every overload in [`ExpressoDbFunctions.cs`](src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoDbFunctions.cs) and `ExpressoFunctionTranslations.Table()` is keyed on `DateTime` or `TimeSpan`. With a `DateOnly` or `TimeOnly` argument, `Find` returns nothing and the expression falls back to the default CLR form.
- **R3 (P1), real.**
  - Oracle EF Core stores `TimeOnly` and `DateOnly` as ISO text, as the translation comments say.
  - The sample schema [`oracle/schema.sql`](samples/database/oracle/schema.sql) has `INTERVAL DAY(0) TO SECOND(0)` time columns and a `DATE` `date_of_birth`.
  - [`SampleDbContext.cs`](samples/Expresso.Sample.WebApi.EfCore/DataAccess/SampleDbContext.cs) maps neither of them.
- **R4 (P2), real.**
  - `Propagate` and `Unary` compute null state from the arguments only, and `VisitIsNull` reads that state.
  - So `isnull(f(x))` is wrong when the SQL function itself returns NULL. Examples: SQLite or MySQL `sqrt` of a negative number, and Oracle functions that return `''`, which Oracle treats as NULL.
- **R5 (P2), real.** DB2 cannot use a correlated subquery in `ORDER BY` (`SQL0206N`). The only workaround is [`LiftedSort.cs`](test/Rendering/Expresso.Rendering.Integration.Test/Ef/LiftedSort.cs), which lives in the test project and is not a public API.
- **R6 (P2), real.**
  - `IncludeSorted` calls `OrderedNavigation` once for each leaf path.
  - Each call rebuilds the shared ancestor's sort keys, which creates new parameter captures.
  - EF Core then sees two different filters on the same navigation and throws.
- **R7 (P2), real.** EF Core has no `Right` override. The base `s.Length` translates to `LEN` on SQL Server, which ignores trailing spaces. EF6 already uses `DbFunctions.Right` there.
- **R8 (P2), real for PostgreSQL in both EF Core and EF6.**
  - The base `Substring(0, n)` translates to `substring(s, 1, -n)`, which PostgreSQL rejects for a negative `n`.
  - The dialect renderer emits native `LEFT`/`RIGHT`, which accept a negative length.
  - EF6 overrides `Left`/`Right` only for SQL Server.
- **R9 (P2), real; this is my own bug.** [`SampleEf6Context.cs`](samples/Expresso.Sample.WebApi.NetFx.Ef6/DataAccess/SampleEf6Context.cs) calls `Ignore(OpensAt/ClosesAt)` on Oracle and SQLite, so the API returns `00:00`.
- **R10 (P2), real.** `ExpressoFunctions.Left/Right/Substring`, the base `Length` (`.Length`) and the in-memory `IndexOf` count UTF-16 units. PostgreSQL, whose semantics the in-memory profile follows, counts code points.
- **R11 (test harness), real.**
  - [`DifferentialOutcome.cs`](test/Rendering/Expresso.Rendering.Integration.Test/DifferentialOutcome.cs) catches every exception, and `AssertSame` treats any two errors as agreement.
  - [`Ef6ItTests.cs`](test/Rendering/Expresso.Rendering.Integration.Test/Ef6/Ef6ItTests.cs) `AssertedGap` accepts any exception.

## Phase 1: P1 fixes

### R1: run child sorts on the server
- **EF Core sample:** `AuthorRepository` uses `IncludeSorted(_transformer, sort, mapping)` instead of the in-memory `OrderByNested`, as `BookRepository` already does.
- **EF6 sample:** children are loaded with one provider-translated query for each nested level, instead of being sorted in memory.
  - Awards:
    - Query `db.Awards.Where(a => authorIds.Contains(a.AuthorId))` and apply the `IQueryable` `OrderBy(transformer, nested, awardMapping)`.
    - Then group the results by `AuthorId` and keep the server order.
  - Book authors (many-to-many):
    - Query `db.Authors.Where(a => a.Books.Any(b => bookIds.Contains(b.Id)))` and apply `OrderBy`.
    - Each book then lists its authors in that order. This is correct because the nested keys depend only on the author.
  - Shared helper: `DataAccess/NestedSortLoader.cs`.
- **Library:** the XML docs of the `IEnumerable` `OrderBy` and `OrderByNested` overloads, and `docs/linq-rendering.md`, will state that these overloads need `InMemoryExpressionToLinqTransformer`. Provider transformers emit SQL-only functions.
- **Tests:** a unit test checks that the EF6 loader keeps the server order. Smoke tests call `sortfor(awards, ...)` with `dayofweek`, `round` and `indexof` keys on SQL Server.

### R2: DateOnly and TimeOnly markers
1. First, find the gaps with a test matrix in [`ExpressoFunctionTranslationsTests.cs`](test/Rendering/Expresso.Rendering.EntityFrameworkCore.Test/ExpressoFunctionTranslationsTests.cs).
   - Give `TestWidgetContext` `DateOnly` and `TimeOnly` properties.
   - For each provider, run `ToQueryString` on every date and time function: `dayofweek`, `dayofyear`, `year`/`month`/`day`, `addyears`/`addmonths`/`adddays`, `hour`/`minute`/`second`, and `addhours`/`addminutes`/`addseconds`.
2. Add markers to `ExpressoDbFunctions` only where the matrix shows a failure or a divergence:
   - `DayOfWeek(DateOnly)` and `DayOfYear(DateOnly)`
   - `AddYears`, `AddMonths` and `AddDays` on `DateOnly`
   - `AddHours`, `AddMinutes` and `AddSeconds` on `TimeOnly`
   - `Hour`, `Minute` and `Second` on `TimeOnly`
3. Add the matching per-provider translations in `ExpressoFunctionTranslations`.
   - `Table()` gets `DateOnly` and `TimeOnly` first-parameter types.
   - The provider tables move into one file per provider (`ExpressoFunctionTranslations.<Provider>.cs`) to keep each file under 300 lines.
4. Route `DateOnly` and `TimeOnly` arguments to the markers in the `EfCoreExpressionToLinqTransformer` overrides (`DatePart`, `DayOfWeek`, `DateAdd`).
5. Integration regression: a `TemporalWidget` EF Core entity maps `opens` as `TimeOnly`, with a converter where the provider needs one. Differential cases that use `date(created)` exercise `DateOnly` results against ADO.

### R3: Oracle temporal mapping in the EF Core sample
- In `SampleDbContext`, on Oracle only:
  - Convert `TimeOnly` to `TimeSpan` with `HasColumnType("INTERVAL DAY(0) TO SECOND(0)")`.
  - Convert `DateOnly` to `DateTime` with `HasColumnType("DATE")`.
- Oracle's `DateOnly`/`TimeOnly` marker translations check the argument's `TypeMapping.StoreType`.
  - With INTERVAL or DATE storage, they use `EXTRACT` or interval arithmetic.
  - With the provider's text storage, they throw a clear `NotSupportedException`.
- **Tests:** a unit test asserts the Oracle SQL contains no `NVARCHAR2` or `TO_CHAR` text comparison. If Oracle is available, a smoke test filters on `opens` and `dateofbirth`.

## Phase 2: P2 fixes

### R4: NULLs computed by SQL functions
- **New hook** in `ExpressionToLinqTransformerBase`:

```csharp
protected virtual Expression? ComputedNull(string function, IReadOnlyList<LinqNode> args, Expression value) => null;
```

  - `Unary`, `Propagate` and `StringCall` combine it with OR into `IsNull`. The function names are passed in from the `Visit*` methods.
- **Overrides in EF Core and EF6:**
  - SQLite and MySQL `sqrt`: return `x < 0`. This is an explicit predicate, so EF cannot optimize it away.
  - Oracle, for string functions that can return `''` (substring, left, right, the trims, replace, indexof): return a value-is-null test.
    - EF Core uses an `ExpressoDbFunctions.IsNullValue(string)` marker, translated as `CASE WHEN x IS NULL THEN 1 ELSE 0 END = 1`. EF Core cannot simplify that by nullability propagation.
    - EF6 uses `value == null`.
  - Oracle literal `""`: build it as a NULL literal (`IsNull = true`).
- **Tests:** new differential cases `isnull(sqrt(amount - 1000))`, `isnull(substring(name, 0, 0))` and `isnull(left(name, 0))` run on all engines. There are also unit tests of the predicates.

### R5: public DB2-safe sort
- Move `LiftedSort` into the EF Core package as a public API in `src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreLiftedSort.cs`:

```csharp
public static IQueryable<TKey> OrderedKeys<T, TKey>(this IQueryable<T> source, IExpressionToLinqTransformer transformer,
    SortDirective sort, LinqQueryMapping<T> mapping, Expression<Func<T, TKey>> key) where T : class;
```

  - It works the same way as today: projects the keys into a derived table, uses `Distinct` to force the pushdown, orders the outer query, and returns the key values.
  - It supports up to 8 keys through a generic row type and throws `NotSupportedException` above that.
- The integration test's `EfEngineSession` switches to the public API, and the test copy is deleted.
- **EF Core sample on DB2:**
  - Repositories get the ordered ids with `OrderedKeys(...)`.
  - They then load the entities with `Where(ids.Contains)` plus includes, and restore the id order.
  - Other engines keep using `OrderBy`.
- **Tests:** a `ToQueryString` unit test, the existing DB2 integration cases running through the public API, and docs (`linq-rendering.md`, DB2 notes).

### R6: duplicate filters from sibling IncludeSorted chains
- In [`EfCoreQueryableExtensions.cs`](src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreQueryableExtensions.cs), cache the `OrderedNavigation` lambdas for each `SortDirective` reference within one `IncludeSorted` call: `Dictionary<SortDirective, LambdaExpression>` with a reference comparer.
- Sibling paths then reuse the same ancestor include expression.
- **Test:** sibling nested `sortfor` under one ancestor whose key contains a literal. `ToQueryString` must succeed and emit a single ancestor `ORDER BY`.

### R7 and R8: left and right
- **EF Core:**
  - SQL Server: a `Right(string, int)` marker translated as `RIGHT(s, n)`.
  - PostgreSQL: `Left` and `Right` markers translated as native `LEFT`/`RIGHT`.
  - Add the `Left` and `Right` overrides to `EfCoreExpressionToLinqTransformer`.
- **EF6 PostgreSQL:**
  - First try canonical `DbFunctions.Left/Right`, if Npgsql EF6 translates them to `left`/`right`.
  - Otherwise, use a conditional: `n >= 0 ? Substring(0, min) : Substring(0, max(len + n, 0))`.
  - If neither is exact, throw `Unsupported` and record a gap in `Ef6ProviderGaps`, the limits table and the function page.
- **New differential cases** for all engines: `right(concat(name, '  '), 3)` (trailing spaces, no seed change), `left(name, -2)` and `right(name, -2)`. Any engine that diverges is fixed or documented.

### R9: EF6 publisher times on Oracle and SQLite
- **SQLite:** map the `TEXT` columns to string backing properties (`OpensAtText`, `ClosesAtText`). `OpensAt` and `ClosesAt` become `[NotMapped]` and are parsed from them with the invariant culture.
- **Oracle:** EF6 has no type for INTERVAL. After the page loads, run one read-only, parameterized `Database.SqlQuery` for the page's publisher ids: `SELECT id, TO_CHAR(opens_at), TO_CHAR(closes_at) FROM publisher WHERE id IN (...)`. It runs wherever publisher times are returned.
- **Filtering and sorting** on `opens`/`closes` is removed from the field catalog on these two engines, so the API answers 400 "unknown field" rather than giving silent zeros. This is documented in the EF6 sample README.

### R10: Unicode positions in the in-memory profile
- [`ExpressoFunctions.cs`](src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs) gains code-point-aware `Length`, `IndexOf`, `Left`, `Right` and `Substring`. They walk surrogate pairs and use a fast path when the string has no surrogates.
- `InMemoryExpressionToLinqTransformer` overrides `Length` and `IndexOf` to use them.
- **Tests:** unit tests with surrogate pairs such as `"aðŸ˜€b"`, compared with PostgreSQL results. Update `docs/semantics.md` and the function pages.

## Phase 3: R11, a stricter differential harness
- `DifferentialOutcome.Of` classifies exceptions:
  - A `DbException` anywhere in the chain gives `error: database`.
  - A `NotSupportedException` gives `error: unsupported`.
  - Any other exception, such as an EF Core "could not be translated" `InvalidOperationException` or a null reference, is rethrown, so the test fails.
- `AssertSame` accepts two errors only when the reference error is `database` and the candidate error is `database` or `unsupported`.
- `Ef6Gap` gets a `Kind` (default: Unsupported). `AssertedGap` requires that exception type.
- Run every integration suite and fix any case this exposes.

## Wrap-up
- Docs: `docs/linq-rendering.md` (limits table, `OrderedKeys`, a note on in-memory overloads), the affected function pages, `docs/semantics.md`, both sample READMEs, `samples/AGENTS.md` and `src/Rendering/AGENTS.md`.
- Update `CONTEXT.md` and `IMPLEMENTATION.md` with brief notes.
- Verification:
  - `dotnet build` the solution with no errors.
  - All unit tests pass, with at least 85% coverage on the changed code.
  - Integration suites run on the engines available locally.
  - Smoke tests of both sample hosts.

## Risks
- **R3:** Oracle marker translations depend on reading `StoreType`. If a converter hides it, Oracle `DateOnly`/`TimeOnly` support falls back to `NotSupportedException` for text storage only.
- **R4:** the Oracle `IsNullValue` marker must survive EF Core's null-semantics pass. The plan checks this with `ToQueryString` before the change is extended to other functions.
- **R8 (EF6 PostgreSQL):** the outcome depends on what Npgsql EF6 translates. It may end as a documented gap that throws.

## N33/N34 execution plan — 2026-10-07

Authorized by the user's request to fix both findings and pass all unit/integration suites.

- N33 options: preserve native POWER with a narrow zero-underflow correction; change to the pow alias; or declare an EF6 provider gap. The alias has the same legacy result. The first logarithmic threshold was rejected by the non-binary boundary probes: it discarded a valid result for `POWER(0.3,618.89539068045326)`. The final correction evaluates the half exponent in the normal range and multiplies the result to round underflow; an exact binary halfway check handles ties to zero. Only nonzero minimum-subnormal results can be corrected. Logs locate a candidate binary exponent, verified by exact base identity and divisibility; they do not classify general underflow. Preserve ordinary subnormal results. Verify literal/field/nested/nullable operands, sort keys, and binary/non-binary boundary neighbors against native ADO.
- N34 options: cast a completed POWER operand; recursively cast numeric leaves inside POWER; or mark Oracle double literals at IR-to-LINQ construction. Choose the virtual `Literal` override (the base `Parameter` helper is static), so arithmetic consumes NUMBER operands before it can overflow. SQL-only `OracleNumber` overloads map double binds and integer promotions to CAST AS NUMBER; genuine binary columns remain unchanged. Native testing exposed integer promotion to BINARY_DOUBLE in a computed exponent; preserve NUMBER in arithmetic and nested POWER conversions as well. Remove the superseded direct-operand casting helper.
- File sequence: EF6 numeric override and SQL-shape regression tests; Oracle marker, literal override and translation registration plus composition/bind tests; shared differential cases; POWER docs and implementation status; full unit suites on net6.0/net48; full integration suites sequentially on net6.0/net8.0/net48; review closure evidence.
- Acceptance: all unit and integration legs complete with no failures. Existing explicitly documented provider skips are reported separately, not counted as successful coverage. No additional skip or broad error acceptance will be used to hide failures.

The full net48 run additionally reproduced the integer promotion defect in Oracle EF6. Its manifest exposes no NUMBER-preserving double conversion; an integer ROUND identity was tried and the provider still inserted a BINARY_DOUBLE cast. Following the approved exact-subset rule, reject POWER operands containing integer-to-double conversion with a specific `NotSupportedException`. Pin that limit for the affected catalog/differential cases, with unit tests and matching function/limits documentation; double-only POWER stays supported. This is an asserted provider limit, not a skipped test or an accepted arbitrary error.

Plan–review–execute–validate completed: N33/N34 are closed, both full unit legs and all three integration-project runs have zero failures, and the solution build plus both sample startup/Swagger smoke checks pass. Two existing MySQL EF6 time tests remain skipped. Final counts, coverage and the new Oracle EF6 limit are recorded in [the review report](review-LinqRendering.md#n33n34-remediation-and-acceptance-verification--2026-10-07).
