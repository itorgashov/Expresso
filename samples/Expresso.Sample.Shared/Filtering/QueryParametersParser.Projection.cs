using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Expresso.Core.Filtering;
using Expresso.Core.Projection;
using Expresso.Parsing;

namespace Expresso.Sample.Shared.Filtering;

public static partial class QueryParametersParser
{
    /// <summary>Validated projection or a client input error.</summary>
    public sealed class ProjectionParseResult
    {
        /// <summary>Parsed selection, or All on invalid input.</summary>
        public ProjectionDirective Projection { get; init; } = ProjectionDirective.All;
        /// <summary>Whether fields should produce HTTP 400.</summary>
        public bool IsBadRequest { get; init; }
    }
    /// <summary>Parses fields independently of filter/sort policy.</summary>
    public static ProjectionParseResult ParseProjection(string? fields, IProjectionDirectiveParser parser, QueryModel model, bool isPresent = false)
    {
        try
        {
            return new ProjectionParseResult { Projection = parser.Parse(isPresent ? fields ?? string.Empty : fields, model) };
        }
        catch (ArgumentException)
        {
            return new ProjectionParseResult { IsBadRequest = true };
        }
    }
}
