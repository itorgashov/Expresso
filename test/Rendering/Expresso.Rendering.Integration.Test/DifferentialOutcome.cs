namespace Expresso.Rendering.Integration.Test
{
    /// <summary>Result of a query for differential comparison: the ids, or "error" when the engine rejects it.</summary>
    public static class DifferentialOutcome
    {
        public static string Of(Func<IReadOnlyList<int>> query)
        {
            try
            {
                return "ids: " + string.Join(",", query());
            }
            catch (Exception ex)
            {
                return "error: " + ex.GetBaseException().GetType().Name;
            }
        }

        /// <summary>Equal outcomes; two errors count as agreement (the function is unsupported on that engine either way).</summary>
        /// <param name="reference">Reference (ADO) outcome.</param>
        /// <param name="candidate">Outcome under test.</param>
        /// <param name="describe">Optional diagnostics (for example the generated SQL) added to a failure.</param>
        public static void AssertSame(string reference, string candidate, Func<string>? describe = null)
        {
            var bothFailed = reference.StartsWith("error", StringComparison.Ordinal) && candidate.StartsWith("error", StringComparison.Ordinal);
            if (!bothFailed && reference != candidate)
            {
                Assert.Fail($"Expected {reference}, actual {candidate}{Environment.NewLine}{describe?.Invoke()}");
            }
        }
    }
}
