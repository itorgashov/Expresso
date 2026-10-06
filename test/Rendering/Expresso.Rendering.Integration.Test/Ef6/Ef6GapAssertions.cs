#if NETFRAMEWORK
using System.Text.RegularExpressions;

namespace Expresso.Rendering.Integration.Test.Ef6
{
    /// <summary>Checks that a documented EF6 gap failed in the way its <see cref="Ef6GapKind"/> describes.</summary>
    internal static class Ef6GapAssertions
    {
        public static void Assert(Ef6Gap gap, Exception exception)
        {
            var chain = Enumerate(exception).ToList();
            var text = string.Join(" ", chain.Select(e => e.Message));
            if (gap.Kind == Ef6GapKind.Unsupported)
            {
                Xunit.Assert.Contains(typeof(NotSupportedException), chain.Select(e => e.GetType()));
                Xunit.Assert.Contains(gap.Reason, text);
                return;
            }

            var database = chain.OfType<System.Data.Common.DbException>().FirstOrDefault();
            Xunit.Assert.NotNull(database);
            var code = DifferentialOutcome.NativeCode(database!);
            var oracle = Regex.Match(gap.Reason, @"ORA-(\d+)");
            if (oracle.Success)
            {
                Xunit.Assert.Contains(oracle.Groups[1].Value, code + " " + text);
                return;
            }

            Xunit.Assert.False(string.IsNullOrEmpty(code));
        }

        private static IEnumerable<Exception> Enumerate(Exception exception)
        {
            for (var current = exception; current is not null; current = current.InnerException)
            {
                yield return current;
            }
        }
    }
}
#endif
