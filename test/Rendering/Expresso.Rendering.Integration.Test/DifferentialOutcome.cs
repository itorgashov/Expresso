using System.Data.Common;
using System.Reflection;
using Expresso.Rendering.TestCases;

namespace Expresso.Rendering.Integration.Test
{
    /// <summary>Result of a query for differential comparison: the ids, or a classified error.</summary>
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
                var classified = Classify(ex);
                if (classified is not null)
                {
                    return classified;
                }

                throw;
            }
        }

        /// <summary>
        /// <see cref="DifferentialRejection.None"/> matches only equal id lists.
        /// <see cref="DifferentialRejection.Domain"/> also matches equal id lists, or a reference database error whose
        /// native code value equals one of <paramref name="databaseCodes"/> when the candidate is that same error or an
        /// unsupported result whose message contains <paramref name="unsupportedReason"/>.
        /// </summary>
        public static void AssertSame(
            string reference,
            string candidate,
            DifferentialRejection rejection = DifferentialRejection.None,
            Func<string>? describe = null,
            string? unsupportedReason = null,
            IReadOnlyCollection<string>? databaseCodes = null)
        {
            if (reference.StartsWith("ids:", StringComparison.Ordinal) && reference == candidate)
            {
                return;
            }

            if (rejection == DifferentialRejection.Domain
                && databaseCodes is not null
                && IsExpectedDatabaseCode(reference, databaseCodes)
                && (candidate == reference
                    || (unsupportedReason is not null
                        && candidate.StartsWith("error: unsupported:", StringComparison.Ordinal)
                        && candidate.Contains(unsupportedReason, StringComparison.Ordinal))))
            {
                return;
            }

            Assert.Fail($"Expected {reference}, actual {candidate}{Environment.NewLine}{describe?.Invoke()}");
        }

        /// <summary>True when <paramref name="reference"/> is <c>error: database:Kind:value</c> and <paramref name="databaseCodes"/> contains that value exactly.</summary>
        private static bool IsExpectedDatabaseCode(string reference, IReadOnlyCollection<string> databaseCodes)
        {
            const string prefix = "error: database:";
            if (!reference.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            var native = reference.Substring(prefix.Length);
            var separator = native.LastIndexOf(':');
            var value = separator >= 0 ? native.Substring(separator + 1) : native;
            return databaseCodes.Any(code => code == value);
        }

        /// <summary>Provider-specific code of a <see cref="DbException"/> (SQLite code, SQL Server number, SQLSTATE).</summary>
        public static string NativeCode(DbException exception)
        {
            var direct = ReadCode(exception);
            if (direct is not null)
            {
                return direct;
            }

            // DB2 leaves <c>SqlState</c> empty and stores the SQLSTATE on the first error.
            if (exception.GetType().GetProperty("Errors")?.GetValue(exception) is System.Collections.IEnumerable errors)
            {
                foreach (var error in errors)
                {
                    if (error is null)
                    {
                        continue;
                    }

                    var nested = ReadCode(error);
                    if (nested is not null)
                    {
                        return nested;
                    }
                }
            }

            return "ErrorCode:" + exception.ErrorCode;
        }

        private static string? ReadCode(object source)
        {
            foreach (var name in new[] { "SqliteErrorCode", "Number", "SqlState", "SQLState", "NativeError", "Code" })
            {
                if (source.GetType().GetProperty(name)?.GetValue(source) is { } value)
                {
                    var text = value.ToString();
                    if (!string.IsNullOrEmpty(text) && text != "0")
                    {
                        return name + ":" + text;
                    }
                }
            }

            return null;
        }

        private static string? Classify(Exception ex)
        {
            DbException? database = null;
            NotSupportedException? unsupported = null;
            for (var current = ex; current is not null; current = current.InnerException)
            {
                database ??= current as DbException;
                unsupported ??= current as NotSupportedException;
            }

            if (database is not null)
            {
                return "error: database:" + NativeCode(database);
            }

            if (unsupported is not null)
            {
                return "error: unsupported: " + unsupported.Message;
            }

            return null;
        }
    }
}
