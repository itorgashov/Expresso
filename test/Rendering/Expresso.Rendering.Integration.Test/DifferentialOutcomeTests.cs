using System.Data.Common;

namespace Expresso.Rendering.Integration.Test
{
    public sealed class DifferentialOutcomeTests
    {
        [Fact]
        public void AssertSame_DifferentDatabaseCodes_Fail()
        {
            var ex = Record.Exception(() => DifferentialOutcome.AssertSame("error: database:SqliteErrorCode:14", "error: database:SqliteErrorCode:1"));

            Assert.NotNull(ex);
        }

        [Fact]
        public void AssertSame_SameDatabaseCode_FailsWhenIdsAreRequired()
        {
            var ex = Record.Exception(() => DifferentialOutcome.AssertSame("error: database:SqliteErrorCode:14", "error: database:SqliteErrorCode:14"));

            Assert.NotNull(ex);
        }

        [Fact]
        public void AssertSame_EqualIds_Pass() =>
            DifferentialOutcome.AssertSame("ids: 1,2", "ids: 1,2");

        [Fact]
        public void AssertSame_Domain_ExpectedCodeAndReason_Passes() =>
            DifferentialOutcome.AssertSame(
                "error: database:SqlState:2201F",
                "error: unsupported: negative square root",
                TestCases.DifferentialRejection.Domain,
                unsupportedReason: "negative square root",
                databaseCodes: new[] { "2201F", "3623" });

        [Fact]
        public void AssertSame_Domain_SameExpectedDatabaseCode_Passes() =>
            DifferentialOutcome.AssertSame(
                "error: database:Number:8134",
                "error: database:Number:8134",
                TestCases.DifferentialRejection.Domain,
                unsupportedReason: "division by zero",
                databaseCodes: new[] { "8134" });

        [Fact]
        public void AssertSame_Domain_ExactOracleCode_Passes() =>
            DifferentialOutcome.AssertSame(
                "error: database:Number:1428",
                "error: unsupported: negative square root",
                TestCases.DifferentialRejection.Domain,
                unsupportedReason: "negative square root",
                databaseCodes: new[] { "1428", "2201F" });

        [Fact]
        public void AssertSame_Domain_CodePrefix_Fails()
        {
            var ex = Record.Exception(() => DifferentialOutcome.AssertSame(
                "error: database:Number:14280",
                "error: unsupported: negative square root",
                TestCases.DifferentialRejection.Domain,
                unsupportedReason: "negative square root",
                databaseCodes: new[] { "1428" }));

            Assert.NotNull(ex);
        }

        [Fact]
        public void AssertSame_Domain_IdenticalUnexpectedCode_Fails()
        {
            var ex = Record.Exception(() => DifferentialOutcome.AssertSame(
                "error: database:Number:14280",
                "error: database:Number:14280",
                TestCases.DifferentialRejection.Domain,
                unsupportedReason: "negative square root",
                databaseCodes: new[] { "1428" }));

            Assert.NotNull(ex);
        }

        [Fact]
        public void AssertSame_Domain_InfrastructureCode_Fails()
        {
            var ex = Record.Exception(() => DifferentialOutcome.AssertSame(
                "error: database:SqliteErrorCode:14",
                "error: unsupported: negative square root",
                TestCases.DifferentialRejection.Domain,
                unsupportedReason: "negative square root",
                databaseCodes: new[] { "2201F" }));

            Assert.NotNull(ex);
        }

        [Fact]
        public void AssertSame_Domain_WrongReason_Fails()
        {
            var ex = Record.Exception(() => DifferentialOutcome.AssertSame(
                "error: database:SqlState:2201F",
                "error: unsupported: something else",
                TestCases.DifferentialRejection.Domain,
                unsupportedReason: "negative square root",
                databaseCodes: new[] { "2201F" }));

            Assert.NotNull(ex);
        }

        [Fact]
        public void AssertSame_Domain_EqualIds_Pass() =>
            DifferentialOutcome.AssertSame(
                "ids: 3",
                "ids: 3",
                TestCases.DifferentialRejection.Domain,
                unsupportedReason: "negative square root",
                databaseCodes: new[] { "2201F" });

        [Fact]
        public void Of_RethrowsUnexpectedExceptions() =>
            Assert.Throws<InvalidOperationException>(() => DifferentialOutcome.Of(() => throw new InvalidOperationException("translated")));

        [Fact]
        public void NativeCode_UsesSqliteErrorCode()
        {
            var code = DifferentialOutcome.NativeCode(new SqliteCodeException(14));

            Assert.Equal("SqliteErrorCode:14", code);
        }

        private sealed class SqliteCodeException : DbException
        {
            public SqliteCodeException(int code) => SqliteErrorCode = code;

            public int SqliteErrorCode { get; }
        }
    }
}
