using System.Collections.Generic;

namespace Expresso.Sample.Shared.DataAccess;

internal static class ParameterMerge
{
    public static void Merge(Dictionary<string, object> target, IReadOnlyDictionary<string, object> source)
    {
        foreach (var pair in source)
        {
            if (!target.ContainsKey(pair.Key))
            {
                target.Add(pair.Key, pair.Value);
            }
        }
    }
}
