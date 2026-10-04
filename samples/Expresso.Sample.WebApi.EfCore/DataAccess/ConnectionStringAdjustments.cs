namespace Expresso.Sample.WebApi.EfCore.DataAccess;

internal static class ConnectionStringAdjustments
{
    public static string ForEngine(SampleEngine engine, string connectionString, bool mariaDb)
    {
        var cs = connectionString.Trim();
        if (engine is SampleEngine.MySql || mariaDb)
        {
            if (!cs.Contains("AllowUserVariables=", StringComparison.OrdinalIgnoreCase))
            {
                cs = cs.TrimEnd(';') + ";AllowUserVariables=True";
            }
        }

        if (engine == SampleEngine.Db2 && !cs.Contains("EnableEFCaseSensitivity=", StringComparison.OrdinalIgnoreCase))
        {
            cs = cs.TrimEnd(';') + ";EnableEFCaseSensitivity=true";
        }

        return cs;
    }
}
