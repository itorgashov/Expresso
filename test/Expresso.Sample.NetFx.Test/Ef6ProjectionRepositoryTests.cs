using System.Data;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Expresso.Core.Paging;
using Expresso.Core.Projection;
using Expresso.Parsing;
using Expresso.Rendering.EntityFramework;
using Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

namespace Expresso.Sample.NetFx.Test;

public sealed class Ef6ProjectionRepositoryTests
{
    [Theory]
    [InlineData("book")]
    [InlineData("author")]
    [InlineData("publisher")]
    public async Task RealRepositories_TranslateFilterSortPageAndCount_WithSuppressedExternalIo(string context)
    {
        // Scenario: real EF6 context and repository; intercept only connection/command I/O, never the expression builder or DTOs.
        using var connection = new SqlConnection("Server=unused;Database=unused;Integrated Security=true");
        using var io = new Io(connection);
        using var db = new SampleEf6Context(connection, null);
        var transformer = new Ef6ExpressionToLinqTransformer(db);
        var field = context == "book" ? "title" : context == "author" ? "lastname" : "name";
        var responseName = context == "author" ? "lastName" : field;
        var projection = new ProjectionDirective(new[] { new ProjectionSelection(field) });
        var filter = new FilterParser().Parse("eq(" + field + ",\"value\")", new[] { (field, typeof(string)) });
        var sort = new SortDirectiveParser().Parse(field + ",desc", new[] { (field, typeof(string)) });
        object repository = context == "book" ? new BookRepository(db, transformer) :
            context == "author" ? new AuthorRepository(db, transformer) : new PublisherRepository(db, transformer);
        var rows = await (Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>>)repository.GetType().GetMethod("GetAllAsync")!
            .Invoke(repository, new object?[] { filter, sort, new PagingDirective(skip: 1, take: 2), projection, CancellationToken.None })!;
        Assert.Equal("value", Assert.Single(rows)[responseName]);
        Assert.Single(rows[0]);
        Assert.Contains("OFFSET", Assert.Single(io.Commands));
        Assert.Contains("DESC", io.Commands[0]);
        var total = await (Task<long>)repository.GetType().GetMethod("CountAsync")!.Invoke(repository, new object?[] { filter, CancellationToken.None })!;
        Assert.Equal(7, total);
        Assert.Contains("COUNT", io.Commands.Last());
        Assert.DoesNotContain("OFFSET", io.Commands.Last());
    }

    private sealed class Io : DbCommandInterceptor, IDbConnectionInterceptor, IDisposable
    {
        private readonly DbConnection _connection;
        internal List<string> Commands { get; } = new();
        internal Io(DbConnection connection)
        {
            _connection = connection;
            DbInterception.Add(this);
        }
        public void Dispose() => DbInterception.Remove(this);
        private bool Owns(DbConnection connection) => ReferenceEquals(connection, _connection) ||
            connection.ConnectionString.IndexOf("unused", StringComparison.OrdinalIgnoreCase) >= 0;
        public override void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> context)
        {
            if (command.Connection is not null && Owns(command.Connection)) context.Result = 3;
        }
        public override void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> context)
        {
            if (command.Connection is null || !Owns(command.Connection)) return;
            if (command.CommandText.IndexOf("serverproperty", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var metadata = new DataTable();
                metadata.Columns.Add("EngineEdition", typeof(int));
                metadata.Rows.Add(3);
                context.Result = metadata.CreateDataReader();
                return;
            }
            Commands.Add(command.CommandText);
            var select = Regex.Match(command.CommandText, @"SELECT\s+(.*?)\s+FROM", RegexOptions.Singleline).Groups[1].Value;
            var table = new DataTable();
            var values = new List<object>();
            foreach (var column in select.Split(','))
            {
                var alias = Regex.Matches(column, @"\[([^\]]+)\]").Cast<Match>().Last().Groups[1].Value;
                var text = column.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    column.IndexOf("last_name", StringComparison.OrdinalIgnoreCase) >= 0 || column.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0;
                table.Columns.Add(alias, text ? typeof(string) : typeof(int));
                values.Add(text ? "value" : command.CommandText.Contains("COUNT") ? 7 : 1);
            }
            table.Rows.Add(values.ToArray());
            context.Result = table.CreateDataReader();
        }
        public override void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> context) =>
            throw new InvalidOperationException("Unit tests must never execute database writes.");
        public void BeginningTransaction(DbConnection connection, BeginTransactionInterceptionContext context)
        {
            
        }
        public void BeganTransaction(DbConnection connection, BeginTransactionInterceptionContext context)
        {
            
        }
        public void Closing(DbConnection connection, DbConnectionInterceptionContext context)
        {
            if (Owns(connection)) context.SuppressExecution();
        }
        public void Closed(DbConnection connection, DbConnectionInterceptionContext context)
        {
            
        }
        public void ConnectionStringGetting(DbConnection connection, DbConnectionInterceptionContext<string> context)
        {
            
        }
        public void ConnectionStringGot(DbConnection connection, DbConnectionInterceptionContext<string> context)
        {
            
        }
        public void ConnectionStringSetting(DbConnection connection, DbConnectionPropertyInterceptionContext<string> context)
        {
            
        }
        public void ConnectionStringSet(DbConnection connection, DbConnectionPropertyInterceptionContext<string> context)
        {
            
        }
        public void ConnectionTimeoutGetting(DbConnection connection, DbConnectionInterceptionContext<int> context)
        {
            
        }
        public void ConnectionTimeoutGot(DbConnection connection, DbConnectionInterceptionContext<int> context)
        {
            
        }
        public void DatabaseGetting(DbConnection connection, DbConnectionInterceptionContext<string> context)
        {
            
        }
        public void DatabaseGot(DbConnection connection, DbConnectionInterceptionContext<string> context)
        {
            
        }
        public void DataSourceGetting(DbConnection connection, DbConnectionInterceptionContext<string> context)
        {
            
        }
        public void DataSourceGot(DbConnection connection, DbConnectionInterceptionContext<string> context)
        {
            
        }
        public void Disposing(DbConnection connection, DbConnectionInterceptionContext context)
        {
            
        }
        public void Disposed(DbConnection connection, DbConnectionInterceptionContext context)
        {
            
        }
        public void EnlistingTransaction(DbConnection connection, EnlistTransactionInterceptionContext context)
        {
            
        }
        public void EnlistedTransaction(DbConnection connection, EnlistTransactionInterceptionContext context)
        {
            
        }
        public void Opening(DbConnection connection, DbConnectionInterceptionContext context)
        {
            if (Owns(connection)) context.SuppressExecution();
        }
        public void Opened(DbConnection connection, DbConnectionInterceptionContext context)
        {
            
        }
        public void ServerVersionGetting(DbConnection connection, DbConnectionInterceptionContext<string> context)
        {
            if (Owns(connection)) context.Result = "15.0.0";
        }
        public void ServerVersionGot(DbConnection connection, DbConnectionInterceptionContext<string> context)
        {
            
        }
        public void StateGetting(DbConnection connection, DbConnectionInterceptionContext<ConnectionState> context)
        {
            if (Owns(connection)) context.Result = ConnectionState.Open;
        }
        public void StateGot(DbConnection connection, DbConnectionInterceptionContext<ConnectionState> context)
        {
            
        }
    }
}
