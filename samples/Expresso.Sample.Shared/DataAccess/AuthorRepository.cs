using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Sample.Shared.Models;
using Expresso.SqlServer;
using Microsoft.Data.SqlClient;

namespace Expresso.Sample.Shared.DataAccess;

public sealed class AuthorRepository : IRepository<Author>
{
    private const string WhereParamPrefix = "wparam";
    private const string OrderParamPrefix = "oparam";
    private const string AwardOrderParamPrefix = "awardOrder";

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IExpressionToQueryClauseTransformer _criteriaTransformer;

    private readonly SqlQueryMapping _queryMapping = new SqlQueryMapping(
        SampleSqlMappings.AuthorItemFields,
        new[] { SampleSqlMappings.AwardsOnAuthor });

    private const string BaseSelect =
        "SELECT" +
        " a.id," +
        " a.first_name," +
        " a.last_name," +
        " a.display_name," +
        " a.date_of_birth," +
        " a.created_at" +
        " FROM dbo.author AS a";

    public AuthorRepository(
        ISqlConnectionFactory connectionFactory,
        IExpressionToQueryClauseTransformer criteriaTransformer)
    {
        _connectionFactory = connectionFactory;
        _criteriaTransformer = criteriaTransformer;
    }

    public async Task<IReadOnlyList<Author>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using (connection)
        {
            var (sql, parameters) = BuildSelectQuery(filterCriteria, sortDirective);

            var authors = new List<Author>();
            using (var command = new SqlCommand(sql, connection))
            {
                command.AddParameters(parameters);
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        authors.Add(ReadAuthor(reader));
                    }
                }
            }

            await LoadAwardsAsync(connection, authors, sortDirective, cancellationToken);
            return authors;
        }
    }

    public async Task<Author?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using (connection)
        {
            var sql = BaseSelect + " WHERE a.id = @id";

            Author? author = null;
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        author = ReadAuthor(reader);
                    }
                }
            }

            if (author is null)
            {
                return null;
            }

            await LoadAwardsAsync(connection, new List<Author> { author }, sortDirective: null, cancellationToken);
            return author;
        }
    }

    private (string sql, Dictionary<string, object>? parameters) BuildSelectQuery(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective)
    {
        var sql = new StringBuilder(BaseSelect);
        Dictionary<string, object>? parameters = null;

        if (filterCriteria is not null)
        {
            var result = _criteriaTransformer.RenderWhereClause(filterCriteria, _queryMapping, WhereParamPrefix);
            sql.Append(" WHERE ");
            sql.Append(result.whereClause);
            parameters = new Dictionary<string, object>(result.parameters);
        }

        if (sortDirective is not null && sortDirective.Items.Count > 0)
        {
            var result = _criteriaTransformer.RenderOrderByClause(sortDirective, _queryMapping, OrderParamPrefix);
            sql.Append(" ORDER BY ");
            sql.Append(result.orderByClause);
            parameters ??= new Dictionary<string, object>();
            SqlParameterExtensions.MergeParameters(parameters, result.parameters);
        }

        return (sql.ToString(), parameters);
    }

    private async Task LoadAwardsAsync(
        SqlConnection connection,
        IReadOnlyList<Author> authors,
        SortDirective? sortDirective,
        CancellationToken cancellationToken)
    {
        if (authors.Count == 0)
        {
            return;
        }

        var authorIds = authors.Select(a => a.Id).Distinct().ToList();
        var awardSort = NestedSortHelper.ResolveNested(sortDirective, "awards");
        var awardOrderBy = NestedSortHelper.RenderOrderByOrDefault(
            awardSort,
            "aw.year, aw.title",
            SampleSqlMappings.AwardItemFields,
            _criteriaTransformer,
            AwardOrderParamPrefix,
            parameters: null);

        var idParameters = string.Join(", ", authorIds.Select((_, i) => $"@authorId{i}"));
        var sql =
            "SELECT aw.author_id, aw.title, aw.year" +
            " FROM dbo.award AS aw" +
            $" WHERE aw.author_id IN ({idParameters})" +
            $" ORDER BY aw.author_id, {awardOrderBy}";

        var awardsByAuthorId = new Dictionary<int, List<Award>>();
        using (var command = new SqlCommand(sql, connection))
        {
            for (var i = 0; i < authorIds.Count; i++)
            {
                command.Parameters.AddWithValue($"@authorId{i}", authorIds[i]);
            }

            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var authorId = reader.GetInt32(0);
                    if (!awardsByAuthorId.TryGetValue(authorId, out var awards))
                    {
                        awards = new List<Award>();
                        awardsByAuthorId[authorId] = awards;
                    }

                    awards.Add(new Award
                    {
                        Title = reader.GetString(1),
                        Year = reader.GetInt16(2),
                    });
                }
            }
        }

        foreach (var author in authors)
        {
            if (awardsByAuthorId.TryGetValue(author.Id, out var awards))
            {
                author.Awards.AddRange(awards);
            }
        }
    }

    private static Author ReadAuthor(SqlDataReader reader) =>
        new Author
        {
            Id = reader.GetInt32(0),
            FirstName = reader.GetString(1),
            LastName = reader.GetString(2),
            DisplayName = reader.GetString(3),
            DateOfBirth = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            CreatedAt = reader.GetDateTime(5),
        };
}
