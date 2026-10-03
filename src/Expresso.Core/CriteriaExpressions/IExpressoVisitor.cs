namespace Expresso.Core.CriteriaExpressions
{
    /// <summary>
    /// Visits every concrete expression node. Renderers implement one method per node,
    /// so adding a function to the IR is a compile error until every renderer handles it.
    /// </summary>
    /// <typeparam name="TContext">Per-call state passed down the tree.</typeparam>
    /// <typeparam name="TResult">Value produced for each node.</typeparam>
    public interface IExpressoVisitor<in TContext, out TResult>
    {
        // Operands
        TResult VisitField(Field node, TContext context);
        TResult VisitLiteral(Literal node, TContext context);
        TResult VisitCollectionRef(CollectionRef node, TContext context);

        // Logical
        TResult VisitAnd(AndFunc node, TContext context);
        TResult VisitOr(OrFunc node, TContext context);
        TResult VisitNot(NotFunc node, TContext context);

        // Comparison
        TResult VisitEq(EqFunc node, TContext context);
        TResult VisitNeq(NeqFunc node, TContext context);
        TResult VisitGt(GtFunc node, TContext context);
        TResult VisitGte(GteFunc node, TContext context);
        TResult VisitLt(LtFunc node, TContext context);
        TResult VisitLte(LteFunc node, TContext context);

        // Membership / null
        TResult VisitIn(InFunc node, TContext context);
        TResult VisitIsNull(IsNullFunc node, TContext context);

        // Arithmetic
        TResult VisitAbs(AbsFunc node, TContext context);
        TResult VisitAdd(AddFunc node, TContext context);
        TResult VisitSub(SubFunc node, TContext context);
        TResult VisitMult(MultFunc node, TContext context);
        TResult VisitDiv(DivFunc node, TContext context);
        TResult VisitMod(ModFunc node, TContext context);
        TResult VisitFloor(FloorFunc node, TContext context);
        TResult VisitCeiling(CeilingFunc node, TContext context);
        TResult VisitRound(RoundFunc node, TContext context);
        TResult VisitSign(SignFunc node, TContext context);
        TResult VisitPower(PowerFunc node, TContext context);
        TResult VisitSqrt(SqrtFunc node, TContext context);
        TResult VisitMin(MinFunc node, TContext context);
        TResult VisitMax(MaxFunc node, TContext context);

        // Collection quantifiers and aggregates
        TResult VisitAny(AnyFunc node, TContext context);
        TResult VisitAll(AllFunc node, TContext context);
        TResult VisitNone(NoneFunc node, TContext context);
        TResult VisitCollectionCount(CollectionCountFunc node, TContext context);
        TResult VisitCollectionMin(CollectionMinFunc node, TContext context);
        TResult VisitCollectionMax(CollectionMaxFunc node, TContext context);
        TResult VisitCollectionSum(CollectionSumFunc node, TContext context);
        TResult VisitCollectionAvg(CollectionAvgFunc node, TContext context);

        // String predicates
        TResult VisitStrStartswith(StrStartswithFunc node, TContext context);
        TResult VisitStrEndswith(StrEndswithFunc node, TContext context);
        TResult VisitStrContains(StrContainsFunc node, TContext context);

        // String transforms and inspection
        TResult VisitSubString(SubStringFunc node, TContext context);
        TResult VisitLeft(LeftFunc node, TContext context);
        TResult VisitRight(RightFunc node, TContext context);
        TResult VisitConcat(ConcatFunc node, TContext context);
        TResult VisitLower(LowerFunc node, TContext context);
        TResult VisitUpper(UpperFunc node, TContext context);
        TResult VisitTrim(TrimFunc node, TContext context);
        TResult VisitLTrim(LTrimFunc node, TContext context);
        TResult VisitRTrim(RTrimFunc node, TContext context);
        TResult VisitReplace(ReplaceFunc node, TContext context);
        TResult VisitLen(LenFunc node, TContext context);
        TResult VisitIndexOf(IndexOfFunc node, TContext context);

        // DateTime getters
        TResult VisitYear(YearFunc node, TContext context);
        TResult VisitMonth(MonthFunc node, TContext context);
        TResult VisitDay(DayFunc node, TContext context);
        TResult VisitDayOfYear(DayOfYearFunc node, TContext context);
        TResult VisitHour(HourFunc node, TContext context);
        TResult VisitMinute(MinuteFunc node, TContext context);
        TResult VisitSecond(SecondFunc node, TContext context);
        TResult VisitDayOfWeek(DayOfWeekFunc node, TContext context);
        TResult VisitDate(DateFunc node, TContext context);
        TResult VisitTime(TimeFunc node, TContext context);

        // DateTime arithmetic
        TResult VisitAddYears(AddYearsFunc node, TContext context);
        TResult VisitAddMonths(AddMonthsFunc node, TContext context);
        TResult VisitAddDays(AddDaysFunc node, TContext context);
        TResult VisitAddHours(AddHoursFunc node, TContext context);
        TResult VisitAddMinutes(AddMinutesFunc node, TContext context);
        TResult VisitAddSeconds(AddSecondsFunc node, TContext context);
    }
}
