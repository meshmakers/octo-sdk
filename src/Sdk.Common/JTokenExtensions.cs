using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common;

/// <summary>
/// Extension methods for <see cref="JObject"/>
/// </summary>
public static class JTokenExtensions
{
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
                ((JObject)currentNode).Add(pathPart, nodeToAdd);
                currentNode = currentNode.SelectToken(pathPart)!;
            }
            else
            {
                currentNode = partNode;

                if (isLast)
                {
                    currentNode.Replace(value);
                }
            }
        }
    }
}