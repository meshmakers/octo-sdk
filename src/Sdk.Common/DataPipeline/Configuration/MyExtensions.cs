using System.Reflection;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;

internal static class MyExtensions
{
    public static string GetConfigurationQualifiedName(this Type type)
    {
        var attr = type.GetCustomAttribute<NodeAttribute>();
        if (attr == null)
        {
            throw new InvalidOperationException($"Type {type.FullName} does not have a NodeAttribute");
        }
        return attr.Name + "@" + attr.Version;
    }
}