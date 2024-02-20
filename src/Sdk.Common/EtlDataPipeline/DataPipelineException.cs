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

    internal static Exception FirstElementMustBeType(Mark currentStart)
    {
        return new DataPipelineException($"First element must be a type. Line {currentStart.Line}, column {currentStart.Column}.");
    }

    internal static Exception UnknownConfigurationType(string typeValue)
    {
        return new DataPipelineException($"Unknown configuration type '{typeValue}'.");
    }
    
    internal static Exception UnknownConfigurationType(Type type)
    {
        return new DataPipelineException($"Unknown configuration type '{type.FullName}'.");
    }


    internal static Exception InvalidYamlConfigurationTypeMissing()
    {
        return new DataPipelineException("Invalid YAML configuration. Type attribute is missing.");
    }

    internal static Exception WriteYamlNotSupported()
    {
        return new DataPipelineException("WriteYaml is not supported.");
    }

    internal static Exception UnknownObjectPipelineNode(string nodeQualifiedName)
    {
        return new DataPipelineException($"Unknown object pipeline node '{nodeQualifiedName}'.");
    }

    internal static Exception NoExtractsConfigured()
    {
        return new DataPipelineException("No extracts configured.");
    }

    internal static Exception SourceIsNull()
    {
        return new DataPipelineException("Source is null.");
    }

    internal static Exception PathNotFound(string path)
    {
        return new DataPipelineException($"Path '{path}' not found.");
    }

    internal static Exception ValueIsObjectButMustBePrimitive(string path)
    {
        return new DataPipelineException($"Value at path '{path}' is an object but must be a primitive.");
    }
}
