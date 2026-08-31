using System;
using System.Collections.Generic;
using System.Linq;
using Expresso.Core.Sorting;
using Expresso.SqlServer;

namespace Expresso.Sample.Shared.DataAccess;

internal static class NestedSortHelper
{
    public static SortDirective? ResolveNested(SortDirective? root, params string[] path)
    {
        if (root is null || path.Length == 0)
        {
            return null;
        }

        var current = root;
        foreach (var segment in path)
        {
            var nested = current.Nested.FirstOrDefault(n =>
                n.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));
            if (nested is null)
            {
                return null;
            }

            current = nested.Directive;
        }

        return current;
    }

    public static string RenderOrderByOrDefault(
        SortDirective? nestedDirective,
        string defaultOrderBy,
        Dictionary<string, string> itemFieldToColumn,
        IExpressionToQueryClauseTransformer transformer,
        string paramPrefix,
        Dictionary<string, object>? parameters)
    {
        if (nestedDirective is not null && nestedDirective.Items.Count > 0)
        {
            var mapping = new SqlQueryMapping(itemFieldToColumn);
            var result = transformer.RenderOrderByClause(nestedDirective, mapping, paramPrefix);
            if (parameters is not null)
            {
                SqlParameterExtensions.MergeParameters(parameters, result.parameters);
            }

            return result.orderByClause;
        }

        return defaultOrderBy;
    }
}
