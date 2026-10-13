using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Rendering;
using Expresso.Sample.Shared.Models;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Reads authors with minimal projection and conditional related queries.</summary>
public sealed class AuthorRepository : ProjectedSampleRepository<Author>
{
    /// <summary>Creates the repository for the selected engine.</summary>
    public AuthorRepository(ISampleDb db, IExpressionToQueryClauseTransformer renderer,
        IProjectionToSqlTransformer projection, SampleProjectionMappings mappings) :
        base(db, renderer, projection, Query(db.Sql), mappings.Authors,
            db.Sql.TableAs("author", "a"), db.Sql.Col("a", "id")) { }
    private static SqlQueryMapping Query(ISampleSql sql) => new SqlQueryMapping(new SampleSqlMappings(sql).AuthorItemFields, new[] { new SampleSqlMappings(sql).AwardsOnAuthor });
}
