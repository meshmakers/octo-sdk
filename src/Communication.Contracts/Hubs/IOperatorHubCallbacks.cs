namespace Meshmakers.Octo.Communication.Contracts.Hubs;

/// <summary>
/// Callback interface for operator management connections.
/// The Communication Controller calls these methods on connected operators
/// to notify them of tenant lifecycle events.
/// </summary>
public interface IOperatorHubCallbacks
{
    /// <summary>
    /// Called when a new tenant is created and communication should be set up.
    /// The operator should create a CommunicationPool CR for this tenant.
    /// </summary>
    Task TenantCreatedAsync(string tenantId);

    /// <summary>
    /// Called before a tenant is deleted and communication should be torn down.
    /// The operator should delete the CommunicationPool CR for this tenant.
    /// </summary>
    Task TenantDeletedAsync(string tenantId);
}
