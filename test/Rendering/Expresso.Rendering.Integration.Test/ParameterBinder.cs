using System.Data.Common;
using System.Globalization;
#if NET6_0_OR_GREATER
using IBM.Data.Db2;
#endif
using Microsoft.Data.Sqlite;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Expresso.Rendering.Integration.Test
{
    public static class ParameterBinder
    {
        public static void At(DbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name.StartsWith("@", StringComparison.Ordinal) ? name : "@" + name;
#if NET6_0_OR_GREATER
            if (command is DB2Command)
            {
                parameter.Value = CoerceDb2(value);
                command.Parameters.Add(parameter);
                return;
            }
#endif
            if (command is SqliteCommand)
            {
                parameter.Value = CoerceSqlite(value);
                command.Parameters.Add(parameter);
                return;
            }

            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        public static void Colon(DbCommand command, string name, object value)
        {
            if (command is OracleCommand oracleCommand)
            {
                oracleCommand.BindByName = true;
            }

            var parameter = command.CreateParameter();
            parameter.ParameterName = name.TrimStart('@', ':');
            Assign(parameter, value);
            command.Parameters.Add(parameter);
        }

        private static object CoerceSqlite(object value)
        {
#if !NET6_0_OR_GREATER
            if (value is DateTime dateTime && dateTime.TimeOfDay == TimeSpan.Zero)
            {
                return dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
#endif

            return value;
        }

        private static void Assign(DbParameter parameter, object value)
        {
            if (parameter is OracleParameter oracle)
            {
                AssignOracle(oracle, value);
                return;
            }

            parameter.Value = value;
        }

#if NET6_0_OR_GREATER
        private static object CoerceDb2(object value)
        {
            if (value is null || value is DBNull)
            {
                return DBNull.Value;
            }

            if (value is TimeOnly timeOnly)
            {
                value = timeOnly.ToTimeSpan();
            }
            else if (value is DateOnly dateOnly)
            {
                return dateOnly.ToDateTime(TimeOnly.MinValue);
            }

            return value switch
            {
                Guid guid => guid.ToString(),
                bool flag => flag ? (short)1 : (short)0,
                byte code => (short)code,
                _ => value
            };
        }
#endif

        private static void AssignOracle(OracleParameter parameter, object value)
        {
            if (value is null || value is DBNull)
            {
                parameter.Value = DBNull.Value;
                return;
            }

#if NET6_0_OR_GREATER
            if (value is TimeOnly timeOnly)
            {
                value = timeOnly.ToTimeSpan();
            }
            else if (value is DateOnly dateOnly)
            {
                parameter.OracleDbType = OracleDbType.TimeStamp;
                parameter.Value = dateOnly.ToDateTime(TimeOnly.MinValue);
                return;
            }
#endif

            switch (value)
            {
                case Guid guid:
                    parameter.OracleDbType = OracleDbType.Raw;
                    parameter.Value = guid.ToByteArray();
                    return;
                case bool flag:
                    parameter.OracleDbType = OracleDbType.Int16;
                    parameter.Value = flag ? (short)1 : (short)0;
                    return;
                case TimeSpan interval:
                    parameter.OracleDbType = OracleDbType.IntervalDS;
                    parameter.Value = new OracleIntervalDS(
                        interval.Days,
                        interval.Hours,
                        interval.Minutes,
                        interval.Seconds,
                        interval.Milliseconds);
                    return;
                default:
                    parameter.Value = value;
                    return;
            }
        }
    }
}
