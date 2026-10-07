# Review: LINQ, EF Core, EF6 renderers and sample hosts

**Latest status:** N33 and N34 are fixed and closed after implementation and live acceptance verification on 2026-10-07. Both full unit legs passed (**2,097 net6.0; 2,064 net48**), as did all three full integration-project runs (**769 net6.0; 1,854 net8.0; 1,617 net48**, zero failures). Two pre-existing MySQL EF6 time tests remain skipped. Oracle EF6 POWER requiring integer-to-double promotion now throws a documented provider-limit error; the affected tests assert that error rather than skipping. The full solution builds, and both sample hosts passed startup/Swagger smoke checks. See [N33/N34 remediation and acceptance verification](#n33n34-remediation-and-acceptance-verification--2026-10-07). N32 remains closed. Earlier reviews are retained as history.

This review assesses the implementation against `LINQRENDERERPLAN.md`, `EFSAMPLEPLAN.md`, the documented function semantics, and the existing ADO renderers.

Reviewed on 2026-10-05 at HEAD `ff672f6` (package version `0.10.0`). Feature history inspected: `65fd95b..ff672f6`, including the initial renderers, documentation changes, and sample hosts. This report is the only repository file added by the review; no implementation, test, configuration, or planning files were changed.

## Assessment

The architecture follows the intended separation: the shared visitor constructs LINQ expressions, provider transformers override translation differences, and the sample hosts supply their own entity mappings. The passing unit suites provide useful coverage, but they do not establish the promised behavior across all supported CLR types, provider semantics, and sample execution paths.

There are **11 findings: 3 high priority (P1) and 8 medium priority (P2)**. The P1 findings affect ordinary, accepted sample requests. Several P2 findings silently change query results rather than raising an explicit unsupported-function error. The DB2 finding is supported by the implementation's own integration workaround; it was not reproduced against a DB2 server.

| ID | Priority | Finding | Evidence |
| --- | --- | --- | --- |
| R1 | P1 | Materialized child sorts execute EF functions in memory | Compiled-lambda failures reproduced; sample call sites inspected |
| R2 | P1 | DateOnly/TimeOnly operations lack required EF Core translations | Translation failures reproduced with installed providers |
| R3 | P1 | Oracle sample publisher times use an incompatible string conversion | Actual sample model and converter inspected; conversion failures reproduced |
| R4 | P2 | Computed SQL NULLs are lost by the separate null-state tracking | Generated SQL and SQLite scalar results verified |
| R5 | P2 | DB2 aggregate-sort workaround exists only in integration tests | Static inspection; live database confirmation outstanding |
| R6 | P2 | Sibling sorted includes conflict when a shared ancestor has literal keys | EF Core query-compilation failure reproduced |
| R7 | P2 | SQL Server right() miscounts trailing spaces | Generated SQL verified against documented SQL behavior |
| R8 | P2 | PostgreSQL negative left/right lengths become invalid substring calls | EF Core SQL generation verified; server behavior supported by primary sources |
| R9 | P2 | EF6 Oracle/SQLite sample publisher times silently become zero | Entity mapping, field catalogs, and response mapping inspected |
| R10 | P2 | In-memory string positions count UTF-16 units instead of PostgreSQL characters | Compiled predicates reproduced |
| R11 | P2 | Differential and provider-gap assertions accept unrelated failures | Assertion logic inspected |

P1 means a supported path needs prompt correction; P2 means a concrete correctness or validation gap that should be addressed in the normal fix cycle. Evidence qualifications below are part of each finding.

## Findings

### R1 — P1: execute nested child sorts in the correct execution environment

**Locations:** [EF6 BookRepository.cs:68](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.NetFx.Ef6/DataAccess/BookRepository.cs:68), [EF6 AuthorRepository.cs:52](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.NetFx.Ef6/DataAccess/AuthorRepository.cs:52), [EF Core AuthorRepository.cs:48](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.EfCore/DataAccess/AuthorRepository.cs:48), [LinqQueryExtensions.cs:56](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/LinqQueryExtensions.cs:56).

These repositories first materialize entities and then pass the provider-specific `_transformer` to the `IEnumerable` overload of `OrderByNested`. That overload compiles the generated key and executes it as .NET code. EF expressions are not generally executable in memory: EF6 canonical functions and Expresso provider markers deliberately throw there.

For example, on SQL Server the EF6 books request with `sort=sortfor(authors,dayofweek(createdat)),asc` reaches `DbFunctions.DiffDays` while sorting loaded authors. A direct reproduction of this compiled child-sort path threw `NotSupportedException: This function can only be invoked from LINQ to Entities.` Sorting by `right(displayname,2)` produced the same failure through `DbFunctions.Right`. In the EF Core authors endpoint, `sort=sortfor(awards,left(title,100)),asc` generates `Substring(0,100)` and throws `ArgumentOutOfRangeException` for a shorter title; the equivalent SQL function accepts the oversized length.

Even simple child keys use `InMemorySortComparer`, so ordinal string comparison and NULL placement can differ from the selected database. This defeats the samples' intended parity with the ADO hosts.

**Suggested direction:** use provider-backed child queries for EF6 and `IncludeSorted` for the EF Core authors endpoint, as the EF Core books endpoint already does. If client sorting is retained, choose an explicitly executable profile and acknowledge its semantic differences; merely replacing the transformer does not preserve database collation or NULL ordering. The EF6 client-sorting choice in `EFSAMPLEPLAN.md` needs reconsideration for that reason.

**Regression check:** exercise the actual repositories/endpoints with function-based child sorts and nullable/string keys, comparing both successful responses and child order with the ADO host. The integration session's server-side child-query path does not test these sample call sites.

### R2 — P1: add provider translations for the DateOnly/TimeOnly types exposed by the EF Core sample

**Locations:** [ExpressoFunctionTranslations.cs:16](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.cs:16), [EfCoreExpressionToLinqTransformer.cs:41](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.cs:41), [ExpressionToLinqTransformerBase.DateTime.cs:60](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.DateTime.cs:60), [RequestFieldsInfoProvider.cs:39](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.EfCore/Filtering/RequestFieldsInfoProvider.cs:39).

Marker lookup matches exact argument types. The custom day-of-week registrations cover `DateTime`, and the time-addition registrations cover `TimeSpan`; they do not cover the sample's `DateOnly` and `TimeOnly` fields. Missing markers fall back to CLR members that some installed EF providers cannot translate.

With a model mapped to these types, `ToQueryString()` reproduced:

| Filter | Providers with reproduced failure | Failing fallback |
| --- | --- | --- |
| `eq(dayofweek(day),0)` on DateOnly | SQL Server, Oracle | `(int)DateOnly.DayOfWeek` |
| `eq(hour(addseconds(at,10)),0)` on TimeOnly | SQL Server, SQLite, Oracle | `TimeOnly.Add(TimeSpan)` |

These are accepted filters. In the actual sample the corresponding fields are `dateofbirth`, `opens`, and `closes`, so this is an exposed API failure, not just a hypothetical consumer model. PostgreSQL translated both probes successfully; support cannot be inferred from that provider's result. The integration model uses `DateTime`/`TimeSpan`, while object-level DateOnly/TimeOnly tests do not verify SQL translation.

**Suggested direction:** cover each accepted CLR date/time type in the provider override matrix, including typed markers where required. Verify additions and date parts together rather than assuming the framework's native translation covers them.

**Regression check:** provider-model translation tests and differential cases using DateOnly/TimeOnly fields, with nullable variants and time rollover.

### R3 — P1: map Oracle sample times to the shipped INTERVAL columns

**Locations:** [SampleDbContext.cs:40](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.EfCore/DataAccess/SampleDbContext.cs:40), [Oracle schema.sql:9](C:/Users/itorg/source/repos/Expresso/samples/database/oracle/schema.sql:9).

The shipped Oracle schema defines publisher times as `INTERVAL DAY(0) TO SECOND(0)`, but the sample leaves TimeOnly mapping to the provider default. Inspecting the actual `SampleDbContext` with the installed Oracle provider produced:

```text
Publisher.OpensAt / ClosesAt:
  Store type: NVARCHAR2(48)
  Reader: GetString
  Converter: string -> TimeOnly.Parse(string, InvariantCulture, None)
```

The selected converter threw `FormatException` for interval strings `+0 09:00:00` and `+00 09:00:00.000000000`. Oracle represents intervals with a day component; recent ODP.NET versions allow `GetString` for interval/date types, so the defect is the incompatible conversion and store mapping, not necessarily an invalid reader accessor. See [Oracle interval representation](https://docs.oracle.com/en/database/oracle/oracle-data-access-components/19.3/odpnt/IntervalDSCtor2.html) and [GetString behavior](https://docs.oracle.com/en/database/oracle/oracle-database/21/odpnt/DataReaderGetString.html).

The sample filter `eq(opens,"09:00")` also binds `N'09:00:00'` as a string against the interval column. Thus reads and comparisons do not have a consistent native interval mapping. The model likewise chooses `NVARCHAR2(10)` for DateOnly `DateOfBirth`, although the schema uses `DATE`; that additional mismatch needs native mapping verification and should not depend on NLS string conversions.

**Suggested direction:** explicitly map Oracle temporal fields through supported native DateTime/TimeSpan representations and conversions, checking materialization and predicate translation against the existing schema.

**Regression check:** read publishers with the default 09:00/17:00 values, filter by opening time, and read/filter nullable birth dates on Oracle. Model/converter failures were reproduced locally; an end-to-end Oracle request was not run.

### R4 — P2: preserve NULLs produced by SQL operations, not only NULL inputs

**Locations:** [ExpressionToLinqTransformerBase.Numeric.cs:56](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:56), [ExpressionToLinqTransformerBase.Logic.cs:89](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Logic.cs:89), [ExpressionToLinqTransformerBase.String.cs:52](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:52).

`Propagate` determines a scalar's null state only from its arguments. `VisitIsNull` tests that state without examining the computed result. SQL functions can return NULL even when every input is non-NULL, causing accepted filters to silently return the wrong rows.

Reproduced examples:

| Expression | Generated EF Core predicate | Expected engine behavior |
| --- | --- | --- |
| SQLite `isnull(sqrt(number))`, non-nullable numeric field | `WHERE 0` | `sqrt(-1)` is NULL; scalar `SELECT sqrt(-1) IS NULL` returned 1 |
| Oracle `isnull(indexof(name,""))` | `WHERE name IS NULL` | Oracle treats the empty search string as NULL, making the computed result NULL for non-null names too |
| Oracle `isnull(concat("",""))` | `WHERE 0 = 1` | Concatenating two empty Oracle strings produces NULL |

The SQLite scalar check used only an in-memory connection. The Oracle cases were confirmed at SQL-generation level and compared with the existing dialect semantics. The documented Oracle empty-substring gap is another manifestation; fixing or documenting that single function does not resolve the shared null-state assumption.

**Suggested direction:** make provider-computed nullability part of scalar translation, retaining the necessary guards for executable in-memory expressions. Audit nesting and aggregates, since these also consume the scalar's recorded null state.

**Regression check:** `isnull` around domain-error arithmetic and empty-string operations, including computed values nested inside other functions and aggregates.

### R5 — P2: move the DB2 sort workaround into a supported consumer path

**Locations:** [LinqQueryExtensions.cs:35](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/LinqQueryExtensions.cs:35), [BookRepository.cs:35](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.EfCore/DataAccess/BookRepository.cs:35), former `test/Rendering/Expresso.Rendering.Integration.Test/Ef/LiftedSort.cs:22` (removed by remediation), [EfEngineSession.cs:59](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.Integration.Test/Ef/EfEngineSession.cs:59), [Db2It.cs:63](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.Integration.Test/Db2It.cs:63).

The integration code explicitly records DB2's rejection of correlated subqueries in ORDER BY (`SQL0206N`). Its DB2 session replaces normal sorting with `LiftedSort.OrderedIds`, projecting keys into a derived table before ordering the outer query. That helper exists only in the test project.

The public `OrderBy` extension and EF Core sample instead place generated keys directly into ORDER BY. A books request with `sort=count(authors),asc` uses a correlated collection aggregate and does not take the workaround. Consequently, the integration results validate a rewritten test query that consumers cannot obtain through the advertised extension.

**Suggested direction:** supply and use a provider-supported production sorting path for these keys, or explicitly reject/document this limitation. The parity tests should exercise the same public path as the sample; a fixture-specific rewrite cannot establish that API's correctness.

**Regression check:** run collection-aggregate sorting through the public API and the DB2 sample, including multiple sort keys. This finding is based on the existing workaround and divergent call paths; a local DB2 probe could not complete because the native CLI library was unavailable, so live failure confirmation remains outstanding.

### R6 — P2: reuse the shared ancestor's sort expression across sibling includes

**Locations:** [EfCoreQueryableExtensions.cs:64](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreQueryableExtensions.cs:64), [EfCoreQueryableExtensions.cs:129](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreQueryableExtensions.cs:129).

`Paths` emits one complete include chain per leaf. With two sibling nested collections, `OrderedNavigation` rebuilds their common ancestor's keys twice. Captured literal boxes become different EF parameters, so EF regards the two ancestor filters as different.

Reproduction: a `children` collection sorted by `add(score,1)`, with both `children/a` and `children/b` sorted by `value`. `IncludeSorted(...).ToQueryString()` threw:

```text
The filters ... OrderBy(e => (int?)(e.Score + __Value_1))
and ... OrderBy(e => (int?)(e.Score + __Value_0))
have both been configured on the same included navigation.
Only one unique filter per navigation is allowed.
```

All mappings and directives were valid; neither sibling had a contradictory parent sort. This prevents supported branching `sortfor` directives from compiling when the repeated ancestor includes parameterized expressions.

**Suggested direction:** reuse the built ancestor navigation/key expression across leaf chains, preserving parameter identity rather than rebuilding equivalent captures.

**Regression check:** two sibling grandchildren with a shared computed parent sort containing a literal; retain coverage for simple field keys and deeper branches.

### R7 — P2: SQL Server right() must account for trailing spaces

**Locations:** [ExpressionToLinqTransformerBase.String.cs:34](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:34), [EfCoreExpressionToLinqTransformer.cs:37](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.cs:37).

EF Core inherits a right implementation based on `string.Length` and `Substring`. SQL Server translates the length to LEN, which excludes trailing spaces. For `right(name,2)` the generated branch is equivalent to:

```sql
SUBSTRING(name, LEN(name) - 2 + 1, 2)
```

For the value `abc ` this starts at position 2 and produces `bc`; the ADO renderer's native RIGHT returns `c `. Therefore filtering or sorting by right can disagree with ADO for padded values. The EF6 SQL Server override already uses the canonical Right function and does not have this particular fallback problem. [Microsoft's LEN documentation](https://learn.microsoft.com/en-us/sql/t-sql/functions/len-transact-sql?view=sql-server-ver16) confirms the trailing-space behavior.

**Suggested direction:** use a SQL Server native RIGHT translation rather than deriving a position from LEN.

**Regression check:** trailing spaces, an all-space value, NULL, and zero/oversized lengths. Generated SQL was reproduced; no SQL Server query was executed during this review.

### R8 — P2: preserve PostgreSQL's accepted negative left/right lengths

**Locations:** [ExpressionToLinqTransformerBase.String.cs:30](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:30), [Ef6ExpressionToLinqTransformer.cs:110](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.cs:110).

Negative lengths are accepted by the IR. PostgreSQL LEFT/RIGHT deliberately give them meaning, and the ADO and in-memory implementations use that behavior. For example, `left("abc",-1)` should be `ab`, while `right("abc",-1)` should be `bc`. [PostgreSQL's string-function documentation](https://www.postgresql.org/docs/16/functions-string.html) defines these cases.

With EF Core/Npgsql, the generated SQL instead used `substring(name,1,@n)` for left and a calculated start with `substring(...,@n)` for right, where `@n=-1`. PostgreSQL rejects negative substring lengths, as its [substring implementation](https://github.com/postgres/postgres/blob/REL_16_STABLE/src/backend/utils/adt/varlena.c#L913) shows. A valid filter therefore becomes an execution error instead of the intended result. EF6's PostgreSQL branch selects the same base CLR implementations, so that path also needs verification/correction; its generated PostgreSQL SQL was not separately reproduced.

**Suggested direction:** use provider-native LEFT/RIGHT or expressions that explicitly implement negative-length behavior.

**Regression check:** -1, a negative length equal to or exceeding the character count, NULL, and ordinary positive lengths against PostgreSQL ADO. SQL generation, rather than a live PostgreSQL execution, was verified here.

### R9 — P2: do not return fabricated zero times from the EF6 Oracle/SQLite samples

**Locations:** [SampleEf6Context.cs:51](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.NetFx.Ef6/DataAccess/SampleEf6Context.cs:51), [ViewModelMapper.cs:49](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.NetFx.Ef6/ViewModels/ViewModelMapper.cs:49), [BookLinqMappings.cs:35](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.NetFx.Ef6/DataAccess/BookLinqMappings.cs:35), [RequestFieldsInfoProvider.cs:51](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.NetFx.Ef6/Filtering/RequestFieldsInfoProvider.cs:51).

For Oracle and SQLite the context ignores `Publisher.OpensAt` and `ClosesAt`. They remain non-nullable TimeSpan properties with their default zero value, which the response mapper returns without qualification. A publisher stored with 09:00/17:00 opening hours is therefore represented as 00:00/00:00 by the sample.

The request field catalog and LINQ mappings still advertise `opens`/`closes`, so accepted filtering and sorting can target members that EF does not map. That produces a provider translation failure rather than a useful validation response. The documented EF6 Edm.Time limitation explains why direct mapping is difficult, but does not justify silently changing the response data.

**Suggested direction:** use a supported backing representation or explicit read projection for these values, and make unsupported query capabilities explicit where translation cannot be provided. Keep the intended response contract aligned with the ADO host.

**Regression check:** unfiltered publisher responses and time filters on both providers, using known non-zero stored values. This finding follows directly from static mapping/default-value behavior; these hosts were not run against databases during the review.

### R10 — P2: make the in-memory character functions match PostgreSQL for supplementary Unicode

**Locations:** [ExpressionToLinqTransformerBase.String.cs:82](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:82), [InMemoryExpressionToLinqTransformer.cs:67](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/InMemoryExpressionToLinqTransformer.cs:67), [ExpressoFunctions.cs:34](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:34).

The in-memory profile promises PostgreSQL function semantics, but Length, IndexOf, Substring, Left, and Right use .NET UTF-16 code-unit counts and offsets. PostgreSQL text functions use character positions; in UTF8 a supplementary character occupies one position. See [PostgreSQL string functions](https://www.postgresql.org/docs/16/functions-string.html).

For the value `😀a`, compiled in-memory predicates returned false for all three expressions:

```text
eq(len(name),2)
eq(indexof(name,"a"),1)
eq(left(name,1),"😀")
```

The actual .NET length/index are 3 and 2, and Left takes only the high surrogate. Besides differing filter results, slicing can produce invalid standalone surrogate content. Ordinal string comparison does not require splitting characters this way.

**Suggested direction:** implement character-aware offsets for the PostgreSQL reference profile, with a netstandard-compatible implementation where needed. Count Unicode scalar values, not grapheme clusters; the latter would introduce another semantic difference.

**Regression check:** supplementary characters before and within searched/sliced ranges, plus BMP text and negative lengths. In-memory failures were reproduced; PostgreSQL expectations come from its documented character semantics, not a live server test.

### R11 — P2: distinguish expected provider rejection from arbitrary exceptions in parity tests

**Locations:** [DifferentialOutcome.cs:24](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.Integration.Test/DifferentialOutcome.cs:24), [Ef6ItTests.cs:56](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.Integration.Test/Ef6/Ef6ItTests.cs:56).

`DifferentialOutcome.Of` catches every exception. `AssertSame` then regards any two errors as agreement, regardless of type or cause. For example, `error: PostgresException` and `error: NullReferenceException` pass without comparison. An intended database rejection on the reference side can therefore hide an unrelated renderer defect, and a shared connection/setup failure can pass a differential case outright.

The EF6 gap check similarly requires only that some exception occur. A connectivity error or unrelated mapping failure satisfies a case intended to demonstrate a specific unsupported function. This weakens the evidence behind the completed provider-parity checklist.

**Suggested direction:** describe expected rejection per case, verify an appropriate failure category/reason, and fail on infrastructure or unexpected implementation errors. Exact cross-provider exception equality is not necessary; accepting every exception is too broad.

**Regression check:** assert that unrelated exception categories fail parity and gap checks, while the intended unsupported/domain-error cases remain accepted.

## Verification and limits

Existing feature unit suites were run with `--no-build --no-restore`, Release configuration, and `Category!=Integration`:

| Project | Framework | Passed | Failed |
| --- | --- | ---: | ---: |
| Expresso.Rendering.Linq.Test | net6.0 | 252 | 0 |
| Expresso.Rendering.EntityFrameworkCore.Test | net8.0 | 195 | 0 |
| Expresso.Rendering.EntityFramework.Test | net48 | 186 | 0 |
| **Total** | | **633** | **0** |

Commands:

```powershell
dotnet test test\Rendering\Expresso.Rendering.Linq.Test\Expresso.Rendering.Linq.Test.csproj -c Release -f net6.0 --no-build --no-restore --filter "Category!=Integration" --nologo
dotnet test test\Rendering\Expresso.Rendering.EntityFrameworkCore.Test\Expresso.Rendering.EntityFrameworkCore.Test.csproj -c Release -f net8.0 --no-build --no-restore --filter "Category!=Integration" --nologo
dotnet test test\Rendering\Expresso.Rendering.EntityFramework.Test\Expresso.Rendering.EntityFramework.Test.csproj -c Release -f net48 --no-build --no-restore --filter "Category!=Integration" --nologo
```

A temporary harness outside the repository compiled the current Core, Parsing, LINQ, and EF Core source, plus the actual EF Core sample context/entities, using the installed provider assemblies. It exercised `ToQueryString`, model metadata, conversion delegates, and compiled in-memory expressions. A separate net48 probe exercised the EF6 compiled-sort path using the existing Release assemblies. These probes created no repository code changes. SQLite checks consisted only of scalar SELECTs against `:memory:`; no sample schema or seed scripts were executed.

No full solution rebuild, fresh-build unit run, coverage measurement, live sample-host smoke run, or real-server integration suite was performed. The passing unit counts refer to the existing Release test artifacts; current-source compilation and the additional reproductions provide separate evidence. DB2 native-driver availability prevented completing its local provider probe.

Inspection also covered visitor/SQL-walker compatibility, public mapping and expression APIs, NULL/boolean construction, collection aggregation, parameter capture, provider registration, sorted includes, sample engine/configuration setup, documentation, and CI/package wiring. No additional concrete defect was established in those reviewed areas beyond the findings above. Documented EF6 provider gaps were not counted as new findings merely because a provider lacks a capability.

## Recommended follow-up order

1. Correct the sample execution/mapping failures (R1–R3), then verify actual endpoint behavior across the affected providers.
2. Correct result parity and public query paths (R4–R10), adding the focused regression cases identified above.
3. Tighten error assertions (R11) before treating a passing full provider matrix as evidence that unsupported cases fail for the intended reason.

Any implementation should proceed through the repository's plan-review workflow. This review makes recommendations only and does not implement them.

## Remediation recheck — 2026-10-05

**Decision: keep the review open.** The working-tree remediation was compared with `LINQREVIEWPLAN.md` and `C:\Users\itorg\.cursor\plans\linq_review_remediation_02a9ae9f.plan.md`. Several changes correct the original defects, but there are **11 remaining findings: four P1 and seven P2**, including regressions introduced by the fixes. No source or test files were changed during this recheck.

### Disposition of the original findings

| Original finding | Recheck result |
| --- | --- |
| R1: client execution of provider child sorts | Execution-path fix is in place: EF Core authors use sorted includes; EF6 uses server child queries. Provider failures below are separate remaining issues. |
| R2: modern temporal types | Partially fixed. SQL Server/SQLite TimeOnly additions now translate; Oracle still lacks most required translations (N3). |
| R3: Oracle EF Core sample store mapping | Original mapping defect corrected at model/converter/SQL-generation level: INTERVAL/TimeSpan and DATE/DateTime are used. Temporal function support remains incomplete under R2/N3. |
| R4: computed NULLs | Partially fixed. SQLite negative SQRT and empty Oracle concat are handled, but indexof construction, empty search patterns, and arithmetic/error cases remain defective (N1, N6, N9). |
| R5: DB2 public sorting path | Public `OrderedKeys` now exists and the test copy is removed. The sample's materialized reorder step fails (N2). |
| R6: repeated ancestor include filters | Original literal-key/sibling reproduction now compiles. The cache introduces a different navigation-reuse bug (N10). |
| R7: SQL Server right/trailing spaces | Correct native RIGHT translation verified. |
| R8: negative PostgreSQL left/right | EF Core native LEFT/RIGHT verified. EF6/Npgsql still emits incorrect substr expressions (N8). |
| R9: EF6 publisher times | SQLite list hydration is present, but Oracle list reads and both engines' single-item reads remain defective (N4, N5). |
| R10: Unicode positions | Length/index/left/right probes now pass. Substring boundary calculation still uses UTF-16 length and can throw (N7). |
| R11: error comparisons | Unexpected non-database exceptions now escape, but arbitrary database errors still compare equal (N11). |

### N1 — P1: Oracle indexof now fails while constructing expressions in both EF profiles

**Locations:** [EF Core ComputedNull.cs:30](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:30), [EF6 ComputedNull.cs:30](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.ComputedNull.cs:30).

`indexof` is included in `OracleEmptyStringFunctions`, but its result is an integer. EF Core passes that result to `IsNullValue(string)`, and EF6 compares it with a string-typed NULL. Therefore even `eq(indexof(name,"a"),0)` fails before SQL generation:

```text
EF Core: ArgumentException: Expression of type 'System.Int32' cannot be used
         for parameter of type 'System.String' ... IsNullValue(System.String).
EF6: InvalidOperationException: The binary operator Equal is not defined
     for the types 'System.Int32' and 'System.String'.
```

Both failures were reproduced against the current implementation. Treat indexof separately from string-returning functions; preserve its integer type and the null state of its inputs. Add ordinary and empty-search Oracle indexof cases to expression-construction and provider SQL tests. Removing the previous Oracle indexof test case does not validate this replacement behavior.

### N2 — P1: every non-empty explicitly sorted DB2 sample result can fail after loading

**Location:** [EfCoreListSort.cs:39](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.EfCore/DataAccess/EfCoreListSort.cs:39).

After `ToListAsync`, `items.ToDictionary(e => EF.Property<int>(e,"Id"))` executes `EF.Property` on ordinary objects. This method only works inside translated EF queries. A direct reproduction of the exact dictionary operation threw:

```text
InvalidOperationException: The EF.Property<T> method may only be used
within Entity Framework LINQ queries.
```

The helper is shared by books, authors, and publishers. Thus a DB2 request with a parent sort and at least one result fails even if `OrderedKeys` and entity loading succeed. Use an executable CLR key accessor for the materialized phase, while retaining an expression selector for the database phase. Test the entire two-phase helper, including dictionary construction and order restoration; testing `OrderedKeys` alone misses this failure. No DB2 server was needed to reproduce the materialized-phase defect.

### N3 — P1: Oracle DateOnly/TimeOnly support is still substantially incomplete

**Location:** [ExpressoFunctionTranslations.cs:194](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.cs:194).

Oracle entries mirror only DateOnly dayofweek. They register no TimeOnly date parts/additions, no DateOnly additions or dayofyear, and no StoreType-aware handling promised by the plan. The transformer still falls back to CLR members that the provider cannot translate.

Using the actual corrected sample context, `eq(hour(opens),9)` and `eq(hour(addseconds(opens,10)),9)` both threw translation `InvalidOperationException`. The sample author queries also failed for year/dayofyear/addyears/addmonths/adddays on `dateofbirth`. A separate DateOnly/TimeOnly model confirmed the broader TimeOnly hour/minute/second/addhours/addminutes/addseconds failures. Adding public marker overloads without registering the matching provider translations does not provide support.

Implement the missing Oracle native-storage translations and verify them using the sample's converters, including nullable dates and nested functions. Also verify the planned explicit rejection of unsupported text storage. The promised provider-model matrix and TemporalWidget integration entity are absent from the current test changes; the existing test model still uses DateTime/TimeSpan. The failure remains in a supported sample path, not merely an untested theoretical mapping.

### N4 — P1: the new EF6 Oracle publisher-time loader cannot read the shipped schema correctly

**Locations:** [PublisherTimes.cs:37](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.NetFx.Ef6/DataAccess/PublisherTimes.cs:37), [PublisherTimes.cs:54](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.NetFx.Ef6/DataAccess/PublisherTimes.cs:54), [Oracle schema.sql:4](C:/Users/itorg/source/repos/Expresso/samples/database/oracle/schema.sql:4).

There are two independent defects in this new read path:

- The raw SELECT uses unquoted `publisher`, `id`, `opens_at`, and `closes_at`, although the schema creates quoted lowercase names. Those references do not identify the shipped objects. Oracle requires quotes when referring to these names; see [Oracle identifier rules](https://docs.oracle.com/en/database/oracle/oracle-database/26/sqlrf/Database-Object-Names-and-Qualifiers.html).
- `TO_CHAR` returns interval text containing a day component, while `ParseTime` accepts only `hh:mm:ss`. The installed Oracle interval type produced `+000000000 09:00:00.000000000`; applying the loader's parsing expression threw `FormatException`. A shorter interval form such as `+0 09:00:00` is incompatible too.

Quote the actual schema identifiers and convert the interval to an explicitly supported representation before parsing. Test the complete Oracle list response using the shipped columns and non-zero times. Identifier mismatch is established statically against the schema; interval parsing was reproduced locally. The raw SELECT was not executed against Oracle during this recheck.

### N5 — P2: single-publisher reads still return zero opening/closing times on EF6 SQLite and Oracle

**Locations:** [PublisherRepository.cs:50](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.NetFx.Ef6/DataAccess/PublisherRepository.cs:50), [Publisher.cs:16](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.NetFx.Ef6/Entities/Publisher.cs:16), [PublishersController.cs:59](C:/Users/itorg/source/repos/Expresso/samples/Expresso.Sample.WebApi.NetFx.Ef6/Controllers/PublishersController.cs:59).

Only `GetAllAsync` calls `PublisherTimes.Apply`. `GetByIdAsync` directly returns the loaded entity. The ignored TimeSpan members are ordinary auto-properties; loading SQLite text backing properties does not populate them. Consequently `/api/publishers/{id}` still serializes 00:00/00:00 for a stored 09:00/17:00 publisher. Oracle has the same omitted hydration path.

Apply time hydration consistently before both response paths, or make the backing-property conversion part of the entity/projection contract. Add list-versus-single response parity checks. This follows directly from the repository/entity/controller code and does not require a server to establish the missing call.

### N6 — P2: empty Oracle search literals now silently reject all rows

**Locations:** [EF Core ComputedNull.cs:12](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:12), [EF6 ComputedNull.cs:12](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.ComputedNull.cs:12), [SQL string walker.cs:125](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Common/ExpressionToSqlQueryClauseTransformerBase.String.cs:125).

Globally converting an empty string literal to NULL makes `LikeTest`'s argument guard false. EF Core SQL generation reproduced `WHERE 0 = 1` for each of `contains(name,"")`, `startswith(name,"")`, and `endswith(name,"")`.

The ADO walker instead builds the literal LIKE pattern before parameterization: `%%` for contains and `%` for prefix/suffix. Those patterns match non-null names. Raw empty-string scalar semantics therefore cannot be applied indiscriminately to a search literal that the dialect rewrites into a non-empty wildcard pattern. EF6 has the same new literal normalization and shared guard.

Preserve the dialect behavior at the search-function boundary and test all three empty-literal patterns, NULL sources, and computed patterns against Oracle ADO. EF Core SQL failure was reproduced; EF6 impact is supported by the shared construction path.

### N7 — P2: Unicode substring can throw for valid out-of-range lengths/positions

**Location:** [ExpressoFunctions.cs:42](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:42).

Substring still clamps its end with `source.Length`, then interprets the resulting positions as code-point indices. With a surrogate pair, that can send `IndexFromCodePoint` beyond the string's character count. Both reproductions threw `IndexOutOfRangeException`:

```csharp
ExpressoFunctions.Substring("😀a", 1, 100); // expected "😀a"
ExpressoFunctions.Substring("😀a", 3, 1);   // expected ""
```

Use the code-point count throughout boundary calculation, retaining the existing handling of negative starts and oversized lengths. The new tests cover length/index/left/right, but do not combine Unicode input with substring boundary cases.

### N8 — P2: EF6/Npgsql canonical Left/Right still do not implement negative lengths

**Location:** [Ef6ExpressionToLinqTransformer.cs:110](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.cs:110).

The new PostgreSQL branch selects canonical `DbFunctions.Left/Right`. Query SQL was generated with the actual Npgsql EF6 6.4.3 provider and Npgsql 6.0.11, using a fixed manifest token without opening a connection. For a sort key with length -1, the provider emitted:

```sql
-- Left:
substr(name, 1, @negative_length)
-- Right:
substr(name, char_length(name) + 1 - @negative_length)
```

The left expression still raises PostgreSQL's negative substring-length error. The right expression starts beyond the end and returns an empty slice instead of dropping the first character. For `abc`, native LEFT/RIGHT with -1 should return `ab`/`bc`; see [PostgreSQL string semantics](https://www.postgresql.org/docs/16/functions-string.html) and [substring length validation](https://github.com/postgres/postgres/blob/REL_16_STABLE/src/backend/utils/adt/varlena.c#L913).

Use the plan's exact conditional fallback or another verified provider path. Test final PostgreSQL SQL/results; checking only that the expression tree contains `Left(` or `Right(` is insufficient. The EF Core native-marker fix is correct and does not need this fallback.

### N9 — P2: arithmetic NULL/error parity remains incomplete after the SQRT-specific fix

**Locations:** [Numeric.cs:65](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:65), [ComputedNull hook.cs:138](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.cs:138).

Numeric binary operators still call `Propagate` without a function name, so the new hook cannot account for their computed NULLs. On SQLite, `isnull(div(number,0))` for a non-nullable numeric field still generates `WHERE 0`, while the scalar check `SELECT (1.0 / 0) IS NULL` returned 1. The underlying R4 result-loss defect therefore remains beyond the explicitly patched SQRT example.

There is also an uncovered rejection mismatch in the newly added `isnull-sqrt-neg` differential case. The in-memory profile returned false for `isnull(sqrt(number))` with number -1; PostgreSQL rejects a negative square root ([PostgreSQL implementation](https://github.com/postgres/postgres/blob/REL_16_STABLE/src/backend/utils/adt/float.c#L1381)). The case is included in `CollationFreeCases` and thus in the in-memory/PostgreSQL integration comparison. This predicts a remaining parity failure; that real-server integration case was not run here.

Cover binary arithmetic computed NULLs and the reference profile's domain-error semantics, including functions whose result is consumed by `isnull`. Add expected result/rejection assertions rather than only testing the new hook's presence.

### N10 — P2: the sorted-include cache can silently replace one navigation with another

**Locations:** [EfCoreQueryableExtensions.cs:64](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreQueryableExtensions.cs:64), [EfCoreQueryableExtensions.cs:119](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreQueryableExtensions.cs:119).

The cache key contains only the SortDirective reference. A valid programmatically constructed directive can reuse the same sort object for different collection mappings. The second collection then receives the first collection's cached navigation lambda.

Reproduction: `children/a` and `children/b`, both mapped to Leaf collections and sharing one `value,asc` directive. Generated SQL joined Leaf only through `AId`; the `BId` navigation was absent. The query succeeds while omitting an explicitly requested collection. The original repeated-ancestor/literal case is fixed, but this new reuse case is not.

Key the cache by both the collection/navigation mapping and directive identity. Test distinct sibling navigations sharing a directive, alongside the actual branching/literal ancestor reproduction. The added test duplicates the same `tags` path and uses a simple field key, so it does not exercise either the original failing shape or this new one.

### N11 — P2: unrelated database failures still pass differential comparison

**Location:** [DifferentialOutcome.cs:32](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.Integration.Test/DifferentialOutcome.cs:32).

All DbExceptions collapse to `error: database`, and equality of those strings returns success. Using the actual helper, a SQLite cannot-open-database exception (code 14) on one side and a syntax-error exception (code 1) on the other were accepted as equivalent. Expected function rejection can likewise hide unrelated invalid generated SQL.

The change correctly exposes unexpected non-database failures, but does not complete the original requirement to distinguish expected rejection from implementation/infrastructure errors. Record expected rejection per case and preserve enough native error information to validate it. Infrastructure failures must fail the test; gap checks should also verify the relevant unsupported reason/error code, not just a broad exception class.

### Validation and remaining plan gaps

All these unit runs freshly built the current projects in Release, with `--no-restore --filter "Category!=Integration"`:

| Project | Framework | Passed | Failed |
| --- | --- | ---: | ---: |
| LINQ tests | net6.0 | 318 | 0 |
| LINQ tests | net48 | 318 | 0 |
| EF Core tests | net8.0 | 232 | 0 |
| EF6 tests | net48 | 216 | 0 |
| **Total test executions** | | **1,084** | **0** |

Additional probes compiled current Core/Parsing/LINQ/EF Core source, inspected the actual sample model, generated provider SQL, compiled predicates, and executed scalar SELECTs on SQLite `:memory:`. EF6 probes used the freshly built libraries; the Npgsql probe used the sample's provider versions and a fixed manifest token. No external database server connection, schema/seed script, or sample host was used.

The passing suites do not cover several promised regression checks. In addition to the missing temporal model matrix/TemporalWidget and sample helper/endpoints, `For_EveryRegisteredTranslation_HasGoldenSql` was weakened: it now checks only that translations print some text, then checks GoldenBase entries against a set made from GoldenBase itself. That final assertion is tautological and no longer ensures every registered translation has an expected SQL assertion. Restore meaningful coverage of the registered set and actual provider/model translation.

The sample publisher field removal also needs the planned README/function-capability documentation. Full solution build, real-server integration runs, coverage measurement, and sample smoke testing were not repeated in this recheck. They should follow the functional fixes rather than be treated as substitutes for the concrete failures above.

Local probe evidence is retained outside the repository in [RemediationProbe-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/RemediationProbe-output.txt) and [Ef6RemediationProbe-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/Ef6RemediationProbe-output.txt). This recheck updates the review only; implementation and tests remain the user's working-tree changes.

## Second remediation recheck — 2026-10-05

**The review cannot close yet.** Most of the concrete N1–N11 reproductions are fixed, and the expanded unit suites pass. Eight P2 findings remain: carried-forward N9/N11 and N12–N17 below. The original P1 construction, materialized-sort, missing-registration and publisher-loader failures no longer reproduce in their original forms.

This recheck examines the current uncommitted implementation and tests at HEAD `ff672f6`, against `LINQREVIEWPLAN.md`, the original feature plan, the previous findings and same-engine ADO behavior. Only this review document was edited in the repository.

### Disposition of N1–N11

| Finding | Current disposition |
| --- | --- |
| N1: Oracle indexof construction | Fixed: integer results are no longer passed to string NULL checks. Ordinary and empty-search predicates construct successfully in both profiles. Empty-search scalar NULL semantics remain wrong under N12. |
| N2: DB2 materialized reorder | Fixed: the sample compiles a CLR property selector and calls `OrderByKeys`. A materialized `[1,2]` input with keys `[2,1]` now restores `[2,1]` without `EF.Property`. The actual DB2 two-query path was not executed against a server. |
| N3: Oracle modern temporal translations | Missing registrations are present; the sample's native DATE/INTERVAL queries now generate SQL, and actual text-backed properties are explicitly rejected. Composition and boundary parity remain open under N13, N15 and N16. |
| N4: Oracle publisher-time SELECT/parser | Fixed at source/unit-test level: shipped lowercase identifiers are quoted, `EXTRACT` produces clock text, and both clock and signed-day/fraction interval text parse. No Oracle server read was performed. |
| N5: single-publisher hydration | Fixed: `GetByIdAsync` now invokes the same hydration helper as list reads. Verified by repository inspection; endpoints were not smoke-tested. |
| N6: empty Oracle LIKE search literals | Fixed: all three empty-literal search predicates generate an effective LIKE/non-null predicate instead of `WHERE 0 = 1`. Removing global empty-literal normalization introduces the scalar regressions in N12. |
| N7: Unicode substring bounds | Fixed: bounds use code-point length. Both the oversized-length and past-end surrogate-pair reproductions return the expected string/empty string. New regression tests pass on both frameworks. |
| N8: EF6/Npgsql negative left/right | Fixed: actual provider SQL now uses CASE with a non-negative left slice length and the correct negative-right start/length. The installed sample provider versions were used without a connection. |
| N9: arithmetic NULL/domain behavior | The named binary hook, SQLite zero-divisor checks and in-memory PostgreSQL-style rejection fix those specific reproductions. EF profiles still omit domain-error evaluation (N9 below), and MySQL still has the computed-NULL defect (N14). |
| N10: include navigation cache | Fixed: cache identity includes both collection mapping and directive. SQL contains both sibling foreign keys for a shared directive and for the branching ancestor/literal reproduction. Both new regression tests pass. |
| N11: error equivalence | Partially fixed: different native codes fail, but infrastructure errors can still pass as expected domain rejection or identical errors. Remains open below. |

The previous tautological golden-coverage check is repaired: it compares the registered provider/method/type set against the expected SQL set. The EF6 sample README now describes the Oracle/SQLite field restrictions and hydration paths.

### N9 — P2, still open: EF isnull predicates can omit domain-error evaluation entirely

**Locations:** [ExpressionToLinqTransformerBase.Logic.cs:89](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Logic.cs:89), [EF Core ComputedNull.cs:12](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:12), [EF6 ComputedNull.cs:12](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.ComputedNull.cs:12).

The in-memory domain guard now correctly throws inside `isnull`. The EF profiles still use only the argument NULL state for providers where the arithmetic expression raises an error rather than returning NULL. `VisitIsNull` therefore drops the arithmetic value from the final predicate.

For a non-nullable numeric field, current SQL generation for `isnull(sqrt(number))` produces `WHERE FALSE` on PostgreSQL and `WHERE 0 = 1` on SQL Server: neither query contains SQRT. The same elimination applies to `isnull(div(number,0))` when no provider computed-NULL condition is registered.

On PostgreSQL, a negative square root is a domain error ([native implementation](https://github.com/postgres/postgres/blob/REL_16_STABLE/src/backend/utils/adt/float.c#L1381)). The ADO expression retains the SQRT call inside `IS NULL`. Consequently the existing `isnull-sqrt-neg` differential case, which subtracts 1000 from every non-null seed amount, still compares ADO rejection with an EF Core empty id list. The new `Domain` setting permits an unsupported exception, not that successful empty result. This predicts a remaining opt-in integration failure; no PostgreSQL server run was made here.

Preserve evaluation of potentially rejecting SQL expressions inside `isnull`, while keeping the corrected NULL-input and SQLite/MySQL SQRT behavior. Verify native domain errors through the final provider SQL and expected-rejection tests, not only the in-memory guard. Cover SQRT and zero division/modulo on error-producing providers.

### N11 — P2, still open: differential comparison accepts infrastructure failures

**Locations:** [DifferentialOutcome.cs:35](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.Integration.Test/DifferentialOutcome.cs:35), [DifferentialOutcome.cs:40](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.Integration.Test/DifferentialOutcome.cs:40), [RendererDifferentialCases.cs:14](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.TestCases/RendererDifferentialCases.cs:14).

The native-code fix correctly rejects the former SQLite code-14-versus-code-1 reproduction. However, `Domain` accepts **any** reference database error paired with **any** candidate `NotSupportedException`. Exact equality also accepts any two identical database errors, even for a case whose rejection setting is `None`.

Both of these calls were accepted using the current helper:

```text
Domain: reference SQLite cannot-open-database (14)
        candidate NotSupportedException("negative square root")
None:   reference SQLite cannot-open-database (14)
        candidate SQLite cannot-open-database (14)
```

Neither establishes renderer parity. A connection failure on the ADO side can hide a candidate implementation rejection; a shared infrastructure or syntax failure can make an ordinary differential case pass. Recording just `None`/`Domain` does not identify the expected native rejection or candidate reason.

Require expected rejection codes/reasons per case and provider, reject infrastructure errors before comparison, and require success for cases without an expected rejection. Add negative tests for both reproductions above and for unrelated candidate unsupported reasons. The six current helper tests pass but do not cover these paths.

### N12 — P2: fixing empty Oracle LIKE patterns restores incorrect scalar NULL results

**Locations:** [ExpressionToLinqTransformerBase.cs:114](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.cs:114), [ExpressionToLinqTransformerBase.String.cs:52](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:52), [EF Core ComputedNull.cs:8](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:8), [EF6 ComputedNull.cs:8](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.ComputedNull.cs:8).

Both Oracle overrides of `Literal` were removed. Empty literals consequently carry no NULL state, while the computed-NULL whitelist does not cover concat, len, lower or integer indexof results. Current EF Core SQL and freshly built EF6 predicates reproduce:

| Filter | EF Core SQL predicate | EF6 predicate | Required Oracle behavior |
| --- | --- | --- | --- |
| `isnull(concat("",""))` | `0 = 1` | `False` | TRUE |
| `isnull(indexof(name,""))` | `Name IS NULL` | `Name == null` | TRUE for non-null names too |
| `isnull(len(""))` | `0 = 1` | `False` | TRUE |
| `isnull(lower(""))` | `0 = 1` | `False` | TRUE |

Oracle treats a zero-length scalar string as NULL; its scalar functions propagate that NULL. The existing ADO renderer retains `(expression IS NULL)`, so these filters must not reject the non-null rows. See [Oracle NULL semantics](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/Nulls.html). EF6's actual Oracle indexof SQL additionally emits `NVL(INSTR(...),0) - 1`, turning an empty search's NULL result into -1; removing the string marker alone does not correct that provider translation.

Restore scalar empty-string NULL semantics while handling literal LIKE patterns at their own boundary. Keep the corrected empty-search LIKE behavior, and test scalar empties, empty concat and integer indexof NULLs in both profiles. SQL/expression generation was reproduced locally; no Oracle server query was executed.

### N13 — P2: Oracle temporal guards reject the renderer's own date/time conversions

**Locations:** [ExpressoFunctionTranslations.cs:207](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.cs:207), [ExpressoFunctionTranslations.Oracle.cs:14](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.Oracle.cs:14), [ExpressoFunctionTranslations.Oracle.cs:103](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.Oracle.cs:103).

`Date(DateTime)` and `Time(DateTime)` still return formatted text, which the provider maps to `NVARCHAR2`. The new DateOnly/TimeOnly consumers then reject that text as unsupported property storage. These accepted filters fail during SQL generation:

```text
eq(year(date(created)),2026)
eq(hour(time(created)),9)
eq(hour(addseconds(time(created),10)),9)
```

The first two were also reproduced with the **actual sample author model**, using its native `CreatedAt` column. The exception instructs the caller to change a property mapping, but the rejected text is produced internally by Expresso; changing `CreatedAt` storage cannot change those marker translations. For example, the ADO `year(date(created))` path is `EXTRACT(YEAR FROM TRUNC(created))`, which does not require text storage.

Make conversion results composable with the new temporal markers, retaining native date/interval SQL or explicitly converting the renderer's known text representation. Restrict the storage rejection to unsupported model mappings. Add tests combining conversions, getters and additions, including the sample context.

### N14 — P2: MySQL zero-divisor NULLs are still folded to false in both EF profiles

**Locations:** [EF Core ComputedNull.cs:19](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:19), [EF6 ComputedNull.cs:19](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.ComputedNull.cs:19).

The new `div`/`mod` checks apply only to SQLite. With a non-nullable double field, both `isnull(div(number,0))` and `isnull(mod(number,0))` generate `WHERE FALSE` through Pomelo; both EF6 MySQL predicates build as `e => False`.

MySQL SELECT division by zero and modulo by zero return NULL, so the corresponding ADO `(... IS NULL)` predicates are TRUE for non-null inputs. See the official [division](https://dev.mysql.com/doc/refman/8.0/en/arithmetic-functions.html) and [modulo](https://dev.mysql.com/doc/refman/8.0/en/mathematical-functions.html#function_mod) documentation. This is the same result-loss defect as N9 on another supported provider, including the existing `isnull-div-zero` differential case.

Apply the appropriate computed-NULL rule to MySQL in both profiles, preserving nullable-input propagation. Test division and modulo inside `isnull`, comparisons and sort keys. Generated SQL and predicates were verified locally; MySQL server execution was not performed.

### N15 — P2: Oracle DateOnly month/year arithmetic differs from the ADO renderer

**Locations:** [ExpressoFunctionTranslations.Oracle.cs:63](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.Oracle.cs:63), [ExpressionToOracleQueryClauseTransformer.cs:176](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Oracle/ExpressionToOracleQueryClauseTransformer.cs:176), [OracleTemporalTests.cs:54](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.EntityFrameworkCore.Test/OracleTemporalTests.cs:54).

The new DateOnly translations use `ADD_MONTHS(date, amount)` and `ADD_MONTHS(date, amount * 12)`. The Oracle ADO renderer instead adds `NUMTOYMINTERVAL(amount,'MONTH'/'YEAR')`. These have different month-end and invalid-date behavior:

- For 2021-02-28 plus one month, `ADD_MONTHS` yields 2021-03-31; interval addition preserves March 28. Thus `eq(day(addmonths(born,1)),31)` can select a row only through EF Core.
- For 2020-02-29 plus one year, `ADD_MONTHS` clamps to February 28; interval addition rejects the invalid February 29 result.

The sample model's generated SQL confirms `ADD_MONTHS` in both cases. The behavioral difference follows from Oracle's documented [ADD_MONTHS rule](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/ADD_MONTHS.html) and [interval arithmetic validation](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/Data-Types.html); the boundary queries were not executed on an Oracle server.

Use the same interval arithmetic as the dialect renderer, preserving type mappings and parameterization. Add last-day, leap-day, negative-amount and rejection cases. The new test asserting `ADD_MONTHS` currently pins SQL that does not meet same-engine parity.

### N16 — P2: Oracle TimeOnly component extraction loses negative interval signs

**Location:** [ExpressoFunctionTranslations.Oracle.cs:18](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.Oracle.cs:18).

`TimeNumber` extracts unsigned `([0-9]{2})` groups from interval text. Additions can legitimately produce negative intervals even when a stored TimeOnly is non-negative. For a stored 09:00 opening time:

```text
eq(hour(addhours(opens,-10)),-1)
```

The ADO path uses the signed interval HOUR component; the new EF Core SQL extracts `01` from the negative interval and compares +1 to -1. Minute and second components lose their signs in the same way.

A local ODP.NET interval probe produced `-000000000 01:30:05.000000000` / `-01:30:05`; the exact regex patterns returned `01`, `30` and `05`. Generated SQL from the actual publisher model contains the same unsigned extraction after `NUMTODSINTERVAL`. This establishes the discarded sign; the SQL result difference is inferred from that translation and the ADO `EXTRACT` path, not a live Oracle comparison.

Preserve signed components, preferably through native interval extraction that matches the ADO expression. Add negative-result hour/minute/second tests, including subtraction across midnight; testing only positive additions misses this defect.

### N17 — P2: Oracle concat gaps are classified as renderer rejection instead of database rejection

**Locations:** [Ef6ProviderGaps.cs:15](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.Integration.Test/Ef6/Ef6ProviderGaps.cs:15), [Ef6ProviderGaps.cs:46](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.Integration.Test/Ef6/Ef6ProviderGaps.cs:46), [Ef6ItTests.cs:58](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.Integration.Test/Ef6/Ef6ItTests.cs:58).

The Oracle `concat`/`concat-null-eq` records describe ORA-12704 but omit `Kind`, whose default is `Unsupported`. `AssertedGap` therefore requires a `NotSupportedException` and the full recorded reason. The transformer does not reject concat; actual Oracle provider SQL generation succeeds and emits the documented mixed `N''`/VARCHAR2 guards. The documented execution failure is a database exception, which cannot satisfy the new gap assertion.

Consequently an opt-in Oracle EF6 integration run will fail when it encounters this documented database rejection. This is a static harness inconsistency supported by actual provider SQL generation; an Oracle server run was not performed.

Set the correct database gap kind and assert the specific expected Oracle code (12704), rather than a non-empty code or generic `ORA-` substring. Add a helper test proving that the expected database rejection passes and an unrelated database error fails.

### Validation for this recheck

All runs below freshly built current projects in Release with `--no-restore`; renderer runs used `Category!=Integration`. The integration-project run selected only `DifferentialOutcomeTests` and excluded `Category=Integration`.

| Project | Framework | Passed | Failed |
| --- | --- | ---: | ---: |
| LINQ tests | net6.0 | 323 | 0 |
| LINQ tests | net48 | 323 | 0 |
| EF Core tests | net8.0 | 278 | 0 |
| EF6 tests | net48 | 223 | 0 |
| DifferentialOutcome unit tests | net8.0 | 6 | 0 |
| **Total test executions** | | **1,153** | **0** |

The first differential-helper build encountered sandbox denial while MSBuild read the Windows SDK location. The same filtered unit command completed successfully with approved SDK access. No database integration tests were enabled.

Additional probes compiled current Core/Parsing/LINQ/EF Core source, generated SQL through installed providers, inspected the actual EF Core sample model, evaluated materialized order restoration and Unicode bounds, and exercised the actual differential helper. EF6 probes used the freshly built libraries with the sample's Npgsql and Oracle provider versions and fixed manifest tokens. SQLite checks used scalar SELECTs on `:memory:` only.

No external database connection, schema/seed script, DDL/DML, sample host or endpoint smoke test was used. Full solution build, live engine integration and coverage measurement were not repeated. The absent modern-temporal integration matrix remains a coverage limitation, but the closure decision here rests on the concrete issues above.

Current probe evidence: [SecondRecheckProbe-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/SecondRecheckProbe-output.txt), [Ef6SecondRecheck-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/Ef6SecondRecheck-output.txt). Older probes contain superseded failures; use these files and this section for the current assessment.

## Third remediation recheck — 2026-10-05

This recheck verifies the reported fixes for N9 and N11–N17 in the current working tree, including regressions in the affected paths. HEAD remains `ff672f6`; the remediation is uncommitted. Only this review document was edited.

**The review remains open with six P2 findings.** N11–N13 are partially corrected. N18–N20 cover additional failures exposed by the affected translation and assertion paths. The findings below are supported by current source, generated SQL, actual sample-model inspection, and local assertion reproductions; no external database execution is claimed.

### Disposition of the reported fixes

| Finding | Current result |
| --- | --- |
| N9 | Original field-based reproductions fixed. EF Core SQL retains SQRT/division through NULLIF, and EF6 SQL retains SQRT, division and modulo inside IS NULL. EF Core's literal-only evaluation still bypasses database arithmetic; see N18. DB2's registrations were inspected, without native SQL generation or execution. |
| N11 | The SQLite cannot-open reproductions now fail, and cases requiring ids reject equal database errors. However, expected native codes are matched as substrings rather than complete codes; N11 remains open. The stricter reason comparison also exposes missing EF6 gap cases, recorded separately as N20. |
| N12 | The original all-empty concat, len, lower and empty indexof null-state reproductions are corrected. Empty LIKE searches still generate predicates. Mixed empty/non-empty concat is now incorrectly marked NULL in both profiles; N12 remains open. Other literal-only string operations also fail before SQL translation; see N18. |
| N13 | Native date/time results now compose with year/hour/addseconds, including the actual sample author model. Text-mapped properties are still rejected. Direct comparisons with date/time literals fail when binding parameters; N13 remains open. The new clock conversion also drops fractions; see N19. |
| N14 | Fixed: both profiles include MySQL in the zero-divisor computed-NULL rule, and current EF Core SQL preserves that condition. Live MySQL parity was not rerun. |
| N15 | Fixed: DateOnly month/year additions use NUMTOYMINTERVAL rather than ADD_MONTHS. Current sample-model SQL for the month-end/leap-day-shaped queries confirms the common interval path. Boundary execution was not rerun. |
| N16 | Fixed: hour, minute and second share the sign multiplier. Generated sample-model SQL for negative-result minute and second additions includes the sign test as well as the component regex. Negative-result database values were not reexecuted. |
| N17 | Fixed for the reported gap: Oracle concat records have Database kind, and the helper requires the recorded ORA-12704 code. Its test accepts 12704 and rejects 942. Live Oracle concat rejection was not rerun. |

Earlier closed findings were not reopened merely because live engine runs were unavailable. The closure decision rests on the concrete failures below.

### N12 — P2, still open: a single empty Oracle concat argument makes the entire result NULL

**Locations:** [OracleConcat:49](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:49), [EF Core concat:80](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.cs:80), [EF6 concat:212](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.cs:212).

The new helper returns `LinqEx.True` as the result's NULL state when **any** argument is an empty literal. Oracle concatenation ignores an empty/NULL operand when another operand is non-null; NULL results only when every operand is NULL/empty. See [Oracle concatenation semantics](https://docs.oracle.com/en/database/oracle/oracle-database/18/sqlrf/Concatenation-Operator.html).

Current reproductions:

| Filter | EF Core result | EF6 predicate | Required result |
| --- | --- | --- | --- |
| `isnull(concat(name,""))` | No WHERE clause; selects every row | `e => True` | TRUE only when name is NULL/empty |
| `eq(concat(name,""),"Alice")` | `WHERE 0 = 1` | `e => False` | Match non-null name Alice, subject to the documented EF6 Oracle concat execution gap |
| `isnull(concat("a",""))` | No WHERE clause | `e => True` | FALSE |
| `eq(concat("a",""),"a")` | `WHERE 0 = 1` | Folded false by the same helper | TRUE |

This is a regression in the remediation, independent of EF6's documented ORA-12704 gap: `isnull` alone is already wrong and emits no concat call. Treat an empty literal as a NULL **operand** when combining all operand null states; preserve the non-empty arguments. Cover mixed operands, all-empty operands, nullable fields and nested scalar consumers in both profiles.

### N13 — P2, still open: Oracle date/time conversions cannot bind comparison literals

**Locations:** [Oracle temporal mappings:110](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.cs:110), [Oracle conversion entries:199](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.cs:199), [OracleTimeOfDay:115](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.Oracle.cs:115).

The new native translations return SQL expressions whose CLR types are `DateOnly`/`TimeOnly`, but their mappings are plain `DateTimeTypeMapping`/`TimeSpanTypeMapping`, without the converters used for native properties in the sample model. When a comparison literal inherits these mappings, the provider receives a DateOnly/TimeOnly object that ODP.NET cannot bind.

Both accepted filters fail during `ToQueryString()`:

```text
eq(date(created),"2026-10-05")
eq(time(created),"09:00:00")
```

The failure is `ArgumentException: Value does not fall within the expected range`, at `OracleParameter.set_Value`, called from `RelationalTypeMapping.CreateParameter`. Both failures also reproduce using the **actual sample author context** and its CreatedAt property. In that same context, `year(date(created))`, `hour(time(created))`, and `eq(date(created),born)` generate SQL successfully. Thus the getter-composition test does not exercise the failing parameter path.

Use mappings that support the advertised CLR result types and convert parameters to the native provider types. Verify direct equality/range predicates and predicates on shifted conversion results, alongside nested getters and native-property comparisons. Merely changing the SQL store-type string will not supply the missing conversion.

### N18 — P2: EF Core evaluates literal-only functions with CLR semantics before SQL translation

**Locations:** [Divide/Modulo:24](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:24), [Sqrt:52](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:52), [Substring/Left:25](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:25), [DomainNull:44](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:44).

Captured literals allow EF Core's parameter extraction to evaluate ordinary BCL calls and binary arithmetic. The new outer `IsDomainNull` marker preserves field-based arithmetic in SQL, but does not prevent this evaluation of its argument. Ordinary comparisons can also be fully folded before SQL generation.

Current parsed-filter reproductions:

| Filter | Provider | Current outcome | Required database behavior |
| --- | --- | --- | --- |
| `eq(div(1.0,0.0),0.0)` | SQL Server, PostgreSQL | `WHERE 0 = 1` / `WHERE FALSE`, with no division | Preserve the engine's zero-division error rather than return a successful empty result |
| `eq(left("a",100),"a")` | SQL Server, Oracle | Query-parameter evaluation throws ArgumentOutOfRangeException from string.Substring | SQL substring/left clamps to the available text |
| `isnull(left("",1))` | Oracle | Same CLR substring exception | NULL source produces a NULL scalar, so IS NULL is TRUE |
| `isnull(substring("",1,1))` | Oracle | Same CLR substring exception | TRUE |

Directly constructed valid IR supplies an additional domain reproduction: `IsNull(Sqrt(Literal(-1.0)))` generates a **NaN parameter** and no SQRT call on PostgreSQL, SQL Server and Oracle; literal double division similarly becomes Infinity. This IR probe bypasses the parser's pre-existing inability to infer a numeric type for a literal-only sqrt argument. PostgreSQL accepts floating NaN/Infinity values ([numeric type documentation](https://www.postgresql.org/docs/16/datatype-numeric.html)), whereas its native SQRT of a negative value raises a domain error ([implementation](https://github.com/postgres/postgres/blob/REL_16_STABLE/src/backend/utils/adt/float.c#L1381)). Generated SQL demonstrates the bypass; server outcomes were not executed here.

Keep affected operations on provider translation paths even when every operand is a captured literal, or deliberately evaluate them with the same engine semantics. Test final SQL/command generation for literal-only functions and nested constant subexpressions, not just predicates that contain a mapped field. PostgreSQL's existing translated LEFT marker already avoids the reproduced substring exception there.

### N19 — P2: Oracle time(timestamp) now discards fractional seconds

**Locations:** [OracleTimeOfDay:115](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.Oracle.cs:115), [OracleInterval mapping:111](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.cs:111), [ADO time conversion:161](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Oracle/ExpressionToOracleQueryClauseTransformer.cs:161).

The new conversion reconstructs the interval from HH24, MI and SS as integers. Oracle's SS element returns whole seconds; fractional seconds require FF. See [Oracle datetime format elements](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/Format-Models.html). The ADO renderer instead subtracts TRUNC(timestamp), preserving timestamp fractions. The implementation's SECOND(0) result mapping also describes whole-second precision.

Using the actual sample Book/Publisher model, `eq(time(created),opens)` generates:

```sql
NUMTODSINTERVAL(
  TO_NUMBER(TO_CHAR(created_at,'HH24')) * 3600
  + TO_NUMBER(TO_CHAR(created_at,'MI')) * 60
  + TO_NUMBER(TO_CHAR(created_at,'SS')),
  'SECOND') = opens_at
```

For CreatedAt `09:00:00.500` and OpensAt `09:00:00`, this expression discards .500 and compares equal, while the ADO interval comparison does not. This result difference is inferred from the actual generated SQL and documented format semantics, without an Oracle server run. Comparing two mapped fields avoids N13's parameter-binding failure and shows that this is a separate defect.

Preserve supported timestamp fractional precision when producing the interval, including through later additions and direct comparisons. Add a fractional-timestamp case against both a literal and a native clock field. The documented EF6 precision gaps do not authorize this EF Core divergence.

### N11 — P2, still open: expected native error codes are matched by substring

**Locations:** [DifferentialOutcome.AssertSame:50](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.Integration.Test/DifferentialOutcome.cs:50), [domain case codes:98](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.TestCases/RendererDifferentialCases.cs:98).

The helper now requires a case's code/reason, but uses `reference.Contains(code)` against the complete classified outcome. A different native code containing an allowed code still passes. Using the **actual isnull-sqrt-neg case metadata**, this call is accepted:

```text
reference: error: database:Number:14280
candidate: error: unsupported: negative square root
```

The case allows 1428; 14280 is an unrelated Oracle subpartition error ([Oracle error documentation](https://docs.oracle.com/en/error-help/db/ora-14280/)). The same aliasing occurs with two identical unexpected errors because the candidate-equality branch follows the substring check. This was a local helper reproduction using classified outcome strings; no statement producing a database error was run.

Compare complete native code values, with an explicit code kind/provider where needed, rather than substring fragments. Add tests that reject prefix/suffix aliases while accepting the exact intended code. The new code-14 negative tests are useful but do not cover this hole.

### N20 — P2: domain cases omit the documented EF6 PostgreSQL/Oracle SQRT gaps

**Locations:** [PostgreSQL gaps:31](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.Integration.Test/Ef6/Ef6ProviderGaps.cs:31), [Oracle gaps:43](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.Integration.Test/Ef6/Ef6ProviderGaps.cs:43), [EF6 differential assertion:29](C:/Users/itorg/source/repos/Expresso/test/Rendering/Expresso.Rendering.Integration.Test/Ef6/Ef6ItTests.cs:29), [SQRT override:175](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.cs:175).

Both provider gap dictionaries contain `sqrt`, but omit the differential id `isnull-sqrt-neg`. That differential filter necessarily visits SQRT. The current renderer correctly throws its documented provider limitation before executing a query:

```text
PostgreSQL: the provider exposes no store functions
Oracle:     the provider manifest has no SQRT
```

The new domain comparator accepts an unsupported candidate only when it contains the case's **in-memory** reason, `negative square root`. Neither provider message contains that reason. `AssertedGap` does not intercept the missing case id, so the opt-in EF6 differential tests fail despite the documented, intentional provider rejection.

A local probe used the freshly built EF6 transformer and integration assembly: both dictionaries reported no gap for this id, and passing each actual classified renderer exception against its expected ADO domain code made `AssertSame` fail. Reference ADO outcomes were supplied to the helper; no PostgreSQL/Oracle connection was opened.

Include this differential id in the appropriate provider gaps, or pass explicit provider-specific expected rejection reasons without weakening the in-memory/domain checks. Add a unit check that exercises the domain case with the actual EF6 renderer and gap metadata.

### Validation for the third recheck

These runs freshly built the current projects in Release with `--no-restore`; all excluded `Category=Integration`. The integration-project run selected only DifferentialOutcomeTests and Ef6GapAssertionTests.

| Project | Framework | Passed | Failed |
| --- | --- | ---: | ---: |
| LINQ tests | net6.0 | 323 | 0 |
| LINQ tests | net48 | 323 | 0 |
| EF Core tests | net8.0 | 293 | 0 |
| EF6 tests | net48 | 226 | 0 |
| DifferentialOutcome and EF6 gap assertion unit tests | net48 | 11 | 0 |
| **Total test executions** | | **1,176** | **0** |

Additional scratch probes compiled current source for Core/Parsing/LINQ/EF Core and used the installed providers to generate SQL. They included the actual EF Core sample models, mixed concat operands, direct temporal comparisons, constant expressions, domain wrappers, and negative minute/second additions. EF6 probes used freshly built feature/test assemblies with the sample's Npgsql and Oracle providers and fixed manifest tokens. Temporary harness assembly-binding issues were corrected before drawing conclusions from their results.

Live integration, external database connections, sample endpoint smoke tests, full solution builds, and coverage measurement were not repeated. No schema/seed script or real-server DDL/DML was run. These passing unit suites therefore do not resolve the six reproduced or source-supported findings above.

Current evidence: [ThirdRecheckProbe-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/ThirdRecheckProbe-output.txt), [Ef6ThirdRecheck-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/Ef6ThirdRecheck-output.txt). Those scratch outputs also retain probes from earlier reviews; use the THIRD RECHECK labels and this section for the latest conclusions.

## Fourth remediation recheck — 2026-10-05

This recheck verifies the six reported fixes and checks the changed translation paths for regressions. HEAD remains `ff672f6`, with the remediation in the working tree. Only this review document was edited.

**N11–N13 and N18–N20 are resolved for their reported reproductions. One new P2 issue, N21, prevents closing the review.** The remaining issue is a concrete SQL-generation regression, independent of the absence of live engine runs.

### Disposition of the six fixes

| Finding | Current result |
| --- | --- |
| N11 | Fixed. Expected errors compare the entire native code value. The new tests reject both 14280 versus expected 1428 and two identical unexpected errors, while accepting exact expected codes. The SQLite infrastructure-error checks remain covered. |
| N12 | Fixed. Oracle concat combines every operand's null state, treating an empty literal as a NULL operand. Current EF Core SQL for `isnull(concat(name,""))` is `Name IS NULL`; the equality filter retains concatenation. All-empty concat remains NULL, and both profiles' mixed-operand regression tests pass. |
| N13 | Fixed. Oracle conversion-result mappings now have DateOnly-to-DateTime and TimeOnly-to-TimeSpan converters. Equality, date range, shifted date, fractional time literal, and comparison with born generate commands successfully, including through the actual sample author model. |
| N18 | Fixed for the reported arithmetic-domain and substring/left clamping failures. Literal SQRT/division/modulo stay in SQL on the checked domain-error providers. Parsed literal-only left/substring calls generate native SQL on SQL Server, PostgreSQL, Oracle, MySQL and SQLite. SQLite/MySQL can still fold known zero-divisor predicates to their correct NULL outcome. The expanded Oracle empty-string shortcut introduces N21 below. |
| N19 | Fixed. Oracle `time(created)` now generates `created - TRUNC(created)`, matching ADO. The actual sample Book/Publisher comparison retains this subtraction, and the .500 time parameter binds successfully. Nested hour/addseconds continue to use the interval path. Fractional-second database execution was not repeated. |
| N20 | Fixed. Both EF6 gap dictionaries include isnull-sqrt-neg. The new net48 unit test builds the actual differential filter through both EF6 providers and validates each documented rejection against the corresponding gap. |

The native-expression, parameter-binding and assertion reproductions now behave as expected. No earlier finding was reopened solely because database integration was not rerun.

### N21 — P2: EF Core Oracle replace is incorrectly NULL when its search or replacement is empty

**Locations:** [ComputedNull shortcut:28](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:28), [Oracle function list:8](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:8), [Replace visitor:139](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:139).

The N18 remediation expands the early empty-literal rule to every function in OracleEmptyStringFunctions. That list includes replace, and the rule returns a constant TRUE NULL state when **any** argument is an empty literal. Oracle REPLACE gives the arguments different roles: a NULL replacement removes occurrences; a NULL search returns the source unchanged. Neither makes every result NULL. See [Oracle REPLACE semantics](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/REPLACE.html).

Current parsed-filter SQL reproductions with a nullable string field:

| Filter | Current EF Core Oracle SQL | Required behavior for name Bob |
| --- | --- | --- |
| `isnull(replace(name,"a",""))` | No WHERE clause; every row is selected | FALSE; there is no a to remove, so the result is Bob |
| `eq(replace(name,"a",""),"Bob")` | `WHERE 0 = 1` | TRUE |
| `isnull(replace(name,"","x"))` | No WHERE clause | FALSE; the empty search is NULL and leaves Bob unchanged |
| `eq(replace(name,"","x"),"Bob")` | `WHERE 0 = 1` | TRUE |

The generated queries contain no REPLACE call because the incorrect null state eliminates the scalar expression. This also breaks common empty-replacement requests such as removing hyphens from ISBNs. It is an EF Core Oracle regression; EF6's early shortcut was not expanded to replace in this remediation.

Apply Oracle's argument-specific REPLACE rules rather than treating all empty operands as strict NULL inputs. Preserve an empty-source NULL, replacement deletion, search no-op, and a result that can become empty/NULL after deletion. Check nullable search/replacement fields as well as literals, and verify final provider SQL and null predicates instead of only the expression tree. The current new tests cover empty left/substring sources but do not cover these REPLACE cases.

SQL generation was reproduced locally using the current source and installed Oracle EF provider. The expected result follows from the documented native function semantics and the existing ADO REPLACE call; no Oracle server query was executed.

### Validation for the fourth recheck

All runs freshly built the current projects in Release with `--no-restore` and excluded `Category=Integration`. The integration-project run selected only DifferentialOutcomeTests and Ef6GapAssertionTests.

| Project | Framework | Passed | Failed |
| --- | --- | ---: | ---: |
| LINQ tests | net6.0 | 323 | 0 |
| LINQ tests | net48 | 323 | 0 |
| EF Core tests | net8.0 | 338 | 0 |
| EF6 tests | net48 | 227 | 0 |
| DifferentialOutcome and EF6 gap assertion unit tests | net48 | 15 | 0 |
| **Total test executions** | | **1,226** | **0** |

The first EF Core build hit the sandbox's Windows SDK read restriction; the same filtered test command completed with approved SDK access. No tests requiring database fixtures were enabled.

Scratch probes compiled the current Core/Parsing/LINQ/EF Core source, including the new literal marker partials, and generated SQL with the installed providers. They checked all former EF Core reproductions, the actual sample author and Book/Publisher models, literal arithmetic and string functions, and the new REPLACE regression. The new net48 helper test independently exercises the actual EF6 transformer and gap metadata.

Live integration, external database connections, sample endpoint smoke tests, full solution builds and coverage measurement were not repeated. No schema/seed script or real-server DDL/DML was run. N21 is the only open finding identified in this recheck; the passing unit suites do not cover it.

Current probe evidence: [FourthRecheck-current-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/FourthRecheck-current-output.txt). This extract contains current SQL-generation evidence; some reused probe labels retain THIRD RECHECK, but all output was regenerated against the current source in this recheck.

## Fifth remediation recheck — 2026-10-05

This recheck verifies the N21 fix and follows the affected paths through provider SQL generation, literal evaluation, nullable arguments, nested scalar consumers, and the other string functions. HEAD remains `ff672f6`, with the remediation in the working tree. Only this review document was edited in the repository.

**N21's reported regression is fixed. Three newly identified P2 correctness issues, N22–N24, remain. The review cannot be closed yet.** These findings are concrete reproductions, rather than requests to repeat integration testing.

| Finding | Disposition |
| --- | --- |
| N21 | Resolved for all four reported field-based filters. Oracle EF Core retains REPLACE for empty search/replacement arguments, checks the computed result through NULLIF, and inherits argument nullability from the source alone. |
| N22 | Open: EF Core still evaluates several literal-only string functions in the CLR, causing exceptions or incorrect constant predicates. |
| N23 | Open: EF6 Oracle still treats NULL search/replacement fields as strict NULL inputs to REPLACE. |
| N24 | Open: both EF profiles render SQLite right with zero/negative lengths differently from the ADO renderer. |

### Verification of N21 and the earlier fixes

The four original filters now contain REPLACE in the final Oracle SQL. In particular, `eq(replace(name,"a",""),"Bob")` retains a parameterized REPLACE equality instead of becoming `WHERE 0 = 1`. The generated result-null check also retains the call:

```sql
CASE WHEN NULLIF(REPLACE("r"."Name", :search, :replacement), NULL) IS NULL
     THEN 1 ELSE 0 END
```

The source-only `NullFromArguments` override correctly avoids null guards on optional search/replacement fields in **EF Core**. Current SQL generation also handles full deletion, an empty source, nested len/trim/lower consumers, and nullable search/replacement fields. The new Oracle test exercises the original reproductions and nullable fields; it passes.

The previous arithmetic-domain wrappers, literal division/modulo/substring/left translations, mixed Oracle concat operands, native temporal comparisons and sample-model composition still generate SQL. The full unit runs also retain coverage for exact domain-error matching, infrastructure-error rejection and the EF6 SQRT gap metadata. I found no new regression in those previously repaired paths. N22 concerns additional literal string functions; it does not invalidate the arithmetic/left/substring reproductions that were fixed.

### N22 — P2: EF Core literal-only string calls still execute with CLR semantics

**Locations:** [literal overrides:8](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.Literals.cs:8), [REPLACE hook:95](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:95), [length/indexof hooks:99](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:99), [trim/lower hooks:80](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:80), [right fallback:79](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:79).

The literal-marker remediation covers SQRT, division, modulo, substring and left. REPLACE, len, trim, lower, and providers without a right marker still fall back to BCL expressions. With no row parameter in their operands, EF Core evaluates these subtrees while extracting query parameters. The native function is then absent from SQL, or command generation fails before reaching the provider.

The remaining REPLACE failure is a direct counterpart of N21:

```text
eq(replace("Bob","","x"),"Bob")
isnull(replace("Bob","","x"))
```

Both fail during Oracle `ToQueryString()`:

```text
InvalidOperationException: An exception was thrown while attempting to evaluate a LINQ query parameter expression.
Inner ArgumentException: The value cannot be an empty string. (Parameter 'oldValue')
```

Oracle treats the empty search as NULL and returns Bob unchanged, so the predicates should respectively be TRUE and FALSE. The ADO renderer emits native REPLACE and EF6 still generates it for these literal operands. See [Oracle REPLACE semantics](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/REPLACE.html). The same EF Core CLR exception was reproduced with the SQL Server, PostgreSQL, MySQL and SQLite profiles; an isolated SQLite scalar SELECT confirmed that native REPLACE returns Bob.

This problem also silently changes accepted filters on SQLite:

| Parsed filter | Current EF Core SQL result | Native SQLite result |
| --- | --- | --- |
| `eq(len("😀"),1)` | Boolean parameter 0; no LENGTH call | TRUE |
| `eq(indexof("😀a","a"),1)` | Boolean parameter 0; no INSTR call | TRUE |
| `eq(trim("<TAB>"),"<TAB>")` | Boolean parameter 0; no TRIM call | TRUE |
| `eq(lower("Ä"),"Ä")` | Boolean parameter 0; no LOWER call | TRUE |
| `eq(right("😀a",2),"😀a")` | Boolean parameter 0; no SUBSTR call | TRUE |

`<TAB>` denotes an actual U+0009 character inside each quoted filter argument. These filters were parsed, not merely constructed as IR. Native values were verified using scalar SELECTs against a disposable SQLite `:memory:` connection: length is 1, the zero-based index is 1, trim preserves the tab, lower preserves Ä, and right returns the two Unicode characters. SQLite counts Unicode code points, trims spaces by default and performs ASCII-only lowercasing; see its [scalar function definitions](https://www.sqlite.org/lang_corefunc.html). CLR Length/IndexOf count UTF-16 units, Trim removes the tab, ToLower changes Ä, and the right fallback slices UTF-16 units.

Literal negative right calls also fail with `ArgumentOutOfRangeException` on the Oracle, MySQL and SQLite EF Core profiles, while their dialect renderers emit valid native scalar SQL. SQL Server/PostgreSQL right markers already keep these operands in SQL.

**Suggested direction:** extend the existing provider translation pattern to preserve engine semantics for the uncovered literal string calls. Avoid replacing them with one shared CLR helper, since character counting, case conversion, trimming and right differ by engine. Check the final provider SQL and compare literal-only calls with equivalent field-based/native calls. Regression coverage should include the empty-search REPLACE case, supplementary Unicode characters, a tab, non-ASCII case conversion and zero/negative right lengths.

### N23 — P2: EF6 Oracle REPLACE still propagates NULL from optional arguments

**Locations:** [default argument-null propagation:142](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.cs:142), [EF6 Oracle computed-null handling:33](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.ComputedNull.cs:33), [EF Core source-only override:49](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:49).

The new argument-null hook is overridden only in EF Core. EF6 Oracle continues to inherit `AnyNull(arguments)`. Although its REPLACE result is also checked for NULL, that computed check is ORed with the optional argument's null state. A NULL search/replacement therefore determines the predicate before the native result can matter.

Fresh EF6 assemblies and the installed Oracle provider generated these results for a nullable `name` field:

| Filter | Current EF6 result when name is NULL | Required Oracle result |
| --- | --- | --- |
| `isnull(replace("Bob",name,"x"))` | TRUE | FALSE: NULL search leaves Bob unchanged |
| `eq(replace("Bob",name,"x"),"Bob")` | FALSE | TRUE |
| `eq(replace("Boba","a",name),"Bob")` | FALSE | TRUE: NULL replacement removes a |

For example, the first filter generates:

```sql
WHERE "Extent1"."Name" IS NULL
   OR REPLACE(:source, "Extent1"."Name", :replacement) IS NULL
```

The two equality filters negate the same OR guard, so they reject every row with NULL name. This wrong guard is visible in both the actual provider SQL and the generated predicate; a diagnostic compiled evaluation confirmed its short-circuit result for NULL. No Oracle query was executed. The required behavior follows from [Oracle REPLACE semantics](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/REPLACE.html).

**Suggested direction:** apply Oracle's source-only argument propagation in EF6 as well, while retaining the computed-result NULL check for full deletion. Add EF6 tests for nullable search and replacement fields, including negation and nested consumers. This behavior is not a documented EF6 provider gap.

### N24 — P2: SQLite right with zero/negative lengths differs from ADO in both EF profiles

**Locations:** [shared right fallback:34](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:34), [EF Core right override:79](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:79), [EF6 right override:118](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.cs:118), [SQLite ADO right:31](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Sqlite/ExpressionToSqliteQueryClauseTransformer.cs:31).

SQLite has no EF Core right marker, and the EF6 override falls back to the shared expression. Both providers consequently generate the same three-argument substring construction:

```sql
CASE WHEN length(name) <= :n THEN name
     ELSE substr(name, length(name) - :n + 1, :n) END
```

The existing SQLite dialect renderer instead emits `substr(name,-(:n))`. These expressions differ even with a field operand, so this is separate from N22's literal evaluation problem.

Using name abc, isolated SQLite SELECTs produced:

| Length | Existing ADO expression | EF Core / EF6 expression |
| ---: | --- | --- |
| 0 | abc | empty string |
| -1 | abc | empty string |

Therefore both `eq(right(name,0),name)` and `eq(right(name,-1),name)` include abc through ADO and exclude it through either EF profile. Actual SQL generation was checked for both profiles; no table creation or fixture setup was needed. Positive and oversized lengths were included as controls. See the documented [SQLite substring semantics](https://www.sqlite.org/lang_corefunc.html#substr).

The existing `right-neg-two` differential case compares against ce. That expected string does not expose this SQLite difference on the current seed: SQLite's ADO expression and the EF expression can both fail the equality while returning different strings. Passing that differential case therefore cannot establish right parity.

**Suggested direction:** match the existing SQLite dialect expression for both profiles. If EF6 cannot express it with supported canonical/store functions, reject that path explicitly and document the gap. Add cases whose predicates distinguish the returned values, including zero, negative, positive, oversized and nullable inputs.

### Minor documentation leftovers

The code fixes have outpaced some function descriptions:

- [right.md:84](C:/Users/itorg/source/repos/Expresso/docs/functions/string-transform/right.md:84) still states that EF Core has no provider overrides and shows SQL Server's old LEN/CASE implementation. Current SQL Server/PostgreSQL overrides emit native RIGHT.
- [semantics.md:111](C:/Users/itorg/source/repos/Expresso/docs/semantics.md:111) still says Oracle `isnull(substring(name,1,0))` is FALSE through EF. Current EF Core SQL checks the SUBSTR result through NULLIF and preserves its NULL. The claimed limitation is obsolete.

Refresh these descriptions alongside the remaining fixes. These documentation discrepancies are lower priority than N22–N24.

### Validation for the fifth recheck

Both full solution test commands completed successfully in Release, freshly building their applicable projects with `--no-restore` and excluding integration:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0 --no-restore --filter "Category!=Integration"
dotnet test Expresso.slnx -c Release -f net48 --no-restore --filter "Category!=Integration"
```

| Suite | net6.0 leg | net48 leg |
| --- | ---: | ---: |
| Core | 610 | 582 |
| Parsing | 155 | 148 |
| LINQ | 323 | 323 |
| EF Core (net8.0 in both legs) | 339 | 339 |
| EF6 (net48 in both legs) | 227 | 227 |
| Six SQL dialect suites | 150 | 150 |
| Integration-project assertion/helper unit tests | 13 | 15 |
| **Total executions** | **1,817** | **1,784** |

**3,601 passed; zero failed or skipped.** The counts include repeated EF Core/EF6 executions across the two solution legs, not 3,601 unique tests. Windows SDK access required approved execution outside the restricted test sandbox; the net48 test runner also printed extension-loading warnings but completed every selected suite successfully.

Scratch probes compiled the current Core/Parsing/LINQ/EF Core source and generated commands with the installed providers, including the actual EF Core sample models. EF6 probes used the freshly built feature assemblies, installed Oracle/PostgreSQL/SQL Server/SQLite providers, fixed manifest tokens and disabled initializers. The SQL shown in this review comes from those current outputs. SQLite scalar verification used only SELECTs on disposable in-memory connections.

Live integration against configured engines, external database connections, sample endpoint smoke tests, a separate full solution build and coverage measurement were not run. No schema/seed scripts, real-server DDL/DML or sample hosts were executed. The outstanding findings are established independently of those unperformed checks.

Current evidence: [FifthRecheck-current-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/FifthRecheck-current-output.txt), [Ef6FifthRecheck-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/Ef6FifthRecheck-output.txt). The EF Core extract contains only FIFTH RECHECK labels; the full regenerated probe output also retains earlier reproduction labels.

## Sixth remediation recheck — 2026-10-06

**Decision: keep the review open.** The latest fixes resolve N23 and N24 and the original examples in N22. Checking the neighboring functions exposed remaining instances of the same literal-evaluation problem and an uncovered numeric NULL-state problem. There are **three open P2 findings: N22, N25 and N26**. The full unit suites pass, but do not cover these reproductions.

This recheck inspected the current working tree at HEAD `ff672f6`, including the new literal translations, SQLite right overrides, EF6 Oracle argument-null override, regression tests and documentation changes. Only this report was edited; implementation, tests, configuration and plans were left unchanged.

| Finding | Current disposition |
| --- | --- |
| N21 | Remains resolved: the original Oracle field-based REPLACE filters retain the native call and computed-result NULL check. |
| N22 | Partially resolved: the reported REPLACE, len, indexof, trim, lower and literal right examples now stay in SQL. Literal upper, ltrim, rtrim and some LIKE predicates still use CLR semantics; see below. |
| N23 | Resolved: EF6 Oracle REPLACE inherits argument nullability from its source alone, while retaining the computed-result NULL check. |
| N24 | Resolved: both SQLite EF profiles generate the dialect's two-argument substr with a negated length, including zero and negative lengths. |
| N25 | New: isnull of POWER loses the function and its computed NULL state in both EF profiles. |
| N26 | New: uncovered literal numeric calls still execute in the CLR in EF Core, changing ROUND results and POWER/ABS outcomes. |

### Verification of the fixes

The original empty-search REPLACE reproduction now generates native REPLACE through EF Core on SQL Server, PostgreSQL, MySQL, SQLite and Oracle, without a CLR `ArgumentException`. Oracle retains REPLACE inside its result-null check. SQLite's supplementary-character len/indexof/right examples and the tab/non-ASCII trim/lower examples now contain their native functions instead of a folded boolean. Nested literal trim/len/indexof/left/right/lower/replace combinations also generate SQL on these five providers. Oracle's literal right translation now supplies numeric mappings for its nested LENGTH/arithmetic/GREATEST expressions.

For N23, the current EF6 Oracle output for `isnull(replace("Bob",name,"x"))` is:

```sql
WHERE REPLACE(:source, "Extent1"."Name", :replacement) IS NULL
```

There is no optional `Name IS NULL` guard. The nullable-search equality and nullable-replacement deletion examples likewise retain the native REPLACE comparison and only check its computed result for NULL. Their new EF6 regression tests pass.

For N24, current EF Core and EF6 SQLite commands both contain:

```sql
substr(name, -parameter)
```

Zero, negative, positive and oversized lengths were checked in generated commands; the new tests also cover nullable inputs. The old length/substring CASE is absent from these SQLite calls. The obsolete right-provider and Oracle substring-null documentation statements identified in the fifth recheck have been corrected.

The regenerated probes still handle the previous Oracle temporal comparisons, shifted dates, fractional-time compositions and actual sample-model cases, and retain the previously repaired division/modulo/left calls. No new regression was identified in those reported paths. These are SQL-generation checks, not live-engine execution.

### N22 — P2, partially resolved: remaining literal string functions still use CLR behavior

**Locations:** [BCL search hooks:12](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:12), [upper/trim-side hooks:83](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:83), [EF Core literal overrides:25](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.Literals.cs:25), [literal translation registrations:32](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.Literals.cs:32).

The remediation adds native markers for the original N22 functions, but leaves upper, ltrim, rtrim, startswith and endswith on BCL calls. EF Core evaluates row-independent operands before SQL translation. The following **parsed** filters still produce a boolean parameter containing 0 on SQLite, with no native function/predicate in the command:

| Filter | Current EF Core result | Native SQLite result |
| --- | --- | --- |
| `eq(upper("ä"),"ä")` | FALSE; no UPPER | TRUE |
| `eq(ltrim("<TAB>"),"<TAB>")` | FALSE; no LTRIM | TRUE |
| `eq(rtrim("<TAB>"),"<TAB>")` | FALSE; no RTRIM | TRUE |
| `startswith("Ab","a")` | FALSE; no LIKE | TRUE |
| `endswith("bA","a")` | FALSE; no LIKE | TRUE |

`<TAB>` denotes an actual U+0009 character inside each quoted argument. Scalar SELECTs on the installed SQLite build confirmed all five native results. SQLite's default upper conversion is ASCII-only, its one-argument trim-side functions remove spaces, and its default LIKE comparison ignores ASCII case. See [SQLite scalar function definitions](https://www.sqlite.org/lang_corefunc.html). The existing SQLite contains override is a useful passing control: `contains("Ab","a")` retains LIKE and therefore returns the native TRUE result.

Oracle also evaluates the tab trim-side calls too early. `isnull(ltrim("<TAB>"))` binds an empty string as `:TrimStart_0` and checks `NULLIF(:TrimStart_0,NULL)`; the rtrim counterpart binds `:TrimEnd_0` the same way. Neither command contains LTRIM/RTRIM. Oracle's native default trim set is a single space, so the tab remains non-NULL and these predicates should be FALSE. The incorrect TRUE outcome is inferred from the actual empty-string binding and Oracle semantics; no Oracle command was executed. See [Oracle LTRIM](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/LTRIM.html) and [RTRIM](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/RTRIM.html).

SQL Server's literal startswith/endswith/contains commands also bind FALSE before the database can apply its collation. On a case-insensitive SQL Server collation, the equivalent native LIKE predicates match these ASCII examples. This is a conditional collation discrepancy, not a live SQL Server reproduction; see the documented [collation dependence of SQL Server LIKE](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/like-transact-sql?view=sql-server-ver17).

**Suggested direction:** finish the literal string/predicate audit, including the mirror functions of the repaired lower/trim calls. Preserve provider semantics through the existing native translation pattern, including LIKE escaping and NULL handling. Test final SQL for these parsed examples and their nested consumers, rather than relying only on a marker assertion for the previously reported functions.

### N25 — P2: POWER's computed NULL state is discarded in both EF profiles

**Locations:** [POWER visitor:93](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:93), [computed-null dispatch:148](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.cs:148), [isnull visitor:89](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Logic.cs:89), [EF Core computed-null override:12](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:12), [EF6 computed-null override:13](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.ComputedNull.cs:13).

The SQRT/DIV/MOD remediation names those functions when calling `Propagate` and handles their computed NULL/domain behavior. POWER still invokes `Binary` without a function name. `CombineIsNull` consequently skips `ComputedNull`; for a non-nullable double field its null state is absent. `VisitIsNull` replaces the entire call with FALSE.

The accepted filter `isnull(power(number,0.5))`, where number maps to a non-nullable double property, currently produces:

```sql
-- EF Core SQLite
SELECT "r"."Id" FROM "Rows" AS "r" WHERE 0

-- EF6 SQLite
SELECT NULL AS [C1]
FROM (SELECT 1 AS X) AS [SingleRowTable1]
WHERE 1 = 0
```

For number -1, the native SQLite expression `POWER(-1.0,0.5) IS NULL` returns 1, verified with a scalar SELECT. The ADO renderer retains POWER under IS NULL and therefore includes this value; both EF predicates exclude it without evaluating POWER. SQLite mathematical functions return NULL for domain errors on the reference build used here. See [SQLite mathematical functions](https://www.sqlite.org/lang_mathfunc.html).

The same deletion was reproduced in EF Core SQL Server/PostgreSQL/MySQL/Oracle commands and EF6 SQL Server/PostgreSQL/Oracle commands. Their native calls' live outcomes were not measured. The SQLite result alone establishes a row-parity defect, independently of whether other engines return NULL or raise an error.

**Suggested direction:** give POWER a computed-result/domain policy and preserve its native expression when the result is inspected for NULL. Cover non-nullable and nullable bases, invalid and ordinary inputs, and boolean/nested consumers. Keep this separate from literal preservation: this reproduction uses a field and remains wrong even if every literal-only call is translated natively.

### N26 — P2: literal ROUND, POWER and ABS still execute in the CLR in EF Core

**Locations:** [shared numeric hooks:38](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:38), [ABS hook:11](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:11), [EF Core ROUND override:56](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.cs:56), [literal overrides:8](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.Literals.cs:8).

The literal arithmetic preservation added for SQRT/DIV/MOD does not cover POWER or ABS. ROUND has a native marker only on PostgreSQL/DB2, so the remaining providers also fall back to `Math.Round` for literal operands. EF Core's parameter extraction executes these calls before SQL generation.

| Reproduction | Current EF Core behavior | Native SQLite behavior |
| --- | --- | --- |
| Parsed `eq(round(2.5),3.0)` | Boolean parameter 0; no ROUND | TRUE: round(2.5) is 3 |
| Parsed `eq(round(2.54,20),2.54)` | CLR ArgumentOutOfRangeException for digits outside 0–15 | TRUE; precision 20 is accepted |
| Typed IR equivalent of `not(eq(power(-1.0,0.5),0.0))` | Boolean parameter 1; no POWER | UNKNOWN; excludes rows in WHERE |
| Typed IR equivalent of `eq(abs(-2147483648),0)` with an int literal | CLR OverflowException before command generation | FALSE; ABS returns 2147483648 |

All native SQLite values were verified with scalar SELECTs, including the NULL result of the negated POWER comparison. The POWER/ABS rows deliberately use valid, explicitly typed Core function nodes through the public renderer API; they do not claim that the parser infers those fully literal numeric expressions.

The parsed midpoint ROUND filter also folds to FALSE on SQL Server, MySQL and Oracle. SQL Server's required native result is TRUE because ROUND uses half-away-from-zero rounding, rather than CLR midpoint-to-even rounding; see [SQL Server ROUND](https://learn.microsoft.com/en-us/sql/t-sql/functions/round-transact-sql?view=sql-server-ver17). The precision-20 example throws during `ToQueryString()` on all four of these profiles. These SQL Server/MySQL/Oracle observations are command-generation checks; only SQLite's native values were executed.

PostgreSQL EF Core is a passing ROUND control: it retains `ROUND(parameter::numeric, digits)` for both parsed examples. EF6 SQL Server/SQLite/Oracle also retain canonical ROUND for the midpoint example, while EF6 PostgreSQL correctly raises its documented unsupported-round exception. N26 concerns EF Core's uncovered literal paths, not those controls.

**Suggested direction:** extend literal numeric preservation to the uncovered operations instead of evaluating them with common CLR rules. Include midpoint rounding, engine-valid precision values, domain inputs, integer limits and negated comparisons. POWER also needs the result-null tracking in N25; fixing only one of these paths leaves the other reproduction open.

### Minor documentation clarification

[right.md:93](C:/Users/itorg/source/repos/Expresso/docs/functions/string-transform/right.md:93) and [right.md:97](C:/Users/itorg/source/repos/Expresso/docs/functions/string-transform/right.md:97) describe the new MySQL/DB2/Oracle translations as selected by a literal length. `LiteralCall` actually requires **every operand** to lack a query parameter. For example, current Oracle `right(name,2)` still generates the shared CASE/SUBSTR form; `right("abc",2)` selects the new literal marker. Clarify the operand condition. This is lower priority than the three correctness findings.

### Validation for the sixth recheck

Both full solution test legs completed successfully in Release, freshly building their applicable projects with `--no-restore` and excluding integration:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0 --no-restore --filter "Category!=Integration"
dotnet test Expresso.slnx -c Release -f net48 --no-restore --filter "Category!=Integration"
```

| Suite | net6.0 leg | net48 leg |
| --- | ---: | ---: |
| Core | 610 | 582 |
| Parsing | 155 | 148 |
| LINQ | 323 | 323 |
| EF Core (net8.0 in both legs) | 381 | 381 |
| EF6 (net48 in both legs) | 230 | 230 |
| Six SQL dialect suites | 150 | 150 |
| Integration-project assertion/helper unit tests | 13 | 15 |
| **Total executions** | **1,862** | **1,829** |

**3,691 passed; zero failed or skipped.** This total includes repeated EF Core/EF6 executions across the two legs. Approved execution outside the restricted sandbox was needed for Windows SDK access; the net48 runner printed extension-loading warnings but completed every selected suite successfully.

Additional scratch probes compiled the current Core/Parsing/LINQ/EF Core sources and generated SQL with the installed SQL Server/PostgreSQL/MySQL/SQLite/Oracle providers. EF6 probes used the freshly built feature assemblies and installed SQL Server/PostgreSQL/SQLite/Oracle providers, with fixed manifest tokens and disabled initializers. SQLite native verification used only scalar SELECTs on disposable in-memory connections. DB2 command-generation probing could not complete because its native client failed to load/initialize; no DB2 runtime-parity claim is made from that attempt. The final evidence extract excludes that failed attempt.

Live integration against configured engines, external database connections, sample endpoint smoke tests, a separate full solution build and coverage measurement were not run. No schema/seed scripts, real-server DDL/DML or sample hosts were executed. These limits do not prevent reproducing the open findings above.

Current evidence: [SixthRecheck-current-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/SixthRecheck-current-output.txt), [Ef6SixthRecheck-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/Ef6SixthRecheck-output.txt). Both were regenerated from the current implementation during this recheck. The EF Core extract includes freshly rerun earlier reproductions with their original labels, alongside SIXTH RECHECK labels; it does not reuse earlier saved SQL as current evidence.

## Seventh remediation recheck — 2026-10-06

**Decision: the review still cannot be closed.** The latest remediation report describes the original N22 examples and the N23/N24 fixes already confirmed in the sixth recheck. Those fixes are present and still work. The additional N22 cases and N25/N26 from that recheck have not been resolved in the current implementation; fresh reproductions confirm all three. No new finding IDs are added by this recheck.

| Finding | Current result |
| --- | --- |
| N22, original examples | Fixed: literal REPLACE, len, lower, trim, indexof and right retain their native calls. The reported empty-search REPLACE example generates REPLACE on SQL Server, SQLite and Oracle. SQLite's reported supplementary-character, tab and Ä cases retain length/instr/trim/lower/substr. |
| N22, remaining cases | Open: literal upper/ltrim/rtrim and startswith/endswith still become incorrect FALSE parameters on SQLite. The Oracle tab trim-side null checks also still bind the CLR-trimmed empty string. |
| N23 | Remains fixed: current EF6 Oracle REPLACE null checks no longer inherit nullability from the optional search/replacement. The computed-result null check is retained. |
| N24 | Remains fixed: both current SQLite EF profiles generate two-argument substr with a negated length for the original zero/negative reproductions and positive/oversized controls. |
| N25 | Open: current EF Core and EF6 SQLite isnull(power(number,0.5)) commands contain a constant false WHERE clause and no POWER call. For number -1, the native SQLite expression is TRUE. |
| N26 | Open: EF Core still folds literal round(2.5) to 2, rejects precision 20 in the CLR, folds a negated literal POWER domain comparison to TRUE and throws a CLR overflow for the int-minimum ABS example. |

### Current reproductions of the open findings

The following SQLite checks were regenerated from current source/assemblies. `<TAB>` represents an actual U+0009 character, as in the sixth recheck. The POWER/ABS literal examples use explicitly typed Core nodes through the renderer API; the other examples use the parser.

| Finding and example | Current EF behavior | Native SQLite reference |
| --- | --- | --- |
| N22: `eq(upper("ä"),"ä")` | Boolean parameter 0; no UPPER | TRUE |
| N22: `eq(ltrim("<TAB>"),"<TAB>")`, rtrim counterpart | Boolean parameter 0; no LTRIM/RTRIM | TRUE |
| N22: `startswith("Ab","a")`, `endswith("bA","a")` | Boolean parameter 0; no LIKE | TRUE |
| N25: `isnull(power(number,0.5))`, number -1 | EF Core WHERE 0; EF6 WHERE 1 = 0 | TRUE |
| N26: `eq(round(2.5),3.0)` | EF Core boolean parameter 0; no ROUND | TRUE |
| N26: `eq(round(2.54,20),2.54)` | EF Core ArgumentOutOfRangeException during parameter extraction | TRUE |
| N26: typed `not(eq(power(-1.0,0.5),0.0))` | EF Core boolean parameter 1; no POWER | UNKNOWN; excluded by WHERE |
| N26: typed int `eq(abs(-2147483648),0)` | EF Core OverflowException during parameter extraction | FALSE; ABS itself succeeds |

The causes and remediation directions in the sixth recheck remain applicable: the remaining string and numeric operations lack literal-preserving translations, and POWER is still dispatched without a computed-null function name. See [N22 details](#n22--p2-partially-resolved-remaining-literal-string-functions-still-use-clr-behavior), [N25 details](#n25--p2-powers-computed-null-state-is-discarded-in-both-ef-profiles) and [N26 details](#n26--p2-literal-round-power-and-abs-still-execute-in-the-clr-in-ef-core). The minor right.md operand-condition clarification is also still outstanding.

### Validation for the seventh recheck

The two renderer test projects were freshly built and tested in Release with `Category!=Integration`: **381 EF Core tests on net8.0 and 230 EF6 tests on net48 passed; zero failed or skipped**. The full net6.0/net48 solution legs were not repeated during this recheck; their passing status was reported by the user, and the previous independent full-leg results remain recorded above.

```powershell
dotnet test test/Rendering/Expresso.Rendering.EntityFrameworkCore.Test/Expresso.Rendering.EntityFrameworkCore.Test.csproj -c Release --no-restore --filter "Category!=Integration"
dotnet test test/Rendering/Expresso.Rendering.EntityFramework.Test/Expresso.Rendering.EntityFramework.Test.csproj -c Release -f net48 --no-restore --filter "Category!=Integration"
```

The EF Core scratch harness recompiled current source; the EF6 harness was rebuilt against the newly built feature assemblies. Both generated commands with installed providers without opening external connections. SQLite reference checks used only scalar SELECTs on disposable in-memory connections. Live integration and sample endpoints were not run; DB2 runtime probing was not repeated. Only this review report was changed in the repository.

Fresh evidence: [SeventhRecheck-current-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/SeventhRecheck-current-output.txt), [Ef6SeventhRecheck-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/Ef6SeventhRecheck-output.txt). Older labels in the EF Core extract identify earlier cases rerun during this recheck, rather than reused historical outputs.

## Eighth remediation recheck — 2026-10-06

**Decision: the reported fixes are correct, but the review still cannot be closed.** Current source and newly generated commands resolve the remaining N22 examples, N25 and N26. Broader checks of the same function paths found four further P2 issues, N27–N30. These are distinct reproductions outside the cases fixed in the latest report.

### Remediation verified

| Finding | Current result |
| --- | --- |
| N22 | Resolved for the reported cases: literal upper/ltrim/rtrim retain UPPER/LTRIM/RTRIM; literal startswith/endswith/contains retain LIKE. Oracle tab trim-side null checks contain the native trim function and bind the original tab. Earlier literal replace/len/lower/trim/indexof/right cases still retain their calls. |
| N25 | Resolved for POWER: current EF Core SQL contains POWER inside NULLIF for both non-nullable and nullable bases. Commands were regenerated on SQL Server, PostgreSQL, MySQL, SQLite and Oracle; DB2 registration/source and its passing unit coverage were checked. Current EF6 SQL Server/PostgreSQL/SQLite/Oracle commands compare POWER with NULL. |
| N26 | Resolved for the reported comparisons: literal midpoint round, precision 20, domain power and minimum-int abs retain ROUND/POWER/ABS. SQL generation succeeds without the former CLR folding/exception. |
| N23 / N24 | Remain fixed in the rerun cases: Oracle REPLACE null tracking ignores optional search/replacement nullability; SQLite right uses two-argument substr with negated lengths. |
| right.md clarification | Resolved: the native literal path is now described as requiring neither operand to reference the query row. |

For example, the SQLite POWER null check now contains:

```sql
CASE WHEN NULLIF(POWER("r"."Number", @exponent), NULL) IS NULL
     THEN 1 ELSE 0 END = 1
```

The nullable-base control adds `"r"."Maybe" IS NULL OR ...` and still contains POWER over the nullable column. The literal negated domain comparison keeps both the computed-null guard and POWER inequality. Scalar SELECTs on disposable in-memory SQLite connections confirmed the corresponding TRUE/excluded-row outcomes, native midpoint/precision rounding, minimum-int ABS comparison, tab trimming and LIKE case behavior.

### N27 — P2: in-memory POWER still bypasses PostgreSQL domain and range errors

**Locations:** [shared Power:48](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:48), [VisitPower:93](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:93), [in-memory ComputedNull:15](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/InMemoryExpressionToLinqTransformer.cs:15).

The shared visitor now supplies the `power` function name, which correctly enables the EF fixes. The in-memory profile still inherits Math.Pow and handles only sqrt/div/mod in ComputedNull. Compiled predicates therefore accept finite-input cases that the locked PostgreSQL reference rejects:

| Predicate | Input | Current compiled in-memory result | PostgreSQL reference |
| --- | --- | --- | --- |
| `isnull(power(number,0.5))` | number = -1 | FALSE, without evaluating POWER | Domain error |
| `not(eq(power(number,0.5),0.0))` | number = -1 | TRUE through NaN | Domain error |
| Typed IR: `gt(power(number,-1.0),0.0)` | number = 0 | TRUE through Infinity | Domain error |
| `gt(power(number,1024.0),0.0)` | number = 2 | TRUE through Infinity | Overflow error |
| `eq(power(number,1075.0),0.0)` | number = 0.5 | TRUE through zero | Underflow error |

All but the explicitly labelled typed IR case use accepted parsed filters and a double field. The ordinary control `eq(power(number,2),4.0)` returns TRUE for number 2.

PostgreSQL's dpow explicitly rejects negative bases with fractional exponents and zero bases with negative exponents, and checks finite-input overflow/underflow. The reference column above is inferred from that primary implementation; a live PostgreSQL query was not run. See [PostgreSQL POWER implementation](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/backend/utils/adt/float.c).

Implement the PostgreSQL-compatible in-memory operation and its error checks, including when isnull consumes the result. Cover domain and range errors, normal and NULL inputs, and value/sort consumers. Update the current “Same as Queryable” in-memory POWER documentation to describe the reference behavior. N25's EF remediation does not address this profile.

### N28 — P2: SQL Server bracket searches differ between ADO, EF Core fields and EF Core literals

**Locations:** [StartsWith/EndsWith:75](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.cs:75), [Contains:87](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.cs:87), [LiteralLike:113](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.cs:113), [LikePattern:122](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.cs:122), [ADO escaping:194](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Common/ExpressionToSqlQueryClauseTransformerBase.String.cs:194).

The literal path escapes backslash, percent and underscore, matching ADO. The field path falls through to EF Core's SQL Server string-method translation, which additionally escapes the opening bracket. Actual current commands for the accepted filter `contains(name,"[ab]")` bind:

```sql
-- ADO
[Name] LIKE @p_0 ESCAPE '\'
-- @p_0 = '%[ab]%'

-- EF Core, field source
[r].[Name] IS NOT NULL AND [r].[Name] LIKE @search ESCAPE N'\'
-- @search = '%\[ab]%'
```

The new literal-source counterpart `contains("ab","[ab]")` binds `%[ab]%`, matching ADO rather than the field-source EF path. Startswith and endswith show the same discrepancy: ADO/literal EF use `[ab]%` / `%[ab]`; field EF uses `\[ab]%` / `%\[ab]`.

For name `ab`, ADO and the literal-source command match, while the field-source EF command does not: SQL Server interprets unescaped brackets as a character set. These native outcomes are inferred from the captured SQL/parameters and [Microsoft LIKE semantics](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/like-transact-sql?view=sql-server-ver17), not from a server execution.

This is an EF Core/ADO parity defect and a field/literal inconsistency. It also exposes an existing ADO bracket-escaping limitation relative to the documented literal-substring intent. Use one SQL Server escaping policy across operand forms and renderers. If literal brackets are the intended contract, changing only the new literal path would still leave ADO inconsistent; align that renderer and documentation too. Add bracket-set/range and escaped-character controls to differential coverage. EF6 parameter values were not captured for this reproduction, so this finding makes no EF6 bracket-search claim.

### N29 — P2: SQL Server POWER with an integer base still changes results across ADO and both EF profiles

**Locations:** [binary argument conversion:71](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:71), [VisitPower:93](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:93), [EF Core SqlPower:52](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.Literals.cs:52), [ADO VisitPower:111](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Common/ExpressionToSqlQueryClauseTransformerBase.Visitor.cs:111).

The accepted parsed filter `eq(power(id,0.5),2.0)`, with id catalogued and mapped as int, generates:

```sql
-- ADO: exponent parameter 0.5, comparison parameter 2.0
POWER([Id], @exponent) = @expected

-- EF Core: both parameters float; computed-null guard omitted here for clarity
POWER(CAST([r].[Id] AS float), @exponent) = @expected

-- EF6: generated value comparison, additional null guard omitted
POWER(CAST([Extent1].[Id] AS float), @exponent) = @expected
```

SQL Server POWER returns int for an int base and float for a float base. Consequently, for id 5, the ADO scalar returns 2 and matches; the EF scalar returns approximately 2.236 and does not match. A typed negative-exponent control likewise produces integer zero through ADO but a fractional result through both EF profiles. The positive integer-exponent control `eq(power(id,2),25.0)` has matching scalar results for id 5. Native outcomes are inferred from current generated SQL and [Microsoft POWER return-type semantics](https://learn.microsoft.com/en-us/sql/t-sql/functions/power-transact-sql?view=sql-server-ver17); a live SQL Server comparison was not run.

This violates the plan's same-engine ADO parity independently of N25's null preservation. Preserve the native base type through SQL Server POWER before converting its result to the IR's double type, or explicitly revise the ADO/type contract and approved design together. The current POWER page shows both incompatible forms while promising an always-double result. Test accepted int/byte bases with fractional and negative exponents and overflow cases, rather than only double bases.

### N30 — P2: error-producing ABS and SUBSTRING calls still disappear inside isnull

**Locations:** [VisitIsNull:89](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Logic.cs:89), [VisitAbs:84](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:84), [string function null propagation:130](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:130), [EF Core ComputedNull:12](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:12), [EF6 ComputedNull:12](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.ComputedNull.cs:12).

POWER now preserves its operation in the null-state expression, but other operations still report only argument nullability. VisitIsNull consumes that state and drops the value expression. Fresh commands demonstrate:

| Reproduction | Current EF Core and EF6 behavior | ADO/native operation |
| --- | --- | --- |
| Parsed `isnull(abs(id))`, id mapped as a non-nullable int | SQL Server constant FALSE; PostgreSQL constant FALSE; no ABS | ABS over minimum int can overflow |
| Typed IR `IsNull(Abs(Literal(int.MinValue)))` | Same constant FALSE commands; no ABS; compiled in-memory predicate also returns FALSE | PostgreSQL int ABS rejects minimum int |
| Parsed `isnull(substring(name,1,-1))`, non-NULL name | SQL Server/PostgreSQL only test `Name IS NULL`; no SUBSTRING/SUBSTR | Negative substring length raises an error |

Current SQL Server ADO generation retains `ABS([Id]) IS NULL` and `SUBSTRING([Name], @start, @length) IS NULL`, with start 1 and length -1. The native error behavior is supported by [Microsoft ABS](https://learn.microsoft.com/en-us/sql/t-sql/functions/abs-transact-sql?view=sql-server-ver17), [Microsoft SUBSTRING](https://learn.microsoft.com/en-us/sql/t-sql/functions/substring-transact-sql?view=sql-server-ver17), [PostgreSQL int4abs](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/backend/utils/adt/int.c) and [PostgreSQL substring validation](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/backend/utils/adt/varlena.c). The PostgreSQL reference profile must reject the typed ABS case, instead of succeeding without evaluating it. No live SQL Server/PostgreSQL error query was executed; the generated-call loss is directly reproduced.

Extend error-preserving null-state handling beyond the individually patched sqrt/div/mod/power cases, using each provider's actual error and NULL rules. Keep potentially failing operations observable through isnull and its negation, while retaining correct argument-NULL behavior. Add ABS overflow and negative-substring-length cases to both EF command tests and reference/differential tests. SQLite's successful minimum-int ABS and its accepted negative substring lengths are controls, not failures to impose on SQLite. N26's literal ABS comparison is fixed; this separate isnull consumer still discards the operation.

### Validation for the eighth recheck

Both full Release solution test legs were independently rerun:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0 --no-restore --filter "Category!=Integration" --nologo -v minimal
dotnet test Expresso.slnx -c Release -f net48 --no-restore --filter "Category!=Integration" --nologo -v minimal
```

| Suite | net6.0 leg | net48 leg |
| --- | ---: | ---: |
| Core | 610 | 582 |
| Parsing | 155 | 148 |
| LINQ | 323 | 323 |
| EF Core (net8.0 in both legs) | 438 | 438 |
| EF6 (net48 in both legs) | 231 | 231 |
| Six SQL dialect suites | 150 | 150 |
| Integration-project assertion/helper unit tests | 13 | 15 |
| **Total executions** | **1,920** | **1,887** |

**3,807 passed; zero failed or skipped.** This counts repeated EF Core/EF6 runs across the two legs. The net48 runner printed SDK extension-loading warnings but completed every selected suite successfully.

The EF Core scratch harness recompiled current Core/Parsing/LINQ/EF Core and ADO SQL Server sources. EF6 command probes used the newly built feature assemblies, installed providers, fixed manifest tokens and disabled initializers. Commands were generated without opening external database connections. SQLite execution was limited to scalar SELECTs on disposable in-memory connections. In-memory findings were executed as compiled predicates.

Live integration, configured-engine error/result comparisons, sample endpoint smoke tests, a separate full solution build and coverage measurement were not run. DB2 runtime command generation was not repeated; its source registration and unit coverage passed. No sample hosts, schema/seed scripts or real-server DDL/DML were run. Only this review report was changed in the repository.

Fresh evidence: [EighthRecheck-current-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/EighthRecheck-current-output.txt), [Ef6EighthRecheck-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/Ef6EighthRecheck-output.txt). Both were regenerated during this recheck. Earlier labels in the EF Core extract identify earlier cases freshly rerun against current source, not historical SQL reused as current evidence.

## Ninth remediation recheck — 2026-10-06

**Decision: the review cannot be closed.** Reviewed HEAD `15e000f` plus the current uncommitted N27–N30 remediation. The reported examples are fixed, but N29 and N30 remain incomplete under other consumers. Broader numeric checks found two additional P2 findings, N31 and N32. There are **four open P2 findings** in this recheck.

### Remediation verified

| Finding | Current result |
| --- | --- |
| N27 | Resolved for the reported cases: in-memory POWER rejects a negative base with a fractional exponent, zero with a negative exponent, finite-input overflow and underflow. Compiled comparisons and isnull retain those errors; the new sort tests pass. The boundary controls return the smallest positive double for 0.5^1074 and throw for 0.5^1075; power(2,2) is 4. Nullable-base controls retain UNKNOWN/NULL behavior. |
| N28 | Resolved: current ADO and EF Core field/literal SQL Server searches all escape opening brackets. `contains(name,"[ab]")` binds `%\[ab]%`; startswith/endswith bind `\[ab]%` / `%\[ab]`. Existing percent, underscore and backslash controls still escape correctly; PostgreSQL bracket controls leave the bracket unchanged. |
| N29 | Partially resolved: direct int, byte and nullable-int SQL Server bases retain their native POWER base type before the result conversion. Double bases remain floating point. The conversion still changes native semantics when POWER feeds another operation; see below. |
| N30 | Partially resolved: ABS and SUBSTRING remain in the current EF Core/EF6 commands under isnull and its negation. In-memory minimum-int ABS and negative substring lengths throw, including through isnull. SQL Server LEFT/RIGHT still disappear under the same consumers; see below. |

Earlier string fixes were also rerun, including literal function compositions, Oracle tab trim-side null checks and REPLACE controls, and SQLite right at zero, negative, positive and oversized lengths. No regression was reproduced in those cases. The provider command matrix covered SQL Server, PostgreSQL, MySQL, SQLite and Oracle for EF Core, and SQL Server/PostgreSQL/SQLite/Oracle for EF6; this is command-generation evidence, not execution against those external engines.

### N29 — P2, partially resolved: converting POWER's result changes nested SQL Server operations

**Locations:** [EF Core Power:53](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.Literals.cs:53), [SqlServerIntegerPower:79](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.Literals.cs:79), [EF6 Power:12](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.Numeric.cs:12), [shared numeric promotion:71](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:71).

The direct-base fix is correct, but both EF profiles convert the native integer POWER result to double before its parent consumes it. The ADO expression keeps the native result type through the parent. These accepted parsed filters still differ for an int field `id = 5`:

| Filter | ADO scalar behavior | EF Core / EF6 scalar behavior |
| --- | --- | --- |
| `eq(power(power(id,0.5),0.5),1.0)` | Inner integer POWER is 2; outer integer POWER is 1; TRUE | Inner result is cast to float; outer floating POWER is approximately 1.414; FALSE |
| `eq(div(power(id,1),2),2.0)` | Integer POWER is 5; integer division produces 2; TRUE | POWER is cast to float; division produces 2.5; FALSE |

Fresh SQL for the first reproduction contains these value expressions; additional computed-null guards are omitted here:

```sql
-- ADO
POWER(POWER([Id], @innerExponent), @outerExponent) = @expected

-- EF Core
POWER(CAST(POWER([r].[Id], @innerExponent) AS float), @outerExponent) = @expected

-- EF6
POWER(CAST(POWER([Extent1].[Id], @innerExponent) AS float), @outerExponent) = @expected
```

Both exponent parameters are 0.5 and the expected value is double 1. For the division reproduction, ADO binds the exponent and denominator as Int32 1 and 2; EF Core binds floating-point 1 and 2. A further typed-IR control, `abs(power(-2:int,31:int))`, generates integer ABS over POWER through ADO, but ABS over a float-cast POWER through both EF profiles, changing minimum-int overflow behavior too.

These native outcomes are **inferred from the captured SQL/parameter types**, [Microsoft POWER return types](https://learn.microsoft.com/en-us/sql/t-sql/functions/power-transact-sql?view=sql-server-ver17) and [Microsoft integer division semantics](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/divide-transact-sql?view=sql-server-ver17). They were not executed on SQL Server. The generated type changes are directly reproduced in both EF profiles.

Preserve native SQL Server result types through composed operations, while reconciling the public double result at a boundary that does not change the expression's native behavior. Alternatively, changing the ADO contract requires an explicit design change; casting every intermediate to double does not satisfy the current same-engine parity target. Add nested POWER, division and ABS consumers alongside the direct int/byte/nullable/double controls.

### N30 — P2, partially resolved: SQL Server LEFT and RIGHT still disappear inside isnull

**Locations:** [EF Core ComputedNull:12](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:12), [EF6 ComputedNull:13](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.ComputedNull.cs:13), [VisitLeft/VisitRight:131](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.String.cs:131), [VisitIsNull:89](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Logic.cs:89).

The new error-observing branches handle ABS and SUBSTRING, but omit LEFT/RIGHT outside Oracle's existing empty-result rules. Current SQL Server commands in **both EF profiles** reduce:

| Accepted parsed filter | Generated WHERE condition |
| --- | --- |
| `isnull(left(name,-1))` | Name IS NULL |
| `not(isnull(left(name,-1)))` | Name IS NOT NULL |
| `isnull(right(name,-1))` | Name IS NULL |
| `not(isnull(right(name,-1)))` | Name IS NOT NULL |

There is no LEFT/RIGHT in any of those commands. SQL Server rejects a negative length when [LEFT](https://learn.microsoft.com/en-us/sql/t-sql/functions/left-transact-sql?view=sql-server-ver17) or [RIGHT](https://learn.microsoft.com/en-us/sql/t-sql/functions/right-transact-sql?view=sql-server-ver17) is evaluated. The ADO renderer retains these functions; the LEFT reproduction was freshly generated as `LEFT([Name], @length) IS NULL` with length -1. No live SQL Server error query was run; the loss of the calls is directly verified.

Extend the error-observing null state to these SQL Server functions and test isnull, its negation, literal/field lengths and argument-NULL controls. Keep the provider distinctions: PostgreSQL accepts negative LEFT/RIGHT lengths, and SQLite's established substr behavior must remain intact. This is a remaining instance of N30, not a claim that its ABS/SUBSTRING fixes failed.

### N31 — P2: other arithmetic retains CLR range behavior and EF Core still folds literal arithmetic

**Locations:** [shared arithmetic operations:15](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:15), [numeric visitors:91](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:91), [in-memory ComputedNull:15](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/InMemoryExpressionToLinqTransformer.cs:15), [in-memory Divide/Modulo:61](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/InMemoryExpressionToLinqTransformer.cs:61), [EF Core literal numeric overrides:13](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.Literals.cs:13).

N27 now checks POWER's range, but ADD/SUBTRACT/MULTIPLY still use unchecked CLR expressions. DIVIDE/MODULO check zero without matching the remaining PostgreSQL rules. Compiled predicates and sort-key delegates reproduced the following typed-IR cases (int or double fields as labelled):

| Operation and input | Current in-memory value/sort key | PostgreSQL reference |
| --- | --- | --- |
| int add(id,1), id = 2147483647 | -2147483648 | Integer range error |
| int sub(id,1), id = -2147483648 | 2147483647 | Integer range error |
| int mult(id,65536), id = 65536 | 0; comparison with 0 is TRUE | Integer range error |
| int mod(id,-1), id = -2147483648 | CLR OverflowException | 0; legal operation |
| double add(number,number), number = 1e308 | Infinity | Overflow error |
| double mult(number,2.0), number = 1e308 | Infinity | Overflow error |
| double div(number,1e-308), number = 1e308 | Infinity | Overflow error |
| double mult(number,1e-200), number = 1e-200 | 0 | Underflow error |
| double div(number,1e200), number = 1e-200 | 0 | Underflow error |

All these operations return FALSE under isnull without observing their value/range behavior. Integer `div(int.MinValue,-1)` does raise a CLR overflow as a value, but its isnull consumer also suppresses the range failure; not-isnull is TRUE. The reference outcomes are inferred from the explicit checks in [PostgreSQL int arithmetic](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/backend/utils/adt/int.c) and [PostgreSQL floating arithmetic](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/include/utils/float.h). In particular, int4mod special-cases a denominator of -1 to return 0 before evaluating a remainder. No PostgreSQL execution was performed.

The same CLR arithmetic expressions also remain evaluatable during **EF Core literal parameter extraction**. This produces ordinary incorrect successful results on SQLite, independently of any error-handling policy:

| Accepted parsed filter | Fresh EF Core SQLite command | Executed native SQLite result |
| --- | --- | --- |
| `gt(add(2147483647,1),0)` | WHERE @p, boolean parameter 0; no addition | TRUE: sum is 2147483648 |
| `eq(mult(65536,65536),0)` | WHERE @p, boolean parameter 1; no multiplication | FALSE: product is 4294967296 |

Typed literal SUBTRACT was also folded in command probes. SQLite's native subtraction of 1 from -2147483648 is -2147483649, while the CLR int operation wraps to 2147483647. SQLite results above were **executed as scalar SELECTs on a disposable in-memory connection**, without tables or external I/O. Its integer operations use a 64-bit range, as described in [SQLite arithmetic expressions](https://www.sqlite.org/lang_expr.html).

Address this as an arithmetic family: keep literal ADD/SUBTRACT/MULTIPLY in provider SQL, implement PostgreSQL-compatible in-memory integer/floating range behavior, preserve potentially failing operations through null consumers, and handle the legal int-minimum modulo -1 case. Cover values, comparisons, isnull/not-isnull and sort keys with ordinary, NULL, range-boundary and mixed numeric controls. Do not impose PostgreSQL errors on SQLite's valid wider arithmetic. The current ADD/SUBTRACT/MULTIPLY bodies and ROUND helper already existed at HEAD; this is uncovered coverage, not an observed regression from the N27–N30 edits.

### N32 — P2: in-memory ROUND silently returns the input outside the decimal helper's bounds

**Locations:** [ExpressoFunctions.Round:30](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:30), [in-memory Round:73](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/InMemoryExpressionToLinqTransformer.cs:73), [ROUND contract](C:/Users/itorg/source/repos/Expresso/docs/functions/arithmetic/round.md).

The helper returns the original value whenever its magnitude is at least 7.9e27 or digits is greater than 28. Those limits belong to the decimal-based implementation; they do not establish that rounding is a no-op. Both supported typed-IR expressions produced wrong compiled comparison results and sort keys:

| Expression and double field value | Current in-memory key | PostgreSQL round(numeric,int) reference |
| --- | --- | --- |
| round(number,-30), number = 1e29 | 1e29 | 0 |
| round(number,30), number = 1.234567e-29 | 1.234567e-29 | 1.2e-29 |

Equality predicates against 0.0 and 1.2e-29 respectively returned FALSE. The corresponding current EF Core PostgreSQL expressions still generate `ROUND("Number"::numeric, @digits)`, with precisions -30 and 30. The expected rounding values are inferred from the decimal-place contract in [PostgreSQL ROUND documentation](https://www.postgresql.org/docs/16/functions-math.html); a live PostgreSQL comparison was not run.

Implement the documented rounding semantics for finite doubles across these ranges, or explicitly reject a documented unsupported range rather than silently returning an incorrect value. Include large magnitudes with negative precision and small magnitudes with precision above 28, as well as ordinary midpoint/negative-precision controls. This helper and its early-return conditions were already present before the latest remediation.

### Validation and review limits for the ninth recheck

Both full Release non-integration test legs were independently rerun and passed:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0 --no-restore --filter "Category!=Integration" --nologo -v minimal
dotnet test Expresso.slnx -c Release -f net48 --no-restore --filter "Category!=Integration" --nologo -v minimal
```

| Suite | net6.0 leg | net48 leg |
| --- | ---: | ---: |
| Core | 610 | 582 |
| Parsing | 155 | 148 |
| LINQ | 331 | 331 |
| EF Core (net8.0 in both legs) | 452 | 452 |
| EF6 (net48 in both legs) | 246 | 246 |
| Six SQL dialect suites | 154 | 154 |
| Integration-project assertion/helper unit tests | 13 | 15 |
| **Total executions** | **1,961** | **1,928** |

**3,889 passed; zero failed or skipped.** The total includes repeated EF Core/EF6 executions across the two legs. The net48 runner printed SDK extension-loading warnings but completed every selected suite.

The scratch EF Core harness recompiled current Core/Parsing/LINQ/EF Core and ADO SQL Server source. EF6 probes used freshly built feature assemblies, fixed manifest tokens and disabled initializers. External provider commands were generated without opening their connections. The in-memory predicates and sort-key delegates were executed, and SQLite native checks used only scalar SELECTs on disposable in-memory connections.

Live engine integration, sample endpoint smoke tests, a separate full solution build and coverage measurement were not run. DB2 runtime command generation was not repeated; its registration/source and passing unit coverage were checked. No sample hosts, schema/seed scripts or real-server DDL/DML were run. **Only this review report was changed by this recheck.**

Automatic approval review rejected the optional read of the integration user-secrets file, classifying credential probing and subsequent database access as outside the review authorization. That read was not performed or bypassed; live SQL Server/PostgreSQL scalar validation remains outstanding.

Fresh evidence: [NinthRecheck-current-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/NinthRecheck-current-output.txt), [Ef6NinthRecheck-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/Ef6NinthRecheck-output.txt). Earlier labels in the EF Core extract identify older reproductions freshly rerun against current source, not saved historical results reused as current evidence.

## Tenth remediation recheck — 2026-10-06

**Decision: two P2 findings remain open, N29 and N32.** Reviewed HEAD `15e000f` plus the current uncommitted remediation. The ninth recheck's reported reproductions are fixed. N30 and N31 can be closed for those findings; N29 still has an integer-type composition gap, and N32's new scaling branch introduces two boundary regressions. **No new finding IDs are added.**

### Remediation verified

| Finding | Current result |
| --- | --- |
| N29, ninth-recheck examples | Fixed: POWER/POWER, division directly over integer POWER, and ABS over integer POWER retain integer intermediates in both EF profiles. Only the final double comparison casts their result. Byte and nullable-int controls retain the native base/result type; a double base remains floating point. |
| N30 | Resolved for the reported cases: current SQL Server EF Core null checks contain SUBSTRING/RIGHT inside NULLIF; EF6 retains LEFT/RIGHT IS NULL. Their negations also retain the calls. The new literal and nullable-source tests pass. PostgreSQL negative LEFT/RIGHT behavior in memory and SQLite's established two-argument substr controls remain intact. |
| N31 | Resolved for the reported cases: compiled in-memory integer overflow and floating overflow/underflow now throw NotSupportedException through values, comparisons, isnull/not-isnull and sort keys. int.MinValue modulo -1 returns 0. EF Core literal ADD/SUBTRACT/MULTIPLY retain arithmetic in SQL, including null consumers; the SQLite wider-range examples no longer become constant boolean parameters. |
| N32, ninth-recheck examples | Fixed: round(1e29,-30) returns 0, and round(1.234567e-29,30) returns 1.2e-29. Compiled equality and sort keys agree. Midpoint and ordinary negative-precision controls remain correct. PostgreSQL commands retain ROUND(...::numeric, digits). |
| Earlier controls | POWER domain/range errors, its smallest-positive-double boundary, nullable POWER, SQL Server bracket escaping, Oracle REPLACE and trim-side checks, and SQLite right controls were rerun without reproducing a regression. |

For the repaired division case, both EF profiles now generate the equivalent of `CAST(POWER(Id, @intExponent) / @intDenominator AS float) = @expectedDouble`. The division happens before the cast. The repaired ABS case keeps `ABS(POWER(@intBase, @intExponent))` before conversion, so the integer operation is retained.

### N29 — P2, partially resolved: FLOOR, CEILING and ROUND still convert integer POWER before a later integer operation

**Locations:** [VisitFloor/VisitCeiling:96](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:96), [VisitRound:109](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:109), [EF Core Power:65](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.Literals.cs:65), [EF6 Power:12](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.Numeric.cs:12).

POWER itself now preserves its integer result correctly. The surrounding FLOOR/CEILING visitors still force their argument to double, and ROUND calls ConvertNode to double before dispatching its operation. On SQL Server that changes these functions' native result type, and therefore a later division. Three accepted parsed filters reproduce the remaining gap in **both EF profiles**, with `id` mapped as int:

```text
eq(div(floor(power(id,1)),2),2)
eq(div(ceiling(power(id,1)),2),2)
eq(div(round(power(id,1),0),2),2)
```

The first filter's freshly generated value expressions are:

```sql
-- ADO; exponent and divisor are Int32 1 and 2
FLOOR(POWER([Id], @exponent)) / @divisor = @expected

-- EF Core; divisor and expected value are float
FLOOR(CAST(POWER([r].[Id], @exponent) AS float)) / @divisor = @expected

-- EF6; additional null guards omitted
FLOOR(CAST(POWER([Extent1].[Id], @exponent) AS float)) / @divisor = @expected
```

For id 5, the ADO integer expression yields 2 and matches; both EF floating expressions yield 2.5 and do not match. CEILING and ROUND reproduce the same integer-versus-floating division discrepancy. Native outcomes are **inferred from current SQL/parameter types**, not executed on SQL Server. Microsoft documents integer results for integer inputs to [FLOOR](https://learn.microsoft.com/en-us/sql/t-sql/functions/floor-transact-sql?view=sql-server-ver17), [CEILING](https://learn.microsoft.com/en-us/sql/t-sql/functions/ceiling-transact-sql?view=sql-server-ver17) and [ROUND](https://learn.microsoft.com/en-us/sql/t-sql/functions/round-transact-sql?view=sql-server-ver17); [integer division](https://learn.microsoft.com/en-us/sql/t-sql/language-elements/divide-transact-sql?view=sql-server-ver17) truncates.

The same forced conversions affect integer inputs without POWER, so the remaining cause is the enclosing numeric visitors' handling of SQL Server store types. Preserve the native type through these integer operations and their consumers, deferring a double conversion to a boundary that does not change native behavior. Add the three compositions above, direct integer-input counterparts, nullable inputs and floating-point controls. The existing no-inner-cast assertions cover POWER/POWER, direct division and ABS, but do not exercise these consumers.

This is an **existing composition gap**, not a failure of the newly repaired direct POWER compositions. It continues N29's same-engine parity requirement.

### N32 — P2, partially resolved: the new ROUND scaling branch regresses subnormal values and maximum precision

**Locations:** [Round dispatch:48](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:48), [RoundScaled:97](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:97), [scale calculation:115](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:115), [ROUND argument validation:15](C:/Users/itorg/source/repos/Expresso/src/Expresso.Core/CriteriaExpressions/RoundFunc.cs:15).

The new scaling algorithm resolves the reported magnitude/precision examples, but two other supported inputs now fail:

| Accepted parsed filter and field value | Current in-memory behavior | PostgreSQL reference |
| --- | --- | --- |
| `eq(round(number,2147483647),number)`, number = 1e29 | FALSE; helper and sort key return 0 | TRUE; rounding leaves 1e29 unchanged |
| `eq(round(number,324),number)`, number = double.Epsilon | OverflowException in helper, compiled predicate and sort key | TRUE; rounded numeric 5e-324 converts back to double.Epsilon |

The maximum-precision case adds an int precision to a positive decimal exponent in `places = digits + exponent`. That addition wraps into a large negative int, so the helper takes its zero-result branch. PostgreSQL's numeric_round explicitly clamps an extreme precision before its calculations instead of wrapping it. See [PostgreSQL numeric conversion and rounding implementation](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/backend/utils/adt/numeric.c).

For the subnormal case, the decimal exponent is -324 and `Math.Pow(10, exponent)` becomes zero. The adjustment still leaves the unit at zero; division by that unit produces infinity, and the subsequent decimal cast throws. The negative smallest-double control and a nearby subnormal value also reproduced OverflowException. PostgreSQL's numeric rounding supports this scale, and its [double parser](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/backend/utils/adt/float.c) accepts a representable nonzero subnormal result. These PostgreSQL outcomes are inferred from its primary implementation; no live PostgreSQL query was executed.

Both parsed filters build successfully. Current EF Core PostgreSQL command generation retains:

```sql
ROUND(r."Number"::numeric, @digits) = r."Number"
```

The helper's two failures were also reproduced using the freshly built netstandard library on **.NET Framework 4.8**. Compiled predicate/sort reproductions ran in the current-source modern scratch harness. The original N32 controls remain correct on both harnesses, and `round(2.5,0) = 3`, `round(125,-1) = 130`, and the extreme negative-precision zero control still pass.

These are **regressions introduced by the new RoundScaled branch**: the previous helper returned the input for both positive-precision cases, which was correct for these particular values. Use overflow-safe precision calculations and a significand/exponent representation that does not require an unrepresentable double power of ten. Cover representable subnormals and int precision endpoints, alongside positive/negative values, the repaired magnitude cases and midpoint controls. Rejecting these inputs as generic range errors would still differ from the documented PostgreSQL reference.

### Validation and review limits for the tenth recheck

Both complete Release non-integration solution legs were independently rerun:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0 --no-restore --filter "Category!=Integration" --nologo -v minimal
dotnet test Expresso.slnx -c Release -f net48 --no-restore --filter "Category!=Integration" --nologo -v minimal
```

| Suite | net6.0 leg | net48 leg |
| --- | ---: | ---: |
| Core | 610 | 582 |
| Parsing | 155 | 148 |
| LINQ | 335 | 335 |
| EF Core (net8.0 in both legs) | 499 | 499 |
| EF6 (net48 in both legs) | 253 | 253 |
| Six SQL dialect suites | 154 | 154 |
| Integration-project assertion/helper unit tests | 13 | 15 |
| **Total executions** | **2,019** | **1,986** |

**4,005 passed; zero failed or skipped.** This counts repeated EF Core/EF6 executions across legs. The first sandboxed attempt could not access the local Windows SDK; the approved rerun completed successfully. The net48 runner printed SDK extension-loading warnings but passed every selected suite.

The modern scratch harness compiled current Core/Parsing/LINQ/EF Core and ADO SQL Server source. It generated commands with installed SQL Server/PostgreSQL/MySQL/SQLite/Oracle providers, executed compiled in-memory predicates/sort delegates, and ran native SQLite scalar SELECTs on disposable in-memory connections. The EF6 scratch harness used freshly built renderer assemblies with fixed provider manifest tokens and disabled initializers, and also executed the ROUND helper on .NET Framework. No external database connections were opened.

Live engine integration, sample endpoint execution, a separate full solution build and coverage measurement were not performed. DB2 runtime probing was not repeated; current source registrations and its passing unit suite were checked. No sample hosts, schema/seed scripts or real-server DDL/DML were run. **Only this review report was changed in the repository by this recheck.**

Fresh evidence: [TenthRecheck-current-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/TenthRecheck-current-output.txt), [Ef6TenthRecheck-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/Ef6TenthRecheck-output.txt). Earlier labels identify previous reproductions freshly rerun against current implementation.

## Eleventh remediation recheck — 2026-10-06

Reviewed the latest N29/N32 changes at HEAD `15e000f` plus the current working-tree remediation. This was a review-only pass: no implementation, tests, configuration, plans or status documents were edited. The checks covered the reported fixes, related integer/byte/double and nullable paths, provider dispatch, null consumers, decimal midpoints, compiled predicates and sort keys. **Three P2 findings remain open: N26, N30 and N32. No new finding IDs are introduced.**

### Confirmed fixes and retained controls

| Finding or control | Recheck result |
| --- | --- |
| N29 | Resolved. Both EF profiles retain integer POWER through FLOOR, CEILING and ROUND, and divide the integer result before a later double comparison. Direct int/byte/literal and nullable-int coverage was inspected; additional EF Core nullable and byte command probes preserve the integer operation. Double inputs retain floating-point operations. SQLite's integer argument still converts to double. |
| N32, tenth-recheck failures | Fixed. `round(1e29,2147483647)` returns 1e29; positive/negative smallest-double and nearby subnormal controls no longer throw. Compiled predicates and sort keys for the maximum precision and smallest-double cases pass on the modern and .NET Framework harnesses. |
| N32, ninth-recheck failures | Still fixed. `round(1e29,-30)` returns 0, and `round(1.234567e-29,30)` returns 1.2e-29 on both harnesses. Ordinary midpoint and negative-precision controls remain correct. |
| N30, previously reported calls; N31 and earlier controls | Fresh command/delegate probes retain LEFT/RIGHT/SUBSTRING and ABS under null consumers, arithmetic range checks, literal SQL arithmetic, POWER domain/range checks and its subnormal boundary, SQL Server bracket escaping, Oracle REPLACE, and SQLite right controls. The additional ROUND null-consumer gap below remains in N30's error-preservation family. |

For example, the repaired SQL Server composition now retains this order in both EF profiles:

```sql
-- int exponent and int divisor; conversion occurs after division
CAST(FLOOR(POWER(Id, @exponent)) / @divisor AS float) = @expectedDouble
```

CEILING and ROUND have the same corrected order. EF6 may omit the final cast in the printed comparison; it no longer inserts one inside the rounding operation. These are command-generation checks, not live SQL Server execution.

### N26 — P2, reopened regression: integer ROUND bypasses EF Core provider markers outside SQL Server

**Locations:** [VisitRound:121](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:121), [EF Core Round:64](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.cs:64), [Round marker:34](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoDbFunctions.cs:34), [literal SqlRound marker:163](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoDbFunctions.cs:163), [PostgreSQL registration:12](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.PostgreSql.cs:12).

The shared visitor correctly preserves an integer argument for SQL Server. The EF Core Round override, however, also passes that integer to the other marker lookups. Both Round and SqlRound only accept `(double, int)`. The lookups fail, so these inputs fall through to `Math.Round((double)value, digits)` instead of the registered provider function.

These accepted parsed filters reproduce the regression with the installed providers:

| Filter | Provider | Current result |
| --- | --- | --- |
| `eq(round(1,20),1.0)` | PostgreSQL, SQLite, MySQL, Oracle | Command generation throws InvalidOperationException during parameter extraction, with inner ArgumentOutOfRangeException: rounding digits must be between 0 and 15. No ROUND command is generated. |
| `eq(round(id,1),1.0)`, id mapped as int | PostgreSQL | Command generation rejects Math.Round(double, int) as untranslatable; the required numeric-cast marker is absent. |
| `eq(div(round(maybeid,0),2),2)`, maybeid mapped as nullable int | PostgreSQL | The same untranslatable Math.Round path is used inside the nullable composition. |
| `eq(round(1.0,20),1.0)` | All four providers above | Control succeeds and retains ROUND; PostgreSQL includes the numeric cast. |

The SQLite native scalar `SELECT ROUND(1,20)=1.0, ROUND(1,20)` was executed on a disposable in-memory connection and returned `1 | 1`. Thus the literal failure is a CLR pre-evaluation error, not a SQLite precision restriction. PostgreSQL field translation fails before any connection is opened. Other providers' field ROUND probes generated commands; the reported field failure is specifically reproduced on PostgreSQL. DB2 has the same double-only marker shape in source, but its runtime was not probed in this pass.

This is a **regression from the latest shared argument-type change**, reopening N26's requirement that literal numeric functions reach the database. Previously the shared ROUND visitor converted the argument to double before provider dispatch. Preserve the SQL Server integer branch, then normalize the other paths to a supported marker signature or supply equivalent integer registrations. The PostgreSQL path must retain its numeric cast, and row-independent calls must remain provider functions.

The existing literal ROUND regression tests use double literals such as 2.5 and 2.54. The new integer tests primarily check SQL Server, with a SQLite column translation control; those cases do not detect an integer literal being evaluated in the CLR. Add cross-provider int/byte literal cases at high and negative precision, PostgreSQL integer/nullable columns, and double controls alongside the SQL Server integer compositions.

### N30 — P2, additional remaining case: null consumers discard SQL Server integer ROUND

**Locations:** [VisitRound propagation:121](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressionToLinqTransformerBase.Numeric.cs:121), [EF Core computed-null handling:12](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.ComputedNull.cs:12), [EF6 computed-null handling:13](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.ComputedNull.cs:13).

SQL Server ROUND now correctly retains its integer input and result in value expressions. Its null-state path still uses only operand nullness, so a surrounding isnull can discard the operation. This repeats N30's error-preservation gap for another supported operation, while the previously reported ABS/SUBSTRING/LEFT/RIGHT fixes remain intact.

Fresh command probes in **both EF profiles** reproduce all four cases:

| Accepted filter | EF Core command | EF6 command |
| --- | --- | --- |
| `isnull(round(id,-1))`, id mapped as int | WHERE 0 = 1; no ROUND | WHERE 1 = 0; no ROUND |
| `not(isnull(round(id,-1)))` | No WHERE predicate; no ROUND | No WHERE predicate; no ROUND |
| `isnull(round(maybeid,-1))`, nullable int | WHERE MaybeId IS NULL; no ROUND | WHERE MaybeId IS NULL; no ROUND |
| `isnull(round(2147483647,-1))` | WHERE 0 = 1; no ROUND | WHERE 1 = 0; no ROUND |

For the same parsed inputs, ADO preserves `ROUND(Id, @intPrecision) IS NULL` or its negation; the literal source and precision parameters are Int32. Integer 2147483647 rounded at -1 requires 2147483650, which exceeds SQL Server's int result range. Microsoft documents the retained int return type and arithmetic overflow when a rounded result cannot fit its input type. See [SQL Server ROUND](https://learn.microsoft.com/en-us/sql/t-sql/functions/round-transact-sql?view=sql-server-ver17).

The **loss of ROUND in generated commands is reproduced**. The native overflow is inferred from the documented type/range rules; a live comparison of ADO and EF execution, including SQL Server optimizer behavior under null predicates, was not performed. Do not interpret the generated constant predicate as proof that evaluating the integer function is safe.

VisitRound uses the propagation overload without a function name, and neither EF computed-null override handles round. Carry the operation into the null-state calculation for the SQL Server integer path, using the existing preservation mechanism; account for the provider optimizer as in the other error-prone calls. Add direct/literal/nullable inputs, both isnull and its negation, plus a safe ordinary value control. This is an additional case in the existing N30 family, not evidence that its earlier function fixes regressed.

### N32 — P2, partially resolved: decimal conversion and early returns still change ROUND results

**Locations:** [Round dispatch:48](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:48), [high-precision shortcut:72](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:72), [DecimalExactAtScale:93](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:93), [scientific significand:115](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:115), [scaled early return:120](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:120).

The 64-bit precision calculation and scientific exponent fix the reported wrap and subnormal exceptions. The replacement still fails the documented PostgreSQL reference in two ways: its scaled significand exposes extra binary approximation digits, and its shortcuts return the original double after an intermediate decimal conversion has already discarded digits.

| Input and precision | Current helper / compiled sort key | PostgreSQL reference result |
| --- | --- | --- |
| 1.005e-29, 31 | 1e-29 | 1.01e-29 |
| -1.005e-29, 31 | -1e-29 | -1.01e-29 |
| 1.225e-28, 30 | 1.225e-28, unchanged | 1.23e-28 |
| 1.234567890123456, 30 | 1.234567890123456, unchanged | 1.23456789012346 |

All four helper results, compiled equality failures and compiled sort keys were reproduced using current code on the modern scratch harness and freshly built assemblies on **.NET Framework 4.8**. These accepted parsed filters also return FALSE on the modern harness at their corresponding input values:

```text
eq(round(number,31),0.0000000000000000000000000000101)
eq(round(number,30),0.000000000000000000000000000123)
eq(round(number,30),1.23456789012346)
```

The PostgreSQL column in the table is **inferred from its primary implementation, not a live engine run**. Its float8_numeric conversion formats a double with DBL_DIG (15 significant decimal digits), then numeric_round rounds that decimal value. This produces the stated midpoint inputs and reference results. See [PostgreSQL numeric conversion and rounding implementation](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/backend/utils/adt/numeric.c).

For 1.005e-29, the current `E16` string is `1.0049999999999999E-029`; rounding that significand rounds down instead of away from the decimal midpoint. For 1.225e-28, DecimalExactAtScale first converts to a decimal that has already lost the fractional digits beyond its scale limit. Comparing that rounded decimal with itself cannot establish that the original input needs no further rounding; the shortcut returns the unrounded double. At 30 places the ordinary 16-digit double control also returns the original value rather than the PostgreSQL decimal-converted value.

This continues N32's conversion/rounding parity gap. The specific maximum-precision wrap and subnormal exception regressions from the tenth recheck are resolved; the remaining examples expose other paths in the helper. Use a consistent PostgreSQL-compatible decimal representation for rounding and shortcut decisions, including values outside decimal's magnitude/scale bounds. A nonzero decimal cast is insufficient evidence that the original input has no digits to round. Cover positive/negative tiny midpoints, scale-28 boundaries and inputs with more than 15 significant digits, retaining the repaired precision and subnormal controls.

### Validation and review limits for the eleventh recheck

Both complete Release non-integration solution legs were independently rerun:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0 --no-restore --filter "Category!=Integration" --nologo -v minimal
dotnet test Expresso.slnx -c Release -f net48 --no-restore --filter "Category!=Integration" --nologo -v minimal
```

| Suite | net6.0 leg | net48 leg |
| --- | ---: | ---: |
| Core | 610 | 582 |
| Parsing | 155 | 148 |
| LINQ | 337 | 337 |
| EF Core (net8.0 in both legs) | 506 | 506 |
| EF6 (net48 in both legs) | 257 | 257 |
| Six SQL dialect suites | 154 | 154 |
| Integration-project assertion/helper unit tests | 13 | 15 |
| **Total executions** | **2,032** | **1,999** |

**4,031 passed; zero failed or skipped.** This counts repeated EF Core/EF6 executions across legs. The net48 runner printed SDK extension-loading warnings but every selected suite passed. The newly reproduced cases were checked in temporary review harnesses, not added to the repository tests.

The modern harness compiled current Core/Parsing/LINQ/EF Core and ADO SQL Server source, generated commands with installed SQL Server/PostgreSQL/MySQL/SQLite/Oracle providers, and executed compiled in-memory predicates/sort keys plus SQLite scalar SELECTs on disposable in-memory connections. The EF6 harness used freshly built renderer assemblies, fixed provider manifest tokens and disabled initializers; it generated commands and ran the ROUND helper and compiled delegates on .NET Framework. No external database connections were opened.

Live engine integration, sample endpoint execution, a separate full solution build and coverage measurement were not performed. DB2 runtime probing was not repeated; its source registrations and passing unit suite were checked. No sample hosts, schema/seed scripts or real-server DDL/DML were run. **Only this review report was changed in the repository by this recheck.**

Fresh evidence: [EleventhRecheck-current-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/EleventhRecheck-current-output.txt), [Ef6EleventhRecheck-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/Ef6EleventhRecheck-output.txt). Earlier probe labels in the output identify prior controls freshly rerun against the current implementation.

## Twelfth remediation recheck — 2026-10-06

Reviewed HEAD `15e000f` plus the current working-tree remediation for N26, N30 and N32. This was a review-only pass: no implementation, tests, configuration, plans or status documents were edited. The checks covered the original failures, related integer/byte/double and nullable paths, null consumers, the ROUND scale boundary, compiled predicates and sort keys. **One P2 finding remains open: N32. No new finding IDs are introduced.**

### Confirmed fixes and retained controls

| Finding or control | Recheck result |
| --- | --- |
| N26 | Resolved. The accepted literal filter `eq(round(1,20),1.0)` generates ROUND on PostgreSQL, SQLite, MySQL and Oracle. PostgreSQL integer-column and nullable-integer compositions generate ROUND with the required numeric cast. Byte literals, negative precision and double controls are covered by the new passing IntegerRoundProviderTests. The SQL Server branch still preserves its integer input/result. |
| N30 | Resolved for the remaining ROUND cases. Both EF profiles retain ROUND under isnull and its negation for integer fields and the maximum-int literal. Nullable-int commands retain the column null check and the ROUND call. Ordinary precision-zero controls also retain ROUND in the passing tests. |
| N32, eleventh-recheck cases | Fixed. 1.005e-29 at 31 places returns 1.01e-29; the negative counterpart returns -1.01e-29. 1.225e-28 at 30 places returns 1.23e-28, and 1.234567890123456 at 30 places returns 1.23456789012346. Helper, compiled equality and sort-key results agree on net6.0, the modern scratch harness and .NET Framework 4.8. |
| N29 and earlier N32 controls | SQL Server retains integer POWER through FLOOR/CEILING/ROUND and later integer division in both EF profiles. Maximum precision, smallest/nearby subnormals, large negative precision, small positive precision and ordinary midpoint controls remain correct. |
| Earlier controls | Selected POWER domain/range, arithmetic range, null-consumer, SQL Server bracket escaping, Oracle REPLACE and SQLite right probes were freshly rerun. No other finding was reproduced in these selected controls. |

The repaired PostgreSQL column path now generates the equivalent of:

```sql
ROUND(r."Id"::double precision::numeric, @digits) = @expected
```

The repaired SQL Server null consumer retains `NULLIF(ROUND(value, @digits), NULL) IS NULL` through the EF Core marker; EF6 prints `ROUND(value, @digits) IS NULL`. Its negation retains the same operation. Native overflow/optimizer behavior was not tested against a live SQL Server.

### N32 — P2, partially resolved: the decimal fast path rounds tiny inputs twice

**Locations:** [Round dispatch:55](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:55), [RoundDecimal:75](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:75), [premature decimal conversion:77](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:77), [15-digit scaled path:92](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:92).

RoundScaled now consistently uses the 15-significant-digit representation, and the reported cases are fixed. For `abs(value) < 7.9e27` and `digits <= 28`, Round still enters RoundDecimal first. Casting the whole tiny double to decimal quantizes it to decimal's maximum scale before Math.Round applies the requested precision. That earlier rounding cannot be reversed by the final half-away-from-zero rounding.

These remaining failures were independently reproduced with **freshly built net6.0 assemblies**, freshly built netstandard assemblies on **.NET Framework 4.8**, and the current-source modern harness:

| Input and precision | Current helper / compiled sort key | PostgreSQL reference result | Compiled equality to reference |
| --- | --- | --- | --- |
| 5e-29, 28 | 0 | 1e-28 | FALSE |
| -5e-29, 28 | 0 | -1e-28 | FALSE |
| 1.499e-27, 27 | 2e-27 | 1e-27 | FALSE |
| -1.499e-27, 27 | -2e-27 | -1e-27 | FALSE |

For the first pair, the decimal cast produces zero before the midpoint rounding runs. For the second pair, it changes 1.499e-27 to 1.5e-27; rounding that new midpoint at 27 places then produces 2e-27. The PostgreSQL 15-digit conversion preserves 1.499e-27, which rounds down at the requested precision. These are lost digits in the fast path, not failures in the repaired scientific-string branch.

The PostgreSQL results in the table are **inferred from authoritative conversion/rounding behavior; no live PostgreSQL query was executed**. Its float8_numeric conversion uses DBL_DIG before numeric rounding, and numeric rounding breaks ties away from zero. See [PostgreSQL conversion implementation](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/backend/utils/adt/numeric.c) and [PostgreSQL numeric ROUND semantics](https://www.postgresql.org/docs/16/functions-math.html).

Both accepted parsed filters below return FALSE on the current-source harness at the corresponding input values; PostgreSQL command generation retains ROUND with the numeric cast:

```text
eq(round(number,28),0.0000000000000000000000000001)
eq(round(number,27),0.000000000000000000000000001)
```

A below-midpoint control, 4.99999999999999e-29 at 28 places, correctly returns zero; 1.499e-27 at 28 places correctly returns 1.5e-27. The reported 30/31-place cases and earlier precision/subnormal controls remain fixed on the same harnesses. The current tests add the previously reported high-precision examples but do not exercise these tiny inputs at precision 27/28.

This is a **pre-existing remaining path in N32**, not a regression in the new 15-digit scaled algorithm. Avoid quantizing the whole input to decimal before the requested rounding. Use the same faithful significand/exponent representation, or an equivalent guarded path that preserves the required digits, for these small values too. Add positive/negative half-step and double-rounding cases at the decimal scale boundary, together with the passing controls above. The reviewed N26/N30 fixes are satisfactory; N32 remains the only open code finding in this pass.

### Validation and review limits for the twelfth recheck

Both complete Release non-integration solution legs were independently rerun:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0 --no-restore --filter "Category!=Integration" --nologo -v minimal
dotnet test Expresso.slnx -c Release -f net48 --no-restore --filter "Category!=Integration" --nologo -v minimal
```

| Suite | net6.0 leg | net48 leg |
| --- | ---: | ---: |
| Core | 610 | 582 |
| Parsing | 155 | 148 |
| LINQ | 337 | 337 |
| EF Core (net8.0 in both legs) | 509 | 509 |
| EF6 (net48 in both legs) | 258 | 258 |
| Six SQL dialect suites | 154 | 154 |
| Integration-project assertion/helper unit tests | 13 | 15 |
| **Total executions** | **2,036** | **2,003** |

**4,039 passed; zero failed or skipped.** This counts repeated EF Core/EF6 executions across legs. The net48 runner printed SDK extension-loading warnings but every selected suite passed. Newly reproduced scale-boundary cases were checked in temporary review harnesses, not added to repository tests.

The modern harness compiled current Core/Parsing/LINQ/EF Core and ADO SQL Server source, generated commands with installed SQL Server/PostgreSQL/MySQL/SQLite/Oracle providers, and executed compiled delegates plus native SQLite scalar SELECTs on disposable in-memory connections. The EF6 harness used freshly built renderer assemblies, fixed manifest tokens and disabled initializers. A separate temporary net6.0 harness executed the ROUND helper, compiled equality and sort keys against the actual net6.0 library assemblies. No external database connections were opened.

Live integration, sample endpoint execution, a separate full solution build and coverage measurement were not performed. DB2 runtime probing was not repeated; source registrations and its passing unit suite were checked. No sample hosts, schema/seed scripts or real-server DDL/DML were run. **Only this review report was changed in the repository by this recheck.**

Fresh evidence: [TwelfthRecheck-current-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/TwelfthRecheck-current-output.txt), [Ef6TwelfthRecheck-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/Ef6TwelfthRecheck-output.txt), [TwelfthNet6Round-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/TwelfthNet6Round/TwelfthNet6Round-output.txt). Earlier probe labels identify prior controls freshly rerun against current implementation.

## Thirteenth remediation recheck — 2026-10-06

**Superseded:** the [fourteenth recheck](#fourteenth-remediation-recheck--2026-10-06) reopens N32. This section records the earlier conclusion from the limited sample.

Reviewed HEAD `15e000f` plus the current working-tree remediation. **Decision: close the code review. N32's final reported cases are fixed, and no additional outstanding code issue was found in this recheck.** This conclusion covers the inspected implementation and the checks below; it does not claim that all provider behavior has been reconfirmed against live engines. This was a review-only pass, and only this report was edited in the repository.

### N32 closure evidence

The dispatch now sends magnitudes below 1e-14 directly to the 15-digit significand path, before any whole-value decimal cast. A 15-digit significand at or above that cutoff fits decimal's scale-28 bound, so the cutoff addresses the premature scale reduction identified in the twelfth recheck. See [Round dispatch:55](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:55) and [RoundScaled:95](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:95).

Fresh helper, compiled equality and sort-key checks all agree on **net6.0**, **.NET Framework 4.8**, and the current-source modern harness:

| Input | Precision | Verified result |
| --- | ---: | --- |
| 5e-29 | 28 | 1e-28 |
| -5e-29 | 28 | -1e-28 |
| 1.499e-27 | 27 | 1e-27 |
| -1.499e-27 | 27 | -1e-27 |
| 4.99999999999999e-29 | 28 | 0 |
| 1.499e-27 | 28 | 1.5e-27 |

The accepted parsed equality filters from the twelfth recheck now return TRUE at the relevant values. The nearby above-midpoint and 1.499e-26 controls also pass in the modern harness. Earlier high-precision midpoint/conversion examples, maximum precision, smallest/nearby subnormals, large negative precision and ordinary midpoint cases remain correct.

To check the new boundary, a separate integer-arithmetic model rounds a 15-digit coefficient at the requested precision, without casting the whole input to decimal. A sweep of **24 magnitudes × 2 signs × 23 precisions = 1,104 comparisons per runtime** produced **zero mismatches on net6.0 and zero on .NET Framework 4.8**. It includes the cutoff and its immediately adjacent representable doubles, values around the upper decimal-path bound of 7.9e27, ordinary midpoint inputs, inputs with more than 15 significant digits, negative precision and int maximum precision.

This model is a local semantic check, not live PostgreSQL differential execution. Its 15-digit conversion rule is based on float8_numeric, and its rounding follows the numeric rounding rule already used by the feature. See [PostgreSQL numeric conversion implementation](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/backend/utils/adt/numeric.c). The bounded sweep is additional evidence, not an exhaustive claim about every double.

### Related regression checks

Fresh EF Core commands still retain literal integer ROUND on PostgreSQL, SQLite, MySQL and Oracle; PostgreSQL integer and nullable-integer compositions retain the required numeric cast. SQL Server's integer ROUND branch remains integer, and both EF profiles retain ROUND under isnull and its negation, including nullable inputs and the maximum-int literal. N26/N29/N30 therefore remain closed for the reviewed cases.

The modern and EF6 harnesses also reran selected earlier POWER domain/range, integer numeric composition, null-consumer, arithmetic range, literal SQL arithmetic, SQL Server bracket escaping, Oracle REPLACE and SQLite right controls. No further finding was reproduced in those controls. Native engine results and SQL optimizer behavior were not exercised through an external database connection.

### Validation and remaining limits

Both full Release non-integration solution legs were independently rerun successfully:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0 --no-restore --filter "Category!=Integration" --nologo -v minimal
dotnet test Expresso.slnx -c Release -f net48 --no-restore --filter "Category!=Integration" --nologo -v minimal
```

| Suite | net6.0 leg | net48 leg |
| --- | ---: | ---: |
| Core | 610 | 582 |
| Parsing | 155 | 148 |
| LINQ | 337 | 337 |
| EF Core (net8.0 in both legs) | 509 | 509 |
| EF6 (net48 in both legs) | 258 | 258 |
| Six SQL dialect suites | 154 | 154 |
| Integration-project assertion/helper unit tests | 13 | 15 |
| **Total executions** | **2,036** | **2,003** |

**4,039 passed; zero failed or skipped.** Repeated EF Core/EF6 executions across legs are included in that total. The 2,208 extra ROUND comparisons are separate temporary-harness checks, not additional repository unit tests. The net48 runner printed the existing SDK extension-loading warnings but completed successfully.

The net6.0 harness used the freshly built net6.0 Core/LINQ assemblies; the Framework/EF6 harness used freshly built renderer assemblies, fixed provider manifest tokens and disabled initializers. The modern harness compiled current source, generated commands for installed SQL Server/PostgreSQL/MySQL/SQLite/Oracle providers, and executed compiled delegates and disposable SQLite scalar SELECTs. No external database connections were opened.

Live integration tests, sample endpoint execution, a separate full solution build and coverage measurement were not repeated. DB2 runtime probing was not repeated; its source registration and passing unit suite remain the available evidence in this pass. No sample hosts, schema/seed scripts or real-server DDL/DML were run. These are retained validation limits rather than unresolved code findings; this closure does not certify a fresh live-engine parity run. **No implementation or test changes were made by this review.**

Fresh evidence: [ThirteenthRecheck-current-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/ThirteenthRecheck-current-output.txt), [Ef6ThirteenthRecheck-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/Ef6ThirteenthRecheck-output.txt), [ThirteenthNet6Round-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/TwelfthNet6Round/ThirteenthNet6Round-output.txt). Earlier probe labels identify prior controls freshly rerun against current implementation.

## Fourteenth remediation recheck — 2026-10-06

**Superseded:** the [fifteenth recheck](#fifteenth-remediation-recheck--2026-10-06) confirms that the adjacent-double discrepancy below is fixed. N32 remains open for the runtime-dependent initial significand conversion documented there.

Reviewed HEAD `15e000f` plus the current working-tree remediation, including a broader deterministic sample of the remaining ROUND fast path. **One P2 finding is reopened: N32. The thirteenth recheck's closure was premature.** Its selected 24-magnitude matrix still passes, but it missed other significands for which conversion back from decimal produces a different double. This is an existing remaining conversion path; the cutoff fix continues to repair its reported inputs. No new finding IDs are introduced, and only this review report was edited in the repository.

### N32 — P2, reopened: decimal and significand ROUND paths return adjacent doubles for the same decimal result

**Locations:** [fast-path dispatch:58](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:58), [RoundDecimal result conversion:83](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:83), [negative-precision result conversion:87](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:87), [RoundScaled result parsing:134](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:134).

The new cutoff prevents premature decimal-scale rounding for smaller magnitudes. Above that cutoff, the fast path still converts the rounded decimal directly to double, while RoundScaled parses the rounded significand/exponent text. These conversions can yield adjacent representable doubles. The remaining issue affects equality filters even when no further decimal rounding is required.

For a double field `number = 2.3490724761267527e-14`, the 15-digit decimal representation is `2.34907247612675e-14`. It has exactly 28 fractional places, so rounding it at 28 and 29 places should retain the same value. The current results are:

| Operation | Current in-memory result |
| --- | --- |
| ROUND at 28 places, decimal fast path | 2.3490724761267502e-14 |
| ROUND at 29 places, significand path | 2.34907247612675e-14 |
| Equality between the two results | FALSE |
| Equality of the 28-place result to 2.34907247612675e-14 | FALSE |

The two doubles differ by one representable step. **Helper calls, compiled equality and sort-key results were reproduced on freshly built net6.0 assemblies and on .NET Framework 4.8.** The negative counterpart reproduces the same discrepancy. A second positive input, 5.346184712902729e-14, also fails the paired-precision equality on net6.0.

These accepted parsed filters were independently executed in the current-source modern harness and both return FALSE for the first input:

```text
eq(round(number,28),round(number,29))
eq(round(number,28),0.0000000000000234907247612675)
```

EF Core PostgreSQL command generation retains the two native numeric operations:

```sql
ROUND(r."Number"::numeric, @digits28) = ROUND(r."Number"::numeric, @digits29)
```

The expected PostgreSQL equality is **inferred from its primary implementation, not a live query**. float8_numeric keeps 15 significant digits; the resulting decimal above is unchanged by either precision. numeric_float8 converts a numeric result through its textual form and float8in. See [PostgreSQL numeric conversion and rounding implementation](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/backend/utils/adt/numeric.c). The local paired-precision failure itself is reproduced and does not depend on a model's mismatch count.

The thirteenth recheck's **1,104 selected comparisons per runtime still report zero mismatches**. An expanded deterministic sample used 50 significands at each exponent from -14 through 27, two signs, and eight precision selections. Its **33,600 comparison executions per runtime** found **1,442 exact-result mismatches on net6.0 and 1,506 on .NET Framework 4.8** against the integer-arithmetic 15-digit reference model. Counts include repeats where a generated precision equals a fixed precision. These are local model comparisons, not counts of live database failures; the concrete accepted filter above establishes the open finding.

Make the return conversion consistent with the faithful decimal/significand representation on both paths, rather than letting a direct decimal-to-double cast select a different result. The existing textual conversion path is one local option; reusing the significand algorithm also avoids two conversion policies. Keep exact equality semantics and add the paired-precision filter, a literal comparison, positive/negative counterparts and ordinary-value controls. Extend validation across varied significands, retaining the repaired tiny inputs, subnormals and precision endpoints. A tolerance in the tests would hide an observable filter-result discrepancy.

### Fixes that remain confirmed

The four reported tiny-input cases and their two controls still pass helper/equality/sort checks on both runtime legs. Earlier ROUND high-precision midpoint, maximum precision and subnormal controls also pass. The modern and Framework harnesses freshly reran the existing provider command controls: literal and integer-column ROUND translations, SQL Server integer POWER/FLOOR/CEILING/ROUND compositions, ROUND under null consumers, POWER domain/range, arithmetic range, Oracle REPLACE, SQL Server bracket escaping and SQLite right. No additional finding was reproduced in those selected controls. N26/N29/N30 remain closed for the reviewed cases.

### Validation and review limits

Both full Release non-integration solution legs were independently rerun:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0 --no-restore --filter "Category!=Integration" --nologo -v minimal
dotnet test Expresso.slnx -c Release -f net48 --no-restore --filter "Category!=Integration" --nologo -v minimal
```

| Suite | net6.0 leg | net48 leg |
| --- | ---: | ---: |
| Core | 610 | 582 |
| Parsing | 155 | 148 |
| LINQ | 337 | 337 |
| EF Core (net8.0 in both legs) | 509 | 509 |
| EF6 (net48 in both legs) | 258 | 258 |
| Six SQL dialect suites | 154 | 154 |
| Integration-project assertion/helper unit tests | 13 | 15 |
| **Total executions** | **2,036** | **2,003** |

**4,039 passed; zero failed or skipped.** This includes repeated EF Core/EF6 executions across legs. Temporary-harness model checks are separate from the repository unit-test totals. The net48 runner printed the existing SDK extension-loading warnings and completed successfully.

The net6.0 and Framework probes used freshly built library assemblies; the modern harness compiled current source and generated commands with installed SQL Server/PostgreSQL/MySQL/SQLite/Oracle providers. Provider manifest tokens were fixed and EF6 initializers disabled. Native SQLite scalar checks used disposable in-memory connections. No external database connections were opened.

Live database integration, sample endpoints, a separate full solution build, DB2 runtime probing and coverage measurement were not repeated. No sample hosts, schema/seed scripts or real-server DDL/DML were run. **No implementation, test, configuration, plan or status-document changes were made by this review.**

Fresh evidence: [FourteenthRecheck-current-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/FourteenthRecheck-current-output.txt), [Ef6FourteenthRecheck-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/Ef6FourteenthRecheck-output.txt), [FourteenthNet6Round-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/TwelfthNet6Round/FourteenthNet6Round-output.txt). Earlier probe labels identify prior controls freshly rerun against the current implementation.

## Fifteenth remediation recheck — 2026-10-06

**Superseded:** the [sixteenth recheck](#sixteenth-remediation-recheck--2026-10-06) confirms the integer-midpoint normalization fix. N32 remains open for the final text-to-double conversion documented there.

Reviewed HEAD `15e000f` plus the current working-tree remediation. **The reported direct decimal-to-double conversion defect is fixed. One P2 remains under N32: runtime-dependent midpoint rounding in the initial 15-digit normalization.** No new finding IDs are introduced. Only this review report was edited in the repository.

### Confirmed N32 repair

`RoundDecimal` and its direct result casts are removed. Rounded results now use `RoundScaled` and its textual result conversion, with an early zero result for the very coarse-precision shortcut. On freshly built net6.0 libraries and .NET Framework 4.8, the reported positive/negative `2.3490724761267527e-14` cases and `5.346184712902729e-14` now retain the same result at 28 and 29 places. Helper calls, compiled equality and sort keys pass. The accepted parsed paired-precision and literal-comparison filters also return TRUE in the current-source harness. Ordinary `round(2.5)` still returns `3`.

Both previous review samples now have **zero mismatches on both runtimes**: the 1,104 selected comparisons and 33,600 deterministic comparison executions. The repaired tiny-input cases, high-precision midpoint cases, precision endpoints and subnormal controls also pass. These sample models reuse the runtime's `E14` conversion, so they check subsequent rounding and return conversion but cannot independently validate the initial conversion's midpoint policy. The next finding uses exactly representable integers and an independent integer-arithmetic expectation.

### N32 — P2, still open: E14 normalization rounds exact midpoints differently on net48

**Location:** [RoundScaled initial conversion:74](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.cs:74).

`Math.Abs(value).ToString("E14", culture)` delegates the initial 15-significant-digit rounding to the executing framework. At an exact midpoint, .NET Framework rounds away from zero, while modern .NET rounds to an even last digit. Parsing that text back consistently cannot recover the digit already selected by the formatter. Microsoft documents this runtime difference in [standard numeric format strings](https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings).

For the exactly representable double `1000000000000005`, that conversion alone produces `1000000000000010` on net48 and `1000000000000000` on net6.0. Requested decimal precision `0` or `28` leaves either normalized integer unchanged. Fresh helper, parsed-filter and compiled sort-key probes reproduce:

| Input double | net6.0 ROUND at 0 or 28 places | net48 ROUND at 0 or 28 places |
| --- | --- | --- |
| 1000000000000005 | 1000000000000000 | 1000000000000010 |
| -1000000000000005 | -1000000000000000 | -1000000000000010 |
| 1125899906842625 | 1125899906842620 | 1125899906842630 |
| -1125899906842625 | -1125899906842620 | -1125899906842630 |

For `number = 1000000000000005`, both accepted parsed filters below return **FALSE on net48 and TRUE on net6.0**:

```text
eq(round(number,0),1000000000000000.0)
eq(round(number,28),1000000000000000.0)
```

The negative counterparts and the second magnitude reproduce the mismatch. Both runtimes agree on the neighboring non-midpoints `1000000000000004` and `1000000000000006`, and on `1125899906842635`, whose retained last digit is odd and therefore rounds upward under both policies. This is a conversion-stage midpoint issue; the requested SQL numeric ROUND must continue rounding half away from zero, including `round(2.5) = 3`.

The documented PostgreSQL reference includes the Linux `postgres:16` engine in [docker-compose.it.yml:9](C:/Users/itorg/source/repos/Expresso/docker/docker-compose.it.yml:9). PostgreSQL's [float8_numeric implementation](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/backend/utils/adt/numeric.c) formats the double with `%.*g` and `DBL_DIG` before constructing the numeric value. Its [formatter](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/port/snprintf.c) delegates floating conversion to the C library. The [glibc formatter](https://raw.githubusercontent.com/bminor/glibc/glibc-2.36/stdio-common/printf_fp.c) considers the retained digit's parity and current rounding mode; the default is nearest-even, as described in the [GNU rounding documentation](https://www.gnu.org/software/c-intro-and-ref/manual/html_node/Rounding.html). Consequently, the expected Linux PostgreSQL numeric value for the first input is `1000000000000000`, which ROUND at either precision preserves. **That database result is inferred from primary implementations; no PostgreSQL query was run.**

A local Ubuntu `/usr/bin/printf '%.15g\n'` check independently corroborated all seven positive/negative midpoint and neighboring-control conversions, producing `1e+15` for the first input. These test integers are exactly representable, and the expectation was also calculated by integer quotient/remainder with ties to even, without `E14`. An isolated Windows CRT formatting check used a different midpoint policy; no universal claim is made about every PostgreSQL host's C library. The demonstrated discrepancy concerns the specified PostgreSQL reference on Linux and the supported net48 in-memory renderer.

**Remediation:** make the initial 15-digit conversion policy explicit and consistent across supported TFMs, preserving the exact input until the intended significand rounding is applied. Keep the subsequent requested decimal ROUND half away from zero and the repaired textual result conversion. Add positive/negative even-last-digit midpoints, odd-last-digit and neighboring-value controls, helper calls, accepted equality filters and sort keys on both test legs. Do not derive the expected first-stage result with the same runtime formatter as the implementation.

### Other fixes and validation

Existing provider command controls were freshly rerun in the modern and Framework harnesses: literal/integer ROUND, SQL Server integer POWER/FLOOR/CEILING/ROUND compositions and null consumers, POWER and arithmetic domain/range, Oracle REPLACE, SQL Server bracket escaping and SQLite right. No additional finding was reproduced in the selected controls. Documented EF6 PostgreSQL ROUND rejection remains an expected gap. N26/N29/N30 remain closed for the reviewed cases.

Both full Release non-integration solution legs passed:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0 --no-restore --filter "Category!=Integration" --nologo -v minimal
dotnet test Expresso.slnx -c Release -f net48 --no-restore --filter "Category!=Integration" --nologo -v minimal
```

| Suite | net6.0 leg | net48 leg |
| --- | ---: | ---: |
| Core | 610 | 582 |
| Parsing | 155 | 148 |
| LINQ | 345 | 345 |
| EF Core (net8.0 in both legs) | 509 | 509 |
| EF6 (net48 in both legs) | 258 | 258 |
| Six SQL dialect suites | 154 | 154 |
| Integration-project assertion/helper unit tests | 13 | 15 |
| **Total executions** | **2,044** | **2,011** |

**4,055 passed; zero failed or skipped.** Temporary probe executions are separate from those totals. The net48 runner printed its existing SDK extension-loading warnings and completed successfully. The new repository regressions cover the repaired neighboring-double examples and unchanged 15-digit values, but not exact midpoint input normalization.

The net6.0 and Framework probes used freshly built local libraries. The modern harness compiled current source and generated SQL with installed SQL Server/PostgreSQL/MySQL/SQLite/Oracle providers; EF6 used fixed manifests and disabled initializers. SQLite scalar checks used disposable in-memory connections. No external database connections were opened, and no sample hosts or database schema/seed scripts were run. Live integration and sample endpoints, DB2 runtime probing, a separate full solution build and coverage measurement were not repeated. **No implementation, test, configuration, plan or status-document changes were made by this review.**

Fresh evidence: [FifteenthRecheck-current-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/FifteenthRecheck-current-output.txt), [FifteenthNet6Round-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/TwelfthNet6Round/FifteenthNet6Round-output.txt), [Ef6FifteenthRecheck-output.txt](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/Ef6FifteenthRecheck-output.txt), [net6.0 midpoint probes](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/TwelfthNet6Round/FifteenthTieRound-output.txt), [net48 midpoint probes](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/FifteenthTieRound-output.txt), [Linux formatter controls](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/FifteenthLinuxFormatter-output.txt).

## Sixteenth remediation recheck — 2026-10-06

**Superseded:** the [seventeenth recheck](#seventeenth-remediation-recheck--2026-10-06) confirms both final-conversion repairs and closes N32. The failures below describe the earlier implementation.

Reviewed HEAD `15e000f` plus the current working-tree remediation, including the new `ExpressoFunctions.Round.cs` partial. **The reported N32 normalization fix is correct for the reproduced cases. One P2 remains under N32: final conversion back to double still has precision and range discrepancies.** No new finding IDs are introduced. Only this review report was edited in the repository.

### Confirmed repair

`FifteenDigits` reconstructs the exact IEEE significand and binary exponent, corrects the estimated decimal magnitude with exact comparisons, and applies quotient/remainder ties-to-even rounding. The requested decimal precision still uses half-away-from-zero rounding. The existing textual result conversion remains at [RoundScaled:71](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.Round.cs:71).

The four reported midpoint inputs, their negative counterparts, odd-last-digit and neighboring-value controls now agree on freshly built net6.0 libraries and .NET Framework 4.8. Helper calls, accepted parsed equality filters and sort keys all pass. `round(2.5)` remains `3`. The earlier paired-precision and tiny-input repairs also pass the dedicated and provider harness controls.

### N32 — P2, still open: final result parsing selects an adjacent double on net48 and mishandles finite overflow

**Location:** [RoundScaled result conversion:71](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.Round.cs:71).

**A. Valid finite result, net48 precision discrepancy.** For the exact input double represented by the C# literal `1e-106`, the new first stage produces the correct decimal value. Rounding at 300 places retains that value, but `double.Parse(body, culture)` returns the next representable double on .NET Framework 4.8. The return conversion therefore changes an accepted field-to-field equality:

```text
eq(round(number,300),number)
```

With `number = 1e-106`, this returns **FALSE on net48 and TRUE on net6.0**. The negative counterpart also returns FALSE on net48. Fresh helper and compiled sort-key probes show:

| Value | G17 representation | IEEE bits |
| --- | --- | --- |
| Input double on both runtimes | 9.9999999999999994e-107 | 29ED5B561574765B |
| net6.0 ROUND result and sort key | 9.9999999999999994e-107 | 29ED5B561574765B |
| net48 ROUND result and sort key | 1.0000000000000001e-106 | 29ED5B561574765C |

This comparison uses the original binary column value rather than a parsed numeric comparison literal, avoiding a shared parser conversion masking the discrepancy. The expected result was computed independently with exact decimal arithmetic and corroborated by local Linux `strtod("1E-106")`, which returned bits `29ED5B561574765B` with no range error. The negative conversion returned the corresponding signed bits.

Current EF Core PostgreSQL command generation retains the native numeric ROUND followed by the comparison with the double column:

```sql
-- precision parameter = 300
ROUND(r."Number"::numeric, @digits) = r."Number"
```

PostgreSQL allows an implicit numeric-to-float8 conversion for this comparison, as shown by its [cast catalog](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/include/catalog/pg_cast.dat). Its [numeric_float8 implementation](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/backend/utils/adt/numeric.c) converts the numeric text through float8in; the [float8 input implementation](https://raw.githubusercontent.com/postgres/postgres/REL_16_STABLE/src/backend/utils/adt/float.c) uses `strtod`. Thus the expected Linux PostgreSQL equality is TRUE. **This database result is inferred from primary implementations and native conversion checks, not a live database query.**

**B. Finite result outside double range, inconsistent failure behavior.** For `double.MaxValue` (`1.7976931348623157e308`), the 15-digit normalization is `1.79769313486232e308`, beyond the finite double range. ROUND at precision 0 or 300 retains it. The current return conversion gives:

| Operation | net6.0 | net48 |
| --- | --- | --- |
| Helper result for positive maximum input | PositiveInfinity | OverflowException |
| `eq(round(number,300),number)` | FALSE | OverflowException |
| Compiled sort key | PositiveInfinity | OverflowException inside DynamicInvoke's wrapper |

The negative maximum reproduces negative infinity versus an exception. A finite input silently becomes an infinite helper/key value on net6.0, while net48 throws an exception outside the existing in-memory `NotSupportedException` domain/range classification.

For the native PostgreSQL comparison above, converting the finite numeric ROUND result to float8 should raise a range error. The native Linux conversion of `1.79769313486232E+308` returned infinity with `errno = ERANGE`; PostgreSQL's linked float8 input implementation rejects that outcome for finite numeric text. **The database range failure is inferred, not observed on an engine.** ROUND returning numeric by itself can retain the value; the expected range error here concerns its coercion for comparison with the float8 column. Native numeric sorting was not executed, and no claim is made that it throws the same error.

**Remediation:** preserve the correctly rounded decimal coefficient/exponent when converting to double, with a conversion that selects the same finite binary result on both supported runtimes. An invariant culture alone does not make the older Framework parser correctly rounded. Handle finite values outside the supported result range explicitly and consistently with the profile's range-error contract instead of returning infinity or leaking a raw parser exception. Add positive/negative `1e-106` helper, field-to-field equality and sort-key regressions, plus maximum-value comparison/range cases. Retain the repaired midpoint, tiny-input and subnormal controls. If a native numeric result cannot be represented by the in-memory double API for a particular consumer, document that limit explicitly.

### Independent broad validation

A new reference sample avoids the executing framework's numeric formatter and parser when generating expected results. Python `Decimal.from_float` captures the exact binary input; a 1,200-digit context normalizes to 15 digits with ties to even, then applies requested precision with half-away-from-zero rounding. Expected finite outputs are saved as IEEE bit patterns, consumed by both runtime probes with `BitConverter`.

The deterministic dataset has **6,810 distinct input values**, comprising selected regression inputs, powers of ten and their adjacent doubles from decimal exponent -323 through 308, signed counterparts, and 1,500 seeded random positive binary patterns plus their negatives. Each value has 25 precision selections, including `int.MinValue`/`int.MaxValue`, magnitude-relative boundaries and large positive/negative precisions. There are **170,250 comparison executions per runtime**, including repeated precision selections. **170,231 expect finite double results; 19 exceed double range and are recorded separately.**

| Reference category | net6.0 | net48 |
| --- | ---: | ---: |
| Finite-result executions | 170,231 | 170,231 |
| Exact-bit mismatches | 0 | 618 |
| Unexpected exceptions for finite expected results | 0 | 0 |
| Recorded outside-double-range cases | 19 return infinity | 19 throw OverflowException |

These are local reference-model counts, not counts of live database failures or distinct defects. The accepted `1e-106` filter and native conversion check independently establish the finite-result finding; the maximum-value probe establishes the observed range behavior. This sample also confirms that the fixed first-stage midpoint policy agrees with the independent expectation on the reported cases.

### Other controls, tests and limits

Existing provider command controls were freshly rerun against current source/libraries: literal/integer ROUND, SQL Server integer POWER/FLOOR/CEILING/ROUND compositions and null consumers, POWER and arithmetic domain/range, Oracle REPLACE, SQL Server bracket escaping and SQLite right. No additional finding was reproduced in those selected controls. EF6 PostgreSQL ROUND remains a documented rejection. N26/N29/N30 remain closed for the reviewed cases.

Both full Release non-integration solution legs passed:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0 --no-restore --filter "Category!=Integration" --nologo -v minimal
dotnet test Expresso.slnx -c Release -f net48 --no-restore --filter "Category!=Integration" --nologo -v minimal
```

| Suite | net6.0 leg | net48 leg |
| --- | ---: | ---: |
| Core | 610 | 582 |
| Parsing | 155 | 148 |
| LINQ | 353 | 353 |
| EF Core (net8.0 in both legs) | 509 | 509 |
| EF6 (net48 in both legs) | 258 | 258 |
| Six SQL dialect suites | 154 | 154 |
| Integration-project assertion/helper unit tests | 13 | 15 |
| **Total executions** | **2,052** | **2,019** |

**4,071 passed; zero failed or skipped.** Temporary reference-model executions are separate from the repository totals. The net48 runner printed the existing SDK extension-loading warnings and completed successfully. The new repository midpoint tests pass on both runtimes.

Fresh net6.0 and Framework probes used freshly built local library assemblies. The modern harness compiled current source and generated SQL with installed SQL Server/PostgreSQL/MySQL/SQLite/Oracle providers. EF6 used fixed manifests and disabled initializers; SQLite scalar probes used disposable in-memory connections. Local Linux probes exercised only numeric conversion. No external database connections were opened, no sample hosts were started, and no database schema/seed scripts or real-server DDL/DML were run. Live integration/sample endpoints, DB2 runtime probing, a separate full solution build and coverage measurement were not repeated. **No implementation, test, configuration, plan or status-document changes were made by this review.**

Fresh evidence: [net6.0 boundary filters and keys](C:/Users/itorg/AppData/Local/Temp/Expresso-RoundReview-20261006-16/net6/Boundary-output.txt), [net48 boundary filters and keys](C:/Users/itorg/AppData/Local/Temp/Expresso-RoundReview-20261006-16/net48/Boundary-output.txt), [net6.0 broad reference results](C:/Users/itorg/AppData/Local/Temp/Expresso-RoundReview-20261006-16/net6/Wide-output.txt), [net48 broad reference results](C:/Users/itorg/AppData/Local/Temp/Expresso-RoundReview-20261006-16/net48/Wide-output.txt), [independent reference generator](C:/Users/itorg/AppData/Local/Temp/Expresso-RoundReview-20261006-16/WriteRoundReference.py), [native Linux conversions](C:/Users/itorg/AppData/Local/Temp/Expresso-RoundReview-20261006-16/LinuxParse-output.txt), [current provider commands](C:/Users/itorg/AppData/Local/Temp/Expresso-LinqReview-20261004/SixteenthRecheck-current-output.txt), [current Framework commands](C:/Users/itorg/AppData/Local/Temp/Expresso-Ef6ReviewProbe-20261005/Ef6SixteenthRecheck-output.txt).

## Seventeenth remediation recheck — 2026-10-06

**Subsequent changes:** the [eighteenth recheck](#eighteenth-remediation-recheck--2026-10-07) reviews later integration remediation and opens N33/N34. The N32 repair and closure below remain valid; this section describes the code before those additional changes.

Reviewed HEAD `15e000f` plus the current working-tree remediation, specifically the final conversion in `ExpressoFunctions.Round.Convert.cs`, its caller, the in-memory renderer path, regression tests and the updated round documentation. **N32 is closed. No new findings were identified, and no outstanding code-review findings remain from the preceding rechecks. The review can be closed with the validation limits below.** Only this report was changed in the repository.

### N32 closure evidence

The [new conversion](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.Linq/ExpressoFunctions.Round.Convert.cs:9) reconstructs the decimal coefficient and scale, compares the resulting rational value against exact powers of two, and rounds its binary significand using quotient/remainder ties to even. It handles the subnormal representation and a carry into the next exponent explicitly. The logarithm supplies an estimate only; exact comparisons correct it before conversion. The final result is constructed with `BitConverter`, without the runtime numeric parser. The existing exact-binary 15-digit normalization and requested half-away-from-zero rounding remain intact.

Freshly built library assemblies were copied to isolated net6.0 and net48 probe directories; their hashes match the current build outputs. The prior failures now agree on both runtimes:

| Case | net6.0 and net48 result |
| --- | --- |
| `round(1e-106,300)` | Original bits `29ED5B561574765B` |
| `round(-1e-106,300)` | Original bits `A9ED5B561574765B` |
| `eq(round(number,300),number)` for either input | TRUE |
| Compiled sort key at precision 300 for either input | Same bits as the input |
| `round(double.MaxValue,0)` and precision 300 | `NotSupportedException: value out of range: overflow` |
| Corresponding negative maximum | Same exception and message |
| Maximum-value field comparison and sorting | Consistent range failure |

The direct sort-key probe uses `DynamicInvoke`, which wraps the underlying exception in `TargetInvocationException`. The repository's actual `OrderBy` regression also passes with the expected range-error classification. The finite result limitation is now stated on the [round function page](C:/Users/itorg/source/repos/Expresso/docs/functions/arithmetic/round.md:76). This documents the double-valued in-memory API; it does not assert that native PostgreSQL numeric sorting must reject an unrepresentable double result.

Previously reported integer normalization midpoints, neighboring integer controls, the paired precisions 28/29, positive/negative tiny inputs, subnormals, extreme integer precisions and `round(2.5) = 3` pass the repository suites and selected compiled helper/filter/key probes.

### Independent reference validation

The unchanged broad reference dataset from the sixteenth recheck was rerun against the fresh assemblies. Expected values were generated independently from exact binary inputs using Python `Decimal.from_float`: 15 significant digits with ties to even, requested decimal precision with half-away-from-zero rounding, then expected binary bits. The executing .NET runtime's formatter and parser are not used to generate expectations.

The broad dataset contains **6,810 input values and 25 precision selections: 170,250 executions per runtime**, including repeated precisions. A separate focused dataset contains **348 signed inputs and 16 precisions: 5,568 executions per runtime**. It covers the first 64 positive subnormals and their negatives, values on both sides of the smallest normal double, the largest finite doubles and their negatives, previously reported conversion cases, and binary midpoint controls. **120 finite executions in the focused dataset have exact midpoint results between neighboring doubles**, explicitly exercising final ties-to-even conversion.

| Dataset/result | net6.0 | net48 |
| --- | ---: | ---: |
| Broad finite-result executions | 170,231 | 170,231 |
| Broad exact-bit mismatches / unexpected exceptions | 0 / 0 | 0 / 0 |
| Broad expected range failures with exact exception/message | 19 / 19 | 19 / 19 |
| Focused finite-result executions | 5,414 | 5,414 |
| Focused exact-bit mismatches / unexpected exceptions | 0 / 0 | 0 / 0 |
| Focused expected range failures with exact exception/message | 154 / 154 | 154 / 154 |

These are local model comparisons, not live database executions or unique-case counts across datasets. The range-output records were checked for the exact `NotSupportedException: value out of range: overflow` classification; none returned infinity or a different exception.

### Repository tests and scope

Both full Release non-integration solution legs passed:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0 --no-restore --filter "Category!=Integration" --nologo -v minimal
dotnet test Expresso.slnx -c Release -f net48 --no-restore --filter "Category!=Integration" --nologo -v minimal
```

| Suite | net6.0 leg | net48 leg |
| --- | ---: | ---: |
| Core | 610 | 582 |
| Parsing | 155 | 148 |
| LINQ | 358 | 358 |
| EF Core (net8.0 in both legs) | 509 | 509 |
| EF6 (net48 in both legs) | 258 | 258 |
| Six SQL dialect suites | 154 | 154 |
| Integration-project assertion/helper unit tests | 13 | 15 |
| **Total executions** | **2,057** | **2,024** |

**4,081 passed; zero failed or skipped.** The Framework runner printed its existing SDK extension-loading warnings and completed successfully. Temporary reference-model executions are separate from these totals.

The current recheck focuses on the remaining conversion finding and its surrounding paths. The EF Core, EF6 and dialect unit suites were rerun; the earlier standalone provider command harnesses and sample flows were not repeated in this round. No external database connections were opened, sample hosts started, integration-category tests run, or database scripts/real-server DDL/DML executed. DB2 runtime probing and coverage measurement were not performed. Closure means the identified code-review findings are resolved; it does not establish fresh live-engine parity for all providers. No implementation, test, configuration, plan or status-document changes were made by this review.

Fresh evidence: [net6.0 broad reference results](C:/Users/itorg/AppData/Local/Temp/Expresso-RoundReview-20261006-17/net6/Wide-output.txt), [net48 broad reference results](C:/Users/itorg/AppData/Local/Temp/Expresso-RoundReview-20261006-17/net48/Wide-output.txt), [net6.0 focused conversion boundaries](C:/Users/itorg/AppData/Local/Temp/Expresso-RoundReview-20261006-17/net6/Focused-output.txt), [net48 focused conversion boundaries](C:/Users/itorg/AppData/Local/Temp/Expresso-RoundReview-20261006-17/net48/Focused-output.txt), [net6.0 field comparisons and keys](C:/Users/itorg/AppData/Local/Temp/Expresso-RoundReview-20261006-17/net6/Boundary-output.txt), [net48 field comparisons and keys](C:/Users/itorg/AppData/Local/Temp/Expresso-RoundReview-20261006-17/net48/Boundary-output.txt), [broad reference generator](C:/Users/itorg/AppData/Local/Temp/Expresso-RoundReview-20261006-17/WriteRoundReference.py), [focused reference generator](C:/Users/itorg/AppData/Local/Temp/Expresso-RoundReview-20261006-17/WriteBoundaryReference.py).

## Eighteenth remediation recheck — 2026-10-07

Reviewed the additional integration remediation in commit `9d3a67a` and the current uncommitted changes to EF6 POWER, field validation, time arithmetic, provider-gap assertions and documentation. The broader commit also contains the previously reviewed arithmetic/rounding remediations; those are not new findings in this section. **Two P2 findings remain in the additional changes. N32 stays closed.** Only this review report was changed in the repository.

### N33 — P2: SQLite EF6 flushes valid POWER results to zero

**Location:** [Ef6ExpressionToLinqTransformer.Numeric.cs:43](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.Numeric.cs:43), particularly [FlushSubnormal:50](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.Numeric.cs:50).

The mismatch at `power(0.5,1075)` is real: the installed Microsoft.Data.Sqlite engine returns 0, while System.Data.SQLite 3.46.1 returns the smallest positive subnormal. However, the workaround transforms **every nonzero result below the smallest normal double** to 0. Native SQLite preserves valid results in that range. It therefore fixes the one reported underflow case by changing other successful queries.

Accepted reproductions:

```text
gt(power(0.5,1023.0),0.0)
gt(power(0.5,1074.0),0.0)
```

For both predicates, native SQLite POWER is positive, so a row should match. Fresh EF6 command generation includes the following branch around POWER, and executing the generated predicate excludes the row:

```sql
CASE WHEN ABS(POWER(base, exponent)) > 0
          AND ABS(POWER(base, exponent)) < 2.2250738585072E-308
     THEN 0 ELSE POWER(base, exponent) END
```

| Exponent, base 0.5 | Microsoft.Data.Sqlite native bits | System.Data.SQLite native bits | New EF6 result | Generated `gt(...,0.0)` |
| --- | --- | --- | --- | --- |
| 1022 | 0010000000000000 | 0010000000000000 | Smallest normal value | Row retained |
| 1023 | 0008000000000000 | 0008000000000000 | 0 | Row lost |
| 1073 | 0000000000000002 | 0000000000000002 | 0 | Row lost |
| 1074 | 0000000000000001 | 0000000000000001 | 0 | Row lost |
| 1075 | 0000000000000000 | 0000000000000001 | 0 | Row excluded, as intended for this one input |

These values were observed by executing scalar SELECTs on disposable in-memory connections for both installed providers. The actual EF6 WHERE clause was generated from the current built library and executed on System.Data.SQLite, replacing only its table source with a one-row SELECT and supplying the captured parameters. No tables were created. For exponents 1023, 1073 and 1074 it returned no row despite the positive native result; 1022 retained the row. This is a demonstrated regression introduced by the uncommitted workaround, not another N32 rounding case.

The [SQLite math-function documentation](https://www.sqlite.org/lang_mathfunc.html) describes POWER as exponentiation and notes approximation differences; it does not define all subnormal results as zero. The new function-page description records the coercion, but that changes the locked EF6-versus-ADO parity contract.

**Remediation:** remove the blanket smallest-normal threshold. Preserve genuine nonzero subnormal results and address the provider-specific rounding discrepancy through an exact provider path; if that cannot be implemented, reject/document the unsupported combination through the existing EF6 gap policy rather than silently changing valid results. Add neighboring-boundary controls at 1022/1023 and 1073/1074/1075, with a positive-result predicate as well as the current equality-to-zero case, and check field operands and sort consumers. Update the POWER documentation with the final behavior. The added unit test currently checks only that CASE/ABS/POWER appear in SQL, so it cannot detect the observed row loss.

### N34 — P2: Oracle EF Core NUMBER conversion misses computed POWER arguments

**Location:** [ExpressoFunctionTranslations.Oracle.cs:122](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.Oracle.cs:122), specifically [AsOracleNumberLiteral:128](C:/Users/itorg/source/repos/Expresso/src/Rendering/Expresso.Rendering.EntityFrameworkCore/ExpressoFunctionTranslations.Oracle.cs:128).

The direct `power(2.0,1024.0)` translation now casts both parameters to NUMBER. A genuine BINARY_DOUBLE column also remains binary, as intended. But `AsOracleNumberLiteral` only casts a `SqlConstantExpression` or `SqlParameterExpression`. A computed operand bypasses it, leaving its double parameter leaves as BINARY_DOUBLE:

```text
gt(power(add(1.0,1.0),1024.0),0.0)
gt(power(2.0,add(1023.0,1.0)),0.0)
gt(power(mult(1.0,2.0),1024.0),0.0)
```

Fresh command generation produces these POWER shapes respectively:

```sql
POWER(:a + :b, CAST(:exponent AS NUMBER))
POWER(CAST(:base AS NUMBER), :a + :b)
POWER(:a * :b, CAST(:exponent AS NUMBER))
```

The command's `:a` and `:b` parameters were inspected without opening an Oracle connection: they have CLR type Double and `OracleDbType.BinaryDouble`. By comparison, rendering the same parsed IR through the actual ADO Oracle renderer and binding its parameters through the integration `ParameterBinder.Colon` produces `OracleDbType.Double`, the NUMBER bind used by the reported ADO failure. Thus both the arithmetic operation and one POWER operand remain binary on EF Core, despite every operand in these examples being literal-derived.

Oracle's [numeric precedence rules](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/Data-Types.html) preserve BINARY_DOUBLE through these arithmetic operations, and its [POWER documentation](https://docs.oracle.com/en/database/oracle/oracle-database/19/sqlrf/POWER.html) specifies a BINARY_DOUBLE result when either argument is binary floating point. Consequently these filters are expected to retain the original binary overflow/infinity behavior rather than the ADO NUMBER overflow failure. **The SQL and bind-type discrepancy is reproduced; the Oracle engine outcomes for these new composed cases are inferred from the documented rules and were not executed against an Oracle server.**

This is an incomplete repair of the original Oracle POWER bind-type issue, rather than a demonstrated regression in the previously fixed direct-literal or binary-column cases. A nested POWER whose inner parameters are already cast also generated the expected NUMBER composition in the selected control.

**Remediation:** retain ADO's numeric bind semantics through computed operands, including their literal/parameter leaves, while preserving genuine binary columns and expressions derived from them. Casting only the final arithmetic result is insufficient if binary arithmetic has already overflowed or underflowed inside that result. Add base-side and exponent-side arithmetic compositions to the provider/differential tests, alongside direct-literal and BINARY_DOUBLE-column controls. Do not close the Oracle issue based solely on `power(2.0,1024.0)`.

### Confirmed repairs and surrounding checks

- The extra database-code lists are still specific to domain cases. `DifferentialOutcome.AssertSame` continues to require equal ids for ordinary cases, compares complete native code values, and rejects identical unexpected/infrastructure errors. The DB2 collection fallback checks nested errors after an empty direct state; its fresh unit test passes on both framework legs. No new issue was identified in that change.
- Oracle EF Core time comparisons now create parameters with CLR TimeSpan, `DbType.Object` and `OracleDbType.IntervalDS`. Direct `time(created)` equality, a shifted result and a converted interval column all produced commands without ORA-50028 during local parameter construction. No Oracle connection was opened.
- EF6 Oracle and SQLite TimeSpan fields, including nullable fields and fields nested inside time additions, fail during rendering with the documented Edm.Time reason. DateTime additions retain their separate date-arithmetic reasons. SQL Server and PostgreSQL time expressions still build. These controls support the new early field validation without exposing an additional regression.
- The Oracle `right-trailing` gap covers the existing concat-with-VARCHAR2 ORA-12704 issue. Its database gap assertion still requires the documented native code, not an arbitrary failure. The added function documentation is consistent with that limitation.
- The earlier N32 final conversion remains closed. The new failures are in provider POWER handling; this recheck did not repeat the unchanged broad in-memory rounding matrix.

### Tests, scope and evidence limits

Both full Release non-integration solution legs passed with the current uncommitted changes:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0 --no-restore --filter "Category!=Integration" --nologo -v minimal
dotnet test Expresso.slnx -c Release -f net48 --no-restore --filter "Category!=Integration" --nologo -v minimal
```

| Suite | net6.0 leg | net48 leg |
| --- | ---: | ---: |
| Core | 610 | 582 |
| Parsing | 155 | 148 |
| LINQ | 358 | 358 |
| EF Core (net8.0 in both legs) | 510 | 510 |
| EF6 (net48 in both legs) | 263 | 263 |
| Six SQL dialect suites | 154 | 154 |
| Integration-project assertion/helper unit tests | 14 | 16 |
| **Total executions** | **2,064** | **2,031** |

**4,095 passed; zero failed or skipped.** Existing nullable/analyzer and Framework SDK extension-loading warnings were emitted. Both isolated probe builds passed against current source or freshly built local assemblies.

The user reports green full integration suites. This review independently reran the non-integration suites and local reproductions above; it did not rerun external Oracle/MySQL/MariaDB/SQL Server/DB2 integrations or sample endpoints. Oracle SQL and parameter inspection did not execute commands. SQLite checks used disposable in-memory connections and SELECTs only. No sample hosts were started, schema/seed scripts run, or external DDL/DML performed. No implementation, tests, configuration, plans or status documents were changed by this review.

Fresh evidence: [modern Oracle SQL/ADO binds and SQLite scalars](C:/Users/itorg/AppData/Local/Temp/Expresso-IntegrationFixReview-20261007-18/Modern-output.txt), [EF6 generated predicates, SQLite execution and time gaps](C:/Users/itorg/AppData/Local/Temp/Expresso-IntegrationFixReview-20261007-18/Framework-output.txt), [modern probe source](C:/Users/itorg/AppData/Local/Temp/Expresso-IntegrationFixReview-20261007-18/modern/Program.cs), [Framework probe source](C:/Users/itorg/AppData/Local/Temp/Expresso-IntegrationFixReview-20261007-18/Ef6IntegrationFixProbe.cs).

---

## N33/N34 remediation and acceptance verification — 2026-10-07

The user authorized implementation and required all unit/integration runs to succeed. Both findings are now closed in the working tree based on HEAD `a130a06`. No commit was made. The implementation sequence and decisions are recorded in [LINQREVIEWPLAN.md](LINQREVIEWPLAN.md#n33n34-execution-plan--2026-10-07).

### N33 — fixed: preserve valid SQLite EF6 subnormals

`FlushSubnormal` has been replaced with `RoundPowerUnderflow`. Results larger than the minimum subnormal remain native POWER results. At the minimum-subnormal boundary, a half-exponent POWER stays in the normal range; multiplying that result lets floating-point multiplication round zero-underflow. Exact binary halfway cases get an explicit ties-to-even correction. The SQL logarithms only locate a candidate binary exponent: an exact base identity and divisibility check verify halfway cases.

A general logarithmic underflow threshold was tried and rejected by the new regression tests. For `POWER(0.3,618.89539068045326)` it incorrectly discarded a valid nonzero result. Its immediate neighbor, `POWER(0.3,618.89539068045337)`, must instead become zero. Both cases now match native ADO SQLite. The discarded implementation is not retained.

Live regressions confirm nonzero `0.5^1023`, `0.5^1073`, and `0.5^1074`, zero `0.5^1075`, signed results, field and nested operands, nullable inputs and sort keys. The SQLite-specific matrix compares 15 explicit boundary inputs plus 132 neighboring binary/non-binary inputs against native ADO. No SQLite POWER case was skipped or converted into an unsupported-function gap.

Sources: [EF6 numeric override](src/Rendering/Expresso.Rendering.EntityFramework/Ef6ExpressionToLinqTransformer.Numeric.cs), [SQL-shape regression](test/Rendering/Expresso.Rendering.EntityFramework.Test/SqliteRightTests.cs), [live boundary matrix](test/Rendering/Expresso.Rendering.Integration.Test/Ef6/Ef6ItTests.cs), [shared differential cases](test/Rendering/Expresso.Rendering.TestCases/RendererDifferentialCases.cs).

### N34 — fixed: Oracle NUMBER arithmetic before computed POWER

Oracle EF Core double literal leaves now pass through `OracleNumber`, translated to `CAST(... AS NUMBER)` before arithmetic. Integer promotions in arithmetic and computed POWER operands also retain NUMBER. The former direct-operand-only `OracleNumberPower` helper was removed. Genuine BINARY_DOUBLE columns and arithmetic derived from them remain binary floating point.

The unit tests inspect real generated commands without opening connections and check every double bind's NUMBER cast. Live Oracle EF Core tests confirm composed base/exponent overflow and arithmetic overflow before POWER, alongside field, subnormal and sorting controls. The complete net8.0 integration run passes on every configured engine.

The full net48 run found the corresponding integer-promotion problem in Oracle EF6. Its provider manifest offers no NUMBER-preserving double conversion; even an integer ROUND identity acquired another BINARY_DOUBLE cast. Under the approved exact-subset rule, **Oracle EF6 POWER requiring integer-to-double promotion now throws a specific `NotSupportedException` during rendering**. This affects direct integer operands and integer conversions inside computed operands. Double-only operands remain supported. The catalog `power` case and differential `sort-power-subnormal` / `isnull-power-subnormal-nullable` cases now assert this exact documented provider limit; they are not skipped and arbitrary errors cannot satisfy their assertions. This is a new explicit provider restriction, not an implementation of integer POWER support on Oracle EF6.

Sources: [EF Core literal/numeric overrides](src/Rendering/Expresso.Rendering.EntityFrameworkCore/EfCoreExpressionToLinqTransformer.Literals.cs), [command regressions](test/Rendering/Expresso.Rendering.EntityFrameworkCore.Test/OracleNumberParameterTests.cs), [EF6 limit regressions](test/Rendering/Expresso.Rendering.EntityFramework.Test/OraclePowerPromotionTests.cs), [specific gap assertions](test/Rendering/Expresso.Rendering.Integration.Test/Ef6/Ef6ProviderGaps.cs). Documentation: [POWER](docs/functions/arithmetic/power.md), [EF6 provider limits](docs/linq-rendering.md#ef6-provider-limits).

### Acceptance results

| Run | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Full solution unit leg, net6.0 (`Category!=Integration`) | 2,097 | 0 | 0 |
| Full solution unit leg, net48 (`Category!=Integration`) | 2,064 | 0 | 0 |
| Full integration project, net6.0 | 769 | 0 | 0 |
| Full integration project, net8.0 | 1,854 | 0 | 0 |
| Full integration project, net48 | 1,617 | 0 | 2 |

**8,401 test executions passed; zero failed.** Integration-project totals include their helper unit tests. All integration frameworks ran sequentially with `EXPRESSO_IT=1`, without a case/provider filter, against the configured local engines and fixture-owned widget tables. The two pre-existing skips are MySQL EF6 `time` and `time-add-eq`, caused by its documented TimeSpan binding difference. They are not counted as successful coverage; no new skip was added. All executed tests passed, but these two tests remain unexecuted rather than passing.

Renderer unit line coverage: **98.96% EF6**, **92.98% EF Core**. The full Release solution build passed with zero errors. It emitted the existing NU1903 warning for the EF Core sample's transitive `SQLitePCLRaw.lib.e_sqlite3` 2.1.6; that dependency was not changed in this remediation. Existing nullable/analyzer and Framework SDK extension-loading warnings do not cause test failures.

Both rebuilt EF sample hosts returned HTTP 200 for their Swagger metadata and were stopped afterward. These smoke checks made no sample-database query. No sample schema/seed script was run. N32's in-memory rounding implementation was not changed.

Commands used for the full runs:

```powershell
dotnet test Expresso.slnx -c Release -f net6.0 --no-restore --filter "Category!=Integration"
dotnet test Expresso.slnx -c Release -f net48 --no-restore --filter "Category!=Integration"
$env:EXPRESSO_IT = '1'
dotnet test test/Rendering/Expresso.Rendering.Integration.Test -c Release -f net6.0 --no-restore
dotnet test test/Rendering/Expresso.Rendering.Integration.Test -c Release -f net8.0 --no-restore
dotnet test test/Rendering/Expresso.Rendering.Integration.Test -c Release -f net48 --no-restore
dotnet build Expresso.slnx -c Release --no-restore
```

Evidence: [net6.0 unit log](C:/Users/itorg/AppData/Local/Temp/Expresso-N33-N34-20261007/unit-accepted-net6.log), [net48 unit log](C:/Users/itorg/AppData/Local/Temp/Expresso-N33-N34-20261007/unit-accepted-net48.log), [net6.0 integration log](C:/Users/itorg/AppData/Local/Temp/Expresso-N33-N34-20261007/integration-net6.log), [net8.0 integration log](C:/Users/itorg/AppData/Local/Temp/Expresso-N33-N34-20261007/integration-net8.log), [net48 integration log](C:/Users/itorg/AppData/Local/Temp/Expresso-N33-N34-20261007/integration-accepted-net48.log), [solution build](C:/Users/itorg/AppData/Local/Temp/Expresso-N33-N34-20261007/solution-build.log). Matching TRX and coverage artifacts are in the same temporary results directory.
