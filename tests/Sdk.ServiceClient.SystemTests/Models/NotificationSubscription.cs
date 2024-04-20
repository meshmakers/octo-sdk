using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

namespace Sdk.ServiceClient.SystemTests.Models;

/// <summary>
/// Represents the active payment option - so how the member has defined to pay for an article
/// </summary>
public class NotificationSubscription : RtEntityDto
{
    /// <summary>
    /// Endpoint to send the notification to
    /// </summary>
    public string Endpoint { get; set; } = default!;
    
    /// <summary>
    /// Content of the subscription
    /// </summary>
    public string SubscriptionConfiguration { get; set; } = default!;
}