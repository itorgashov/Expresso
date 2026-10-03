namespace Expresso.Rendering.EntityFramework.Test
{
    /// <summary>SQL Server <c>DbQuery.ToString()</c> fragments for the EF6 overrides (fixed manifest token, no connection).</summary>
    public class Ef6GoldenSqlTests
    {
        [Theory]
        [InlineData("eq", "[Extent1].[Name] = @p__linq__0")]
        [InlineData("dayofweek", "((((DATEDIFF (day, convert(datetime2, '1900-01-07 00:00:00.0000000', 121), [Extent1].[created_at])) % 7) + 7) % 7) = @p__linq__0")]
        [InlineData("dayofyear", "(DATEPART (dayofyear, [Extent1].[created_at])) > @p__linq__0")]
        [InlineData("date", "(cast(cast([Extent1].[created_at] as date) as datetime2)) = @p__linq__0")]
        [InlineData("adddays", "DATEPART (day, CAST( DATEADD (day, @p__linq__0, [Extent1].[created_at]) AS datetime2))")]
        [InlineData("addhours-neg", "DATEADD (hour, @p__linq__0, [Extent1].[created_at])")]
        [InlineData("time-wrap", "(DATEPART (hour, DATEADD (hour, @p__linq__0, [Extent1].[Opens]))) = @p__linq__1")]
        [InlineData("time", "convert (time,convert(varchar(255), DATEPART (hour, [Extent1].[created_at]))")]
        [InlineData("left", "LEFT([Extent1].[Name], @p__linq__0)")]
        [InlineData("right", "RIGHT([Extent1].[Name], @p__linq__0)")]
        [InlineData("sign", "(CASE WHEN ([Extent1].[Amount] > cast(0 as float(53))) THEN 1 WHEN ([Extent1].[Amount] < cast(0 as float(53))) THEN -1 ELSE 0 END) = @p__linq__0")]
        [InlineData("sqrt", "(SQRT( CAST( [Extent1].[Age] AS float))) > @p__linq__0")]
        [InlineData("ltrim", "LTRIM([Extent1].[Notes]) LIKE @p__linq__0 ESCAPE N'~'")]
        [InlineData("avg-score", "( CAST( CAST( [Project2].[C2] AS int) AS float) = @p__linq__0)")]
        [InlineData("concat-null-eq", "CASE WHEN ([Extent1].[Notes] IS NULL) THEN N'' ELSE [Extent1].[Notes] END")]
        [InlineData("concat-null-isnull", "WHERE 1 = 0")]
        [InlineData("concat-null-all", "WHERE 1 = 0")]
        [InlineData("indexof-space", "(CASE WHEN ((0 = ( CAST(DATALENGTH(@p__linq__0) AS int))) AND ([Extent1].[Notes] IS NOT NULL)) THEN 0 ELSE ( CAST(CHARINDEX(@p__linq__1, [Extent1].[Notes]) AS int)) - 1 END)")]
        public void SqlServer_RendersOverride(string caseId, string fragment)
        {
            using var context = new TestWidgetEf6Context();
            Assert.Contains(Ef6Cases.Normalize(fragment), Ef6Cases.Normalize(context.WhereSql(Ef6Cases.Filter(caseId))));
        }
    }
}
