using System;
using Expresso.Core.Filtering;
using Expresso.Core.Projection;
using Expresso.Sample.Shared.DataAccess;

namespace Expresso.Sample.WebApi.NetFx.Filtering;

/// <summary>Response catalogs derived from the configured ADO.NET projection mappings.</summary>
public sealed class RequestProjectionModelProvider : IRequestProjectionModelProvider
{
    private readonly QueryModel _books;
    private readonly QueryModel _authors;
    private readonly QueryModel _publishers;

    /// <summary>Creates cached catalogs from the same engine-configured mappings used by repositories.</summary>
    public RequestProjectionModelProvider(SampleProjectionMappings mappings)
    {
        if (mappings is null) throw new ArgumentNullException(nameof(mappings));
        _books = mappings.Books.GetProjectionModel();
        _authors = mappings.Authors.GetProjectionModel();
        _publishers = mappings.Publishers.GetProjectionModel();
    }

    /// <inheritdoc />
    public QueryModel GetProjectionModel(string context) => context.ToLowerInvariant() switch
    {
        "book" => _books,
        "author" => _authors,
        "publisher" => _publishers,
        _ => QueryModel.Empty,
    };
}
