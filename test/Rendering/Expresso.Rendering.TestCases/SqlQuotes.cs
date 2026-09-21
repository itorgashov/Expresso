namespace Expresso.Rendering.TestCases
{
    public static class SqlQuotes
    {
        public static string Brackets(string dotted) =>
            string.Join(".", dotted.Split('.').Select(p => "[" + p + "]"));

        public static string Double(string dotted) =>
            string.Join(".", dotted.Split('.').Select(p => "\"" + p.Replace("\"", "\"\"") + "\""));

        public static string Backtick(string dotted) =>
            string.Join(".", dotted.Split('.').Select(p => "`" + p.Replace("`", "``") + "`"));
    }
}
