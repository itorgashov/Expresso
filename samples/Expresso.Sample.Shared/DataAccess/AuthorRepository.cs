using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.Sample.Shared.Models;
using Expresso.Rendering;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Loads authors and their awards from the sample database.</summary>
public sealed class AuthorRepository : IRepository<Author>
{
    private const string WhereParamPrefix = "wparam";
    private const string OrderParamPrefix = "oparam";
    private const string AwardOrderParamPrefix = "awardOrder";

    private readonly ISampleDb _db;
    private readonly IExpressionToQueryClauseTransformer _criteriaTransformer;
    private readonly SampleSqlMappings _mappings;
    private readonly SqlQueryMapping _queryMapping;
    private readonly string _baseSelect;

    /// <summary>Creates the repository.</summary>
    /// <param name="db">Open and bind helper for the selected engine.</param>
    /// <param name="criteriaTransformer">Renderer that turns filters and sorts into SQL.</param>
    public AuthorRepository(
        ISampleDb db,
        IExpressionToQueryClauseTransformer criteriaTransformer)
    {
        _db = db;
        _criteriaTransformer = criteriaTransformer;
        var sql = db.Sql;
        _mappings = new SampleSqlMappings(sql);
        _queryMapping = new SqlQueryMapping(
            _mappings.AuthorItemFields,
            new[] { _mappings.AwardsOnAuthor });
        _baseSelect =
            "SELECT" +
            " " + sql.Col("a", "id") + "," +
            " " + sql.Col("a", "first_name") + "," +
            " " + sql.Col("a", "last_name") + "," +
            " " + sql.Col("a", "display_name") + "," +
            " " + sql.Col("a", "date_of_birth") + "," +
            " " + sql.Col("a", "created_at") +
            " FROM " + sql.TableAs("author", "a");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Author>> GetAllAsync(
        FilterCriteria? filterCriteria,
        SortDirective? sortDirective,
        CancellationToken cancellationToken = default)
    {
        var connection = await _db.OpenAsync(cancellationToken);
        using (connection)
        {
            var (sql, parameters) = BuildSelectQuery(filterCriteria, sortDirective);

            var authors = new List<Author>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                _db.BindAll(command, parameters);
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

    /// <inheritdoc />
    public async Task<Author?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var connection = await _db.OpenAsync(cancellationToken);
        using (connection)
        {
            var sql = _baseSelect + " WHERE " + _db.Sql.Col("a", "id") + " = " + _db.Sql.Param("id");

            Author? author = null;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                _db.Bind(command, _db.Sql.Param("id"), id);
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
        var sql = new StringBuilder(_baseSelect);
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
            ParameterMerge.Merge(parameters, result.parameters);
        }

        return (sql.ToString(), parameters);
    }

    private async Task LoadAwardsAsync(
        DbConnection connection,
        IReadOnlyList<Author> authors,
        SortDirective? sortDirective,
        CancellationToken cancellationToken)
    {
        if (authors.Count == 0)
        {
            return;
        }

        var authorIds = authors.Select(a => a.Id).Distinct().ToList();
        var awardParams = new Dictionary<string, object>();
        var awardSort = NestedSortHelper.ResolveNested(sortDirective, "awards");
        var awardOrderBy = NestedSortHelper.RenderOrderByOrDefault(
            awardSort,
            _db.Sql.Col("aw", "year") + ", " + _db.Sql.Col("aw", "title"),
            _mappings.AwardItemFields,
            _criteriaTransformer,
            AwardOrderParamPrefix,
            awardParams);

        var idParameters = string.Join(", ", authorIds.Select((_, i) => _db.Sql.Param("authorId" + i)));
        var sql =
            "SELECT " + _db.Sql.Col("aw", "author_id") + ", " + _db.Sql.Col("aw", "title") + ", " + _db.Sql.Col("aw", "year") +
            " FROM " + _db.Sql.TableAs("award", "aw") +
            " WHERE " + _db.Sql.Col("aw", "author_id") + " IN (" + idParameters + ")" +
            " ORDER BY " + _db.Sql.Col("aw", "author_id") + ", " + awardOrderBy;

        var awardsByAuthorId = new Dictionary<int, List<Award>>();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = sql;
            for (var i = 0; i < authorIds.Count; i++)
            {
                _db.Bind(command, _db.Sql.Param("authorId" + i), authorIds[i]);
            }

            _db.BindAll(command, awardParams);

            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var authorId = SampleDbValues.GetInt32(reader, 0);
                    if (!awardsByAuthorId.TryGetValue(authorId, out var awards))
                    {
                        awards = new List<Award>();
                        awardsByAuthorId[authorId] = awards;
                    }

                    awards.Add(new Award
                    {
                        Title = reader.GetString(1),
                        Year = SampleDbValues.GetInt16(reader, 2),
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

    private static Author ReadAuthor(DbDataReader reader) =>
        new Author
        {
            Id = SampleDbValues.GetInt32(reader, 0),
            FirstName = reader.GetString(1),
            LastName = reader.GetString(2),
            DisplayName = reader.GetString(3),
            DateOfBirth = reader.IsDBNull(4) ? null : SampleDbValues.GetDateTime(reader, 4),
            CreatedAt = SampleDbValues.GetDateTime(reader, 5),
        };
}
