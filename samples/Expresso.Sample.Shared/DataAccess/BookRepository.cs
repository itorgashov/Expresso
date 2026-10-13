using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Rendering;
using Expresso.Sample.Shared.Models;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Reads books with minimal projection and conditional related queries.</summary>
public sealed class BookRepository : ProjectedSampleRepository<Book>
{
    /// <summary>Creates the repository for the selected engine.</summary>
    public BookRepository(ISampleDb db, IExpressionToQueryClauseTransformer renderer,
        IProjectionToSqlTransformer projection, SampleProjectionMappings mappings) :
        base(db, renderer, projection, Query(db.Sql), mappings.Books,
            db.Sql.TableAs("book", "b") + " INNER JOIN " + db.Sql.TableAs("publisher", "p") +
            " ON " + db.Sql.Col("p", "id") + " = " + db.Sql.Col("b", "publisher_id"), db.Sql.Col("b", "id")) { }
    private static SqlQueryMapping Query(ISampleSql sql) => new SqlQueryMapping(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = sql.Col("b", "id"), ["title"] = sql.Col("b", "title"), ["year"] = sql.Col("b", "year"),
            ["isbn"] = sql.Col("b", "isbn"), ["publisher"] = sql.Col("p", "name"), ["price"] = sql.Col("b", "price"),
            ["rating"] = sql.Col("b", "rating"), ["createdat"] = sql.Col("b", "created_at"), ["externalid"] = sql.Col("b", "external_id")
        }, new[] { new SampleSqlMappings(sql).BookAuthors });
}
