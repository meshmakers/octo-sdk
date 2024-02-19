using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using YamlDotNet.Core;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline;

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

    internal static Exception ValueTypeUnsupported(string name, AttributeValueTypesDto valueType)
    {
        return new DataPipelineException($"Value type '{valueType}' is not supported for '{name}'.");
    }

    internal static Exception FirstElementMustBeType(Mark currentStart)
    {
        return new DataPipelineException($"First element must be a type. Line {currentStart.Line}, column {currentStart.Column}.");
    }
}
