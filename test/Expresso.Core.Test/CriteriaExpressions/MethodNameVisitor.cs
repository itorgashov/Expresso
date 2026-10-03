using System.Runtime.CompilerServices;
using Expresso.Core.CriteriaExpressions;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    /// <summary>Returns the name of the visitor method a node dispatched to.</summary>
    internal sealed class MethodNameVisitor : IExpressoVisitor<object?, string>
    {
        public string VisitField(Field node, object? context) => Name();
        public string VisitLiteral(Literal node, object? context) => Name();
        public string VisitCollectionRef(CollectionRef node, object? context) => Name();
        public string VisitAnd(AndFunc node, object? context) => Name();
        public string VisitOr(OrFunc node, object? context) => Name();
        public string VisitNot(NotFunc node, object? context) => Name();
        public string VisitEq(EqFunc node, object? context) => Name();
        public string VisitNeq(NeqFunc node, object? context) => Name();
        public string VisitGt(GtFunc node, object? context) => Name();
        public string VisitGte(GteFunc node, object? context) => Name();
        public string VisitLt(LtFunc node, object? context) => Name();
        public string VisitLte(LteFunc node, object? context) => Name();
        public string VisitIn(InFunc node, object? context) => Name();
        public string VisitIsNull(IsNullFunc node, object? context) => Name();
        public string VisitAbs(AbsFunc node, object? context) => Name();
        public string VisitAdd(AddFunc node, object? context) => Name();
        public string VisitSub(SubFunc node, object? context) => Name();
        public string VisitMult(MultFunc node, object? context) => Name();
        public string VisitDiv(DivFunc node, object? context) => Name();
        public string VisitMod(ModFunc node, object? context) => Name();
        public string VisitFloor(FloorFunc node, object? context) => Name();
        public string VisitCeiling(CeilingFunc node, object? context) => Name();
        public string VisitRound(RoundFunc node, object? context) => Name();
        public string VisitSign(SignFunc node, object? context) => Name();
        public string VisitPower(PowerFunc node, object? context) => Name();
        public string VisitSqrt(SqrtFunc node, object? context) => Name();
        public string VisitMin(MinFunc node, object? context) => Name();
        public string VisitMax(MaxFunc node, object? context) => Name();
        public string VisitAny(AnyFunc node, object? context) => Name();
        public string VisitAll(AllFunc node, object? context) => Name();
        public string VisitNone(NoneFunc node, object? context) => Name();
        public string VisitCollectionCount(CollectionCountFunc node, object? context) => Name();
        public string VisitCollectionMin(CollectionMinFunc node, object? context) => Name();
        public string VisitCollectionMax(CollectionMaxFunc node, object? context) => Name();
        public string VisitCollectionSum(CollectionSumFunc node, object? context) => Name();
        public string VisitCollectionAvg(CollectionAvgFunc node, object? context) => Name();
        public string VisitStrStartswith(StrStartswithFunc node, object? context) => Name();
        public string VisitStrEndswith(StrEndswithFunc node, object? context) => Name();
        public string VisitStrContains(StrContainsFunc node, object? context) => Name();
        public string VisitSubString(SubStringFunc node, object? context) => Name();
        public string VisitLeft(LeftFunc node, object? context) => Name();
        public string VisitRight(RightFunc node, object? context) => Name();
        public string VisitConcat(ConcatFunc node, object? context) => Name();
        public string VisitLower(LowerFunc node, object? context) => Name();
        public string VisitUpper(UpperFunc node, object? context) => Name();
        public string VisitTrim(TrimFunc node, object? context) => Name();
        public string VisitLTrim(LTrimFunc node, object? context) => Name();
        public string VisitRTrim(RTrimFunc node, object? context) => Name();
        public string VisitReplace(ReplaceFunc node, object? context) => Name();
        public string VisitLen(LenFunc node, object? context) => Name();
        public string VisitIndexOf(IndexOfFunc node, object? context) => Name();
        public string VisitYear(YearFunc node, object? context) => Name();
        public string VisitMonth(MonthFunc node, object? context) => Name();
        public string VisitDay(DayFunc node, object? context) => Name();
        public string VisitDayOfYear(DayOfYearFunc node, object? context) => Name();
        public string VisitHour(HourFunc node, object? context) => Name();
        public string VisitMinute(MinuteFunc node, object? context) => Name();
        public string VisitSecond(SecondFunc node, object? context) => Name();
        public string VisitDayOfWeek(DayOfWeekFunc node, object? context) => Name();
        public string VisitDate(DateFunc node, object? context) => Name();
        public string VisitTime(TimeFunc node, object? context) => Name();
        public string VisitAddYears(AddYearsFunc node, object? context) => Name();
        public string VisitAddMonths(AddMonthsFunc node, object? context) => Name();
        public string VisitAddDays(AddDaysFunc node, object? context) => Name();
        public string VisitAddHours(AddHoursFunc node, object? context) => Name();
        public string VisitAddMinutes(AddMinutesFunc node, object? context) => Name();
        public string VisitAddSeconds(AddSecondsFunc node, object? context) => Name();

        private static string Name([CallerMemberName] string name = "") => name;
    }
}