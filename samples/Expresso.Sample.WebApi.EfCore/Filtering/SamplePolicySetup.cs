using System;
using System.Globalization;
using System.Linq;
using Expresso.Core.Filtering;
using Expresso.Core.Policies;
using Expresso.Parsing;
using Expresso.Parsing.Policies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Expresso.Sample.WebApi.EfCore.Filtering;

/// <summary>Reads optional sample policy sections and compiles each endpoint at startup.</summary>
public static class SamplePolicySetup
{
    /// <summary>Compiles a context and registers a typed singleton; configuration errors stop startup.</summary>
    public static void Register<TController>(IServiceCollection services, IConfiguration configuration, string context,
        IRequestFieldsInfoProvider fieldsProvider, Action<string> log, LiteralParseOptions? options = null)
    {
        var filter = fieldsProvider is IRequestQueryModelProvider modelProvider ? modelProvider.GetFilterModel(context) :
            QueryModel.FromFields(fieldsProvider.GetValidFilterFields(context));
        var sort = fieldsProvider is IRequestQueryModelProvider sortProvider ? sortProvider.GetSortModel(context) :
            QueryModel.FromFields(fieldsProvider.GetValidSortFields(context));
        try
        {
            var definition = Read(configuration.GetSection("Expresso:Policies:" + context));
            if (definition != null)
            {
                var compiled = QueryPolicyCompiler.Compile(definition, filter, sort, options);
                filter = compiled.Filter; sort = compiled.Sort;
                foreach (var warning in compiled.Warnings) log($"Policy '{context}': {warning}");
            }
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        { throw new InvalidOperationException($"Policy for context '{context}' could not be compiled: {ex.Message}", ex); }
        services.AddSingleton(new ControllerQueryModels<TController>(filter, sort));
    }

    /// <summary>Reads an array of rule lines, preserving default limits for omitted properties.</summary>
    public static QueryPolicyDefinition? Read(IConfigurationSection section)
    {
        if (!section.Exists()) return null;
        var rules = section.GetSection("Rules");
        if (rules.Value != null) throw new ArgumentException("Rules must be an array of policy lines.");
        var definition = new QueryPolicyDefinition { Rules = string.Join("\n", rules.GetChildren().Select(c =>
            c.Value ?? throw new ArgumentException("Each Rules entry must be a string."))) };
        foreach (var child in section.GetSection("Limits").GetChildren())
        {
            if (!int.TryParse(child.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                throw new ArgumentException($"Invalid limit '{child.Key}'.");
            switch (child.Key.ToLowerInvariant())
            {
                case "maxdepth": definition.Limits.MaxDepth = value; break;
                case "maxnodes": definition.Limits.MaxNodes = value; break;
                case "maxargs": definition.Limits.MaxArgs = value; break;
                case "maxinitems": definition.Limits.MaxInItems = value; break;
                case "maxstringlength": definition.Limits.MaxStringLength = value; break;
                case "maxsortkeys": definition.Limits.MaxSortKeys = value; break;
                default: throw new ArgumentException($"Unknown limit '{child.Key}'.");
            }
        }
        var detail = section["ErrorDetail"];
        if (detail != null)
        {
            if (!Enum.TryParse<QueryPolicyErrorDetail>(detail, true, out var parsed) || !Enum.IsDefined(typeof(QueryPolicyErrorDetail), parsed))
                throw new ArgumentException("Invalid ErrorDetail.");
            definition.ErrorDetail = parsed;
        }
        return definition;
    }
}
