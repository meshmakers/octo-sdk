using Meshmakers.Octo.ConstructionKit.Contracts;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents a group configuration for a plug for data transfer.
/// </summary>
public record GroupConfigurationDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupConfigurationDto"/> class.
    /// </summary>
    /// <param name="name">Name of the group.</param>
    /// <param name="id">Id of the group.</param>
    /// <param name="mappings">Mappings of the group.</param>
    public GroupConfigurationDto(string name, OctoObjectId id, IReadOnlyCollection<MappingConfigurationDto> mappings)
    {
        Name = name;
        Id = id;
        Mappings = mappings;
    }
    
    /// <summary>
    /// Gets or sets the mappings of the group.
    /// </summary>
    public IReadOnlyCollection<MappingConfigurationDto> Mappings { get; } = null!;
    
    /// <summary>
    /// Gets or sets name of the group.
    /// </summary>
    public string Name { get; } = null!;
    
    /// <summary>
    /// Gets or sets the id of the group.
    /// </summary>
    public OctoObjectId Id { get; }

    /// <inheritdoc />
    public virtual bool Equals(GroupConfigurationDto? other)
    {
        if (other == null)
        {
            return false;
        }

        var b = Mappings.All(x => other.Mappings.Any(y => y.Equals(x)));
        return Name.Equals(other.Name) && Id.Equals(other.Id) && b;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        int hash = 20;
        hash = hash * 26 + Mappings.GetHashCode();
        hash = hash * 26 + Name.GetHashCode();
        hash = hash * 26 + Id.GetHashCode();
        return hash;
    }
}