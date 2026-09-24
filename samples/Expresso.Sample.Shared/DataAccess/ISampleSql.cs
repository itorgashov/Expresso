namespace Expresso.Sample.Shared.DataAccess;

/// <summary>Dialect fragments used by the sample repositories (tables, columns, and bind markers).</summary>
public interface ISampleSql
{
    /// <summary>Engine these fragments target.</summary>
    SampleEngine Engine { get; }

    /// <summary>Returns the table reference for <paramref name="name"/>.</summary>
    /// <param name="name">Unqualified table name.</param>
    /// <returns>SQL that names the table.</returns>
    string Table(string name);

    /// <summary>Returns the table reference with an alias.</summary>
    /// <param name="name">Unqualified table name.</param>
    /// <param name="alias">Alias used in the query.</param>
    /// <returns>SQL that names the table and alias.</returns>
    string TableAs(string name, string alias);

    /// <summary>Qualified column reference (for example <c>a.created_at</c>). Oracle and Db2 quote the column name.</summary>
    /// <param name="tableAlias">Table alias already present in the query.</param>
    /// <param name="columnName">Unquoted column name.</param>
    /// <returns>SQL that names the column.</returns>
    string Col(string tableAlias, string columnName);

    /// <summary>Returns the bind marker for a parameter.</summary>
    /// <param name="name">Parameter name without a prefix.</param>
    /// <returns>A marker such as <c>@name</c> or <c>:name</c>.</returns>
    string Param(string name);
}
