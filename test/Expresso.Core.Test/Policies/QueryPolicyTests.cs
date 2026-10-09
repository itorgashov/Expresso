using Expresso.Core.Filtering;
using Expresso.Core.Policies;

namespace Expresso.Core.Test.Policies;

public class QueryPolicyTests
{
    private sealed class Handle : QueryPolicy { }

    [Fact]
    public void ModelsPreserveCatalogAndShareLazyFingerprint()
    {
        var nested = new QueryModel(new[] { ("Title", typeof(string)) });
        var model = new QueryModel(new[] { ("Year", typeof(int)) }, new[] { new CollectionModel("Authors", nested) });
        var handle = new Handle();
        var copy = model.WithPolicy(handle);
        Assert.Null(model.Policy);
        Assert.Same(handle, copy.Policy);
        Assert.NotSame(model, copy);
        Assert.Equal(model.Fields, copy.Fields);
        Assert.Equal(model.Collections, copy.Collections);
        Assert.Same(model.CatalogFingerprint, copy.CatalogFingerprint);
        Assert.Null(copy.WithPolicy(null).Policy);
        Parallel.For(0, 100, _ => Assert.Same(model.CatalogFingerprint, copy.CatalogFingerprint));
        Assert.Equal("", QueryModel.Empty.CatalogFingerprint);
    }

    [Fact]
    public void FingerprintUsesCanonicalNamesTypesAndNestedCatalogs()
    {
        var a = new QueryModel(new[] { ("Year", typeof(int)), ("TITLE", typeof(string)) });
        var b = new QueryModel(new[] { ("title", typeof(string)), ("year", typeof(int)) });
        Assert.Equal(a.CatalogFingerprint, b.CatalogFingerprint);
        Assert.NotEqual(a.CatalogFingerprint, new QueryModel(new[] { ("year", typeof(double)), ("title", typeof(string)) }).CatalogFingerprint);
        Assert.NotEqual(a.CatalogFingerprint, new QueryModel(new[] { ("year", typeof(int)) }).CatalogFingerprint);
        Assert.NotEqual(a.CatalogFingerprint, new QueryModel(a.Fields, new[] { new CollectionModel("a", b) }).CatalogFingerprint);
        Assert.NotEqual(new QueryModel(null, new[] { new CollectionModel("a", a) }).CatalogFingerprint,
            new QueryModel(null, new[] { new CollectionModel("a", QueryModel.Empty) }).CatalogFingerprint);
        a.Fields[0] = ("tampered", typeof(double));
        Assert.Equal(a.CatalogFingerprint, a.WithPolicy(new Handle()).CatalogFingerprint);
    }

    [Fact]
    public void EnvelopeDefaultsAndSetters()
    {
        var d = new QueryPolicyDefinition();
        Assert.Equal("", d.Rules);
        Assert.Equal(QueryPolicyErrorDetail.Generic, d.ErrorDetail);
        Assert.Equal(new[] { 8, 100, 20, 100, 500, 5 }, Values(d.Limits));
        d.Rules = "default allow";
        d.ErrorDetail = QueryPolicyErrorDetail.Detailed;
        d.Limits = new QueryPolicyLimits { MaxDepth = 1, MaxNodes = 2, MaxArgs = 3, MaxInItems = 4, MaxStringLength = 5, MaxSortKeys = 6 };
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, Values(d.Limits));
        Assert.Equal("default allow", d.Rules);
        Assert.Equal(QueryPolicyErrorDetail.Detailed, d.ErrorDetail);
    }

    private static int[] Values(QueryPolicyLimits l) => new[] { l.MaxDepth, l.MaxNodes, l.MaxArgs, l.MaxInItems, l.MaxStringLength, l.MaxSortKeys };

    [Theory]
    [InlineData(QueryPolicyErrorDetail.Generic)]
    [InlineData(QueryPolicyErrorDetail.Detailed)]
    public void ViolationDetailsAreAlwaysAvailable(QueryPolicyErrorDetail detail)
    {
        var e = new QueryPolicyException(QueryPolicyTarget.Sort, QueryPolicyViolationKind.DenyRuleMatched, "len > title", "deny len(title)", "MaxNodes", detail);
        Assert.IsAssignableFrom<ArgumentException>(e);
        Assert.Equal(QueryPolicyTarget.Sort, e.Target);
        Assert.Equal(QueryPolicyViolationKind.DenyRuleMatched, e.Kind);
        Assert.Equal("len > title", e.Path);
        Assert.Equal("deny len(title)", e.RuleText);
        Assert.Equal("MaxNodes", e.LimitName);
        Assert.Equal(detail == QueryPolicyErrorDetail.Detailed, e.Message.Contains("title"));
        Assert.Contains("(root)", new QueryPolicyException(QueryPolicyTarget.Filter, QueryPolicyViolationKind.NotAllowed,
            errorDetail: QueryPolicyErrorDetail.Detailed).Message);
    }

    [Fact]
    public void CompileDiagnosticsAreOrderedAndCopied()
    {
        var errors = new[] {
            new QueryPolicyDiagnostic("S2", QueryPolicySeverity.Error, 2, 1, "unknown function"),
            new QueryPolicyDiagnostic("S1", QueryPolicySeverity.Error, 1, 2, "unknown field") };
        var e = new QueryPolicyCompileException(errors);
        Assert.IsAssignableFrom<ArgumentException>(e);
        Assert.Equal("S1", e.Diagnostics[0].Code);
        Assert.Equal(1, e.Diagnostics[0].Line);
        Assert.Equal(2, e.Diagnostics[0].Column);
        Assert.Equal(QueryPolicySeverity.Error, e.Diagnostics[0].Severity);
        Assert.Equal("unknown field", e.Diagnostics[0].Message);
        Assert.Contains("S1 (1,2)", e.Message);
        Assert.Throws<ArgumentNullException>(() => new QueryPolicyCompileException(null!));
        Assert.Throws<ArgumentNullException>(() => new QueryPolicyDiagnostic(null!, QueryPolicySeverity.Error, 1, 1, ""));
        Assert.Throws<ArgumentNullException>(() => new QueryPolicyDiagnostic("S1", QueryPolicySeverity.Error, 1, 1, null!));
    }
}
