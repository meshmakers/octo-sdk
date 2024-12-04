using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Project node configuration.
/// </summary>
[NodeName("Project", 1)]
public record ProjectNodeConfiguration : SourceTargetPathNodeConfiguration
{
    /// <summary>
    /// Gets or sets the fields to project
    /// </summary>
    public required ICollection<FieldConfiguration> Fields { get; set; }

    /// <summary>
    /// If true, only the fields specified in the configuration will be included in the output
    /// </summary>
    public bool Clear { get; set; } = false;
}

/// <summary>
/// Settings for a field to project
/// </summary>
public class FieldConfiguration
{
    /// <summary>
    /// JSON path to the source field
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// True if the field should be included, false if it should be hidden
    /// </summary>
    public bool Inclusion { get; set; } = true;
}

/// <summary>
/// Projects an object to a new object
/// </summary>
[NodeConfiguration(typeof(ProjectNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class ProjectNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.NodeContext.GetNodeConfiguration<ProjectNodeConfiguration>();

        var data = dataContext.SelectByPath<JToken>(c.Path);

        List<JToken> result = new();
        foreach (var jToken in data)
        {
            if (jToken == null)
            {
                continue;
            }

            JToken? jTokenClone;
            if (c.Clear)
            {
                jTokenClone = new JObject();
            }
            else
            {
                jTokenClone = jToken.DeepClone();
            }

            foreach (var fc in c.Fields)
            {
                var field = jToken.SelectToken(fc.Path);
                if (field != null)
                {
                    if (!fc.Inclusion)
                    {
                        field.Parent?.Remove();
                    }
                    else
                    {
                        jTokenClone.ReplaceNested(fc.Path, field);
                    }
                }
            }
            
            result.Add(jTokenClone);
        }
        
        if (result.Count == 1)
        {
            dataContext.SetValueByPath(c.TargetPath, c.TargetValueKind, c.TargetValueWriteMode, result[0]);
        }
        else
        {
            dataContext.SetValueByPath(c.TargetPath, c.TargetValueKind, c.TargetValueWriteMode, result);
        }

        await next(dataContext);
    }
}