using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.SimulationNodes;

internal static class JObjectExtensions
{
    public static T GetValue<T>(this JObject jObject, string key, T defaultValue)
    {
        var v = jObject[key];
        if (v == null)
        {
            return defaultValue;
        }

        return v.Value<T>() ?? defaultValue;
    }
}