using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Parsing.Policies.Catalog;

namespace Expresso.Parsing.Test.Policies;

public class FunctionCatalogTests
{
    internal static QueryModel Model()
    {
        var fields = TypeKinds.Types.Where(p => p.Key != TypeKind.Collection)
            .Select(p => (p.Key.ToString().ToLowerInvariant(), p.Value))
            .Append(("other", typeof(long))).ToArray();
        return new QueryModel(fields, new[] { new CollectionModel("items", new QueryModel(fields)) });
    }
    internal static string Operand(TypeKind kind) => kind == TypeKind.Collection ? "items" :
        kind == TypeKind.Boolean ? "eq(int,1)" : TypeKinds.Each(kind).First().ToString().ToLowerInvariant();

    private static readonly TypeKind[] OperandKinds = TypeKinds.Types.Keys.Append(TypeKind.Other).ToArray();

    internal static string Query(FunctionInfo f, int count) => f.Names[0] + "(" + string.Join(",",
        Enumerable.Range(0, count).Select(i => Operand(f.Signatures[0].At(i)))) + ")";

    [Fact]
    public void EveryVisitorFunctionIsCataloguedOnce()
    {
        var functions = typeof(IExpressoVisitor<,>).GetMethods().Select(m => m.GetParameters()[0].ParameterType)
            .Where(t => typeof(AbstractFunction).IsAssignableFrom(t)).OrderBy(t => t.FullName).ToArray();
        Assert.Equal(64, functions.Length);
        Assert.Equal(functions, FunctionCatalog.All.Select(f => f.NodeType).OrderBy(t => t.FullName));
        Assert.Equal(64, FunctionCatalog.ByType.Count);
        Assert.All(FunctionCatalog.All, f => Assert.Single(FunctionCatalog.Resolve("@" + f.Category).Where(x => x == f)));
        Assert.Equal(typeof(SubStringFunc), Assert.Single(FunctionCatalog.Resolve("SUBSTR")).NodeType);
        Assert.Equal(2, FunctionCatalog.Resolve("min").Count());
        Assert.Empty(FunctionCatalog.Resolve("@unknown"));
        Assert.DoesNotContain(FunctionCatalog.Resolve("@predicate"), f => f.Category == "logical");
    }

    public static IEnumerable<object[]> Arities() => FunctionCatalog.All.SelectMany(f =>
        Enumerable.Range(0, 6).Select(n => new object[] { f.NodeType.Name, n }));

    [Theory, MemberData(nameof(Arities))]
    public void ArityAndTypesAgreeWithExpressionParser(string type, int count)
    {
        var f = FunctionCatalog.All.Single(x => x.NodeType.Name == type);
        var query = Query(f, count);
        if (count < f.Minimum || count > f.Maximum)
            Assert.ThrowsAny<Exception>(() => new ExpressionParser().Parse(query, Model()));
        else
        {
            var node = new ExpressionParser().Parse(query, Model())!;
            Assert.Equal(f.NodeType, node.GetType());
            Assert.Contains(f.Signatures, s => (s.Result & TypeKinds.Of(node.ReturnType)) != 0);
        }
    }

    [Fact]
    public void EveryCatalogSignatureAndArgumentKindAgreesWithExpressionParser()
    {
        // Probe every signature and every argument position with each catalogued CLR kind.
        // The policy type table should accept exactly the combinations constructible by the IR parser.
        var parser = new ExpressionParser();
        var model = Model();
        foreach (var function in FunctionCatalog.All)
        foreach (var signature in function.Signatures)
        {
            int count = function.Minimum;
            var baseline = Enumerable.Range(0, count).Select(i => TypeKinds.Each(signature.At(i)).First()).ToArray();
            for (int position = 0; position < count; position++)
            foreach (var candidate in OperandKinds)
            {
                var kinds = (TypeKind[])baseline.Clone();
                kinds[position] = candidate;
                string query = function.Names[0] + "(" + string.Join(",", kinds.Select(Operand)) + ")";
                bool catalogAccepts = function.Signatures.Any(s => kinds.Select((kind, index) =>
                    (s.At(index) & kind) != 0).All(valid => valid));
                bool parserAccepts;
                try { parserAccepts = parser.Parse(query, model)?.GetType() == function.NodeType; }
                catch (Exception) { parserAccepts = false; }
                Assert.True(catalogAccepts == parserAccepts,
                    $"{function.NodeType.Name}: {query}; catalog={catalogAccepts}; parser={parserAccepts}");
            }
        }
    }
}
