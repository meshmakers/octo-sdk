using Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Configuration;

namespace Meshmakers.Octo.Sdk.Common.EtlDataPipeline.Nodes.Transforms;

/// <summary>
/// Configuration for a linear scaler node.
/// </summary>
public class LinearScalerNodeConfiguration : TransformNodeConfiguration
{
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
[Node("LinearScaler", 1, typeof(LinearScalerNodeConfiguration))]
public class LinearScalerNode : ITransformPipelineNode
{
    /// <inheritdoc />
    public Task ProcessObjectAsync(ITransformDataContext transformDataContext)
    {
        var c = transformDataContext.GetNodeConfiguration<LinearScalerNodeConfiguration>();
        var scale = (c.ScaleOutputMax - c.ScaleOutputMin) / (c.ScaleInputMax - c.ScaleInputMin);

        var value = transformDataContext.GetSourceValueByPath<double>(c.SourcePath ?? "$");
        var scaledValue = c.ScaleOutputMin + (value - c.ScaleInputMin) * scale;
        
        transformDataContext.SetTargetValueByName(c.TargetPropertyName, scaledValue);
        return Task.CompletedTask;
    }
}