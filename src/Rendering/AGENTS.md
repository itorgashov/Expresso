# Renderers — agent guide

Every renderer must return the same rows as the SQL dialect renderer on the same engine. A difference is a bug unless it is a documented EF6 gap.

## SQL dialects

- Shared walking lives in `Expresso.Rendering.Common` (`ExpressionToSqlQueryClauseTransformerBase` partials, visitor in `...Visitor.cs` / `...Visitor.Functions.cs`).
- A dialect difference goes into a `protected virtual Append*` hook on the base, overridden in the dialect project. Do not add a type switch in the walker.
- Each dialect project stays thin: the transformer subclass plus `Add{Engine}ExpressionTransformations()`.
- Golden SQL is asserted through `RendererParityTests` in `test/Rendering/Expresso.Rendering.TestCases`; each dialect test project supplies a `DialectSql`. Add the case there, not in one dialect's tests.

## LINQ (`Expresso.Rendering.Linq`)

- `ExpressionToLinqTransformerBase` partials build `LinqNode` (value plus `IsNull`; booleans keep SQL three-valued logic). Literals are `ParameterBox<T>.Value` captures so providers parameterize them.
- Profiles: `QueryableExpressionToLinqTransformer` (BCL members EF translates) and `InMemoryExpressionToLinqTransformer` (`ExpressoFunctions`, PostgreSQL semantics, ordinal strings).

## EF Core (`net8.0`)

- Override only where the provider's own translation differs from the dialect SQL. Overrides call `ExpressoDbFunctions` markers; SQL per provider is in `ExpressoFunctionTranslations`, registered by `HasExpressoFunctions(providerName)`.
- Nested markers need explicit type mappings. Check the printed SQL in `Expresso.Rendering.EntityFrameworkCore.Test`.

## EF6 (`net48`)

- Use canonical `DbFunctions` or provider store-function stubs in `Ef6Functions`. EF6 cannot register custom SQL.
- Exact subset only. When a provider cannot match the dialect SQL, throw through `Unsupported(function, reason)`. Never approximate.
- A new gap updates three places together: `Ef6ProviderGaps` in the integration tests, the limits table in [docs/linq-rendering.md](../../docs/linq-rendering.md#ef6-provider-limits), and the function page's EF6 section.

## Before you finish

- Unit tests for the dialects and LINQ on `net6.0` and `net48`; EF Core tests on `net8.0`; EF6 tests on `net48`.
- If the change affects results, run the integration suite for the touched engines (see the integration tests [AGENTS.md](../../test/Rendering/Expresso.Rendering.Integration.Test/AGENTS.md)) or say clearly that it was not run.
