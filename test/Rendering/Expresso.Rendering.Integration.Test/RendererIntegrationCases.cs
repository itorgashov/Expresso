using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;

namespace Expresso.Rendering.Integration.Test
{
    public sealed record FilterCase(string Id, FilterCriteria Filter, int[] ExpectedIdsOrdered);

    public sealed record ParentSortCase(string Id, SortDirective Sort, int[] ExpectedIdsOrdered, FilterCriteria? Filter = null);

    public sealed record NestedSortCase(string Id, int WidgetId, SortDirective Sort, string[] ExpectedLabels);

    public static class RendererIntegrationCases
    {
        public static IEnumerable<object[]> FilterCases() =>
            AllFilters().Select(c => new object[] { c });

        public static IEnumerable<object[]> ParentSortCases() =>
            AllParentSorts().Select(c => new object[] { c });

        public static IEnumerable<object[]> NestedSortCases() =>
            AllNestedSorts().Select(c => new object[] { c });

        public static IReadOnlyList<FilterCase> AllFilters()
        {
            var name = new Field("name", typeof(string));
            var age = new Field("age", typeof(int));
            var amount = new Field("amount", typeof(double));
            var notes = new Field("notes", typeof(string));
            var created = new Field("created", typeof(DateTime));
            var active = new Field("active", typeof(bool));
            var code = new Field("code", typeof(byte));
            var tags = new CollectionRef("tags");
            var label = new Field("label", typeof(string), "tags");
            var score = new Field("score", typeof(int), "tags");
            var kind = new Field("kind", typeof(string), "tags.tag_meta");

            return new List<FilterCase>
            {
                new("and", F(new AndFunc(new List<AbstractExpression> { Eq(age, 30), Eq(name, "Alice") })), Ids(1)),
                new("or", F(new OrFunc(new List<AbstractExpression> { Eq(name, "Alice"), Eq(name, "Bob") })), Ids(1, 2)),
                new("not", F(new NotFunc(Eq(name, "Alice"))), Ids(2, 3, 4, 5, 6)),
                new("eq", F(Eq(name, "Bob")), Ids(2)),
                new("neq", F(new NeqFunc(name, new Literal("Bob"))), Ids(1, 3, 4, 5, 6)),
                new("gt", F(new GtFunc(age, new Literal(30))), Ids(4)),
                new("gte", F(new GteFunc(age, new Literal(30))), Ids(1, 3, 4)),
                new("lt", F(new LtFunc(age, new Literal(25))), Ids(5, 6)),
                new("lte", F(new LteFunc(age, new Literal(25))), Ids(2, 5, 6)),
                new("in", F(new InFunc(new List<AbstractExpression> { name, new Literal("Alice"), new Literal("Eve") })), Ids(1, 5)),
                new("isnull", F(new IsNullFunc(notes)), Ids(1)),
                new("abs", F(Eq(new AbsFunc(amount), 12.7)), Ids(5)),
                new("add", F(Eq(new AddFunc(age, new Literal(10)), 40)), Ids(1, 3)),
                new("sub", F(Eq(new SubFunc(age, new Literal(5)), 20)), Ids(2)),
                new("mult", F(Eq(new MultFunc(age, new Literal(2)), 80)), Ids(4)),
                new("div", F(Eq(new DivFunc(age, new Literal(2)), 15)), Ids(1, 3)),
                new("mod", F(Eq(new ModFunc(age, new Literal(10)), 0)), Ids(1, 3, 4, 5)),
                new("floor", F(Eq(new FloorFunc(amount), 50.0)), Ids(1)),
                new("ceiling", F(Eq(new CeilingFunc(amount), 51.0)), Ids(1)),
                new("round", F(Eq(new RoundFunc(amount), 40.0)), Ids(2)),
                new("round-digits", F(new AndFunc(new List<AbstractExpression>
                {
                    new GtFunc(new RoundFunc(amount, new Literal(1)), new Literal(61.3)),
                    new LtFunc(new RoundFunc(amount, new Literal(1)), new Literal(61.5)),
                })), Ids(3)),
                new("sign", F(Eq(new SignFunc(amount), -1)), Ids(5)),
                new("power", F(Eq(new PowerFunc(age, new Literal(1)), 18)), Ids(6)),
                new("sqrt", F(new GtFunc(new SqrtFunc(age), new Literal(6.0))), Ids(4)),
                new("min", F(Eq(new MinFunc(age, new Literal(18)), 0)), Ids(5)),
                new("max", F(new GtFunc(new MaxFunc(age, new Literal(20)), new Literal(30))), Ids(4)),
                new("startswith", F(new StrStartswithFunc(name, new Literal("Ca"))), Ids(3)),
                new("endswith", F(new StrEndswithFunc(name, new Literal("ve"))), Ids(4, 5)),
                new("contains", F(new StrContainsFunc(notes, new Literal("100%"))), Ids(4)),
                new("contains-underscore", F(new StrContainsFunc(notes, new Literal("_off"))), Ids(4)),
                new("contains-backslash", F(new StrContainsFunc(notes, new Literal("\\"))), Ids(6)),
                new("substring", F(Eq(new SubStringFunc(name, new Literal(1), new Literal(3)), "Ali")), Ids(1)),
                new("left", F(Eq(new LeftFunc(name, new Literal(1)), "A")), Ids(1)),
                new("right", F(Eq(new RightFunc(name, new Literal(1)), "e")), Ids(1, 4, 5)),
                new("concat", F(Eq(new ConcatFunc(new List<AbstractExpression> { name, new Literal("X") }), "BobX")), Ids(2)),
                new("lower", F(Eq(new LowerFunc(name), "alice")), Ids(1)),
                new("upper", F(Eq(new UpperFunc(name), "BOB")), Ids(2)),
                new("trim", F(Eq(new TrimFunc(notes), "pad")), Ids(3)),
                new("ltrim", F(new StrStartswithFunc(new LTrimFunc(notes), new Literal("pad"))), Ids(3)),
                new("rtrim", F(new StrEndswithFunc(new RTrimFunc(notes), new Literal("pad"))), Ids(3)),
                new("replace", F(Eq(new ReplaceFunc(name, new Literal("o"), new Literal("a")), "Bab")), Ids(2)),
                new("len", F(Eq(new LenFunc(name), 3)), Ids(2, 5)),
                new("indexof", F(Eq(new IndexOfFunc(name, new Literal("o")), 1)), Ids(2)),
                new("indexof-missing", F(Eq(new IndexOfFunc(name, new Literal("xyz")), -1)), Ids(1, 2, 3, 4, 5, 6)),
                new("year", F(Eq(new YearFunc(created), 2021)), Ids(2)),
                new("month", F(Eq(new MonthFunc(created), 7)), Ids(5)),
                new("day", F(Eq(new DayFunc(created), 31)), Ids(4)),
                new("dayofyear", F(new GtFunc(new DayOfYearFunc(created), new Literal(360))), Ids(4)),
                new("hour", F(Eq(new HourFunc(created), 14)), Ids(2)),
                new("minute", F(Eq(new MinuteFunc(created), 30)), Ids(2)),
                new("second", F(Eq(new SecondFunc(created), 30)), Ids(5)),
                new("dayofweek", F(Eq(new DayOfWeekFunc(created), (int)WidgetSeedData.Created1.DayOfWeek)), Ids(1, 3)),
                new("date", F(Eq(new DateFunc(created), DateLit(new DateTime(2022, 7, 4)))), Ids(5)),
                new("addyears", F(Eq(new YearFunc(new AddYearsFunc(created, new Literal(1))), 2021)), Ids(1, 3, 6)),
                new("addmonths", F(Eq(new MonthFunc(new AddMonthsFunc(created, new Literal(1))), 2)), Ids(1, 3)),
                new("adddays", F(Eq(new DayFunc(new AddDaysFunc(created, new Literal(1))), 16)), Ids(1, 3)),
                new("addhours", F(Eq(new HourFunc(new AddHoursFunc(created, new Literal(2))), 12)), Ids(1, 3)),
                new("addhours-neg", F(Eq(new HourFunc(new AddHoursFunc(created, new Literal(-2))), 8)), Ids(1, 3)),
                new("addminutes", F(Eq(new MinuteFunc(new AddMinutesFunc(created, new Literal(15))), 15)), Ids(1, 3, 6)),
                new("addseconds", F(Eq(new SecondFunc(new AddSecondsFunc(created, new Literal(5))), 5)), Ids(1, 2, 3, 4, 6)),
                new("time", F(Eq(new TimeFunc(created), TimeLit(new TimeSpan(10, 0, 0)))), Ids(1, 3)),
                new("eq-bool", F(Eq(active, true)), Ids(1, 3, 4, 6)),
                new("eq-guid", F(Eq(new Field("externalid", typeof(Guid)), WidgetSeedData.Guid2)), Ids(2)),
                new("eq-time", F(Eq(new Field("opens", typeof(TimeSpan)), WidgetSeedData.OpensEvening)), Ids(2)),
                new("eq-time-midnight", F(Eq(new Field("opens", typeof(TimeSpan)), WidgetSeedData.OpensMidnight)), Ids(4)),
                new("eq-code", F(Eq(code, (byte)2)), Ids(2)),
                new("eq-datetime", F(Eq(created, WidgetSeedData.Created2)), Ids(2)),
                new("not-isnull", F(new NotFunc(new IsNullFunc(notes))), Ids(2, 3, 4, 5, 6)),
                new("any-tag", F(new AnyFunc(tags, Eq(label, "blue"))), Ids(1)),
                new("any-empty-pred", F(new AnyFunc(tags)), Ids(1, 2, 4, 5, 6)),
                new("none-tag", F(new NoneFunc(tags)), Ids(3)),
                new("none-pred", F(new NoneFunc(tags, Eq(label, "blue"))), Ids(2, 3, 4, 5, 6)),
                new("all-pred", F(new AllFunc(tags, new GtFunc(score, new Literal(0)))), Ids(1, 2, 3, 4, 6)),
                new("count-tags", F(Eq(new CollectionCountFunc(tags), 2)), Ids(1, 4)),
                new("count-red", F(Eq(new CollectionCountFunc(tags, Eq(label, "red")), 1)), Ids(1, 2, 4, 6)),
                new("min-score", F(Eq(new CollectionMinFunc(tags, score), 5)), Ids(2)),
                new("max-score", F(Eq(new CollectionMaxFunc(tags, score), 100)), Ids(6)),
                new("sum-score", F(Eq(new CollectionSumFunc(tags, score), 30)), Ids(1, 4)),
                new("avg-score", F(Eq(new CollectionAvgFunc(tags, score), 5.0)), Ids(2)),
                new("nested-any", F(new AnyFunc(tags, new AnyFunc(new CollectionRef("tag_meta", "tags"), Eq(kind, "size")))), Ids(1)),
            };
        }

        public static IReadOnlyList<ParentSortCase> AllParentSorts()
        {
            var name = new Field("name", typeof(string));
            var age = new Field("age", typeof(int));
            var tags = new CollectionRef("tags");
            var score = new Field("score", typeof(int), "tags");
            return new List<ParentSortCase>
            {
                new("sort-name-asc", S(name, SortDirection.Ascending), Ids(1, 2, 3, 4, 5, 6)),
                new("sort-age-desc-name", new SortDirective(new List<SortDirectiveItem>
                {
                    new() { Expression = age, Direction = SortDirection.Descending },
                    new() { Expression = name, Direction = SortDirection.Ascending },
                }), Ids(4, 1, 3, 2, 6, 5)),
                new("sort-count-tags", new SortDirective(new List<SortDirectiveItem>
                {
                    new() { Expression = new CollectionCountFunc(tags), Direction = SortDirection.Descending },
                    new() { Expression = name, Direction = SortDirection.Ascending },
                }), Ids(1, 4, 2, 5, 6, 3)),
                new("sort-min-score", new SortDirective(new List<SortDirectiveItem>
                {
                    new() { Expression = new CollectionMinFunc(tags, score), Direction = SortDirection.Descending },
                    new() { Expression = name, Direction = SortDirection.Ascending },
                }), Ids(6, 4, 1, 2, 5), F(new AnyFunc(tags))),
                new("sort-bool", new SortDirective(new List<SortDirectiveItem>
                {
                    new() { Expression = new EqFunc(new Field("active", typeof(bool)), new Literal(true)), Direction = SortDirection.Descending },
                    new() { Expression = name, Direction = SortDirection.Ascending },
                }), Ids(1, 3, 4, 6, 2, 5)),
            };
        }

        public static IReadOnlyList<NestedSortCase> AllNestedSorts()
        {
            var label = new Field("label", typeof(string), "tags");
            var score = new Field("score", typeof(int), "tags");
            return new List<NestedSortCase>
            {
                new("nested-label-asc", 1, S(label, SortDirection.Ascending), new[] { "blue", "red" }),
                new("nested-score-desc", 1, S(score, SortDirection.Descending), new[] { "blue", "red" }),
            };
        }

        private static FilterCriteria F(BooleanFunction expr) => new() { Expression = expr };

        private static EqFunc Eq(AbstractExpression left, object right) => new(left, new Literal(right));

        private static SortDirective S(AbstractExpression expr, SortDirection dir) =>
            new(new List<SortDirectiveItem> { new() { Expression = expr, Direction = dir } });

        private static int[] Ids(params int[] ids) => ids;

        private static object DateLit(DateTime date)
        {
#if NET6_0_OR_GREATER
            return DateOnly.FromDateTime(date);
#else
            return date.Date;
#endif
        }

        private static object TimeLit(TimeSpan time)
        {
#if NET6_0_OR_GREATER
            return TimeOnly.FromTimeSpan(time);
#else
            return time;
#endif
        }
    }
}
