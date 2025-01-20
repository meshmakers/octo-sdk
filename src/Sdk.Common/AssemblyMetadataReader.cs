using System.Reflection;

namespace Meshmakers.Octo.Sdk.Common;

/// <summary>
///     Reads metadata from the assembly.
/// </summary>
public static class AssemblyMetadataReader
{
    /// <summary>
    ///     Returns the product version from the assembly.
    /// </summary>
    /// <returns></returns>
    public static string GetProductVersion()
    {
        var attribute = Assembly
            .GetExecutingAssembly()
            .GetCustomAttributes<AssemblyFileVersionAttribute>()
            .Single();
        return attribute.Version;
    }

    /// <summary>
    ///     Returns the copyright from the assembly.
    /// </summary>
    /// <returns></returns>
    public static string GetCopyright()
    {
        var attribute = Assembly
            .GetExecutingAssembly()
            .GetCustomAttributes<AssemblyCopyrightAttribute>()
            .SingleOrDefault();
        if (attribute == null)
        {
            return "Development Version";
        }

        return attribute.Copyright;
    }
}