namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.StreamData;

/// <summary>
///     Interface for the StreamData services.
/// </summary>
public interface IStreamDataServicesClient : IServiceClient
{
    /// <summary>
    ///     Enables the StreamData for a tenant
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    Task EnableAsync(string tenantId);

    /// <summary>
    ///     Disables StreamData  for a tenant
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    Task DisableAsync(string tenantId);
}