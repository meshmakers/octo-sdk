using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger;
using Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.Common.Adapters;

/// <summary>
/// Implements a default pipeline debugger
/// </summary>
public sealed class AdapterPipelineDebugger : DefaultPipelineDebugger
{
    private readonly IAdapterHubClient _adapterHubClient;
    private readonly IPipelineDebugSerializer _pipelineDebugSerializer;

    /// <summary>
    /// Creates a new instance of <see cref="T:Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Debugger.DefaultPipelineDebugger" />
    /// </summary>
    /// <param name="adapterHubClient"></param>
    /// <param name="loggerFactory"></param>
    /// <param name="pipelineDebugSerializer"></param>
    public AdapterPipelineDebugger(IAdapterHubClient adapterHubClient, ILoggerFactory loggerFactory, IPipelineDebugSerializer pipelineDebugSerializer)
    : base(loggerFactory)
    {
        _adapterHubClient = adapterHubClient;
        _pipelineDebugSerializer = pipelineDebugSerializer;
    }

    /// <inheritdoc />
    public override async Task EndPipelineExecutionAsync()
    {
        var debugInformationRoot = GetDebugInformation();
        var debugData = await _pipelineDebugSerializer.SerializeAsync(debugInformationRoot);
        if (PipelineRtEntityId == null)
        {
            throw PipelineDebuggerException.PipelineRtEntityIdNotSet();
        }

        await _adapterHubClient.SendDebugDataAsync(PipelineRtEntityId.Value, debugData);
    }
}