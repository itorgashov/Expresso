using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Expresso.Rendering.EntityFramework;
using Expresso.Sample.WebApi.NetFx.Ef6.Entities;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

internal static class PublisherTimes
{
    public static void Apply(SampleEf6Context db, IReadOnlyList<Publisher> publishers)
    {
        if (publishers.Count == 0)
        {
            return;
        }

        var provider = Ef6Providers.Resolve(db.Database.Connection);
        if (provider == Ef6Provider.Sqlite)
        {
            foreach (var publisher in publishers)
            {
                publisher.OpensAt = ParseTime(publisher.OpensAtText);
                publisher.ClosesAt = ParseTime(publisher.ClosesAtText);
            }

            return;
        }

        if (provider == Ef6Provider.Oracle)
        {
            var ids = publishers.Select(p => p.Id).ToList();
            var parameters = ids.Select((id, index) => new Oracle.ManagedDataAccess.Client.OracleParameter("p" + index, id)).ToArray();
            var inList = string.Join(",", parameters.Select(p => ":" + p.ParameterName));
            var sql = OracleSelect(inList);
            var rows = db.Database.SqlQuery<PublisherTimeRow>(sql, parameters).ToList();
            var byId = rows.ToDictionary(r => r.Id);
            foreach (var publisher in publishers)
            {
                if (byId.TryGetValue(publisher.Id, out var row))
                {
                    publisher.OpensAt = ParseTime(row.OpensAtText);
                    publisher.ClosesAt = ParseTime(row.ClosesAtText);
                }
            }
        }
    }

    internal static string OracleSelect(string inList) =>
        "SELECT \"id\" AS Id, " + Clock("opens_at") + " AS OpensAtText, " + Clock("closes_at") + " AS ClosesAtText FROM \"publisher\" WHERE \"id\" IN (" + inList + ")";

    private static string Clock(string column) =>
        "TO_CHAR(EXTRACT(HOUR FROM \"" + column + "\"), 'FM00') || ':' || TO_CHAR(EXTRACT(MINUTE FROM \"" + column + "\"), 'FM00') || ':' || TO_CHAR(TRUNC(EXTRACT(SECOND FROM \"" + column + "\")), 'FM00')";

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

    private sealed class PublisherTimeRow
    {
        public int Id { get; set; }

        public string? OpensAtText { get; set; }

        public string? ClosesAtText { get; set; }
    }
}
