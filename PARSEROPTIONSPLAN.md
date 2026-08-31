# Configurable date/time literal parsing

Status: **complete**. Package version **0.7.0** (additive public API).

## Scope

- Public `LiteralParseOptions` POCO in `Expresso.Parsing` (culture + format arrays).
- `ExpressionParser` instance-based (no singleton); options threaded through `CreateLiteral`.
- `AddRequestParametersParsers(Action<LiteralParseOptions>)` and `AddRequestParametersParsers(LiteralParseOptions)`.
- No `IConfiguration` / `IOptions` inside the library — host binds config to POCO.

## Defaults (unchanged when options not set)

- `DateTime` / `DateOnly`: ISO `yyyy-MM-dd` exact, then culture fallback.
- `TimeOnly`: `HH:mm:ss`, `HH:mm` exact, then fallback.
- `TimeSpan`: invariant `hh:mm` / `hh:mm:ss` exact only (clock range `[0, 24h)`).
