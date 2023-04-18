using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared.DataTransferObjects;

namespace Meshmakers.Octo.Frontend.Client.System;

public interface IIdentityServicesSetupClient : IServiceClient
{
    Task AddAdminUser(AdminUserDto adminUserDto);
}
