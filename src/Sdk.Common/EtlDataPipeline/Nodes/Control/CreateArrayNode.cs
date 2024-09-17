using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for CreateArray node
/// </summary>
[NodeName("CreateArray", 1)]
public class CreateArrayNodeConfiguration : SourceTargetPathNodeConfiguration
{
    /// <inheritdoc />
    public CreateArrayNodeConfiguration()
    {
        TargetValueKind = ValueKind.Simple;
    }
}

/// <summary>
/// Create an array from a single object
/// </summary>
[NodeConfiguration(typeof(CreateArrayNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class CreateArrayNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.NodeContext.GetNodeConfiguration<CreateArrayNodeConfiguration>();

        if (dataContext.Current != null)
        {
            var source = dataContext.Current.SelectTokens(c.Path);

            var target = new JArray { source };

            dataContext.SetValueByPath(c.TargetPath, c.TargetValueKind, c.TargetValueWriteMode, target);
        }

        await next(dataContext);
    }
}