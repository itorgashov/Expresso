namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Default SQL fragments: unquoted identifiers and <c>@</c> parameters.</summary>
public abstract class SampleSqlBase : ISampleSql
{
    /// <inheritdoc />
    public abstract SampleEngine Engine { get; }

    /// <inheritdoc />
    public abstract string Table(string name);

    /// <inheritdoc />
    public virtual string TableAs(string name, string alias) => Table(name) + " AS " + alias;

    /// <inheritdoc />
    public virtual string Col(string tableAlias, string columnName) => tableAlias + "." + columnName;

    /// <inheritdoc />
    public virtual string Param(string name) => "@" + name;
}
