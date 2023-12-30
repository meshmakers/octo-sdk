using Meshmakers.Octo.ConstructionKit.Contracts;


// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a mapping configuration for a plug for data transfer.
/// </summary>
public record MappingConfigurationDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MappingConfigurationDto"/> class.
    /// </summary>
    /// <param name="name">Name of the mapping.</param>
    /// <param name="id">Id of the mapping.</param>
    /// <param name="configuration">Configuration of the mapping.</param>
    public MappingConfigurationDto(string name, OctoObjectId id, string configuration)
    {
        Name = name;
        Id = id;
        Configuration = configuration;
    }
    
    /// <summary>
    /// Gets or sets name of the mapping.
    /// </summary>
    public string Name { get;  } = null!;
    
    /// <summary>
    /// Gets or sets the id of the mapping.
    /// </summary>
    public OctoObjectId Id { get; }
    
    /// <summary>
    /// Gets or sets the configuration of the mapping. This is a JSON string.
    /// </summary>
    public string Configuration { get; } = null!;

    /// <inheritdoc />
    public virtual bool Equals(MappingConfigurationDto? other)
    {
        if (other == null)
            return false;
        return Name.Equals(other.Name) && Id.Equals(other.Id) && Configuration.Equals(other.Configuration);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 23 + Name.GetHashCode();
        hash = hash * 23 + Id.GetHashCode();
        hash = hash * 23 + Configuration.GetHashCode();
        return hash;
    }
}