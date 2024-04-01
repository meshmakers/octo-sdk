using MassTransit;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;

/// <summary>
/// Handles exceptions thrown by the pipeline debugger
/// </summary>
public class PipelineDebuggerException : Exception
{
    private PipelineDebuggerException()
    {
    }

    private PipelineDebuggerException(string message) : base(message)
    {
    }

    private PipelineDebuggerException(string message, Exception inner) : base(message, inner)
    {
    }

    internal static Exception PipelineRtEntityIdNotSet()
    {
        throw new PipelineDebuggerException("Pipeline RtEntityId not set");
    }
}
