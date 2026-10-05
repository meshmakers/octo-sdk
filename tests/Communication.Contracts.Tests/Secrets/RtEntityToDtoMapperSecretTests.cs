using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts.DependencyGraph;
using Meshmakers.Octo.ConstructionKit.Contracts.Services;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;
using CkTypeAssociationDto = Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects.CkTypeAssociationDto;

namespace Communication.Contracts.Tests.Secrets;

/// <summary>
///     AB#5534 (WP4 of AB#5528): <see cref="RtEntityToDtoMapper" /> never maps the value of a
///     Secret attribute - neither envelope nor legacy clear text - only whether it is set.
/// </summary>
public class RtEntityToDtoMapperSecretTests
{
    private const string TenantId = "test";
    private const string ModelId = "Test-1.0.0";
    private static readonly RtCkId<CkTypeId> TypeId = new("Test/Account");
    private static readonly RtCkId<CkRecordId> RecordId = new("Test/Credential");

    private readonly RtEntityToDtoMapper _mapper;

    public RtEntityToDtoMapperSecretTests()
    {
        var cache = A.Fake<ICkCacheService>();
        var recordGraph = new CkRecordGraph(new CkId<CkRecordId>($"{ModelId}/Credential"), false, false, [], null, [],
            [], Attributes(
                Attribute("Key", AttributeValueTypesDto.String),
                Attribute("Value", AttributeValueTypesDto.Secret)), "");
        var typeGraph = new CkTypeGraph(new CkId<CkTypeId>($"{ModelId}/Account"), false, false, true, [], null, null, [],
            [], Attributes(
                Attribute("Name", AttributeValueTypesDto.String),
                Attribute("Password", AttributeValueTypesDto.Secret),
                Attribute("ApiKey", AttributeValueTypesDto.Secret),
                Attribute("Token", AttributeValueTypesDto.Secret),
                Attribute("UnsetSecret", AttributeValueTypesDto.Secret),
                Attribute("Primary", AttributeValueTypesDto.Record, new CkId<CkRecordId>($"{ModelId}/Credential")),
                Attribute("Credentials", AttributeValueTypesDto.RecordArray, new CkId<CkRecordId>($"{ModelId}/Credential")),
                // A String attribute holding an RtSecretValue: CK cache not (yet) aware of the type change.
                Attribute("StaleCache", AttributeValueTypesDto.String)),
            [], new CkGraphDirectedAssociations(new List<CkTypeAssociationDto>()), "", false);

        A.CallTo(() => cache.GetRtCkType(A<string>._, A<RtCkId<CkTypeId>>._)).Returns(typeGraph);
        A.CallTo(() => cache.GetRtCkRecord(A<string>._, A<RtCkId<CkRecordId>>._)).Returns(recordGraph);
        _mapper = new RtEntityToDtoMapper(cache);
    }

    [Fact]
    public void SecretAttributes_MapToNullValueAndIsSet_NeverTheEnvelopeOrPlaintext()
    {
        var protectedValue = SecretTestValues.NewProtected(out var envelope);
        var legacyV1 = SecretTestValues.NewLegacyV1Value();
        var entity = new RtEntity(TypeId, OctoObjectId.GenerateNewId(), new Dictionary<string, object?>
        {
            ["Name"] = "visible",
            ["Password"] = protectedValue,
            ["ApiKey"] = SecretTestValues.FakePlaintext, // legacy clear text in a Secret slot
            ["Token"] = RtSecretValue.LegacyPlaintext(legacyV1),
            ["UnsetSecret"] = null,
            ["StaleCache"] = SecretTestValues.NewProtected(out var staleEnvelope)
        });

        var dto = _mapper.ConvertToDto(TenantId, entity);

        var attributes = dto.Attributes!.ToDictionary(a => a.AttributeName);
        Assert.Equal("visible", attributes["name"].Value);
        Assert.Null(attributes["name"].SecretIsSet);

        AssertSecret(attributes["password"], true);
        AssertSecret(attributes["apiKey"], true);
        AssertSecret(attributes["token"], true);
        AssertSecret(attributes["unsetSecret"], false);
        AssertSecret(attributes["staleCache"], true);

        var json = JsonSerializer.Serialize(dto);
        SecretTestValues.AssertNoSecretContent(json, envelope, legacyV1, staleEnvelope);
        Assert.Contains("\"secretIsSet\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"secretIsSet\":false", json, StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyLegacyString_IsNotSet()
    {
        var entity = new RtEntity(TypeId, OctoObjectId.GenerateNewId(), new Dictionary<string, object?>
        {
            ["Password"] = string.Empty
        });

        var dto = _mapper.ConvertToDto(TenantId, entity);

        AssertSecret(Assert.Single(dto.Attributes!), false);
    }

    [Fact]
    public void SecretSubAttributesOfRecordsAndRecordArrays_MapToIsSetOnly()
    {
        var single = new RtRecord(RecordId, new Dictionary<string, object?>
        {
            ["Key"] = "primary",
            ["Value"] = SecretTestValues.NewProtected(out var singleEnvelope)
        });
        var first = new RtRecord(RecordId, new Dictionary<string, object?>
        {
            ["Key"] = "a",
            ["Value"] = SecretTestValues.FakePlaintext
        });
        var second = new RtRecord(RecordId, new Dictionary<string, object?>
        {
            ["Key"] = "b",
            ["Value"] = null
        });
        var entity = new RtEntity(TypeId, OctoObjectId.GenerateNewId(), new Dictionary<string, object?>
        {
            ["Primary"] = single,
            ["Credentials"] = new List<object> { first, second }
        });

        var dto = _mapper.ConvertToDto(TenantId, entity);

        var attributes = dto.Attributes!.ToDictionary(a => a.AttributeName);
        var primary = Assert.IsType<RtRecordDto>(attributes["primary"].Value);
        AssertSecret(primary.Attributes!.Single(a => a.AttributeName == "value"), true);
        Assert.Equal("primary", primary.Attributes!.Single(a => a.AttributeName == "key").Value);

        var credentials = Assert.IsAssignableFrom<IEnumerable<object?>>(attributes["credentials"].Value)
            .Cast<RtRecordDto>().ToList();
        Assert.Equal(2, credentials.Count);
        AssertSecret(credentials[0].Attributes!.Single(a => a.AttributeName == "value"), true);
        AssertSecret(credentials[1].Attributes!.Single(a => a.AttributeName == "value"), false);

        var json = JsonSerializer.Serialize(dto);
        SecretTestValues.AssertNoSecretContent(json, singleEnvelope);
        var newtonsoftJson = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
        SecretTestValues.AssertNoSecretContent(newtonsoftJson, singleEnvelope);
    }

    private static void AssertSecret(RtEntityAttributeDto attribute, bool expectedIsSet)
    {
        Assert.Null(attribute.Value);
        Assert.Equal(expectedIsSet, attribute.SecretIsSet);
    }

    private static CkTypeAttributeGraph Attribute(string name, AttributeValueTypesDto valueType,
        CkId<CkRecordId>? recordId = null)
    {
        return new CkTypeAttributeGraph(new CkId<CkAttributeId>($"{ModelId}/{name}"), name, null, valueType, recordId,
            null, null, null, null, true, null);
    }

    private static IReadOnlyDictionary<CkId<CkAttributeId>, CkTypeAttributeGraph> Attributes(
        params CkTypeAttributeGraph[] attributes)
    {
        return attributes.ToDictionary(a => a.CkAttributeId);
    }
}
