using System.Text.Json.Serialization;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.Serialization;
using Meshmakers.Octo.Runtime.Contracts.Serialization;

// ReSharper disable MemberCanBePrivate.Global
namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Represents data of a configuration (mail, database, etc.)
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public record ConfigurationDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationDto"/> class.
    /// </summary>
    /// <param name="configurationRtId">ID of the configuration.</param>
    /// <param name="configurationTypeId">ID of the configuration type.</param>
    /// <param name="configurationName">Name of the configuration.</param>
    /// <param name="configurationValue">Value of the configuration.</param>
    public ConfigurationDto(OctoObjectId configurationRtId, CkId<CkTypeId> configurationTypeId,
        string configurationName, string configurationValue)
    {
        ConfigurationRtId = configurationRtId;
        ConfigurationTypeId = configurationTypeId;
        ConfigurationName = configurationName;
        ConfigurationValue = configurationValue;
    }

    /// <summary>
    /// Gets or sets the runtime id of the configuration.
    /// </summary>
    [JsonConverter(typeof(OctoObjectIdConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonOctoObjectIdConverter))]
    public OctoObjectId ConfigurationRtId { get; }

    /// <summary>
    /// Returns the type of the configuration.
    /// </summary>
    [JsonConverter(typeof(CkIdTypeIdConverter))]
    public CkId<CkTypeId> ConfigurationTypeId { get; }

    /// <summary>
    /// Returns the name of the configuration.
    /// </summary>
    public string ConfigurationName { get; }

    /// <summary>
    /// Returns the value of the configuration.
    /// </summary>
    public string ConfigurationValue { get; }

    /// <inheritdoc />
    public virtual bool Equals(ConfigurationDto? other)
    {
        return !(other == null) &&
               ConfigurationRtId.Equals(other.ConfigurationRtId) &&
               ConfigurationTypeId.Equals(other.ConfigurationTypeId) &&
               ConfigurationName.Equals(other.ConfigurationName) &&
               ConfigurationValue.Equals(other.ConfigurationValue);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return (((20 * 26 + ConfigurationRtId.GetHashCode()) * 26 +
                 ConfigurationTypeId.GetHashCode()) * 26 +
                ConfigurationName.GetHashCode()) * 26 +
               ConfigurationValue.GetHashCode();
    }
}