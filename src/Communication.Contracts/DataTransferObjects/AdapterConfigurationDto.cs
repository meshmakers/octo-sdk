using Meshmakers.Octo.ConstructionKit.Contracts;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents an adapter configuration for data transfer.
/// </summary>
public record AdapterConfigurationDto
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AdapterConfigurationDto" /> class.
    /// </summary>
    /// <param name="adapterRtEntityId">Id of the adapter.</param>
    /// <param name="adapterConfiguration">Configuration of the adapter</param>
    /// <param name="pipelines">Data pipeline configurations.</param>
    public AdapterConfigurationDto(RtEntityId adapterRtEntityId, string? adapterConfiguration, ICollection<PipelineConfigurationDto> pipelines)
    {
        AdapterRtEntityId = adapterRtEntityId;
        AdapterConfiguration = adapterConfiguration;
        Pipelines = pipelines;
    }

    /// <summary>
    ///     Gets or sets the id of the adapter.
    /// </summary>
    public RtEntityId AdapterRtEntityId { get; }

    /// <summary>
    ///     Gets or sets the configuration of the adapter.
    /// </summary>
    public string? AdapterConfiguration { get; }

    /// <summary>
    /// Gets or sets the data pipeline configurations.
    /// </summary>
    public ICollection<PipelineConfigurationDto> Pipelines { get; }


    /// <inheritdoc />
    public virtual bool Equals(AdapterConfigurationDto? other)
    {
        if (other == null)
        {
            return false;
        }

        var b = Pipelines
            .All(x => other.Pipelines.Any(y => y.Equals(x)));
        return AdapterRtEntityId.Equals(other.AdapterRtEntityId)
               && Equals(AdapterConfiguration, other.AdapterConfiguration)
               && b
               && Pipelines.Count.Equals(other.Pipelines.Count);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = 18;
        hash = hash * 24 + AdapterRtEntityId.GetHashCode();
        hash = hash * 24 + AdapterConfiguration?.GetHashCode() ?? 0;
        hash = hash * 24 + Pipelines.GetHashCode();
        return hash;
    }
}