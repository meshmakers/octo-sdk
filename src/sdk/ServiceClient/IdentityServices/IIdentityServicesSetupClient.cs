using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.Client.IdentityServices;

public interface IIdentityServicesSetupClient : IServiceClient
{
    Task AddAdminUser(AdminUserDto adminUserDto);
}
