using Expresso.Core.CriteriaExpressions;

namespace Expresso.Parsing.Policies.Catalog;

internal static class FunctionCatalog
{
    internal static readonly IReadOnlyList<FunctionInfo> All = Build();
    internal static readonly IReadOnlyDictionary<Type, FunctionInfo> ByType = All.ToDictionary(f => f.NodeType);
    internal static IEnumerable<FunctionInfo> Resolve(string name)
    {
        if (name == "*") return All;
        if (name.StartsWith("@", StringComparison.Ordinal))
        {
            var category = name.Substring(1);
            if (category.Equals("predicate", StringComparison.OrdinalIgnoreCase))
                return All.Where(f => f.Category != "logical" && f.Signatures.All(s => s.Result == TypeKind.Boolean));
            return All.Where(f => f.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }
        return All.Where(f => f.Names.Contains(name, StringComparer.OrdinalIgnoreCase));
    }

    private static FunctionInfo[] Build() => new[]
    {
        Define<AndFunc>("and", "logical", 2, int.MaxValue, TypeKind.Boolean, new[] { TypeKind.Boolean }, count: DefaultCountKind.Args),
        Define<OrFunc>("or", "logical", 2, int.MaxValue, TypeKind.Boolean, new[] { TypeKind.Boolean }, count: DefaultCountKind.Args),
        Define<NotFunc>("not", "logical", 1, 1, TypeKind.Boolean, new[] { TypeKind.Boolean }),
        Define<EqFunc>("eq", "comparison", 2, 2, TypeKind.Boolean, new[] { TypeKind.Scalar }, same: true),
        Define<NeqFunc>("neq", "comparison", 2, 2, TypeKind.Boolean, new[] { TypeKind.Scalar }, same: true),
        Define<GtFunc>("gt", "comparison", 2, 2, TypeKind.Boolean, new[] { TypeKind.Ordered }, same: true),
        Define<GteFunc>("gte", "comparison", 2, 2, TypeKind.Boolean, new[] { TypeKind.Ordered }, same: true),
        Define<LtFunc>("lt", "comparison", 2, 2, TypeKind.Boolean, new[] { TypeKind.Ordered }, same: true),
        Define<LteFunc>("lte", "comparison", 2, 2, TypeKind.Boolean, new[] { TypeKind.Ordered }, same: true),
        Define<InFunc>("in", "membership-null", 2, int.MaxValue, TypeKind.Boolean, new[] { TypeKind.SortValue }, same: true, count: DefaultCountKind.InItems),
        Define<IsNullFunc>("isnull", "membership-null", 1, 1, TypeKind.Boolean, new[] { TypeKind.Scalar }),
        Define<AbsFunc>("abs", "arithmetic", 1, 1, TypeKind.Numeric, new[] { TypeKind.Numeric }, returns: 0),
        Define<AddFunc>("add", "arithmetic", 2, 2, TypeKind.Numeric, new[] { TypeKind.Numeric, TypeKind.Numeric }, returns: 0),
        Define<SubFunc>("sub", "arithmetic", 2, 2, TypeKind.Numeric, new[] { TypeKind.Numeric, TypeKind.Numeric }, returns: 0),
        Define<MultFunc>("mult", "arithmetic", 2, 2, TypeKind.Numeric, new[] { TypeKind.Numeric, TypeKind.Numeric }, returns: 0),
        Define<DivFunc>("div", "arithmetic", 2, 2, TypeKind.Numeric, new[] { TypeKind.Numeric, TypeKind.Numeric }, returns: 0),
        Define<ModFunc>("mod", "arithmetic", 2, 2, TypeKind.Numeric, new[] { TypeKind.Numeric, TypeKind.Numeric }, returns: 0),
        Define<MinFunc>("min", "arithmetic", 2, 2, TypeKind.Numeric, new[] { TypeKind.Numeric, TypeKind.Numeric }, returns: 0),
        Define<MaxFunc>("max", "arithmetic", 2, 2, TypeKind.Numeric, new[] { TypeKind.Numeric, TypeKind.Numeric }, returns: 0),
        Define<FloorFunc>("floor", "arithmetic", 1, 1, TypeKind.Double, new[] { TypeKind.Numeric }),
        Define<CeilingFunc>("ceiling|ceil", "arithmetic", 1, 1, TypeKind.Double, new[] { TypeKind.Numeric }),
        Define<RoundFunc>("round", "arithmetic", 1, 2, TypeKind.Double, new[] { TypeKind.Numeric, TypeKind.Int }),
        Define<SignFunc>("sign", "arithmetic", 1, 1, TypeKind.Int, new[] { TypeKind.Numeric }),
        Define<PowerFunc>("power|pow", "arithmetic", 2, 2, TypeKind.Double, new[] { TypeKind.Numeric, TypeKind.Numeric }),
        Define<SqrtFunc>("sqrt", "arithmetic", 1, 1, TypeKind.Double, new[] { TypeKind.Numeric }),
        Define<AnyFunc>("any", "collection-quantifier", 1, 2, TypeKind.Boolean, new[] { TypeKind.Collection, TypeKind.Boolean }),
        Define<AllFunc>("all", "collection-quantifier", 1, 2, TypeKind.Boolean, new[] { TypeKind.Collection, TypeKind.Boolean }),
        Define<NoneFunc>("none", "collection-quantifier", 1, 2, TypeKind.Boolean, new[] { TypeKind.Collection, TypeKind.Boolean }),
        Define<CollectionCountFunc>("count", "collection-aggregate", 1, 2, TypeKind.Int, new[] { TypeKind.Collection, TypeKind.Boolean }),
        Define<CollectionMinFunc>("min", "collection-aggregate", 2, 2, TypeKind.Ordered | TypeKind.String, new[] { TypeKind.Collection, TypeKind.Ordered | TypeKind.String }, returns: 1),
        Define<CollectionMaxFunc>("max", "collection-aggregate", 2, 2, TypeKind.Ordered | TypeKind.String, new[] { TypeKind.Collection, TypeKind.Ordered | TypeKind.String }, returns: 1),
        Define<CollectionSumFunc>("sum", "collection-aggregate", 2, 2, TypeKind.Numeric, new[] { TypeKind.Collection, TypeKind.Numeric }, returns: 1),
        Define<CollectionAvgFunc>("avg", "collection-aggregate", 2, 2, TypeKind.Double, new[] { TypeKind.Collection, TypeKind.Numeric }),
        Define<StrStartswithFunc>("startswith", "string-predicate", 2, 2, TypeKind.Boolean, new[] { TypeKind.String }),
        Define<StrEndswithFunc>("endswith", "string-predicate", 2, 2, TypeKind.Boolean, new[] { TypeKind.String }),
        Define<StrContainsFunc>("contains", "string-predicate", 2, 2, TypeKind.Boolean, new[] { TypeKind.String }),
        Define<SubStringFunc>("substring|substr", "string-transform", 3, 3, TypeKind.String, new[] { TypeKind.String, TypeKind.Int, TypeKind.Int }),
        Define<LeftFunc>("left", "string-transform", 2, 2, TypeKind.String, new[] { TypeKind.String, TypeKind.Int }),
        Define<RightFunc>("right", "string-transform", 2, 2, TypeKind.String, new[] { TypeKind.String, TypeKind.Int }),
        Define<ConcatFunc>("concat", "string-transform", 2, int.MaxValue, TypeKind.String, new[] { TypeKind.String }, count: DefaultCountKind.Args),
        Define<LowerFunc>("lower", "string-transform", 1, 1, TypeKind.String, new[] { TypeKind.String }),
        Define<UpperFunc>("upper", "string-transform", 1, 1, TypeKind.String, new[] { TypeKind.String }),
        Define<TrimFunc>("trim", "string-transform", 1, 1, TypeKind.String, new[] { TypeKind.String }),
        Define<LTrimFunc>("ltrim", "string-transform", 1, 1, TypeKind.String, new[] { TypeKind.String }),
        Define<RTrimFunc>("rtrim", "string-transform", 1, 1, TypeKind.String, new[] { TypeKind.String }),
        Define<ReplaceFunc>("replace", "string-transform", 3, 3, TypeKind.String, new[] { TypeKind.String }),
        Define<LenFunc>("len", "string-inspect", 1, 1, TypeKind.Int, new[] { TypeKind.String }),
        Define<IndexOfFunc>("indexof", "string-inspect", 2, 2, TypeKind.Int, new[] { TypeKind.String }),
        Define<YearFunc>("year", "datetime-getter", 1, 1, TypeKind.Int, new[] { TypeKind.Calendar }),
        Define<MonthFunc>("month", "datetime-getter", 1, 1, TypeKind.Int, new[] { TypeKind.Calendar }),
        Define<DayFunc>("day", "datetime-getter", 1, 1, TypeKind.Int, new[] { TypeKind.Calendar }),
        Define<DayOfYearFunc>("dayofyear", "datetime-getter", 1, 1, TypeKind.Int, new[] { TypeKind.Calendar }),
        Define<DayOfWeekFunc>("dayofweek", "datetime-getter", 1, 1, TypeKind.Int, new[] { TypeKind.Calendar }),
        Define<HourFunc>("hour", "datetime-getter", 1, 1, TypeKind.Int, new[] { TypeKind.Time }),
        Define<MinuteFunc>("minute", "datetime-getter", 1, 1, TypeKind.Int, new[] { TypeKind.Time }),
        Define<SecondFunc>("second", "datetime-getter", 1, 1, TypeKind.Int, new[] { TypeKind.Time }),
#if NET6_0_OR_GREATER
        Define<DateFunc>("date", "datetime-getter", 1, 1, TypeKind.DateOnly, new[] { TypeKind.Calendar | TypeKind.String }),
        Define<TimeFunc>("time", "datetime-getter", 1, 1, TypeKind.TimeOnly, new[] { TypeKind.DateTime | TypeKind.String | TypeKind.TimeOnly }),
#else
        Define<DateFunc>("date", "datetime-getter", 1, 1, TypeKind.DateTime, new[] { TypeKind.DateTime }),
        Define<TimeFunc>("time", "datetime-getter", 1, 1, TypeKind.TimeSpan, new[] { TypeKind.DateTime | TypeKind.String | TypeKind.TimeSpan }),
#endif
        Define<AddYearsFunc>("addyears", "datetime-add", 2, 2, TypeKind.Calendar, new[] { TypeKind.Calendar, TypeKind.Int }, returns: 0),
        Define<AddMonthsFunc>("addmonths", "datetime-add", 2, 2, TypeKind.Calendar, new[] { TypeKind.Calendar, TypeKind.Int }, returns: 0),
        Define<AddDaysFunc>("adddays", "datetime-add", 2, 2, TypeKind.Calendar, new[] { TypeKind.Calendar, TypeKind.Int }, returns: 0),
        Define<AddHoursFunc>("addhours", "datetime-add", 2, 2, TypeKind.Time, new[] { TypeKind.Time, TypeKind.Int }, returns: 0),
        Define<AddMinutesFunc>("addminutes", "datetime-add", 2, 2, TypeKind.Time, new[] { TypeKind.Time, TypeKind.Int }, returns: 0),
        Define<AddSecondsFunc>("addseconds", "datetime-add", 2, 2, TypeKind.Time, new[] { TypeKind.Time, TypeKind.Int }, returns: 0)
    };

    private static FunctionInfo Define<T>(string names, string category, int min, int max, TypeKind result,
        TypeKind[] args, int returns = -1, bool same = false, DefaultCountKind count = DefaultCountKind.None) =>
        new(typeof(T), names, category, min, max, result, args, returns, same, count);
}
