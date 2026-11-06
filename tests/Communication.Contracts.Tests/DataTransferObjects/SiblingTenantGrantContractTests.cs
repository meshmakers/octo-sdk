using System.Text.Json;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects;
using Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ApiErrors;

namespace Communication.Contracts.Tests.DataTransferObjects;

/// <summary>
///     AB#8340 — wire contract of the sibling-tenant user lookup and grants (docs/CONCEPT-SIBLING-TENANT-GRANTS.md in
///     octo-identity-services). <c>groupIds</c> is optional: requests of existing callers keep their wire shape.
/// </summary>
public class SiblingTenantGrantContractTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CreateMapping_WithoutGroupIds_KeepsTheExistingWireShape()
    {
        var dto = new CreateExternalTenantUserMappingDto
        {
            SourceTenantId = "meshmakers", SourceUserId = "686a6215fd3e5972ff5693fd", SourceUserName = "alice",
            RoleIds = ["r1"]
        };

        var json = JsonSerializer.Serialize(dto, WebOptions);

        Assert.Equal(
            """{"sourceTenantId":"meshmakers","sourceUserId":"686a6215fd3e5972ff5693fd","sourceUserName":"alice","roleIds":["r1"]}""",
            json);
    }

    [Fact]
    public void CreateMapping_WithGroupIds_SendsThem()
    {
        var dto = new CreateExternalTenantUserMappingDto
        {
            SourceTenantId = "meshmakers", SourceUserId = "u1", SourceUserName = "alice", GroupIds = ["g1", "g2"]
        };

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(dto, WebOptions));

        var groupIds = document.RootElement.GetProperty("groupIds").EnumerateArray().Select(e => e.GetString());
        Assert.Equal(["g1", "g2"], groupIds);
    }

    [Fact]
    public void CreateMapping_FromAnOldClientBody_HasNoGroupIds()
    {
        const string body = """{ "sourceTenantId": "meshmakers", "sourceUserId": "u1", "sourceUserName": "alice" }""";

        var dto = JsonSerializer.Deserialize<CreateExternalTenantUserMappingDto>(body, WebOptions);

        Assert.NotNull(dto);
        Assert.Null(dto.GroupIds);
        Assert.Null(dto.RoleIds);
    }

    [Fact]
    public void LookupRequest_SerialisesSourceTenantIdAndIdentifierOnly()
    {
        var json = JsonSerializer.Serialize(
            new SourceUserLookupRequestDto { SourceTenantId = "meshmakers", Identifier = "alice@example.io" }, WebOptions);

        Assert.Equal("""{"sourceTenantId":"meshmakers","identifier":"alice@example.io"}""", json);
    }

    [Fact]
    public void LookupRequest_EmptyFields_PassModelValidation_SoTheServiceAnswersTheUniform404()
    {
        var request = new SourceUserLookupRequestDto();
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        var valid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request), results, true);

        Assert.True(valid);
        Assert.Empty(results);
    }

    [Fact]
    public void LookupResult_DeserialisesTheServiceAnswer_AndCarriesNoEmail()
    {
        const string response =
            """{ "sourceTenantId": "meshmakers", "userId": "u1", "userName": "alice", "displayName": "Alice A." }""";

        var dto = JsonSerializer.Deserialize<SourceUserLookupResultDto>(response, WebOptions);

        Assert.NotNull(dto);
        Assert.Equal("meshmakers", dto.SourceTenantId);
        Assert.Equal("u1", dto.UserId);
        Assert.Equal("alice", dto.UserName);
        Assert.Equal("Alice A.", dto.DisplayName);
        Assert.DoesNotContain(typeof(SourceUserLookupResultDto).GetProperties(),
            p => p.Name.Contains("Mail", StringComparison.OrdinalIgnoreCase) ||
                 p.Name.Contains("Phone", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProblemCodes_AreStable()
    {
        Assert.Equal("SourceUserNotFound", CrossTenantGrantProblemCodes.SourceUserNotFound);
        Assert.Equal("SourceUserLookupRateLimited", CrossTenantGrantProblemCodes.SourceUserLookupRateLimited);
        Assert.Equal("SourceTenantNotPermitted", CrossTenantGrantProblemCodes.SourceTenantNotPermitted);
        Assert.Equal("UnknownGroup", CrossTenantGrantProblemCodes.UnknownGroup);
    }
}
