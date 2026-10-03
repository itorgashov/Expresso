using System.Text;

namespace Expresso.Rendering
{
    /// <summary>
    /// State for rendering one expression scope: the field map and collections visible at this level,
    /// plus the shared SQL builder and bound parameters.
    /// </summary>
    public sealed class SqlRenderScope
    {
        internal SqlRenderScope(
            Dictionary<string, string> fieldToColumnMap,
            StringBuilder builder,
            Dictionary<string, object> parameters,
            string paramNamePrefix,
            Dictionary<string, CollectionSqlMapping> collections)
        {
            FieldToColumnMap = fieldToColumnMap;
            Builder = builder;
            Parameters = parameters;
            ParamNamePrefix = paramNamePrefix;
            Collections = collections;
        }

        /// <summary>Field name to column expression for the current scope.</summary>
        public Dictionary<string, string> FieldToColumnMap { get; }

        /// <summary>SQL text being built.</summary>
        public StringBuilder Builder { get; }

        /// <summary>Bound parameter values keyed by bind name.</summary>
        public Dictionary<string, object> Parameters { get; }

        /// <summary>Bind-name prefix.</summary>
        public string ParamNamePrefix { get; }

        /// <summary>Collections reachable from the current scope.</summary>
        public Dictionary<string, CollectionSqlMapping> Collections { get; }
    }
}
