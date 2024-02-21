using YamlDotNet.Serialization;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration.Serializer;

/// <summary>
/// This implementation of a <see cref="ITypeInspector"/> ensures that property "Type" that is used for the discriminator is ignored during
/// deserialization, because it is used by the TypeDiscriminator to determine the type of the object.
/// </summary>
/// <param name="innerTypeInspector">Inner type inspector</param>
internal class NodeConfigurationTypeInspector(ITypeInspector innerTypeInspector)
    : ITypeInspector
{
    public IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
    {
        return innerTypeInspector.GetProperties(type, container);
    }

    public IPropertyDescriptor GetProperty(Type type, object? container, string name, bool ignoreUnmatched)
    {
        if (name == YamlFields.Type)
        {
            return null!;
        }

        return innerTypeInspector.GetProperty(type, container, name, ignoreUnmatched);
    }
}