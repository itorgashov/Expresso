using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
using Expresso.Core.Paging;
using Expresso.Core.Projection;
using Expresso.Core.Sorting;
using Expresso.Rendering.EntityFramework;
using Expresso.Rendering.Linq;
using Expresso.Sample.WebApi.NetFx.Ef6.Entities;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

/// <summary>Projects publishers and preserves the sample's Oracle/SQLite clock representation workaround.</summary>
public sealed class PublisherRepository : IRepository<Publisher>
{
    private readonly SampleEf6Context _db;
    private readonly IExpressionToLinqTransformer _transformer;
    /// <summary>Creates the repository for the configured provider.</summary>
    public PublisherRepository(SampleEf6Context db, IExpressionToLinqTransformer transformer)
    {
        _db = db;
        _transformer = transformer;
    }
    /// <inheritdoc />
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> GetAllAsync(FilterCriteria? filterCriteria,
        SortDirective? sortDirective, PagingDirective paging, ProjectionDirective projection, CancellationToken cancellationToken = default)
    {
        IQueryable<Publisher> query = _db.Publishers;
        if (filterCriteria is not null) query = query.Where(_transformer, filterCriteria, BookLinqMappings.Publishers);
        query = Ef6ListQuery.OrderAndPage(query, sortDirective, paging, _transformer, BookLinqMappings.Publishers, p => p.Id, _db);
        return await ReadAsync(query, projection, cancellationToken);
    }
    /// <inheritdoc />
    public Task<long> CountAsync(FilterCriteria? filterCriteria, CancellationToken cancellationToken = default) =>
        Ef6ListQuery.CountAsync(_db.Publishers, filterCriteria, BookLinqMappings.Publishers, _transformer, cancellationToken);
    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, object?>?> GetByIdAsync(int id, ProjectionDirective projection, CancellationToken cancellationToken = default) =>
        (await ReadAsync(_db.Publishers.Where(p => p.Id == id), projection, cancellationToken)).FirstOrDefault();

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Ef6Provider, LinqProjectionMapping<Publisher, PublisherProjection>> Mappings = new();

    /// <summary>Gets a cached provider-specific publisher mapping; Oracle placeholders never expose identity as clocks.</summary>
    public static LinqProjectionMapping<Publisher, PublisherProjection> GetMapping(Ef6Provider provider) => Mappings.GetOrAdd(provider, CreateMapping);

    private static LinqProjectionMapping<Publisher, PublisherProjection> CreateMapping(Ef6Provider provider)
    {
        var map = new LinqProjectionMapping<Publisher, PublisherProjection>()
            .Key("id", p => p.Id).Field("id", "id", p => p.Id, p => p.Id)
            .Field("name", "name", p => p.Name, p => p.Name)
            .Field("country", "country", p => p.Country, p => p.Country)
            .Field("location", "location", p => p.Location, p => p.Location);
        if (provider == Ef6Provider.Sqlite)
            map.Field("opens", "opensAt", p => p.OpensAtText, p => p.OpensText, text => PublisherTimes.ParseTime(text))
               .Field("closes", "closesAt", p => p.ClosesAtText, p => p.ClosesText, text => PublisherTimes.ParseTime(text));
        else if (provider == Ef6Provider.Oracle)
            map.Field("opens", "opensAt", p => p.Id, p => p.OpenToken, _ => null)
               .Field("closes", "closesAt", p => p.Id, p => p.CloseToken, _ => null);
        else
            map.Field("opens", "opensAt", p => p.OpensAt, p => p.OpensAt)
               .Field("closes", "closesAt", p => p.ClosesAt, p => p.ClosesAt);
        return map;
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ReadAsync(IQueryable<Publisher> query,
        ProjectionDirective projection, CancellationToken cancellationToken)
    {
        var provider = Ef6Providers.Resolve(_db.Database.Connection);
        var map = GetMapping(provider);
        var result = await query.ToProjectedListAsync(_transformer, projection, map, cancellationToken: cancellationToken, keySource: _db.Publishers);
        var shape = ProjectionShaper.ToSparseObjects(result);
        var opens = projection.IsAll || projection.Find("opens") is not null;
        var closes = projection.IsAll || projection.Find("closes") is not null;
        if (provider != Ef6Provider.Oracle || (!opens && !closes) || shape.Count == 0) return shape;
        var ids = Enumerable.Range(0, shape.Count).Select(i => (int)result.GetKey(i)[0]!).ToArray();
        var times = await PublisherTimes.ReadSelectedAsync(_db, ids, opens, closes, cancellationToken);
        return shape.Select((item, i) =>
        {
            var values = item.ToDictionary(p => p.Key, p => p.Value);
            if (times.TryGetValue(ids[i], out var clocks))
                foreach (var clock in clocks) values[clock.Key] = clock.Value;
            return (IReadOnlyDictionary<string, object?>)values;
        }).ToArray();
    }
    /// <summary>Scalar publisher values and provider-specific clock carriers used before sparse shaping.</summary>
    public sealed class PublisherProjection
    {
        /// <summary>Publisher identity.</summary>
        public int Id { get; set; }
        /// <summary>Publisher name.</summary>
        public string Name { get; set; } = "";
        /// <summary>Publisher country.</summary>
        public string Country { get; set; } = "";
        /// <summary>Optional publisher location.</summary>
        public string? Location { get; set; }
        /// <summary>Opening time on providers that expose native clocks.</summary>
        public TimeSpan OpensAt { get; set; }
        /// <summary>Closing time on providers that expose native clocks.</summary>
        public TimeSpan ClosesAt { get; set; }
        /// <summary>SQLite opening-time text converted after materialization.</summary>
        public string? OpensText { get; set; }
        /// <summary>SQLite closing-time text converted after materialization.</summary>
        public string? ClosesText { get; set; }
        /// <summary>Oracle identity placeholder; the selected opening time is read separately.</summary>
        public int OpenToken { get; set; }
        /// <summary>Oracle identity placeholder; the selected closing time is read separately.</summary>
        public int CloseToken { get; set; }
    }
}
