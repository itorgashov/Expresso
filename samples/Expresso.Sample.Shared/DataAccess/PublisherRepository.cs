using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Rendering;
using Expresso.Sample.Shared.Models;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Reads publishers with minimal projection and conditional related queries.</summary>
public sealed class PublisherRepository : ProjectedSampleRepository<Publisher>
{
    /// <summary>Creates the repository for the selected engine.</summary>
    public PublisherRepository(ISampleDb db, IExpressionToQueryClauseTransformer renderer,
        IProjectionToSqlTransformer projection, SampleProjectionMappings mappings) :
        base(db, renderer, projection, Query(db.Sql), mappings.Publishers,
            db.Sql.TableAs("publisher", "p"), db.Sql.Col("p", "id")) { }
    private static SqlQueryMapping Query(ISampleSql sql) => new SqlQueryMapping(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["id"] = sql.Col("p", "id"), ["name"] = sql.Col("p", "name"), ["country"] = sql.Col("p", "country"),
            ["location"] = sql.Col("p", "location"), ["opens"] = sql.Col("p", "opens_at"), ["closes"] = sql.Col("p", "closes_at") });
}
