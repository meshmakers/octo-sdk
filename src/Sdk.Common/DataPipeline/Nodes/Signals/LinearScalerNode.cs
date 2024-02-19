using Meshmakers.Octo.Sdk.Common.DataPipeline.Configuration;

namespace Meshmakers.Octo.Sdk.Common.DataPipeline.Nodes.Signals;

/// <summary>
/// Configuration for a linear scaler node.
/// </summary>
[Node("LinearScaler", 1, typeof(LinearScalerNode))]
public class LinearScalerConfigurationNode : ConfigurationNode
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
public class LinearScalerNode : ISignalPipelineNode
{
    /// <inheritdoc />
    public Task<object?> ProcessSignalAsync(ISignalDataContext dataContext)
    {
        var c = dataContext.GetNodeConfiguration<LinearScalerConfigurationNode>();
        var scale = (c.ScaleOutputMax - c.ScaleOutputMin) / (c.ScaleInputMax - c.ScaleInputMin);

        var value = dataContext.GetValue<double>();
        var scaledValue = c.ScaleOutputMin + (value - c.ScaleInputMin) * scale;

        return Task.FromResult((object?)scaledValue);
    }
}