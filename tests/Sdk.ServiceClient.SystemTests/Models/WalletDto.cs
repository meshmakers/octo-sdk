using System.Text.Json.Serialization;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;

namespace Sdk.ServiceClient.SystemTests.Models;

/// <summary>
/// Represents a wallet
/// </summary>
public class WalletDto : NamedEntityDto
{
    /// <summary>
    /// Location of fire
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public RtGeospatialValueDto Location { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the last notification update time
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime? LastNotificationUpdate { get; set; }
    
    /// <summary>
    /// Returns subscriptions of the wallet
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public QlQueryConnection<NotificationSubscription> Children { get; set; } = null!;
}