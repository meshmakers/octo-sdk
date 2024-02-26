using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;
using Microsoft.Extensions.Logging;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Project node configuration.
/// </summary>
public class ProjectNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// Gets or sets the fields to project
    /// </summary>
    public ICollection<FieldConfiguration>? Fields { get; set; }
}

/// <summary>
/// Settings for a field to project
/// </summary>
public class FieldConfiguration
{
    /// <summary>
    /// JSON path to the source field
    /// </summary>
    public string? Path { get; set; }
    
    /// <summary>
    /// True if the field should be included, false if it should be hidden
    /// </summary>
    public bool? Inclusion { get; set; }
}

/// <summary>
/// Projects a object to a new object
/// </summary>
[Node("Project", 1, typeof(ProjectNodeConfiguration))]
public class ProjectNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<ProjectNodeConfiguration>();
        dataContext.Logger.LogDebug("Executing {Node} {Description}", nameof(ProjectNode), c.Description);

        if (dataContext.Current == null || c.Fields == null || c.Fields.Count == 0)
        {
            await next(dataContext);
            return;
        }
        

        foreach (var fc in c.Fields)
        {
            var jToken = dataContext.Current.SelectToken(fc.Path ?? "$");
            if (jToken != null)
            {
                if (fc.Inclusion.GetValueOrDefault() == false)
                {
                    jToken.Parent?.Remove();
                }
            }
        }
        
        dataContext.Logger.LogDebug("Executing {Node} {Description} done - executing next", nameof(ProjectNode), c.Description);
        await next(dataContext);
    }
}