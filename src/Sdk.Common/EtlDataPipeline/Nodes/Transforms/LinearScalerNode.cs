using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Configuration for a linear scaler node.
/// </summary>
[NodeName("LinearScaler", 1)]
public class LinearScalerNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// Gets or sets the source path
    /// </summary>
    public string? SourcePath { get; set; }
    
    /// <summary>
    /// Target property name
    /// </summary>
    public string? TargetPropertyName { get; set; }
    
    /// <summary>
    /// Input signal minimum value.
    /// </summary>
    public double ScaleInputMin { get; set; } = -1000000;
    
    /// <summary>
    /// Input signal maximum value.
    /// </summary>
    public double ScaleInputMax { get; set; } =  1000000;
    
    /// <summary>
    /// Output signal minimum value.
    /// </summary>
    public double ScaleOutputMin { get; set; }= -1000000;
    
    /// <summary>
    /// Output signal maximum value.
    /// </summary>
    public double ScaleOutputMax { get; set; } =  1000000;
}

/// <summary>
/// Scales a signal linearly.
/// </summary>
[NodeConfiguration(typeof(LinearScalerNodeConfiguration))]
public class LinearScalerNode(NodeDelegate next) : IPipelineNode
{
    /// <inheritdoc />
    public async Task ProcessObjectAsync(IDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<LinearScalerNodeConfiguration>();

        var scale = (c.ScaleOutputMax - c.ScaleOutputMin) / (c.ScaleInputMax - c.ScaleInputMin);

        var value = dataContext.GetCurrentValueByPath<double>(c.SourcePath ?? "$");
        var scaledValue = c.ScaleOutputMin + (value - c.ScaleInputMin) * scale;
        
        dataContext.SetCurrentValueByPath(c.TargetPropertyName, scaledValue);
        await next(dataContext);
    }
}