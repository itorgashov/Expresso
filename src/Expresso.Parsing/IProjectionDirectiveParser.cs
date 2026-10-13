using Expresso.Core.Filtering;
using Expresso.Core.Projection;

namespace Expresso.Parsing;

/// <summary>Parses comma-separated projection paths against an endpoint allow-list.</summary>
public interface IProjectionDirectiveParser
{
    /// <summary>Parses fields. Null selects the supplied catalog recursively, never additional mapping fields.
    /// An empty catalog selects no public values; present empty and unknown/malformed paths throw <see cref="ArgumentException"/>.</summary>
    ProjectionDirective Parse(string? fields, QueryModel model);
}
