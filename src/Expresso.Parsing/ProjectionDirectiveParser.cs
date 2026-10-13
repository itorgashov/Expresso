using Expresso.Core.Filtering;
using Expresso.Core.Projection;

namespace Expresso.Parsing;

/// <summary>Validates, normalizes, and deduplicates comma-separated projection paths.</summary>
public sealed class ProjectionDirectiveParser : IProjectionDirectiveParser
{
    /// <inheritdoc />
    public ProjectionDirective Parse(string? fields, QueryModel model)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));
        if (fields is null) return Expand(model);
        if (string.IsNullOrWhiteSpace(fields)) throw new ArgumentException("fields must not be empty.", nameof(fields));
        var root = new Node();
        foreach (var item in fields.Split(','))
        {
            var path = item.Split('.').Select(s => s.Trim().ToLowerInvariant()).ToArray();
            Validate(path, model);
            var node = root;
            foreach (var name in path)
            {
                if (node.Full) break;
                if (!node.Children.TryGetValue(name, out var next))
                    node.Children.Add(name, next = new Node());
                node = next;
            }
            node.Full = true;
            node.Children.Clear();
        }
        return new ProjectionDirective(Build(root, model));
    }

    private static void Validate(string[] path, QueryModel model)
    {
        for (var i = 0; i < path.Length; i++)
        {
            var name = path[i];
            if (name.Length == 0) throw new ArgumentException("Projection paths must not have empty segments.");
            if (model.TryGetCollection(name, out var collection))
                model = collection.Items;
            else if (!model.TryGetField(name, out _) || i != path.Length - 1)
                throw new ArgumentException("Unknown projection path: " + string.Join(".", path));
        }
    }

    private static IEnumerable<ProjectionSelection> Build(Node node, QueryModel model)
    {
        foreach (var pair in node.Children)
        {
            if (model.TryGetCollection(pair.Key, out var collection))
                yield return ProjectionSelection.Subtree(pair.Key, pair.Value.Full
                    ? Expand(collection.Items) : new ProjectionDirective(Build(pair.Value, collection.Items)));
            else
                yield return new ProjectionSelection(pair.Key);
        }
    }

    private static ProjectionDirective Expand(QueryModel model)
    {
        var selections = model.Fields.Select(f => new ProjectionSelection(f.Item1))
            .Concat(model.Collections.Select(c => ProjectionSelection.Subtree(c.Name, Expand(c.Items)))).ToArray();
        return selections.Length == 0 ? ProjectionDirective.Empty : new ProjectionDirective(selections);
    }

    private sealed class Node
    {
        public bool Full;
        public Dictionary<string, Node> Children { get; } = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
    }
}
