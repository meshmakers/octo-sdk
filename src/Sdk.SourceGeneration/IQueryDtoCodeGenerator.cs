using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts.Services;

namespace Meshmakers.Octo.Sdk.SourceGeneration;

/// <summary>
///     Interface for generates code for query DTO's
/// </summary>
public interface IQueryDtoCodeGenerator : IEquatable<IQueryDtoCodeGenerator>
{
   /// <summary>
   /// Generates code for a type for GraphQL queries
   /// </summary>
   /// <param name="ns">The namespace of the created code</param>
   /// <param name="ckType">Construction kit type instance</param>
   /// <param name="cacheTenantId">Tenant id to access the correct cache instance</param>
   /// <param name="cacheService">The cache service</param>
   /// <returns></returns>
   string GenerateType(string ns, CkTypeDto ckType, string cacheTenantId, ICkCacheService cacheService);

   /// <summary>
   /// Generates code for a record for GraphQL queries
   /// </summary>
   /// <param name="ns">The namespace of the created code</param>
   /// <param name="ckRecord">CkRecordDto instance</param>
   /// <param name="cacheTenantId">Tenant id to access the correct cache instance</param>
   /// <param name="cacheService">The cache service</param>
   /// <returns></returns>
   string GenerateRecord(string ns, CkRecordDto ckRecord, string cacheTenantId, ICkCacheService cacheService);
}