using System;
using System.Data.Common;
using System.Globalization;

namespace Expresso.Sample.Shared.DataAccess;

internal static class SampleDbValues
{
    public static int GetInt32(DbDataReader reader, int ordinal) =>
        Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    public static short GetInt16(DbDataReader reader, int ordinal) =>
        Convert.ToInt16(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    public static double GetDouble(DbDataReader reader, int ordinal) =>
        Convert.ToDouble(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    public static decimal GetDecimal(DbDataReader reader, int ordinal) =>
        Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    public static DateTime GetDateTime(DbDataReader reader, int ordinal)
    {
        var value = reader.GetValue(ordinal);
        if (value is DateTime dateTime)
        {
            return dateTime;
        }

        return DateTime.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
    }

    public static TimeSpan GetTimeSpan(DbDataReader reader, int ordinal)
    {
        var value = reader.GetValue(ordinal);
        if (value is TimeSpan timeSpan)
        {
            return timeSpan;
        }

        if (value is DateTime dateTime)
        {
            return dateTime.TimeOfDay;
        }

        var type = value.GetType();
        if (string.Equals(type.Name, "OracleIntervalDS", StringComparison.Ordinal))
        {
            var hours = Convert.ToInt32(type.GetProperty("Hours")!.GetValue(value), CultureInfo.InvariantCulture);
            var minutes = Convert.ToInt32(type.GetProperty("Minutes")!.GetValue(value), CultureInfo.InvariantCulture);
            var seconds = Convert.ToInt32(type.GetProperty("Seconds")!.GetValue(value), CultureInfo.InvariantCulture);
            var milliseconds = Convert.ToInt32(type.GetProperty("Milliseconds")!.GetValue(value), CultureInfo.InvariantCulture);
            var days = Convert.ToInt32(type.GetProperty("Days")!.GetValue(value), CultureInfo.InvariantCulture);
            return new TimeSpan(days, hours, minutes, seconds, milliseconds);
        }

        return TimeSpan.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
    }

    public static Guid GetGuid(DbDataReader reader, int ordinal)
    {
        var value = reader.GetValue(ordinal);
        if (value is Guid guid)
        {
            return guid;
        }

        if (value is byte[] bytes)
        {
            return new Guid(bytes);
        }

        return Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!);
    }
}
