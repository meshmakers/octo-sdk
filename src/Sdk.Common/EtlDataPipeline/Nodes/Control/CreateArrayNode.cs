using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Control;

/// <summary>
/// Configuration for CreateArray node
/// </summary>
[NodeName("CreateArray", 1)]
public class CreateArrayNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// Path of single objects used to create an array
    /// </summary>
    public string Path { get; init; } = null!;

    /// <summary>
    /// Path of the array to create
    /// </summary>
    public string? TargetPath { get; init; }
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
        var c = dataContext.GetNodeConfiguration<CreateArrayNodeConfiguration>();

        if (dataContext.Current != null)
        {
            var source = dataContext.Current.SelectTokens(c.Path);

            var target = new JArray { source };

            if (c.TargetPath != null)
            {
                dataContext.SetCurrentValueByPath(c.TargetPath, target);
            }
            else
            {
                dataContext.Current = target;
            }
        }

        await next(dataContext);
    }
}