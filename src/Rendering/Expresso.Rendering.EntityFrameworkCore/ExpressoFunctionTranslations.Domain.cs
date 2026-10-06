using System.Linq.Expressions;
using Expresso.Rendering.Linq;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Translation = System.Func<System.Collections.Generic.IReadOnlyList<Microsoft.EntityFrameworkCore.Query.SqlExpressions.SqlExpression>, Microsoft.EntityFrameworkCore.Query.SqlExpressions.SqlExpression>;

namespace Expresso.Rendering.EntityFrameworkCore
{
    internal static partial class ExpressoFunctionTranslations
    {
        /// <summary>
        /// <c>CASE WHEN NULLIF(value, NULL) IS NULL THEN 1 ELSE 0 END = 1</c>.
        /// Comparing a non-nullable value with NULL is folded to false, which drops domain errors inside <c>isnull</c>.
        /// <c>NULLIF</c> is nullable and does not propagate nullability, so EF keeps the call and the value is still evaluated.
        /// </summary>
        private static SqlExpression FoldSafeIsNull(SqlExpression value, RelationalTypeMapping boolMapping, RelationalTypeMapping intMapping)
        {
            var nullable = LinqEx.NullableType(Nullable.GetUnderlyingType(value.Type) ?? value.Type);
            var one = new SqlConstantExpression(Expression.Constant(1), intMapping);
            var nullConstant = new SqlConstantExpression(Expression.Constant(null, nullable), value.TypeMapping);
            var lifted = new SqlFunctionExpression(
                "NULLIF",
                new[] { value, nullConstant },
                nullable: true,
                argumentsPropagateNullability: new[] { false, false },
                value.Type,
                value.TypeMapping);
            var isNull = new SqlBinaryExpression(
                ExpressionType.Equal,
                lifted,
                nullConstant,
                typeof(bool),
                boolMapping);
            return new SqlBinaryExpression(
                ExpressionType.Equal,
                new CaseExpression(new[] { new CaseWhenClause(isNull, one) }, new SqlConstantExpression(Expression.Constant(0), intMapping)),
                one,
                typeof(bool),
                boolMapping);
        }

        /// <summary>Reads the mappings when the translation runs, after the static fields are initialized.</summary>
        private static IEnumerable<(string Marker, Type Value, Translation Translation)> DomainNullEntries(
            Func<RelationalTypeMapping> boolMapping,
            Func<RelationalTypeMapping> intMapping) =>
            new (string, Type, Translation)[]
            {
                (nameof(ExpressoDbFunctions.IsDomainNull), typeof(double), a => FoldSafeIsNull(a[0], boolMapping(), intMapping())),
                (nameof(ExpressoDbFunctions.IsDomainNull), typeof(int), a => FoldSafeIsNull(a[0], boolMapping(), intMapping())),
                (nameof(ExpressoDbFunctions.IsDomainNull), typeof(string), a => FoldSafeIsNull(a[0], boolMapping(), intMapping())),
            };
    }
}
