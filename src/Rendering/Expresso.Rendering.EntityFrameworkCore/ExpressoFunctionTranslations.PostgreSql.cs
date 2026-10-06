using Microsoft.EntityFrameworkCore.Storage;
using Translation = System.Func<System.Collections.Generic.IReadOnlyList<Microsoft.EntityFrameworkCore.Query.SqlExpressions.SqlExpression>, Microsoft.EntityFrameworkCore.Query.SqlExpressions.SqlExpression>;

namespace Expresso.Rendering.EntityFrameworkCore
{
    internal static partial class ExpressoFunctionTranslations
    {
        private static IEnumerable<(string Marker, Type Value, Translation Translation)> PostgreSqlEntries()
        {
            var entries = new (string, Type, Translation)[]
            {
                (nameof(ExpressoDbFunctions.Round), typeof(double), a =>
                    Function("ROUND", typeof(double), Cast(a[0], typeof(decimal), new DecimalTypeMapping("numeric")), a[1])),
                (nameof(ExpressoDbFunctions.Left), typeof(string), a => Function("LEFT", typeof(string), a[0], a[1])),
                (nameof(ExpressoDbFunctions.Right), typeof(string), a => Function("RIGHT", typeof(string), a[0], a[1])),
            };
            return entries
                .Concat(DomainNullEntries(() => PostgreSqlBool!, () => PostgreSqlInt!))
                .Concat(LiteralEntries("substr"))
                .Concat(StringLiteralEntries("LENGTH", indexOf: a => MinusOne(Function("STRPOS", typeof(int), a[0], a[1]))))
                .Concat(NumericLiteralEntries(round: false));
        }
    }
}
