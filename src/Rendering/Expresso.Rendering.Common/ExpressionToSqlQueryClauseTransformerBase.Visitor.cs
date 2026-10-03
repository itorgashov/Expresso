using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using System.Text;
using V = Expresso.Core.CriteriaExpressions.IExpressoVisitor<Expresso.Rendering.SqlRenderScope, System.Text.StringBuilder>;

namespace Expresso.Rendering
{
    public abstract partial class ExpressionToSqlQueryClauseTransformerBase : V
    {
        private StringBuilder Named(string sqlName, IReadOnlyList<AbstractExpression> arguments, SqlRenderScope s)
        {
            GenerateNamedFunction(sqlName, arguments, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        private StringBuilder Comparison(ComparisonFunction node, string op, SqlRenderScope s)
        {
            GenerateComparisonClause(node, op, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        private StringBuilder Arith(NumericArithFunction node, string op, SqlRenderScope s)
        {
            GenerateArithOperationClause(node, op, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        private StringBuilder Aggregate(string aggregateSql, CollectionRef collection, AbstractExpression? selector, AbstractExpression? predicate, SqlRenderScope s)
        {
            GenerateCollectionAggregateClause(aggregateSql, collection, selector, predicate, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        private StringBuilder Exists(CollectionRef collection, AbstractExpression? predicate, bool negate, bool negatePredicate, SqlRenderScope s)
        {
            GenerateExistsClause(collection, predicate, negate, negatePredicate, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitField(Field node, SqlRenderScope s)
        {
            GenerateFieldReference(node, s.FieldToColumnMap, s.Builder);
            return s.Builder;
        }

        StringBuilder V.VisitLiteral(Literal node, SqlRenderScope s) =>
            s.Builder.Append(AddParameter(node.Value, s.Parameters, s.ParamNamePrefix));

        StringBuilder V.VisitCollectionRef(CollectionRef node, SqlRenderScope s) =>
            throw new NotSupportedException($"Expression type '{nameof(CollectionRef)}' is not supported.");

        StringBuilder V.VisitAnd(AndFunc node, SqlRenderScope s)
        {
            GenerateAndClause(node, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitOr(OrFunc node, SqlRenderScope s)
        {
            GenerateOrClause(node, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitNot(NotFunc node, SqlRenderScope s)
        {
            GenerateNotClause(node, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitEq(EqFunc node, SqlRenderScope s) => Comparison(node, "=", s);
        StringBuilder V.VisitNeq(NeqFunc node, SqlRenderScope s) => Comparison(node, "!=", s);
        StringBuilder V.VisitGt(GtFunc node, SqlRenderScope s) => Comparison(node, ">", s);
        StringBuilder V.VisitGte(GteFunc node, SqlRenderScope s) => Comparison(node, ">=", s);
        StringBuilder V.VisitLt(LtFunc node, SqlRenderScope s) => Comparison(node, "<", s);
        StringBuilder V.VisitLte(LteFunc node, SqlRenderScope s) => Comparison(node, "<=", s);

        StringBuilder V.VisitIn(InFunc node, SqlRenderScope s)
        {
            GenerateInClause(node, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitIsNull(IsNullFunc node, SqlRenderScope s)
        {
            GenerateIsNullClause(node, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitAbs(AbsFunc node, SqlRenderScope s) => Named("ABS", node.Arguments, s);
        StringBuilder V.VisitAdd(AddFunc node, SqlRenderScope s) => Arith(node, "+", s);
        StringBuilder V.VisitSub(SubFunc node, SqlRenderScope s) => Arith(node, "-", s);
        StringBuilder V.VisitMult(MultFunc node, SqlRenderScope s) => Arith(node, "*", s);
        StringBuilder V.VisitDiv(DivFunc node, SqlRenderScope s) => Arith(node, "/", s);

        StringBuilder V.VisitMod(ModFunc node, SqlRenderScope s)
        {
            AppendMod(node, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitFloor(FloorFunc node, SqlRenderScope s) => Named("FLOOR", node.Arguments, s);
        StringBuilder V.VisitCeiling(CeilingFunc node, SqlRenderScope s) => Named(CeilingFunctionName, node.Arguments, s);

        StringBuilder V.VisitRound(RoundFunc node, SqlRenderScope s)
        {
            AppendRound(node, s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitSign(SignFunc node, SqlRenderScope s) => Named("SIGN", node.Arguments, s);
        StringBuilder V.VisitPower(PowerFunc node, SqlRenderScope s) => Named("POWER", node.Arguments, s);
        StringBuilder V.VisitSqrt(SqrtFunc node, SqlRenderScope s) => Named("SQRT", node.Arguments, s);

        StringBuilder V.VisitMin(MinFunc node, SqlRenderScope s)
        {
            GenerateMinMaxClause(node.Arguments[0], node.Arguments[1], "<", s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitMax(MaxFunc node, SqlRenderScope s)
        {
            GenerateMinMaxClause(node.Arguments[0], node.Arguments[1], ">", s.FieldToColumnMap, s.Builder, s.Parameters, s.ParamNamePrefix, s.Collections);
            return s.Builder;
        }

        StringBuilder V.VisitAny(AnyFunc node, SqlRenderScope s) =>
            Exists(node.Collection, node.Predicate, negate: false, negatePredicate: false, s);

        StringBuilder V.VisitNone(NoneFunc node, SqlRenderScope s) =>
            Exists(node.Collection, node.Predicate, negate: true, negatePredicate: false, s);

        StringBuilder V.VisitAll(AllFunc node, SqlRenderScope s) =>
            node.Predicate is null
                ? s.Builder.Append("(1 = 1)")
                : Exists(node.Collection, node.Predicate, negate: true, negatePredicate: true, s);

        StringBuilder V.VisitCollectionCount(CollectionCountFunc node, SqlRenderScope s) =>
            Aggregate("COUNT(*)", node.Collection, selector: null, node.Predicate, s);

        StringBuilder V.VisitCollectionMin(CollectionMinFunc node, SqlRenderScope s) =>
            Aggregate("MIN", node.Collection, node.Selector, predicate: null, s);

        StringBuilder V.VisitCollectionMax(CollectionMaxFunc node, SqlRenderScope s) =>
            Aggregate("MAX", node.Collection, node.Selector, predicate: null, s);

        StringBuilder V.VisitCollectionSum(CollectionSumFunc node, SqlRenderScope s) =>
            Aggregate("SUM", node.Collection, node.Selector, predicate: null, s);

        StringBuilder V.VisitCollectionAvg(CollectionAvgFunc node, SqlRenderScope s) =>
            Aggregate("AVG", node.Collection, node.Selector, predicate: null, s);
    }
}
