using Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes.Transforms;

/// <summary>
/// 
/// </summary>
public abstract class ObjectIteratorConfigurationNode<TSignalConfigurationNode> : TransformConfigurationNode
    where TSignalConfigurationNode : TokenConfigurationNode
{
    /// <summary>
    /// List of transformations to apply to the signal
    /// </summary>
    public ICollection<TSignalConfigurationNode> Transforms { get; set; } = null!;
}

/// <summary>
/// 
/// </summary>
public class TokenConfigurationNode : TransformConfigurationNode
{
    /// <summary>
    /// List of transformations to apply to the signal
    /// </summary>
    public ICollection<TransformConfigurationNode>? Transforms { get; set; }
}

/// <summary>
/// 
/// </summary>
public abstract class ObjectIteratorNode<TObjectIteratorConfigurationNode, TTokenConfigurationNode> : ITransformPipelineNode
    where TObjectIteratorConfigurationNode : ObjectIteratorConfigurationNode<TTokenConfigurationNode>
    where TTokenConfigurationNode : TokenConfigurationNode
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
    protected static async Task ProcessToken(ITransformDataContext dataContext, TTokenConfigurationNode iteratorConfigurationNode, JToken? jToken)
    {
        if (jToken is JArray jArray)
        {
            var targetArray = new JArray();
            //  int row = 0;
            foreach (var jArrayToken in jArray)
            {
                var transformationDataContext = new TransformDataContext(dataContext.ServiceProvider, jArrayToken);
                await RunTransforms(transformationDataContext, iteratorConfigurationNode);
                targetArray.Add(transformationDataContext.Target);
            }
            dataContext.SetTargetValueByName(iteratorConfigurationNode.TargetPropertyName, targetArray);
        }
        else
        {
            var transformationDataContext = new TransformDataContext(dataContext.ServiceProvider, jToken);
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
        var nodeLookupService = dataContext.ServiceProvider.GetRequiredService<INodeLookupService>();

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