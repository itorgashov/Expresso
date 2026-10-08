using System.Collections.Generic;
using System.Text;
using Expresso.Core.Paging;
using Expresso.Rendering;

namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Appends a rendered paging clause and its parameters to a list query.</summary>
internal static class SamplePagingSql
{
    public const string ParamPrefix = "pparam";

    public static void Append(
        StringBuilder sql,
        ref Dictionary<string, object>? parameters,
        IExpressionToQueryClauseTransformer transformer,
        PagingDirective paging)
    {
        if (paging.IsEmpty)
        {
            return;
        }

        var rendered = transformer.RenderPagingClause(paging, ParamPrefix);
        if (rendered.pagingClause.Length == 0)
        {
            return;
        }

        sql.Append(' ');
        sql.Append(rendered.pagingClause);
        parameters ??= new Dictionary<string, object>();
        ParameterMerge.Merge(parameters, rendered.parameters);
    }
}
