using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts;
using Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects;
using Meshmakers.Octo.ConstructionKit.Contracts.DependencyGraph;
using Meshmakers.Octo.ConstructionKit.Contracts.Services;
using Meshmakers.Octo.Runtime.Contracts.RepositoryEntities;
using Meshmakers.Octo.Runtime.Contracts.Secrets;
using CkTypeAssociationDto = Meshmakers.Octo.ConstructionKit.Contracts.DataTransferObjects.CkTypeAssociationDto;

namespace Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
///     <see cref="RtEntityToDtoMapper" /> never maps an attribute with CK access <c>Hidden</c> - neither on the entity
///     nor inside records and record arrays - and keeps <c>MethodOnly</c> and <c>ReadOnly</c> attributes (they are
///     readable). Fixed dummy values stand in for a password hash, a security stamp and an authenticator token.
/// </summary>
public class RtEntityToDtoMapperAccessTests
{
    private const string TenantId = "test";
    private const string ModelId = "Test-1.0.0";
    private const string DummyHash = "DUMMY-HASH-VALUE-0001";
    private const string DummyStamp = "DUMMY-STAMP-VALUE-0002";
    private const string DummyToken = "DUMMY-TOKEN-VALUE-0003";
    private const string DummyNested = "DUMMY-NESTED-VALUE-0004";

    private static readonly RtCkId<CkTypeId> TypeId = new("Test/User");
    private static readonly RtCkId<CkRecordId> RecordId = new("Test/Login");

    private readonly RtEntityToDtoMapper _mapper;

    public RtEntityToDtoMapperAccessTests()
    {
        var cache = A.Fake<ICkCacheService>();
        var recordGraph = new CkRecordGraph(new CkId<CkRecordId>($"{ModelId}/Login"), false, false, [], null, [],
            [], Attributes(
                Attribute("Provider", AttributeValueTypesDto.String),
                Attribute("ProviderKey", AttributeValueTypesDto.String, access: CkAttributeAccessDto.Hidden)), "");
        var typeGraph = new CkTypeGraph(new CkId<CkTypeId>($"{ModelId}/User"), false, false, true, [], null, null, [],
            [], Attributes(
                Attribute("UserName", AttributeValueTypesDto.String),
                Attribute("PasswordHash", AttributeValueTypesDto.String, access: CkAttributeAccessDto.Hidden),
                Attribute("SecurityStamp", AttributeValueTypesDto.String, access: CkAttributeAccessDto.Hidden),
                Attribute("AuthenticatorToken", AttributeValueTypesDto.Secret, access: CkAttributeAccessDto.Hidden),
                Attribute("LockoutCount", AttributeValueTypesDto.Int, access: CkAttributeAccessDto.MethodOnly),
                Attribute("CreatedFrom", AttributeValueTypesDto.String, access: CkAttributeAccessDto.ReadOnly),
                Attribute("PrimaryLogin", AttributeValueTypesDto.Record, new CkId<CkRecordId>($"{ModelId}/Login")),
                Attribute("Logins", AttributeValueTypesDto.RecordArray, new CkId<CkRecordId>($"{ModelId}/Login")),
                Attribute("HiddenLogin", AttributeValueTypesDto.Record, new CkId<CkRecordId>($"{ModelId}/Login"),
                    CkAttributeAccessDto.Hidden)),
            [], new CkGraphDirectedAssociations(new List<CkTypeAssociationDto>()), "", false);

        A.CallTo(() => cache.GetRtCkType(A<string>._, A<RtCkId<CkTypeId>>._)).Returns(typeGraph);
        A.CallTo(() => cache.GetRtCkRecord(A<string>._, A<RtCkId<CkRecordId>>._)).Returns(recordGraph);
        _mapper = new RtEntityToDtoMapper(cache);
    }

    [Fact]
    public void HiddenTypeAttributes_AreNotMapped_ReadableOnesAre()
    {
        var entity = new RtEntity(TypeId, OctoObjectId.GenerateNewId(), new Dictionary<string, object?>
        {
            ["UserName"] = "alice",
            ["PasswordHash"] = DummyHash,
            ["SecurityStamp"] = DummyStamp,
            ["AuthenticatorToken"] = RtSecretValue.LegacyPlaintext(DummyToken),
            ["LockoutCount"] = 3,
            ["CreatedFrom"] = "import"
        });

        var dto = _mapper.ConvertToDto(TenantId, entity);

        var attributes = dto.Attributes!.ToDictionary(a => a.AttributeName);
        Assert.Equal(["createdFrom", "lockoutCount", "userName"], attributes.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("alice", attributes["userName"].Value);
        Assert.Equal(3, attributes["lockoutCount"].Value);
        Assert.Equal("import", attributes["createdFrom"].Value);
        AssertNoDummyValue(dto);
        // A Hidden Secret is not even described (no secretIsSet / secretSetAt).
        Assert.DoesNotContain("authenticatorToken", JsonSerializer.Serialize(dto), StringComparison.Ordinal);
    }

    [Fact]
    public void HiddenRecordMembers_AreNotMapped_InRecordsAndRecordArrays()
    {
        var entity = new RtEntity(TypeId, OctoObjectId.GenerateNewId(), new Dictionary<string, object?>
        {
            ["PrimaryLogin"] = Login("primary", DummyNested),
            ["Logins"] = new List<object> { Login("a", DummyHash), Login("b", DummyStamp) },
            ["HiddenLogin"] = Login("hidden", DummyToken)
        });

        var dto = _mapper.ConvertToDto(TenantId, entity);

        var attributes = dto.Attributes!.ToDictionary(a => a.AttributeName);
        Assert.False(attributes.ContainsKey("hiddenLogin"));

        var primary = Assert.IsType<RtRecordDto>(attributes["primaryLogin"].Value);
        var primaryMember = Assert.Single(primary.Attributes!);
        Assert.Equal("provider", primaryMember.AttributeName);
        Assert.Equal("primary", primaryMember.Value);

        var logins = Assert.IsAssignableFrom<IEnumerable<object?>>(attributes["logins"].Value)
            .Cast<RtRecordDto>().ToList();
        Assert.Equal(2, logins.Count);
        Assert.All(logins, l => Assert.Equal("provider", Assert.Single(l.Attributes!).AttributeName));

        AssertNoDummyValue(dto);
    }

    [Fact]
    public void EntityWithOnlyHiddenValues_MapsToNoAttributes()
    {
        var entity = new RtEntity(TypeId, OctoObjectId.GenerateNewId(), new Dictionary<string, object?>
        {
            ["PasswordHash"] = DummyHash,
            ["SecurityStamp"] = DummyStamp
        });

        var dto = _mapper.ConvertToDto(TenantId, entity);

        Assert.Empty(dto.Attributes!);
        AssertNoDummyValue(dto);
    }

    private static RtRecord Login(string provider, string providerKey) =>
        new(RecordId, new Dictionary<string, object?> { ["Provider"] = provider, ["ProviderKey"] = providerKey });

    private static void AssertNoDummyValue(RtEntityDto dto)
    {
        foreach (var json in new[] { JsonSerializer.Serialize(dto), Newtonsoft.Json.JsonConvert.SerializeObject(dto) })
        {
            foreach (var dummy in new[] { DummyHash, DummyStamp, DummyToken, DummyNested })
            {
                Assert.DoesNotContain(dummy, json, StringComparison.Ordinal);
            }

            Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("providerKey", json, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static CkTypeAttributeGraph Attribute(string name, AttributeValueTypesDto valueType,
        CkId<CkRecordId>? recordId = null, CkAttributeAccessDto access = CkAttributeAccessDto.ReadWrite)
    {
        return new CkTypeAttributeGraph(new CkId<CkAttributeId>($"{ModelId}/{name}"), name, null, valueType, recordId,
            null, null, null, null, true, null) { Access = access };
    }

    private static IReadOnlyDictionary<CkId<CkAttributeId>, CkTypeAttributeGraph> Attributes(
        params CkTypeAttributeGraph[] attributes)
    {
        return attributes.ToDictionary(a => a.CkAttributeId);
    }
}
