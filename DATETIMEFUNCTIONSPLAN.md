---
name: DateTime Functions
overview: "Add 15 DateTime functions to Expresso (9 getters + 6 add-with-int) following the existing string-functions pattern."
---

# DateTime functions — Plan

> Status: implemented, documented, and released as **0.3.0**. See [IMPLEMENTATION.md](IMPLEMENTATION.md) and [docs/functions/README.md](docs/functions/README.md).

## Scope

**In scope (15 functions):**

| Category | Functions | Args | Return type |
|---|---|---|---|
| Getters | `year`, `month`, `day`, `dayofyear`, `hour`, `minute`, `second`, `dayofweek` | `(datetime)` | `int` |
| Getter | `date` | `(datetime)` | `DateTime` |
| Add | `addyears`, `addmonths`, `adddays`, `addhours`, `addminutes`, `addseconds` | `(datetime, int)` | `DateTime` |

- Function names: case-insensitive (existing convention).
- Add functions: **int second argument only**; **zero and negative** values allowed (e.g. `adddays(createdat,-7)`). Parser uses existing `CoerceToInt` → `CreateLiteral(..., typeof(int))`, which already parses `-1` via `int.TryParse` even though `GetLiteralType` does not treat leading `-` as numeric.
- **Out of scope:** version bump, user-facing docs (`docs/`, README), other DB renderers, millisecond/microsecond, fractional add, `CONTEXT.md`/`IMPLEMENTATION.md` beyond a one-line status note after execution.

## Architecture (mirror string functions)

```mermaid
flowchart LR
    Query["Query string"] --> Parser["ExpressionParser.DateTimeFunctions.cs"]
    Parser --> IR["Expresso.Core CriteriaExpressions"]
    IR --> Renderer["ExpressionToSqlServerQueryClauseTransformer.DateTime.cs"]
    Renderer --> SQL["DATEPART / DATEADD / CAST"]
```

Follow the same layering as [STRINGFUNCTIONSPLAN.md](STRINGFUNCTIONSPLAN.md):

1. **Core** — IR node per function + shared base classes
2. **Parsing** — new partial `ExpressionParser.DateTimeFunctions.cs`, wired from `CreateFunction` default branch
3. **Rendering.SqlServer** — new partial `ExpressionToSqlServerQueryClauseTransformer.DateTime.cs`, dispatched from `GenerateClause` default branch (alongside `TryGenerateStringFunction`)

## 1. Core IR ([src/Expresso.Core/CriteriaExpressions/](src/Expresso.Core/CriteriaExpressions/))

### Shared base classes ([Abstract/](src/Expresso.Core/CriteriaExpressions/Abstract/))

**`DateTimeSingleArgIntFunction : AbstractFunction`**
- Constructor `(AbstractExpression argument)`
- `AssertNotNull` + `AssertExpressionOfTypes(argument, typeof(DateTime))`
- `ReturnType = typeof(int)`

**`DateTimeAddFunction : AbstractFunction`**
- Constructor `(AbstractExpression dateTime, AbstractExpression amount)`
- Arg0: `DateTime`; arg1: `int` (no minimum/maximum — zero and negative allowed at IR level)
- `ReturnType = typeof(DateTime)`

**`DateFunc : AbstractFunction`** (standalone, not inheriting int base)
- Arg0: `DateTime`; `ReturnType = typeof(DateTime)`

### Concrete classes (thin wrappers calling base)

| Class | Base | File |
|---|---|---|
| `YearFunc` … `SecondFunc`, `DayOfWeekFunc` | `DateTimeSingleArgIntFunction` | `YearFunc.cs` … `DayOfWeekFunc.cs` |
| `DateFunc` | own ctor | `DateFunc.cs` |
| `AddYearsFunc` … `AddSecondsFunc` | `DateTimeAddFunction` | `AddYearsFunc.cs` … `AddSecondsFunc.cs` |

Each sealed class follows [LenFunc.cs](src/Expresso.Core/CriteriaExpressions/LenFunc.cs) / [LeftFunc.cs](src/Expresso.Core/CriteriaExpressions/LeftFunc.cs) style (minimal body, validation in base).

## 2. Parser ([src/Expresso.Parsing/](src/Expresso.Parsing/))

### New file: `ExpressionParser.DateTimeFunctions.cs`

```csharp
private static bool TryCreateDateTimeFunction(string functionName, List<AbstractExpression> arguments, out AbstractExpression result)
```

Switch (lowercase names): `year`, `month`, `day`, `dayofyear`, `hour`, `minute`, `second`, `dayofweek`, `date`, `addyears`, `addmonths`, `adddays`, `addhours`, `addminutes`, `addseconds`.

Helpers (same file):
- `CoerceToDateTime(AbstractExpression)` — `StringLiteral` → `CreateLiteral(..., typeof(DateTime))`; else pass through
- Reuse existing `CoerceToInt` and `RequireCount` from [ExpressionParser.StringFunctions.cs](src/Expresso.Parsing/ExpressionParser.StringFunctions.cs) (same partial class)

Arity messages (plain `Exception`, matching existing style):
- Getters: `"Year() function should have 1 argument."` (etc.)
- Add: `"Adddays() function should have 2 arguments."` (etc.)

Wire in [ExpressionParser.cs](src/Expresso.Parsing/ExpressionParser.cs) `CreateFunction` default branch **after** `TryCreateStringFunction`, before `Unknown function`:

```csharp
if (TryCreateDateTimeFunction(functionName, arguments, out var dateTimeFunction))
    return dateTimeFunction;
```

## 3. SQL Server renderer ([src/Rendering/Expresso.Rendering.SqlServer/](src/Rendering/Expresso.Rendering.SqlServer/))

### New partial: `ExpressionToSqlServerQueryClauseTransformer.DateTime.cs`

`TryGenerateDateTimeFunction(...)` switch — return `true` when handled.

| IR | SQL pattern |
|---|---|
| `YearFunc` | `YEAR(arg)` |
| `MonthFunc` | `MONTH(arg)` |
| `DayFunc` | `DAY(arg)` |
| `DayOfYearFunc` | `DATEPART(dayofyear, arg)` |
| `HourFunc` | `DATEPART(hour, arg)` |
| `MinuteFunc` | `DATEPART(minute, arg)` |
| `SecondFunc` | `DATEPART(second, arg)` |
| `DateFunc` | `CAST(arg AS date)` |
| `DayOfWeekFunc` | `((DATEPART(weekday, arg) + @@DATEFIRST - 1) % 7)` |
| `AddYearsFunc` | `DATEADD(year, amount, arg)` |
| `AddMonthsFunc` | `DATEADD(month, amount, arg)` |
| `AddDaysFunc` | `DATEADD(day, amount, arg)` |
| `AddHoursFunc` | `DATEADD(hour, amount, arg)` |
| `AddMinutesFunc` | `DATEADD(minute, amount, arg)` |
| `AddSecondsFunc` | `DATEADD(second, amount, arg)` |

**`dayofweek` note (per your choice):** formula normalizes to C# `DayOfWeek` (Sunday=0 … Saturday=6) when SQL Server `DATEFIRST` is **7** (default US). Add a code comment in the renderer; no user docs in this task.

**`DATEADD`:** SQL Server accepts negative `amount` natively — no special casing.

Reuse `GenerateNamedFunction` where arity matches (single-arg getters can use `DATEPART` wrapper or dedicated one-liners; two-arg add uses `DATEADD` with arg order: unit, amount, date — map `Arguments[1]` then `Arguments[0]` or build explicitly).

Update [ExpressionToSqlServerQueryClauseTransformer.cs](src/Rendering/Expresso.Rendering.SqlServer/ExpressionToSqlServerQueryClauseTransformer.cs) `default` branch:

```csharp
if (TryGenerateStringFunction(...) || TryGenerateDateTimeFunction(...))
```

## 4. Tests

### Core — [test/Expresso.Core.Test/CriteriaExpressions/](test/Expresso.Core.Test/CriteriaExpressions/)

One test class per function (or grouped: `DateTimeGetterFuncTests` + `DateTimeAddFuncTests` to limit file count — prefer **one file per function** to match existing `LenFuncTests` / `LeftFuncTests` convention).

Each getter test class:
- Valid `DateTime` arg → correct `ReturnType` (`int` or `DateTime` for `DateFunc`)
- `null` arg → `ArgumentNullException`
- Wrong type (e.g. `string`) → `ArgumentException`

Each add test class:
- Valid `DateTime` + `int` args → `ReturnType == DateTime`
- Wrong types on arg0/arg1
- **No test rejecting negative/zero** (they must be allowed)

Representative classes: `YearFuncTests`, `DateFuncTests`, `AddDaysFuncTests` (others parallel).

### Parsing — [test/Expresso.Parsing.Test/FilterParserTests.cs](test/Expresso.Parsing.Test/FilterParserTests.cs)

Add tests using existing `dateFrom` / `dateTo` fields (`typeof(DateTime)`):
- `Parse_ValidYearFunction_ReturnsYearFunc` — `eq(year(dateFrom), 2020)`
- `Parse_ValidAddDaysWithNegative_ReturnsAddDaysFunc` — `gt(adddays(dateFrom,-7), dateTo)`
- `Parse_AddDaysOnStringField_Throws` — `adddays(name, 1)` → exception
- `Parse_DateFunction_ReturnsDateFunc`
- `Parse_InvalidYearArity_ThrowsException` — `year()` / `year(a,b)`

Optional: [SortDirectiveParserTests.cs](test/Expresso.Parsing.Test/SortDirectiveParserTests.cs) — `year(dateFrom),asc` parses for ORDER BY.

### Rendering — new [test/Rendering/Expresso.Rendering.SqlServer.Test/DateTimeFunctionTransformerTests.cs](test/Rendering/Expresso.Rendering.SqlServer.Test/DateTimeFunctionTransformerTests.cs)

Mirror [StringFunctionTransformerTests.cs](test/Rendering/Expresso.Rendering.SqlServer.Test/StringFunctionTransformerTests.cs):
- Field map: `{ "createdat", "b.created_at" }`
- WHERE tests for representative functions:
  - `year(createdat)` → `(YEAR([b].[created_at]))`
  - `date(createdat)` → `(CAST([b].[created_at] AS date))`
  - `dayofweek(createdat)` → formula with `DATEPART` and `@@DATEFIRST`
  - `adddays(createdat, -7)` → `(DATEADD(day, @param_0, [b].[created_at]))` with `-7` parameter
  - `addmonths(createdat, 0)` → zero amount
- ORDER BY: `year(createdat),asc` renders without error

## 5. Plan file and status

- Create [DATETIMEFUNCTIONSPLAN.md](DATETIMEFUNCTIONSPLAN.md) in repo root (feature plan per workspace rules).
- After execution: brief note in [IMPLEMENTATION.md](IMPLEMENTATION.md) only (no docs/ changes).

## 6. Validation

```powershell
dotnet test .\Expresso.slnx -c Release
```

All existing + new tests must pass. **Do not** change `Directory.Build.props` version.

## File checklist (new/modified)

| Action | Path |
|---|---|
| Add | `src/Expresso.Core/CriteriaExpressions/Abstract/DateTimeSingleArgIntFunction.cs` |
| Add | `src/Expresso.Core/CriteriaExpressions/Abstract/DateTimeAddFunction.cs` |
| Add | 15 `*Func.cs` IR classes |
| Add | `src/Expresso.Parsing/ExpressionParser.DateTimeFunctions.cs` |
| Modify | `src/Expresso.Parsing/ExpressionParser.cs` (wire TryCreateDateTimeFunction) |
| Add | `src/Rendering/Expresso.Rendering.SqlServer/ExpressionToSqlServerQueryClauseTransformer.DateTime.cs` |
| Modify | `ExpressionToSqlServerQueryClauseTransformer.cs` (dispatch) |
| Add | ~15 Core test files + `DateTimeFunctionTransformerTests.cs` |
| Modify | `FilterParserTests.cs` (+ optional `SortDirectiveParserTests.cs`) |
| Add | `DATETIMEFUNCTIONSPLAN.md` |
