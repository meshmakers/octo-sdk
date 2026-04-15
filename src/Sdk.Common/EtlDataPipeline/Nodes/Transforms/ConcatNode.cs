using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Configuration data type conversion
/// </summary>
[NodeName("Concat", 1)]
public record ConcatNodeConfiguration : PathNodeConfiguration
{
    /// <summary>
    /// Data type that the value is cast to during transformation
    /// </summary>
    // ReSharper disable once CollectionNeverUpdated.Global
    [PropertyGroup("Data", 0)]
    public required List<ConcatItem> Parts { get; set; }
    
    /// <summary>
    /// Defines the path of the concatenated string. The path is relative to the select Path
    /// </summary>
    [PropertyGroup("Paths", 2, "jsonpath")]
    public required string ConcatSubPath { get; set; } 
}

/// <summary>
/// Represents an item to concat.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public record ConcatItem
{
    /// <summary>
    /// The value to concat. Either this or <see cref="ValuePath"/> must be set.
    /// </summary>
    public string? Value { get; set; }
    
    /// <summary>
    /// The value path to concat. Either this or <see cref="Value"/> must be set.
    /// </summary>
    public string? ValuePath { get; set; }
}

/// <summary>
/// Concatenates the values to a single string
/// </summary>
[NodeConfiguration(typeof(ConcatNodeConfiguration))]
// ReSharper disable once ClassNeverInstantiated.Global
public class ConcatNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext, INodeContext nodeContext)
    {
        var c = nodeContext.GetNodeConfiguration<ConcatNodeConfiguration>();
        
        var tokens = dataContext.SelectByPath(c.Path);

        foreach (var token in tokens.ToArray())
        {
            var value = string.Join("", c.Parts.Select(p => p.Value ?? token.GetSimpleValueByPath<string>(p.ValuePath)));
            token.SetValueByPath(c.ConcatSubPath, ValueKinds.Simple, TargetValueWriteModes.Overwrite, value);
        }

        await next(dataContext, nodeContext);
    }
}