using System.Text.RegularExpressions;
using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Configuration for the FormatString node that formats strings with JSON path placeholders
/// </summary>
[NodeName("FormatString", 1)]
public record FormatStringNodeConfiguration : TargetPathNodeConfiguration
{
    /// <summary>
    /// Gets or sets the format string with placeholders like {$.path.to.value}
    /// </summary>
    public required string Format { get; set; }

    /// <summary>
    /// Gets or sets the string to use for null values (default: "NULL")
    /// </summary>
    public string NullValue { get; set; } = "NULL";
}

/// <summary>
/// Formats a string by replacing JSON path placeholders with actual values
/// </summary>
[NodeConfiguration(typeof(FormatStringNodeConfiguration))]
public class FormatStringNode(NodeDelegate next) : IPipelineNode
{
    private static readonly Regex PlaceholderRegex = new Regex(@"\{(\$[^}]*)\}", RegexOptions.Compiled);

    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<FormatStringNodeConfiguration>();

        // Find all placeholders in the format string
        var matches = PlaceholderRegex.Matches(c.Format);
        var formattedString = c.Format;

        foreach (Match match in matches)
        {
            var placeholder = match.Groups[0].Value; // The full placeholder including braces
            var jsonPath = match.Groups[1].Value; // The JSON path without braces

            try
            {
                // Try to get the value at the JSON path
                var token = dataContext.Current?.SelectToken(jsonPath);

                if (token == null)
                {
                    // Path not found
                    nodeContext.Error($"JSON path '{jsonPath}' not found");
                    throw PipelineExecutionException.PathNotFound(nodeContext.NodePath, jsonPath);
                }

                // Check if it's a simple value (not an object or array)
                if (token.Type == JTokenType.Object || token.Type == JTokenType.Array)
                {
                    nodeContext.Error($"JSON path '{jsonPath}' resolves to a non-simple value (object or array)");
                    throw new PipelineExecutionException($"[{nodeContext.NodePath}]: JSON path '{jsonPath}' must resolve to a simple value, but found {token.Type}");
                }

                // Get the value as string
                string? value = null;
                if (token.Type == JTokenType.Null)
                {
                    value = c.NullValue;
                }
                else
                {
                    value = token.ToString();
                }

                // Replace the placeholder with the actual value
                formattedString = formattedString.Replace(placeholder, value);
            }
            catch (Exception ex) when (ex is not PipelineExecutionException)
            {
                // Handle any JSON path parsing errors
                nodeContext.Error($"Error evaluating JSON path '{jsonPath}': {ex.Message}");
                throw new PipelineExecutionException($"[{nodeContext.NodePath}]: Error evaluating JSON path '{jsonPath}'", ex);
            }
        }

        // Set the formatted string at the target path
        dataContext.SetValueByPath(c.TargetPath, c.DocumentMode, c.TargetValueKind, c.TargetValueWriteMode, formattedString);

        await next(dataContext, nodeContext);
    }
}