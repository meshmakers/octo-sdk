using Meshmakers.Octo.ConstructionKit.Contracts;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Represents a data pipeline configuration for an adapter for data transfer.
/// </summary>
public record DataPipelineConfigurationDto
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DataPipelineConfigurationDto" /> class.
    /// </summary>
    /// <param name="name">Name of the data pipeline.</param>
    /// <param name="id">Id of the data pipeline.</param>
    /// <param name="dataPipelineConfiguration">Data pipeline configuration.</param>
    public DataPipelineConfigurationDto(string name, OctoObjectId id, string dataPipelineConfiguration)
    {
        Name = name;
        Id = id;
        DataPipelineConfiguration = dataPipelineConfiguration;
    }

    /// <summary>
    ///     Gets or sets the configuration of the data pipeline.
    /// </summary>
    public string DataPipelineConfiguration { get; } = null!;

    /// <summary>
    ///     Gets or sets name of the group.
    /// </summary>
    public string Name { get; } = null!;

    /// <summary>
    ///     Gets or sets the id of the group.
    /// </summary>
    public OctoObjectId Id { get; }

    /// <inheritdoc />
    public virtual bool Equals(DataPipelineConfigurationDto? other)
    {
        if (other == null)
        {
            return false;
        }

        return Name.Equals(other.Name) && Id.Equals(other.Id) && DataPipelineConfiguration.Equals(other.DataPipelineConfiguration);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = 20;
        hash = hash * 26 + DataPipelineConfiguration.GetHashCode();
        hash = hash * 26 + Name.GetHashCode();
        hash = hash * 26 + Id.GetHashCode();
        return hash;
    }
}