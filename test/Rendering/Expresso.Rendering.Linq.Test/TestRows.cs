using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.Linq.Test
{
    public sealed class Row
    {
        public int Id { get; set; }
        public int? Score { get; set; }
        public string? Text { get; set; }
        public double? Amount { get; set; }
        public byte Code { get; set; }
        public bool? Flag { get; set; }
        public DateTime? When { get; set; }
        public TimeSpan Clock { get; set; }
#if NET6_0_OR_GREATER
        public DateOnly Day { get; set; }
        public TimeOnly At { get; set; }
#endif
        public List<Child> Children { get; set; } = new();
    }

    public sealed class Child
    {
        public int? Value { get; set; }
        public string? Name { get; set; }
        public byte Small { get; set; }
    }

    /// <summary>Mapping, IR shortcuts and query helpers for scenario tests over <see cref="Row"/>.</summary>
    public static class TestRows
    {
        public static readonly InMemoryExpressionToLinqTransformer InMemoryT = new();
        public static readonly QueryableExpressionToLinqTransformer QueryableT = new();

        public static readonly Field Id = new("id", typeof(int));
        public static readonly Field Score = new("score", typeof(int));
        public static readonly Field Text = new("text", typeof(string));
        public static readonly Field Amount = new("amount", typeof(double));
        public static readonly Field Code = new("code", typeof(byte));
        public static readonly Field Flag = new("flag", typeof(bool));
        public static readonly Field When = new("when", typeof(DateTime));
        public static readonly Field Clock = new("clock", typeof(TimeSpan));
        public static readonly CollectionRef Children = new("children");
        public static readonly Field Value = new("value", typeof(int), "children");
        public static readonly Field Name = new("name", typeof(string), "children");
        public static readonly Field Small = new("small", typeof(byte), "children");

        public static LinqQueryMapping<Child> ChildMapping() =>
            new LinqQueryMapping<Child>()
                .Field("value", c => c.Value)
                .Field("name", c => c.Name)
                .Field("small", c => c.Small);

        public static LinqQueryMapping<Row> Mapping()
        {
            var mapping = new LinqQueryMapping<Row>()
                .Field("id", r => r.Id)
                .Field("score", r => r.Score)
                .Field("text", r => r.Text)
                .Field("amount", r => r.Amount)
                .Field("code", r => r.Code)
                .Field("flag", r => r.Flag)
                .Field("when", r => r.When)
                .Field("clock", r => r.Clock)
                .Collection("children", r => r.Children, ChildMapping());
#if NET6_0_OR_GREATER
            mapping.Field("day", r => r.Day).Field("at", r => r.At);
#endif
            return mapping;
        }

        public static FilterCriteria F(BooleanFunction expression) => new() { Expression = expression };

        public static Literal L(object value) => new(value);

        public static EqFunc Eq(AbstractExpression left, object right) => new(left, L(right));

        public static SortDirective Sort(AbstractExpression expression, SortDirection direction = SortDirection.Ascending) =>
            new(new List<SortDirectiveItem> { new() { Expression = expression, Direction = direction } });

        public static int[] Ids(IEnumerable<Row> rows, BooleanFunction filter, IExpressionToLinqTransformer? transformer = null) =>
            rows.Where(transformer ?? InMemoryT, F(filter), Mapping()).Select(r => r.Id).OrderBy(i => i).ToArray();

        public static int[] QueryableIds(IEnumerable<Row> rows, BooleanFunction filter) =>
            rows.AsQueryable().Where(QueryableT, F(filter), Mapping()).Select(r => r.Id).OrderBy(i => i).ToArray();
    }
}
