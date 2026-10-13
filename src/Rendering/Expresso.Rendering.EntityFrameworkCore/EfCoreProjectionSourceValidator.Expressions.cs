using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Expresso.Rendering.EntityFrameworkCore;

internal sealed partial class EfCoreProjectionSourceValidator
{
    private readonly HashSet<Expression> _expressions = new(ReferenceEqualityComparer.Instance);

    private static bool IsExpressionValue(Type type) => typeof(Expression).IsAssignableFrom(type);
    private static bool IsOpaqueValue(Type type) => IsSequence(type) || IsExpressionValue(type);

    private void VisitStoredExpression(Expression expression, Expression? boundBody = null)
    {
        // Active recursion is opaque; repeated independent uses still need their own parameter bindings checked.
        if (!_expressions.Add(expression)) throw OpaqueSource();
        try { Visit(boundBody ?? expression); }
        finally { _expressions.Remove(expression); }
    }

    private static LambdaExpression? LambdaArgument(Expression argument)
    {
        if (argument is UnaryExpression { NodeType: ExpressionType.Quote } quote) argument = quote.Operand;
        if (argument is LambdaExpression lambda) return lambda;
        if (TryReadStoredValue(argument, out var value) && value is LambdaExpression stored) return stored;
        if (IsExpressionValue(argument.Type) || typeof(Delegate).IsAssignableFrom(argument.Type)) throw OpaqueSource();
        return null;
    }

    private bool IsMappedAccess(Expression owner, string name) => IsBoundOwner(owner) && HasMappedMember(owner, name, false);

    private bool IsBoundOwner(Expression owner)
    {
        owner = Unwrap(owner);
        if (owner is ParameterExpression) return true;
        if (owner is ConditionalExpression conditional) return IsBoundOwner(conditional.IfTrue) && IsBoundOwner(conditional.IfFalse);
        if (owner is MemberExpression { Expression: { } parent } member)
            return IsBoundOwner(parent) && HasMappedMember(parent, member.Member.Name, true);
        if (owner is MethodCallExpression call && call.Method.DeclaringType == typeof(EF) && call.Method.Name == nameof(EF.Property) &&
            TryReadStoredValue(call.Arguments[1], out var name) && name is string property)
            return IsBoundOwner(call.Arguments[0]) && HasMappedMember(call.Arguments[0], property, true);
        return false;
    }

    private bool HasMappedMember(Expression owner, string name, bool reference)
    {
        if (_models.Count == 0) return true; // Preserve parameter-bound constructed LINQ/test providers without EF metadata.
        var type = EffectiveOwnerType(owner);
        return _models.SelectMany(m => m.GetEntityTypes()).Any(e => e.ClrType.IsAssignableFrom(type) &&
            (reference ? e.FindNavigation(name)?.IsCollection == false :
                e.FindNavigation(name)?.IsCollection == true || e.FindSkipNavigation(name)?.IsCollection == true || e.FindProperty(name) is not null));
    }

    private static Type EffectiveOwnerType(Expression owner)
    {
        // Provenance can strip a narrowing cast, but navigation metadata must retain
        // its derived type. Only widening conversions can hide that effective type.
        while (owner is UnaryExpression { Method: null } unary &&
            unary.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs &&
            unary.Type.IsAssignableFrom(unary.Operand.Type))
            owner = unary.Operand;
        return owner.Type;
    }

    private static Expression Unwrap(Expression expression)
    {
        while (expression is UnaryExpression { Method: null } unary &&
            unary.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs) expression = unary.Operand;
        return expression;
    }
}
