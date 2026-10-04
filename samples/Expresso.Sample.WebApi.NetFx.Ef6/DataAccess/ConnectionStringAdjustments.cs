using System;

namespace Expresso.Sample.WebApi.NetFx.Ef6.DataAccess;

internal static class ConnectionStringAdjustments
{
    public static string ForEngine(SampleEngine engine, string connectionString, bool mariaDb)
    {
        var cs = connectionString.Trim();
        if (engine == SampleEngine.Sqlite && !cs.Contains("BinaryGUID=", StringComparison.OrdinalIgnoreCase))
        {
            cs = cs.TrimEnd(';') + ";BinaryGUID=False";
        }

        if ((engine is SampleEngine.MySql || mariaDb) &&
            cs.Contains("SslMode=None", StringComparison.OrdinalIgnoreCase))
        {
            cs = System.Text.RegularExpressions.Regex.Replace(
                cs,
                "SslMode=None",
                "SslMode=Disabled",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return cs;
    }
}
