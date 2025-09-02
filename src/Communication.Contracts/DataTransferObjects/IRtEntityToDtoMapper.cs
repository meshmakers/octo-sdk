using Meshmakers.Octo.Runtime.Contracts;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Interface for mapping RtEntity to RtEntityDto
/// </summary>
public interface IRtEntityToDtoMapper
{
    /// <summary>
    /// Converts a RtEntity to RtEntityDto
    /// </summary>
    /// <param name="tenantId">Tenant Id</param>
    /// <param name="rtEntity">The RtEntity to convert</param>
    /// <param name="attributeValueResolveFlags">Defines how attribute values are resolved</param>
    /// <returns></returns>
    /// <exception cref="MapperException">Thrown if the CkTypeId is undefined</exception>
    RtEntityDto ConvertToDto(string tenantId, RtEntity rtEntity,
        AttributeValueResolveFlags attributeValueResolveFlags = AttributeValueResolveFlags.Default);
}