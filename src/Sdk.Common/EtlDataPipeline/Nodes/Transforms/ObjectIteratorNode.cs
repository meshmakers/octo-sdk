using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Object iterator node configuration
/// </summary>
public abstract class ObjectIteratorNodeConfiguration<TSignalConfigurationNode> : TransformNodeConfiguration
    where TSignalConfigurationNode : TokenConfigurationNode
{
    /// <summary>
    /// List of transformations to apply to the signal
    /// </summary>
    public ICollection<TSignalConfigurationNode> Transformations { get; set; } = null!;
}

/// <summary>
/// Transform configuration node for one token
/// </summary>
public class TokenConfigurationNode : ITransformNodeConfiguration
{
    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public string? SourcePath { get; set; }

    /// <inheritdoc />
    public string? TargetPropertyName { get; set; }

    /// <summary>
    /// List of transformations to apply to the signal
    /// </summary>
    public ICollection<TransformNodeConfiguration>? Transforms { get; set; }
}

/// <summary>
/// Object iterator node
/// </summary>
public abstract class ObjectIteratorNode<TTokenConfigurationNode>
    : ITransformPipelineNode where TTokenConfigurationNode : TokenConfigurationNode
{
    /// <inheritdoc />
    public abstract Task ProcessObjectAsync(ITransformDataContext dataContext);

    /// <summary>
    /// Processes the iterator
    /// </summary>
    /// <param name="dataContext"></param>
    /// <param name="iteratorConfigurationNode"></param>
    /// <param name="jToken"></param>
    /// <exception cref="Exception"></exception>
    protected static async Task ProcessToken(ITransformDataContext dataContext, TTokenConfigurationNode iteratorConfigurationNode,
        JToken? jToken)
    {
        if (jToken is JArray jArray)
        {
            var targetArray = new JArray();
            foreach (var jArrayToken in jArray)
            {
                var transformationDataContext =
                    new TransformDataContext(dataContext.GlobalServiceProvider, dataContext.PipelineServiceProvider, jArrayToken);
                await RunTransforms(transformationDataContext, iteratorConfigurationNode);
                targetArray.Add(transformationDataContext.Target);
            }

            dataContext.SetTargetValueByName(iteratorConfigurationNode.TargetPropertyName, targetArray);
        }
        else
        {
            var transformationDataContext = new TransformDataContext(dataContext.GlobalServiceProvider, dataContext.PipelineServiceProvider,
                jToken);
            await RunTransforms(transformationDataContext, iteratorConfigurationNode);
            dataContext.SetTargetValueByName(iteratorConfigurationNode.TargetPropertyName, transformationDataContext.Target);
        }

        //
        // if (jToken is JValue jValue)
        // {
        //     object? value;
        //     // switch (tn.ValueType)
        //     // {
        //     //     case AttributeValueTypesDto.String:
        //     //         value = jValue.Value<string>();
        //     //         break;
        //     //     case AttributeValueTypesDto.Int:
        //     //         value = jValue.Value<int>();
        //     //         break;
        //     //     case AttributeValueTypesDto.Int64:
        //     //         value = jValue.Value<long>();
        //     //         break;
        //     //     case AttributeValueTypesDto.Boolean:
        //     //         value = jValue.Value<bool>();
        //     //         break;
        //     //     case AttributeValueTypesDto.Double:
        //     //         value = jValue.Value<double>();
        //     //         break;
        //     //     default:
        //     //         throw DataPipelineException.ValueTypeUnsupported(tn.SourcePath, tn.ValueType);
        //     // }
        //
        //     dataContext.Target.Add(new PipelineItem { Properties = { { tn.TargetPath, value } } });  
        // }
        // else if (jToken is JArray jArray)
        // {
        //     //  int row = 0;
        //     foreach (var jArrayToken in jArray)
        //     {
        //         if (jArrayToken is JValue jValue2)
        //         {
        //                     
        //         }
        //         else if (jArrayToken is JObject jObject)
        //         {
        //                     
        //             foreach (var keyValuePair in jObject)
        //             {
        //                         
        //             }
        //         }
        //     }
        // }
        //
    }

    private static async Task RunTransforms(ITransformDataContext dataContext, TTokenConfigurationNode iteratorConfigurationNode)
    {
        if (iteratorConfigurationNode.Transforms == null)
        {
            dataContext.SetTargetValue(dataContext.Source);
            return;
        }

        var nodeLookupService = dataContext.GlobalServiceProvider.GetRequiredService<INodeLookupService>();

        foreach (var transformConfigurationNode in iteratorConfigurationNode.Transforms)
        {
            if (!nodeLookupService.TryGetNodeQualifiedName(transformConfigurationNode.GetType(), out var nodeQualifiedName))
            {
                throw DataPipelineException.UnknownConfigurationType(transformConfigurationNode.GetType());
            }

            if (!nodeLookupService.TryGetTransformPipelineNode(nodeQualifiedName, out var node))
            {
                throw DataPipelineException.UnknownObjectPipelineNode(nodeQualifiedName);
            }

            ((TransformDataContext)dataContext).SetConfigurationNode(transformConfigurationNode);
            await node.ProcessObjectAsync(dataContext);
        }
    }
}