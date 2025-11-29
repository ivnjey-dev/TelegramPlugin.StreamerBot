using System.Collections.Generic;

namespace DefaultNamespace;

public static class CloneDictionary
{
    public static Dictionary<string, object> Clone(this IDictionary<string, object> src)
    {
        var copy = new Dictionary<string, object>(src.Count);
        foreach (var kv in src)
        {
            copy[kv.Key] = kv.Value;
        }

        return copy;
    }
}