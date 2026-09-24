using System;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Expresso.Sample.Shared.DataAccess;

public sealed class OracleStyleParameterBinder : ISampleParameterBinder
{
    public void Bind(DbCommand command, string name, object? value)
    {
        var bindByName = command.GetType().GetProperty("BindByName");
        bindByName?.SetValue(command, true);

        var parameter = command.CreateParameter();
        parameter.ParameterName = name.TrimStart('@', ':');
        AssignOracle(parameter, value);
        command.Parameters.Add(parameter);
    }

    private static void AssignOracle(DbParameter parameter, object? value)
    {
        if (value is null || value is DBNull)
        {
            parameter.Value = DBNull.Value;
            return;
        }

        var oracleDbType = parameter.GetType().GetProperty("OracleDbType");

        if (value is Guid guid)
        {
            SetOracleDbType(oracleDbType, parameter, "Raw");
            parameter.Value = guid.ToByteArray();
            return;
        }

        if (value is TimeSpan interval)
        {
            SetOracleDbType(oracleDbType, parameter, "IntervalDS");
            parameter.Value = CreateOracleIntervalDs(interval);
            return;
        }

        parameter.Value = value;
    }

    private static void SetOracleDbType(PropertyInfo? oracleDbType, DbParameter parameter, string enumName)
    {
        if (oracleDbType is null)
        {
            return;
        }

        var enumType = oracleDbType.PropertyType;
        oracleDbType.SetValue(parameter, Enum.Parse(enumType, enumName));
    }

    private static object CreateOracleIntervalDs(TimeSpan interval)
    {
        var type =
            Type.GetType("Oracle.ManagedDataAccess.Types.OracleIntervalDS, Oracle.ManagedDataAccess.Core", throwOnError: false)
            ?? Type.GetType("Oracle.ManagedDataAccess.Types.OracleIntervalDS, Oracle.ManagedDataAccess", throwOnError: false);
        if (type is null)
        {
            throw new InvalidOperationException("Oracle.ManagedDataAccess is not loaded; cannot bind TIME values.");
        }

        return Activator.CreateInstance(
            type,
            interval.Days,
            interval.Hours,
            interval.Minutes,
            interval.Seconds,
            interval.Milliseconds)!;
    }
}
