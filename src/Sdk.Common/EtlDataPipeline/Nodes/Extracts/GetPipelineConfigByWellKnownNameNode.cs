using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Extracts;

/// <summary>
/// Configuration for node to get pipeline configuration by well known name
/// </summary>
[NodeName("GetPipelineConfigByWellKnownName", 1)]
public record GetPipelineConfigByWellKnownNameNodeConfiguration : TargetPathNodeConfiguration
{
    /// <summary>
    /// The well known name of the pipeline configuration (static value)
    /// </summary>
    public string? WellKnownName { get; init; }

    /// <summary>
    /// JSON path to get the well known name from the input data
    /// </summary>
    public string? WellKnownNamePath { get; init; }
}

/// <summary>
/// Node that retrieves pipeline configuration by WellKnownName from the GlobalConfiguration.
/// The configuration is loaded from the pipeline's Uses association and made available
/// in the data context at the configured target path.
/// </summary>
[NodeConfiguration(typeof(GetPipelineConfigByWellKnownNameNodeConfiguration))]
public class GetPipelineConfigByWellKnownNameNode(NodeDelegate next, IEtlContext etlContext) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<GetPipelineConfigByWellKnownNameNodeConfiguration>();

        var wellKnownName = ResolveWellKnownName(c, dataContext);

        if (!etlContext.GlobalConfiguration.IsDefined(wellKnownName))
        {
            throw new PipelineExecutionException(
                $"Pipeline configuration '{wellKnownName}' not found in GlobalConfiguration. " +
                "Ensure the pipeline has a 'Uses' association to a configuration entity with this WellKnownName.");
        }

        var rawJson = etlContext.GlobalConfiguration.GetRawJson(wellKnownName);
        var pipelineConfigJson = JToken.Parse(rawJson);

        dataContext.SetValueByPath(c.TargetPath, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode,
            pipelineConfigJson);

        await next(dataContext, nodeContext);
    }

    private static string ResolveWellKnownName(GetPipelineConfigByWellKnownNameNodeConfiguration c,
        IDataContext dataContext)
    {
        if (c.WellKnownName == null && c.WellKnownNamePath == null)
        {
            throw new PipelineExecutionException(
                "Either 'wellKnownName' or 'wellKnownNamePath' must be set on GetPipelineConfigByWellKnownName node.");
        }

        if (c.WellKnownName != null)
        {
            return c.WellKnownName;
        }

        var wellKnownNameValue = dataContext.GetSimpleValueByPath<string>(c.WellKnownNamePath!);
        if (wellKnownNameValue == null)
        {
            throw new PipelineExecutionException(
                $"WellKnownName value at path '{c.WellKnownNamePath}' is null.");
        }

        return wellKnownNameValue;
    }
}
