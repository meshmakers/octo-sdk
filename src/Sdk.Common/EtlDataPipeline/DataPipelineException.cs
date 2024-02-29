using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using YamlDotNet.Core;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline;

/// <summary>
/// Exception thrown by the data pipeline.
/// </summary>
public class DataPipelineException : Exception
{
    /// <summary>
    /// Constructor.
    /// </summary>
    private DataPipelineException()
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message"></param>
    private DataPipelineException(string message) : base(message)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="inner"></param>
    private DataPipelineException(string message, Exception inner) : base(message, inner)
    {
    }

    internal static Exception NoConfigurationNodeSet()
    {
        return new DataPipelineException("No configuration node set.");
    }

    internal static Exception ValueTypeUnsupported(string path, AttributeValueTypesDto valueType)
    {
        return new DataPipelineException($"Value type '{valueType}' is not supported for path '{path}'.");
    }

    internal static Exception UnknownDiscriminator(string discriminatorValue)
    {
        return new DataPipelineException($"Unknown discriminator '{discriminatorValue}'.");
    }
    
    internal static Exception UnknownConfigurationType(Type type)
    {
        return new DataPipelineException($"Unknown configuration type '{type.FullName}'.");
    }

    internal static Exception UnknownObjectPipelineNode(string nodeQualifiedName)
    {
        return new DataPipelineException($"Unknown object pipeline node '{nodeQualifiedName}'.");
    }

    internal static Exception CurrentIsNull()
    {
        return new DataPipelineException("Current is null.");
    }

    internal static Exception ValueIsObjectButMustBePrimitive(string? path)
    {
        return new DataPipelineException($"Value at path '{path}' is an object but must be a primitive.");
    }

    internal static Exception NoDiscriminatorFound()
    {
        return new DataPipelineException("No discriminator found.");
    }

    internal static Exception CannotCreateInstance(Type nodeType)
    {
        return new DataPipelineException($"Cannot create instance of type '{nodeType.FullName}'.");
    }

    internal static Exception ValueIsArrayMustBeScalar(string? path)
    {
        return new DataPipelineException($"Value at path '{path}' is an array but must be a scalar.");
    }

    internal static Exception ValueIsObjectButMustBeArray(string? path)
    {
        return new DataPipelineException($"Value at path '{path}' is an object but must be an array.");
    }

    internal static Exception ValueIsNotArray(string path)
    {
        return new DataPipelineException($"Value at path '{path}' is not an array.");
    }

    internal static Exception EtlContextFactoryNotSet()
    {
        return new DataPipelineException("EtlContextFactory not set.");
    }
}
