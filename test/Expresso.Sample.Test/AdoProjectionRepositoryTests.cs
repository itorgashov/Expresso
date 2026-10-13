using System.Data;
using System.Data.Common;
using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;
using Expresso.Parsing;
using Expresso.Rendering;
using Expresso.Sample.Shared.DataAccess;
#if NETFRAMEWORK
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif

namespace Expresso.Sample.Test;

public sealed class AdoProjectionRepositoryTests
{
    [Theory]
    [InlineData("book")]
    [InlineData("author")]
    [InlineData("publisher")]
    public async Task RealRepositories_RenderFilterSortPage_SelectSparseValues_AndCountIndependently(string context)
    {
        // Scenario: actual sample repository, parser, and renderer; only ADO command execution is fake, never a database connection.
        var db = new FakeDb();
        var renderer = new ExpressionToSqlServerQueryClauseTransformer();
        var mappings = new SampleProjectionMappings(db.Sql);
        var map = context == "book" ? mappings.Books : context == "author" ? mappings.Authors : mappings.Publishers;
        var field = context == "book" ? "title" : context == "author" ? "lastname" : "name";
        var responseName = context == "author" ? "lastName" : field;
        var projection = new ProjectionDirectiveParser().Parse(field, map.GetProjectionModel());
        var filter = new FilterParser().Parse("eq(" + field + ",\"value\")", new[] { (field, typeof(string)) });
        var sort = new SortDirectiveParser().Parse(field + ",desc", new[] { (field, typeof(string)) });
        object repository = context == "book" ? new BookRepository(db, renderer, renderer, mappings) :
            context == "author" ? new AuthorRepository(db, renderer, renderer, mappings) : new PublisherRepository(db, renderer, renderer, mappings);
        db.Rows.Enqueue(new[] { new object?[] { "value", 1 } });
        var rows = await InvokeList(repository, filter, sort, new PagingDirective(skip: 1, take: 2), projection);
        Assert.Equal("value", Assert.Single(rows)[responseName]);
        Assert.Single(rows[0]);
        var root = Assert.Single(db.Commands);
        Assert.Contains(" WHERE ", root.Sql);
        Assert.Contains(" DESC", root.Sql);
        Assert.Contains("OFFSET", root.Sql);
        Assert.DoesNotContain("value", root.Sql);
        Assert.Contains(root.Parameters.Values, value => Equals(value, "value"));
        var total = await (Task<long>)repository.GetType().GetMethod("CountAsync")!.Invoke(repository, new object?[] { filter, CancellationToken.None })!;
        Assert.Equal(7, total);
        Assert.StartsWith("SELECT COUNT(*)", db.Commands.Last().Sql);
        Assert.DoesNotContain("OFFSET", db.Commands.Last().Sql);
        Assert.Contains(" WHERE ", db.Commands.Last().Sql);
        db.Rows.Enqueue(Array.Empty<object?[]>());
        var byId = await (Task<IReadOnlyDictionary<string, object?>?>)repository.GetType().GetMethod("GetByIdAsync")!
            .Invoke(repository, new object?[] { 999, projection, CancellationToken.None })!;
        Assert.Null(byId);
    }

    [Fact]
    public async Task BookRepository_UsesLibraryExecutor_ForRequestedAuthorAndAwardLevels()
    {
        // Scenario: title plus award title travels through book-author relation; each level selects only its response fields and keys.
        var db = new FakeDb();
        var renderer = new ExpressionToSqlServerQueryClauseTransformer();
        var mappings = new SampleProjectionMappings(db.Sql);
        var projection = new ProjectionDirectiveParser().Parse("title,authors.awards.title", mappings.Books.GetProjectionModel());
        db.Rows.Enqueue(new[] { new object?[] { "book", 1 } });
        db.Rows.Enqueue(new[] { new object?[] { 10, 1 } });
        db.Rows.Enqueue(new[] { new object?[] { "award", 100, 10 } });
        var result = await new BookRepository(db, renderer, renderer, mappings).GetAllAsync(null, null, PagingDirective.None, projection);
        var author = Assert.Single((IEnumerable<IReadOnlyDictionary<string, object?>>)result[0]["authors"]!);
        Assert.Equal("award", Assert.Single((IEnumerable<IReadOnlyDictionary<string, object?>>)author["awards"]!)["title"]);
        Assert.Equal(3, db.Commands.Count);
        Assert.DoesNotContain("last_name", db.Commands[1].Sql);
        Assert.DoesNotContain("[aw].[year] AS", db.Commands[2].Sql);
        Assert.All(db.Commands.Skip(1), c => Assert.Single(c.Parameters));
    }

    private static Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> InvokeList(object repository,
        FilterCriteria filter, SortDirective sort, PagingDirective paging, ProjectionDirective projection) =>
        (Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>>)repository.GetType().GetMethod("GetAllAsync")!
            .Invoke(repository, new object?[] { filter, sort, paging, projection, CancellationToken.None })!;

    internal sealed class FakeDb : ISampleDb
    {
        public ISampleSql Sql { get; } = SampleSqlFactory.Create(SampleEngine.SqlServer);
        internal Queue<object?[][]> Rows { get; } = new();
        internal List<(string Sql, Dictionary<string, object?> Parameters)> Commands { get; } = new();
        public Task<DbConnection> OpenAsync(CancellationToken cancellationToken = default) => Task.FromResult<DbConnection>(new Connection(this));
        public void Bind(DbCommand command, string name, object? value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
        public void BindAll(DbCommand command, Dictionary<string, object>? parameters)
        {
            if (parameters is not null) foreach (var p in parameters) Bind(command, p.Key, p.Value);
        }
    }

    private sealed class Connection : DbConnection
    {
        private readonly FakeDb _db;
        internal Connection(FakeDb db) => _db = db;
#if NET6_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.AllowNull]
#endif
        public override string ConnectionString { get; set; } = "";
        public override string Database => "fake";
        public override string DataSource => "fake";
        public override string ServerVersion => "1";
        public override ConnectionState State => ConnectionState.Open;
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new Command(_db, this);
    }

    private sealed class Command : DbCommand
    {
        private readonly FakeDb _db;
        private readonly SqlCommand _parameters = new();
        internal Command(FakeDb db, DbConnection connection)
        {
            _db = db;
            DbConnection = connection;
        }
#if NET6_0_OR_GREATER
        [System.Diagnostics.CodeAnalysis.AllowNull]
#endif
        public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbTransaction? DbTransaction { get; set; }
        protected override DbParameterCollection DbParameterCollection => _parameters.Parameters;
        public override void Cancel() { }
        public override int ExecuteNonQuery() => throw new NotSupportedException();
        public override object ExecuteScalar()
        {
            Record();
            return 7;
        }
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new SqlParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            Record();
            var rows = _db.Rows.Dequeue();
            var table = new DataTable();
            var count = rows.Length == 0 ? 2 : rows[0].Length;
            for (var index = 0; index < count; index++) table.Columns.Add("c" + index, typeof(object));
            foreach (var row in rows) table.Rows.Add(row.Select(value => value ?? DBNull.Value).ToArray());
            return table.CreateDataReader();
        }
        private void Record() => _db.Commands.Add((CommandText, Parameters.Cast<DbParameter>().ToDictionary(p => p.ParameterName, p => (object?)p.Value)));
    }
}
