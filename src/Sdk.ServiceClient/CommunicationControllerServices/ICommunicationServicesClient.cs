namespace Meshmakers.Octo.Sdk.ServiceClient.CommunicationControllerServices;

/// <summary>
///     Implementation of the client proxy for communication services services.
/// </summary>
public interface ICommunicationServicesClient : IServiceClient
{
    /// <summary>
    ///     Enables the communication controller for a tenant
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    Task EnableAsync(string tenantId);

    /// <summary>
    ///     Disables the communication controller for a tenant
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    Task DisableAsync(string tenantId);
}