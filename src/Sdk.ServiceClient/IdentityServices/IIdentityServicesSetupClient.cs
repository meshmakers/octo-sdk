using Meshmakers.Octo.Common.Shared.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

/// <summary>
/// Interface for the identity services setup.
/// </summary>
public interface IIdentityServicesSetupClient : IServiceClient
{
    /// <summary>
    /// Adds an admin user to a newly created octo instance installation.
    /// </summary>
    /// <param name="adminUserDto">The data transfer object</param>
    /// <returns></returns>
    Task AddAdminUser(AdminUserDto adminUserDto);
}
