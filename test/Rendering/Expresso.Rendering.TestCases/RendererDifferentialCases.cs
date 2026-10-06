using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.TestCases
{
    /// <summary>How a differential case may fail on an engine that rejects the function.</summary>
    public enum DifferentialRejection
    {
        /// <summary>Both sides must return the same id list.</summary>
        None,

        /// <summary>
        /// Equal id lists match. A database error matches only when its code is one of the case's expected codes,
        /// and the other side is that same error or a <see cref="NotSupportedException"/> whose message contains the case's reason.
        /// </summary>
        Domain,
    }

    /// <summary>Edge case without a fixed expectation: every renderer on an engine must agree with that engine's ADO result.</summary>
    /// <param name="Id">Case id.</param>
    /// <param name="Filter">Filter, or <see langword="null"/> for all rows.</param>
    /// <param name="Sort">Sort, or <see langword="null"/> for id order.</param>
    /// <param name="DependsOnCollation">Result depends on the database collation (not comparable with ordinal in-memory order).</param>
    /// <param name="Rejection">How a domain error may differ between the reference engine and the candidate.</param>
    /// <param name="UnsupportedReason">Text the in-memory <see cref="NotSupportedException"/> must contain when <paramref name="Rejection"/> is <see cref="DifferentialRejection.Domain"/>.</param>
    /// <param name="DatabaseCodes">Fragments of the reference engine's native error code that count as this case's domain error.</param>
    public sealed record DifferentialCase(
        string Id,
        FilterCriteria? Filter,
        SortDirective? Sort = null,
        bool DependsOnCollation = false,
        DifferentialRejection Rejection = DifferentialRejection.None,
        string? UnsupportedReason = null,
        IReadOnlyList<string>? DatabaseCodes = null)
    {
        public override string ToString() => Id;
    }

    /// <summary>
    /// Probes where engines are known to split (integer vs decimal <c>/</c>, <c>AVG(int)</c>, rounding mode, NULL in
    /// <c>concat</c>, empty or NULL <c>indexof</c>, time-of-day overflow, collation, NULL sort position) on the shared widget seed.
    /// </summary>
    public static class RendererDifferentialCases
    {
        public static IEnumerable<object[]> Cases() => All().Select(c => new object[] { c });

        public static IEnumerable<object[]> CollationFreeCases() => All().Where(c => !c.DependsOnCollation).Select(c => new object[] { c });

        public static IReadOnlyList<DifferentialCase> All()
        {
            var name = new Field("name", typeof(string));
            var age = new Field("age", typeof(int));
            var amount = new Field("amount", typeof(double));
            var notes = new Field("notes", typeof(string));
            var opens = new Field("opens", typeof(TimeSpan));
            var tags = new CollectionRef("tags");
            var label = new Field("label", typeof(string), "tags");
            var score = new Field("score", typeof(int), "tags");

            return new List<DifferentialCase>
            {
                new("div-odd-eq", F(Eq(new DivFunc(age, L(4)), 7))),
                new("div-odd-gt", F(new GtFunc(new DivFunc(age, L(4)), L(7)))),
                new("avg-fraction", F(new GtFunc(new CollectionAvgFunc(tags, new LenFunc(label)), L(3.0)))),
                new("round-half", F(Eq(new RoundFunc(amount), 51.0))),
                new("round-half-digits", F(Eq(new RoundFunc(new MultFunc(amount, L(10.0)), L(-1)), 510.0))),
                new("concat-null-isnull", F(new IsNullFunc(new ConcatFunc(new List<AbstractExpression> { name, notes })))),
                new("concat-null-eq", F(Eq(new ConcatFunc(new List<AbstractExpression> { name, notes }), "Alice"))),
                new("concat-null-all", F(new IsNullFunc(new ConcatFunc(new List<AbstractExpression> { notes, notes })))),
                new("time-hour", F(Eq(new HourFunc(opens), 17))),
                new("time-wrap", F(Eq(new HourFunc(new AddHoursFunc(opens, L(10))), 3))),
                new("time-carry", F(Eq(new MinuteFunc(new AddMinutesFunc(opens, L(40))), 10))),
                new("time-negative", F(Eq(new MinuteFunc(new AddMinutesFunc(opens, L(-570))), 30))),
                new("time-seconds", F(Eq(new SecondFunc(new AddSecondsFunc(opens, L(3605))), 5))),
                new("time-add-eq", F(Eq(new AddHoursFunc(opens, L(1)), new TimeSpan(10, 0, 0)))),
                new("right-long", F(Eq(new RightFunc(name, L(10)), "Alice"))),
                new("left-long", F(Eq(new LeftFunc(name, L(10)), "Bob"))),
                new("substring-long", F(Eq(new SubStringFunc(name, L(4), L(10)), "ce"))),
                new("indexof-case", F(Eq(new IndexOfFunc(name, L("a")), 0))),
                new("indexof-empty", F(Eq(new IndexOfFunc(name, L("")), 0))),
                new("indexof-space", F(Eq(new IndexOfFunc(notes, L(" ")), 0))),
                new("indexof-null", F(new IsNullFunc(new IndexOfFunc(notes, L("o"))))),
                new("contains-case", F(new StrContainsFunc(notes, L("x"))), DependsOnCollation: true),
                new("startswith-case", F(new StrStartswithFunc(name, L("a"))), DependsOnCollation: true),
                new("endswith-case", F(new StrEndswithFunc(name, L("E"))), DependsOnCollation: true),
                new("contains-empty", F(new StrContainsFunc(notes, L("")))),
                new("startswith-empty", F(new StrStartswithFunc(notes, L("")))),
                new("endswith-empty", F(new StrEndswithFunc(notes, L("")))),
                new("sum-empty-isnull", F(new IsNullFunc(new CollectionSumFunc(tags, score)))),
                new("min-label", F(Eq(new CollectionMinFunc(tags, label), "blue"))),
                new("sort-notes-asc", null, Sort((notes, SortDirection.Ascending), (name, SortDirection.Ascending)), DependsOnCollation: true),
                new("sort-notes-desc", null, Sort((notes, SortDirection.Descending), (name, SortDirection.Ascending)), DependsOnCollation: true),
                new("sort-len-notes-asc", null, Sort((new LenFunc(notes), SortDirection.Ascending), (name, SortDirection.Ascending))),
                new("sort-len-notes-desc", null, Sort((new LenFunc(notes), SortDirection.Descending), (name, SortDirection.Ascending))),
                new("sort-div", null, Sort((new DivFunc(age, L(4)), SortDirection.Ascending), (name, SortDirection.Descending))),
                new("isnull-sqrt-neg", F(new IsNullFunc(new SqrtFunc(new SubFunc(amount, L(1000.0))))), Rejection: DifferentialRejection.Domain, UnsupportedReason: "negative square root", DatabaseCodes: new[] { "2201F", "3623", "1428", "22003" }),
                new("isnull-div-zero", F(new IsNullFunc(new DivFunc(amount, L(0.0)))), Rejection: DifferentialRejection.Domain, UnsupportedReason: "division by zero", DatabaseCodes: new[] { "22012", "8134", "1476" }),
                new("isnull-substring-zero", F(new IsNullFunc(new SubStringFunc(name, L(0), L(0))))),
                new("isnull-left-zero", F(new IsNullFunc(new LeftFunc(name, L(0))))),
                new("right-trailing", F(Eq(new RightFunc(new ConcatFunc(new List<AbstractExpression> { name, L("  ") }), L(3)), "e  "))),
                new("left-neg-two", F(Eq(new LeftFunc(name, L(-2)), "Ali"))),
                new("right-neg-two", F(Eq(new RightFunc(name, L(-2)), "ce"))),
            };
        }

        private static FilterCriteria F(BooleanFunction expression) => new() { Expression = expression };

        private static Literal L(object value) => new(value);

        private static EqFunc Eq(AbstractExpression left, object right) => new(left, new Literal(right));

        private static SortDirective Sort(params (AbstractExpression Expression, SortDirection Direction)[] items) =>
            new(items.Select(i => new SortDirectiveItem { Expression = i.Expression, Direction = i.Direction }).ToList());
    }
}
