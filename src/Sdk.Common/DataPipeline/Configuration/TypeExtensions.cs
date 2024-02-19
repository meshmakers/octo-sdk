using System.Reflection;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;

internal static class TypeExtensions
{
    public static NodeAttribute GetNodeAttribute(this Type type)
    {
        var customAttribute = type.GetCustomAttribute<NodeAttribute>();
        if (customAttribute == null)
        {
            throw new InvalidOperationException($"Type '{type.FullName}' does not have a NodeAttribute");
        }

        return customAttribute;
    }
    
    public static string GetConfigurationQualifiedName(this Type type)
    {
        var customAttribute = type.GetNodeAttribute();
        return customAttribute.Name + "@" + customAttribute.Version;
    }
    
    public static Type GetConfigurationNodeType(this Type type)
    {
        var customAttribute = type.GetNodeAttribute();
        return customAttribute.ConfigurationNodeType;
    }
}