using Meshmakers.Common.Shared;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts.DependencyGraph;
using Meshmakers.Octo.ConstructionKit.Contracts.Services;
using Meshmakers.Octo.Runtime.Contracts;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;
using Meshmakers.Octo.Runtime.Contracts.Secrets;

namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// Converts RtEntity to RtEntityDto
/// </summary>
/// <remarks>
/// <para>
/// Secret attributes (AB#5528) are mapped to <see cref="RtEntityAttributeDto.Value" /> = <c>null</c>
/// plus their read state (<see cref="SecretValueStates.Describe(RtSecretValue?, Func{string?, bool}?)" />, AB#5534 round 2): this applies to
/// record sub-attributes as well.
/// </para>
/// <list type="bullet">
/// <item><see cref="RtEntityAttributeDto.SecretSetAt" />: when the value was set (null for legacy / not set).</item>
/// <item>Without key-ring knowledge (no protector, or a protector without configured keys):
/// <see cref="RtEntityAttributeDto.SecretIsSet" /> counts every protected value as set and
/// <see cref="RtEntityAttributeDto.SecretKeyMissing" /> is <c>null</c> (unknown).</item>
/// <item>With a configured <see cref="ISecretAttributeProtector" />: a protected value whose key id is not in
/// the key ring - or a legacy <c>enc:v1</c> string while <see cref="ISecretAttributeProtector.IsLegacyV1KeyConfigured" />
/// is <c>false</c> - maps to <c>SecretIsSet = false</c>, <c>SecretKeyMissing = true</c>; otherwise
/// <c>SecretKeyMissing = false</c>.</item>
/// </list>
/// </remarks>
/// <param name="ckCacheService">Construction Kit Cache Service</param>
/// <param name="secretAttributeProtector">
/// Optional key ring of the host (resolved from DI when registered); only used when
/// <see cref="ISecretAttributeProtector.IsConfigured" /> - it never decrypts.
/// </param>
public class RtEntityToDtoMapper(
    ICkCacheService ckCacheService,
    ISecretAttributeProtector? secretAttributeProtector = null) : IRtEntityToDtoMapper
{
    // Single constructor on purpose (DI picks it; the protector is optional).
    private readonly Func<string?, bool>? _isKnownKeyId =
        secretAttributeProtector is { IsConfigured: true } ? secretAttributeProtector.IsKnownKeyId : null;

    // AB#5532: with key-ring knowledge an enc:v1 string on a host without legacy key is key missing, like in
    // GraphQL (ISecretAttributeProtector.DescribeSecret) and the secrets overview.
    private readonly bool _legacyV1KeyConfigured =
        secretAttributeProtector is not { IsConfigured: true } || secretAttributeProtector.IsLegacyV1KeyConfigured;

    /// <inheritdoc />
    public RtEntityDto ConvertToDto(string tenantId, RtEntity rtEntity,
        AttributeValueResolveFlags attributeValueResolveFlags = AttributeValueResolveFlags.Default)
    {
        var ckTypeGraph =
            ckCacheService.GetRtCkType(tenantId, rtEntity.CkTypeId ?? throw MapperException.CkTypeIdNotSet());

        var entityDto = new RtEntityDto
        {
            RtId = rtEntity.RtId,
            RtState = rtEntity.RtState,
            RtChangedDateTime = rtEntity.RtChangedDateTime,
            RtCreationDateTime = rtEntity.RtCreationDateTime,
            RtArchivedDateTime = rtEntity.RtArchivedDateTime,
            RtWellKnownName = rtEntity.RtWellKnownName,
            CkTypeId = rtEntity.CkTypeId ?? throw MapperException.CkTypeIdNotSet()
        };

        ConvertAttributes(tenantId, ckTypeGraph, rtEntity, entityDto, attributeValueResolveFlags);

        return entityDto;
    }

    private void ConvertAttributes(string tenantId, CkTypeWithAttributesGraph ckTypeWithAttributesGraph,
        RtTypeWithAttributes rtTypeWithAttributes, RtTypeWithAttributesDto rtTypeWithAttributesDto,
        AttributeValueResolveFlags attributeValueResolveFlags)
    {
        rtTypeWithAttributesDto.Attributes ??= new List<RtEntityAttributeDto>();

        foreach (var ckTypeAttributeGraph in ckTypeWithAttributesGraph.AllAttributesByName.Values)
        {
            if (!rtTypeWithAttributes.Attributes.TryGetValue(ckTypeAttributeGraph.AttributeName, out var value))
            {
                continue;
            }

            // AB#5528: a Secret attribute never maps its value - neither ciphertext nor legacy clear
            // text - only whether it is set (concept §4.2). The RtSecretValue check also covers a
            // value whose CK attribute is not (yet) known as Secret in the cache.
            if (ckTypeAttributeGraph.ValueType == AttributeValueTypesDto.Secret || value is RtSecretValue)
            {
                var secretState = OctoSecretStateDto.Describe(value, _isKnownKeyId, _legacyV1KeyConfigured);
                rtTypeWithAttributesDto.Attributes.Add(new RtEntityAttributeDto
                {
                    AttributeName = ckTypeAttributeGraph.AttributeName.ToCamelCase(),
                    Value = null,
                    SecretIsSet = secretState.IsSet,
                    // Unknown (null) without key-ring knowledge - never a misleading false.
                    SecretKeyMissing = _isKnownKeyId == null ? null : secretState.KeyMissing,
                    SecretSetAt = secretState.SetAt
                });
                continue;
            }

            if (value is RtRecord rtRecord)
            {
                value = ConvertToRtRecordDto(tenantId, rtRecord, attributeValueResolveFlags);
            }
            else if (value is IEnumerable<object> rtRecords)
            {
                value = rtRecords.Select(listValue =>
                {
                    if (listValue is RtRecord rtRecord2)
                    {
                        return ConvertToRtRecordDto(tenantId, rtRecord2, attributeValueResolveFlags);
                    }

                    // Defensive: a secret never leaves as a list element either.
                    return listValue is RtSecretValue ? null : listValue;
                });
            }
            else if (attributeValueResolveFlags.HasFlag(AttributeValueResolveFlags.ResolveEnumsToNames) &&
                     ckTypeAttributeGraph is { ValueType: AttributeValueTypesDto.Enum, ValueCkEnumId: not null } &&
                     value is int key)
            {
                var enumGraph = ckCacheService.GetCkEnum(tenantId, ckTypeAttributeGraph.ValueCkEnumId);
                var ckEnumValueDto = enumGraph.Values.FirstOrDefault(o => o.Key == key);
                if (ckEnumValueDto != null)
                {
                    value = ckEnumValueDto.Name;
                }
            }

            var rtEntityAttributeDto = new RtEntityAttributeDto
            {
                AttributeName = ckTypeAttributeGraph.AttributeName.ToCamelCase(),
                Value = value
            };
            rtTypeWithAttributesDto.Attributes.Add(rtEntityAttributeDto);
        }
    }

    private RtRecordDto ConvertToRtRecordDto(string tenantId, RtRecord rtRecord,
        AttributeValueResolveFlags attributeValueResolveFlags)
    {
        var rtRecordDto = new RtRecordDto
        {
            CkRecordId = rtRecord.CkRecordId
        };

        var ckRecordGraph = ckCacheService.GetRtCkRecord(tenantId, rtRecord.CkRecordId);
        ConvertAttributes(tenantId, ckRecordGraph, rtRecord, rtRecordDto, attributeValueResolveFlags);
        return rtRecordDto;
    }
}