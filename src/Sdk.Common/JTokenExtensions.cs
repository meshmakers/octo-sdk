using Meshmakers.Octo.Sdk.Common.EtlDataPipeline;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common;

/// <summary>
/// Extension methods for <see cref="JObject"/>
/// </summary>
public static class JTokenExtensions
{
    /// <summary>
    /// Finds the parent <see cref="JProperty"/> of the given <see cref="JToken"/>
    /// </summary>
    /// <param name="self">Instance to find the parent property for</param>
    /// <returns>The parent <see cref="JProperty"/> or null if not found</returns>
    public static JProperty? FindParentProperty(this JToken self)
    {
        JToken? temp = self;
        while (temp != null)
        {
            if (temp is JProperty property)
            {
                return property; 
            }
            temp = temp.Parent; 
        }

        return null; 
    }
    
    /// <summary>
    /// Replaces value based on path. New object tokens are created for missing parts of the given path.
    /// </summary>
    /// <param name="self">Instance to update</param>
    /// <param name="path">Dot delimited path of the new value. E.g. 'foo.bar'</param>
    /// <param name="value">Value to set.</param>
    public static void ReplaceNested(this JToken self, string path, JToken value)
    {
        if (self is null)
        {
            throw new ArgumentNullException(nameof(self));
        }

        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        var pathParts = path.Split('.');
        JToken currentNode = self;
        
        for (int i = 0; i < pathParts.Length; i++)
        {
            var pathPart = pathParts[i];
            var isLast = i == pathParts.Length - 1;
            var partNode = currentNode!.SelectToken(pathPart);
            
            if (partNode is null)
            {
                var nodeToAdd = isLast ? value : new JObject();
                if (!(currentNode is JObject))
                {
                    throw DataPipelineException.SourceMustBeAnObject(currentNode);
                }
                ((JObject)currentNode).Add(pathPart, nodeToAdd);
                currentNode = currentNode.SelectToken(pathPart)!;
            }
            else
            {
                currentNode = partNode;

                if (isLast)
                {
                    if (currentNode.Parent != null)
                    {
                        currentNode.Replace(value);
                    }
                    else
                    {
                        foreach (var jToken in value.Children())
                        {
                            if (jToken is JProperty jProperty)
                            {
                                currentNode[jProperty.Name] = jProperty.Value.DeepClone();
                            }
                        }
                    }
                }
            }
        }
    }
}