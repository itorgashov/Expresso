using System;
using System.Globalization;
using System.Linq;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

internal static partial class PublisherTimes
{
    internal static string OracleSelect(string inList, bool opens = true, bool closes = true)
    {
        var columns = new[] { "\"id\" AS Id", opens ? Clock("opens_at") + " AS OpensAtText" : null,
            closes ? Clock("closes_at") + " AS ClosesAtText" : null };
        return "SELECT " + string.Join(", ", columns.Where(c => c is not null)) +
            " FROM \"publisher\" WHERE \"id\" IN (" + inList + ")";
    }

    // Oracle concatenation treats NULL as empty, so protect nullable clocks before formatting.
    private static string Clock(string column) =>
        "CASE WHEN \"" + column + "\" IS NULL THEN NULL ELSE " +
        "TO_CHAR(EXTRACT(HOUR FROM \"" + column + "\"), 'FM00') || ':' || TO_CHAR(EXTRACT(MINUTE FROM \"" + column + "\"), 'FM00') || ':' || TO_CHAR(TRUNC(EXTRACT(SECOND FROM \"" + column + "\")), 'FM00') END";

    internal static TimeSpan ParseTime(string? text)
    {
        var trimmed = (text ?? "00:00:00").Trim();
        var space = trimmed.IndexOf(' ');
        if (space >= 0 && (trimmed[0] == '+' || trimmed[0] == '-'))
        {
            trimmed = trimmed.Substring(space + 1);
        }

        var dot = trimmed.IndexOf('.');
        if (dot >= 0)
        {
            trimmed = trimmed.Substring(0, dot);
        }

        return TimeSpan.ParseExact(trimmed, @"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

}
