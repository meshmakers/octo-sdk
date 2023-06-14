using System.Collections.Generic;
using System.Threading.Tasks;
using Meshmakers.Octo.Common.Shared;
using Meshmakers.Octo.Common.Shared.DataTransferObjects;

namespace Meshmakers.Octo.Sdk.Client.AssetRepositoryServices.System;

public interface IAssetServicesClient : IServiceClient
{
    Task<JobDto> GetImportJobStatus(string id);
    Task<string> ImportCkModel(string tenantId, ScopeIdsDto scopeId, string ckModelFilePath);
    Task<string> ImportRtModel(string tenantId, string rtModelFilePath);
    Task<string> ExportRtModel(string tenantId, OctoObjectId queryId);
    Task CleanTenant(string tenantId);
    Task UpdateSystemCkModelOfTenant(string tenantId);
    Task ClearTenantCache(string tenantId);
    Task<IEnumerable<TenantDto>> GetTenants();
    Task CreateTenant(string tenantId, string databaseName);
    Task AttachTenant(string tenantId, string databaseName);
    Task DetachTenant(string tenantId);
    Task DeleteTenant(string tenantId);
}
