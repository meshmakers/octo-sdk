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
    /// <param name="adapterRtId">Id of the adapter.</param>
    /// <param name="adapter">Configuration of the adapter</param>
    /// <param name="dataPipelines">Data pipeline configurations.</param>
    public AdapterConfigurationDto(OctoObjectId adapterRtId, string? adapter, ICollection<DataPipelineConfigurationDto>? dataPipelines)
    {
        AdapterRtId = adapterRtId;
        Adapter = adapter;
        DataPipelines = dataPipelines;
    }

    /// <summary>
    ///     Gets or sets the id of the adapter.
    /// </summary>
    public OctoObjectId AdapterRtId { get; }

    /// <summary>
    ///     Gets or sets the configuration of the adapter.
    /// </summary>
    public string? Adapter { get; }

    /// <summary>
    /// Gets or sets the data pipeline configurations.
    /// </summary>
    public ICollection<DataPipelineConfigurationDto>? DataPipelines { get; set; }


    /// <inheritdoc />
    public virtual bool Equals(AdapterConfigurationDto? other)
    {
        if (other == null)
        {
            return false;
        }

        var b = DataPipelines
            ?.All(x => other.DataPipelines?.Any(y => y.Equals(x)) ?? false) ?? true;
        return AdapterRtId.Equals(other.AdapterRtId)
               && Equals(Adapter, other.Adapter)
               && b;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = 18;
        hash = hash * 24 + AdapterRtId.GetHashCode();
        hash = hash * 24 + Adapter?.GetHashCode() ?? 0;
        hash = hash * 24 + DataPipelines?.GetHashCode() ?? 0;
        return hash;
    }
}