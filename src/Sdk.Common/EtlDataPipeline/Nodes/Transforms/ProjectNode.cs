using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Meshmakers.Octo.Sdk.Common.Services;
using Newtonsoft.Json.Linq;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Project node configuration.
/// </summary>
[NodeName("Project", 1)]
public record ProjectNodeConfiguration : PathNodeConfiguration
{
    /// <summary>
    /// Gets or sets the fields to project
    /// </summary>
    [PropertyGroup("Data Mapping", 0)]
    public required ICollection<FieldConfiguration> Fields { get; set; }

    /// <summary>
    /// If true, the properties of the root object will be cleared that is selected by 'Path'
    /// </summary>
    [PropertyGroup("Options", 1)]
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
    /// True if the field should be included, false if it should be removed
    /// </summary>
    public bool Inclusion { get; set; } = false;
}

/// <summary>
/// Projects an object to a new object
/// </summary>
[NodeConfiguration(typeof(ProjectNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class ProjectNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<ProjectNodeConfiguration>();

        var data = dataContext.SelectByPath(c.Path);

        foreach (var jToken in data)
        {
            var clone = jToken.DeepClone();
            if (c.Clear)
            {
                foreach (var child in jToken.Children().ToArray())
                {
                    child.Remove();
                }
            }

            foreach (var fc in c.Fields)
            {
                if (!fc.Inclusion && !c.Clear)
                {
                    var field = jToken.SelectToken(fc.Path);
                    if (field != null)
                    {
                        var property = field.FindParentProperty();
                        if (property != null)
                        {
                            property.Remove();
                        }
                        else
                        {
                            nodeContext.Error($"Parent property not found for field {fc.Path}");
                            throw PipelineExecutionException.ParentPropertyNotFound(nodeContext.NodePath,
                                fc.Path);
                        }
                    }
                }
                else if (fc.Inclusion && c.Clear)
                {
                    var field = clone.SelectToken(fc.Path);
                    if (field != null)
                    {
                        jToken.ReplaceNested(fc.Path, field);
                    }
                }
            }
        }

        await next(dataContext, nodeContext);
    }
}