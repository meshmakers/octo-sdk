using System.Reflection;
using System.Text.RegularExpressions;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

namespace Sdk.SourceGeneration.Tests;

/// <summary>
///     AB#9044 (identity wave 2, Feature F5.2): the method clients a consumer generates from <c>System.Identity</c> 3.8.0
///     for groups, clients (roles, overlay URIs, secrets, mirrors, create/update/delete), roles, API scopes, API
///     resources, data permissions and policies, external tenant user mappings and admin provisioning, e-mail domain
///     group rules and identity providers - the replacements of the obsolete REST methods of
///     <c>IIdentityServicesClient</c>. The SDK ships no System.Identity surface (AB#8416, option C); these tests prove
///     what a consumer gets: one client method per CK method, the signatures of the replacements, declared error
///     constants equal to the model's errors, and answers recorded from the asset repository's method gateway (tenant
///     with System.Identity 3.8.0, scripted handler; steps in docs/method-clients.md) as typed results. The fixture is
///     System.Identity 3.9.0 since AB#9926, which adds <c>Client.SetMirrorRoles-1</c> (answers recorded on 3.9.0). The documents of
///     every generated client are validated against the recorded schema by
///     <see cref="UserMethodClientTests.IdentityDocument_ValidatesAgainstTheRecordedSchemaOfATenantWithSystemIdentity" />.
/// </summary>
public class IdentityWave2MethodClientTests : IClassFixture<MethodClientRuntimeTests.LoopbackGraphQlService>
{
    private const string Ns = "Consumer.DataTransferObjects.System.Identity.v3";
    private const string TargetRtId = "65f1c0ffee0000000000abcd";
    private const string Result = "global::Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods.CkMethodResult";
    private const string List = "global::System.Collections.Generic.IReadOnlyList";

    private static readonly string[] IdentityProviderTypes =
    [
        "AzureEntraIdIdentityProvider", "FacebookIdentityProvider", "GoogleIdentityProvider", "MicrosoftAdIdentityProvider",
        "MicrosoftIdentityProvider", "OctoTenantIdentityProvider", "OpenLdapIdentityProvider"
    ];

    private static readonly Lazy<SortedDictionary<string, string>> Sources =
        new(() => GeneratorFixtures.Emit(GeneratorFixtures.SystemIdentity, "Consumer"));

    private static readonly Lazy<Assembly> Generated = new(() => GeneratorFixtures.CompileAndLoad(Sources.Value.Values));

    private readonly MethodClientRuntimeTests.LoopbackGraphQlService _service;

    public IdentityWave2MethodClientTests(MethodClientRuntimeTests.LoopbackGraphQlService service)
    {
        _service = service;
        _service.Reset();
    }

    public static TheoryData<string, string> ClientMethods() => new()
    {
        { "Group", "AddChildGroup,AddMember,CreateGroup,DeleteGroup,RemoveChildGroup,RemoveMember,SetRoles,UpdateGroup" },
        {
            "Client",
            "AddRole,AddSecret,ApplyOverlayUris,CleanOverlayEntries,CreateClient,DeleteClient,ProvisionMirrorInTenant,ProvisionMirrors,"
            + "RemoveMirror,RemoveRole,RemoveSecret,RotateMirrorSecret,SetMirrorAutoProvision,SetMirrorRoles,SetRoles,UpdateClient,UpdateSecret"
        },
        { "Role", "CreateRole,DeleteRole,UpdateRole" },
        { "ApiScope", "CreateApiScope,DeleteApiScope,UpdateApiScope" },
        { "ApiResource", "AddSecret,CreateApiResource,DeleteApiResource,RemoveSecret,UpdateApiResource,UpdateSecret" },
        { "DataPermission", "CreateDataPermission,CreatePolicy,DeleteDataPermission,GrantToRole,RevokeFromRole" },
        { "DataPolicy", "DeleteDataPolicy,SetEnforcementMode" },
        {
            "ExternalTenantUserMapping",
            "CreateMapping,DeleteMapping,ProvisionAdmin,ProvisionAdminWithGroups,ProvisionCurrentUser,RemoveAdminProvisioning,UpdateMapping"
        },
        { "EmailDomainGroupRule", "CreateEmailDomainGroupRule,DeleteEmailDomainGroupRule,UpdateEmailDomainGroupRule" },
        { "AzureEntraIdIdentityProvider", "CreateIdentityProvider,DeleteIdentityProvider,UpdateIdentityProvider" },
        { "FacebookIdentityProvider", "CreateIdentityProvider,DeleteIdentityProvider,UpdateIdentityProvider" },
        { "GoogleIdentityProvider", "CreateIdentityProvider,DeleteIdentityProvider,UpdateIdentityProvider" },
        { "MicrosoftAdIdentityProvider", "CreateIdentityProvider,DeleteIdentityProvider,UpdateIdentityProvider" },
        { "MicrosoftIdentityProvider", "CreateIdentityProvider,DeleteIdentityProvider,UpdateIdentityProvider" },
        { "OctoTenantIdentityProvider", "CreateIdentityProvider,DeleteIdentityProvider,UpdateIdentityProvider" },
        { "OpenLdapIdentityProvider", "CreateIdentityProvider,DeleteIdentityProvider,UpdateIdentityProvider" }
    };

    [Theory]
    [MemberData(nameof(ClientMethods))]
    public void Client_HasOneMethodPerCkMethod(string type, string methods)
    {
        var code = Sources.Value[$"{Ns}.Type.{type}MethodClient.g.cs"];

        Assert.Contains($"public partial class Rt{type}MethodClient : global::Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods.CkMethodClientBase", code);
        Assert.Equal(methods.Split(','), ClientMethodNames(code));
        Assert.DoesNotContain("Unsupported by Generator", code);
    }

    [Fact]
    public void AbstractIdentityProvider_HasNoClient_TheConcreteProviderTypesHaveOne()
    {
        // The methods are declared on the abstract base and inherited by the concrete provider types (CreateIdentityProvider-1
        // is static, update and delete take the rtId); an abstract type gets no client, every concrete provider type one.
        Assert.DoesNotContain($"{Ns}.Type.IdentityProviderMethodClient.g.cs", Sources.Value.Keys);
        Assert.All(IdentityProviderTypes, t => Assert.Contains($"{Ns}.Type.{t}MethodClient.g.cs", Sources.Value.Keys));
    }

    [Theory]
    // Replacements of the obsolete REST methods: entities by rtId, create methods static and returning the new rtId.
    [InlineData("Group", $"Task<{Result}<string?>> CreateGroupAsync(string groupName, string? groupDescription = null, {List}<string>? roleNames = null, string? idempotencyKey = null)")]
    [InlineData("Group", $"Task<{Result}> UpdateGroupAsync(string rtId, string groupName, string? groupDescription = null, string? idempotencyKey = null)")]
    [InlineData("Group", $"Task<{Result}> DeleteGroupAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("Group", $"Task<{Result}> SetRolesAsync(string rtId, {List}<string> roleNames, string? idempotencyKey = null)")]
    [InlineData("Group", $"Task<{Result}> AddMemberAsync(string rtId, global::{Ns}.RtGroupMemberKindEnum memberKind, string memberId, string? idempotencyKey = null)")]
    [InlineData("Group", $"Task<{Result}> RemoveMemberAsync(string rtId, global::{Ns}.RtGroupMemberKindEnum memberKind, string memberId, string? idempotencyKey = null)")]
    [InlineData("Group", $"Task<{Result}> AddChildGroupAsync(string rtId, string childGroupId, string? idempotencyKey = null)")]
    [InlineData("Group", $"Task<{Result}> RemoveChildGroupAsync(string rtId, string childGroupId, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}> SetRolesAsync(string rtId, {List}<string> roleNames, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}> AddRoleAsync(string rtId, string roleName, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}> RemoveRoleAsync(string rtId, string roleName, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}<global::{Ns}.RtClientOverlayUrisResultRecordDto?>> ApplyOverlayUrisAsync(string rtId, string overlayName, {List}<string>? redirectUris = null, {List}<string>? postLogoutRedirectUris = null, {List}<string>? allowedCorsOrigins = null, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}<global::{Ns}.RtClientOverlayCleanResultRecordDto?>> CleanOverlayEntriesAsync(string? overlayName = null, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}<string?>> AddSecretAsync(string rtId, string? description = null, global::System.DateTime? expirationDate = null, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}> UpdateSecretAsync(string rtId, string secretValue, string? description = null, global::System.DateTime? expirationDate = null, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}> RemoveSecretAsync(string rtId, string secretValue, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}<global::{Ns}.RtClientMirrorBackfillResultRecordDto?>> ProvisionMirrorsAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}<global::{Ns}.RtClientMirrorProvisionResultRecordDto?>> ProvisionMirrorInTenantAsync(string rtId, string childTenantId, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}> RemoveMirrorAsync(string rtId, string childTenantId, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}> SetMirrorAutoProvisionAsync(string rtId, bool enabled, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}<string?>> RotateMirrorSecretAsync(string rtId, string childTenantId, string? idempotencyKey = null)")]
    // System.Identity 3.9.0: the roles a client hands to its mirrors; dryRun optional (omitted = false), a record per child tenant.
    [InlineData("Client", $"Task<{Result}<global::{Ns}.RtClientMirrorRolesResultRecordDto?>> SetMirrorRolesAsync(string rtId, {List}<string> roleNames, bool? dryRun = null, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}<string?>> CreateClientAsync(string clientId, bool? enabled = null, string? clientName = null, string? clientUri = null, {List}<string>? allowedGrantTypes = null, {List}<string>? redirectUris = null, {List}<string>? postLogoutRedirectUris = null, {List}<string>? allowedCorsOrigins = null, {List}<string>? allowedScopes = null, bool? allowOfflineAccess = null, bool? requireClientSecret = null, string? clientSecret = null, string? frontChannelLogoutUri = null, bool? frontChannelLogoutSessionRequired = null, string? backChannelLogoutUri = null, bool? backChannelLogoutSessionRequired = null, bool? autoProvisionInChildTenants = null, string? idempotencyKey = null)")]
    [InlineData("Client", $"Task<{Result}> DeleteClientAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("Role", $"Task<{Result}<string?>> CreateRoleAsync(string name, string? idempotencyKey = null)")]
    [InlineData("Role", $"Task<{Result}> UpdateRoleAsync(string rtId, string name, string? idempotencyKey = null)")]
    [InlineData("Role", $"Task<{Result}> DeleteRoleAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("ApiScope", $"Task<{Result}> DeleteApiScopeAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("ApiResource", $"Task<{Result}<string?>> AddSecretAsync(string rtId, string? description = null, global::System.DateTime? expirationDate = null, string? idempotencyKey = null)")]
    [InlineData("ApiResource", $"Task<{Result}> DeleteApiResourceAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("DataPermission", $"Task<{Result}<string?>> CreateDataPermissionAsync(string permissionId, string? description = null, string? idempotencyKey = null)")]
    [InlineData("DataPermission", $"Task<{Result}<string?>> CreatePolicyAsync(string rtId, {List}<string> targetCkTypeIds, global::{Ns}.RtDataPolicyScopeEnum scope, global::{Ns}.RtDataPolicyEnforcementModeEnum enforcementMode, {List}<string>? actions = null, string? idempotencyKey = null)")]
    [InlineData("DataPermission", $"Task<{Result}> GrantToRoleAsync(string rtId, string roleName, string? idempotencyKey = null)")]
    [InlineData("DataPermission", $"Task<{Result}> RevokeFromRoleAsync(string rtId, string roleName, string? idempotencyKey = null)")]
    [InlineData("DataPolicy", $"Task<{Result}> SetEnforcementModeAsync(string rtId, global::{Ns}.RtDataPolicyEnforcementModeEnum enforcementMode, string? idempotencyKey = null)")]
    [InlineData("DataPolicy", $"Task<{Result}> DeleteDataPolicyAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("ExternalTenantUserMapping", $"Task<{Result}<global::{Ns}.RtAdminProvisioningResultRecordDto?>> CreateMappingAsync(string sourceTenantId, string sourceUserId, string sourceUserName, {List}<string>? roleIds = null, {List}<string>? groupIds = null, string? idempotencyKey = null)")]
    [InlineData("ExternalTenantUserMapping", $"Task<{Result}> UpdateMappingAsync(string rtId, {List}<string>? roleIds = null, string? idempotencyKey = null)")]
    [InlineData("ExternalTenantUserMapping", $"Task<{Result}> DeleteMappingAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("ExternalTenantUserMapping", $"Task<{Result}<global::{Ns}.RtAdminProvisioningResultRecordDto?>> ProvisionAdminAsync(string targetTenantId, string sourceTenantId, string sourceUserId, string sourceUserName, {List}<string>? roleIds = null, string? idempotencyKey = null)")]
    [InlineData("ExternalTenantUserMapping", $"Task<{Result}<global::{Ns}.RtAdminProvisioningResultRecordDto?>> ProvisionCurrentUserAsync(string targetTenantId, string? idempotencyKey = null)")]
    [InlineData("ExternalTenantUserMapping", $"Task<{Result}> RemoveAdminProvisioningAsync(string targetTenantId, string mappingRtId, string? idempotencyKey = null)")]
    [InlineData("EmailDomainGroupRule", $"Task<{Result}<string?>> CreateEmailDomainGroupRuleAsync(string emailDomainPattern, string targetGroupRtId, string? description = null, string? idempotencyKey = null)")]
    [InlineData("EmailDomainGroupRule", $"Task<{Result}> UpdateEmailDomainGroupRuleAsync(string rtId, string emailDomainPattern, string targetGroupRtId, string? description = null, string? idempotencyKey = null)")]
    [InlineData("EmailDomainGroupRule", $"Task<{Result}> DeleteEmailDomainGroupRuleAsync(string rtId, string? idempotencyKey = null)")]
    [InlineData("GoogleIdentityProvider", $"Task<{Result}> DeleteIdentityProviderAsync(string rtId, string? idempotencyKey = null)")]
    public void Client_Signature_OfAReplacement(string type, string signature)
    {
        Assert.Contains(signature, Sources.Value[$"{Ns}.Type.{type}MethodClient.g.cs"]);
    }

    [Fact]
    public void IdentityProviderClients_ShareTheSignaturesOfTheBase()
    {
        // The parameters of all provider kinds are optional on the base method; the handler checks which ones apply.
        const string create = $"Task<{Result}<string?>> CreateIdentityProviderAsync(string name, bool? enabled = null, string? description = null, bool? allowSelfRegistration = null, string? defaultGroupRtId = null, string? clientId = null, string? clientSecret = null, string? tenantId = null, string? authority = null, string? host = null, int? port = null, bool? useTls = null, string? userBaseDn = null, string? userNameAttribute = null, string? parentTenantId = null, string? idempotencyKey = null)";
        const string update = $"Task<{Result}> UpdateIdentityProviderAsync(string rtId, string name, bool? enabled = null, string? description = null, bool? allowSelfRegistration = null, string? defaultGroupRtId = null, string? clientId = null, string? clientSecret = null, string? tenantId = null, string? authority = null, string? host = null, int? port = null, bool? useTls = null, string? userBaseDn = null, string? userNameAttribute = null, string? parentTenantId = null, string? idempotencyKey = null)";

        Assert.All(IdentityProviderTypes, t =>
        {
            var code = Sources.Value[$"{Ns}.Type.{t}MethodClient.g.cs"];
            Assert.Contains(create, code);
            Assert.Contains(update, code);
        });
    }

    public static TheoryData<string, string, string> DeclaredErrorsOfTheModel()
    {
        var (model, _, _) = GeneratorFixtures.Restore(GeneratorFixtures.SystemIdentity);
        var data = new TheoryData<string, string, string>();
        foreach (var type in new[]
                 {
                     "Group", "Client", "Role", "ApiScope", "ApiResource", "DataPermission", "DataPolicy",
                     "ExternalTenantUserMapping", "EmailDomainGroupRule", "IdentityProvider"
                 })
        {
            var ckType = model.Types!.Single(t => t.TypeId.Name == type);
            foreach (var method in ckType.Methods!.Where(m => m.Errors is { Count: > 0 }))
            {
                // The identity provider methods are declared on the abstract base; check them on one concrete client.
                var client = type == "IdentityProvider" ? "GoogleIdentityProvider" : type;
                data.Add(client, method.MethodId[..method.MethodId.LastIndexOf('-')],
                    string.Join(',', method.Errors!.Select(e => e.Code)));
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(DeclaredErrorsOfTheModel))]
    public void DeclaredErrors_AreConstants_InTheOrderOfTheModel(string client, string method, string codes)
    {
        var errors = Generated.Value.GetType($"{Ns}.Rt{client}MethodClient+{method}Errors");

        Assert.NotNull(errors);
        Assert.Equal(codes.Split(','), errors.GetFields().Select(f => (string)f.GetValue(null)!));
    }

    [Fact]
    public void DeclaredErrors_CoverTheWave2Codes()
    {
        // Guards the theory above against an empty model read: the known codes of the wave are there.
        var codes = DeclaredErrorsOfTheModel().SelectMany(row => row.Data.Item3.Split(',')).ToHashSet();

        Assert.Superset(new HashSet<string>
        {
            "GROUP_CYCLE", "MEMBER_NOT_FOUND", "CHILD_GROUP_NOT_FOUND", "ROLE_NOT_FOUND", "SECRET_NOT_FOUND", "MIRROR_NOT_FOUND",
            "AUTO_PROVISION_DISABLED", "CLIENT_ALREADY_EXISTS", "ROLE_ALREADY_EXISTS", "API_SCOPE_ALREADY_EXISTS",
            "API_RESOURCE_ALREADY_EXISTS", "DATA_PERMISSION_ALREADY_EXISTS", "PERMISSION_ALREADY_GRANTED", "UNKNOWN_GROUP",
            "TENANT_NOT_FOUND", "EMAIL_DOMAIN_GROUP_RULE_ALREADY_EXISTS", "IDENTITY_PROVIDER_ALREADY_EXISTS",
            "SECRET_ENCRYPTION_NOT_CONFIGURED", "MIRROR_ROLE_NOT_ALLOWED", "CLIENT_NOT_CONFIDENTIAL", "MIRROR_ROLE_NOT_FOUND",
            "CONCURRENT_CHANGE"
        }, codes);
    }

    [Fact]
    public void GeneratedSources_CompileWithoutWarnings()
    {
        Assert.All(ClientMethods(), row => Assert.NotNull(Generated.Value.GetType($"{Ns}.Rt{row.Data.Item1}MethodClient")));
    }

    // Answers recorded from the asset repository: the generated documents executed with the client's variables on the
    // real schema and method gateway of a tenant with System.Identity 3.8.0 (scripted handler, docs/method-clients.md).

    [Fact]
    public async Task RecordedAnswer_CreateGroup_ReturnsTheNewRtId()
    {
        _service.Respond(200, Recorded("group-create-success"));

        var result = (CkMethodResult<string?>)await CallAsync("Group", "CreateGroupAsync", "operators", "Plant operators",
            new List<string> { "Reader" }, "key-9044");

        Assert.True(result.IsSuccess);
        Assert.Equal("65f1c0ffee0000000000cafe", result.Value);
        var body = Assert.Single(_service.Bodies);
        Assert.Contains("createGroup(input: $input, idempotencyKey: $idempotencyKey)", body, StringComparison.Ordinal);
        Assert.Contains("\"input\":{\"groupName\":\"operators\",\"groupDescription\":\"Plant operators\",\"roleNames\":[\"Reader\"]}", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_AddChildGroup_CycleIsATypedResult()
    {
        _service.Respond(200, Recorded("group-add-child-group-cycle"));

        var result = (CkMethodResult)await CallAsync("Group", "AddChildGroupAsync", TargetRtId, "65f1c0ffee0000000000c0de", null);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsError(ErrorConstant("Group", "AddChildGroup", "GroupCycle")));
        Assert.Equal("descendant", result.Error!.Parameters["cycle"]);
        Assert.Null(result.InvocationId);
    }

    [Fact]
    public async Task RecordedAnswer_AddMember_SendsTheMemberKindAsEnum()
    {
        _service.Respond(200, Recorded("group-add-member-success"));
        var client = EnumValue("RtGroupMemberKindEnum", "Client");

        var result = (CkMethodResult)await CallAsync("Group", "AddMemberAsync", TargetRtId, client, "octo-cli", null);

        Assert.True(result.IsSuccess);
        Assert.Contains("\"input\":{\"memberKind\":\"CLIENT\",\"memberId\":\"octo-cli\"}", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_ClientSetRoles_UnknownRoleIsATypedResult()
    {
        _service.Respond(200, Recorded("client-set-roles-role-not-found"));

        var result = (CkMethodResult)await CallAsync("Client", "SetRolesAsync", TargetRtId, new List<string> { "Reader", "Auditor" }, null);

        Assert.True(result.IsError(ErrorConstant("Client", "SetRoles", "RoleNotFound")));
        Assert.Equal("Auditor", result.Error!.Parameters["roleName"]);
    }

    [Fact]
    public async Task RecordedAnswer_ClientAddSecret_ReturnsTheGeneratedSecretOnce()
    {
        // The generated secret is the (sensitive) result; the value here is a dummy of the scripted handler.
        _service.Respond(200, Recorded("client-add-secret-success"));

        var result = (CkMethodResult<string?>)await CallAsync("Client", "AddSecretAsync", TargetRtId, "ci deploy key", null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal("DUMMY-GENERATED-SECRET-9044", result.Value);
        Assert.Contains("\"input\":{\"description\":\"ci deploy key\"}", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_ProvisionMirrorInTenant_ReturnsTheRecord()
    {
        _service.Respond(200, Recorded("client-provision-mirror-in-tenant-success"));

        var result = await CallAsync("Client", "ProvisionMirrorInTenantAsync", TargetRtId, "plant-a", null);

        var value = ResultValue(result);
        Assert.Equal(1L, Convert.ToInt64(Property(value, "NewlyProvisioned")));
        Assert.Equal(0L, Convert.ToInt64(Property(value, "AlreadyPresent")));
        Assert.Equal(1L, Convert.ToInt64(Property(value, "FlaggedClientsConsidered")));
    }

    // System.Identity 3.9.0: Client.SetMirrorRoles-1, answers recorded on a tenant with System.Identity 3.9.0.

    [Fact]
    public async Task RecordedAnswer_SetMirrorRoles_DryRun_ReturnsThePlanPerChildTenant()
    {
        _service.Respond(200, Recorded("client-set-mirror-roles-dry-run-success"));

        var result = await CallAsync("Client", "SetMirrorRolesAsync", TargetRtId,
            new List<string> { "CommunicationManagement" }, true, null);

        var value = ResultValue(result);
        Assert.Equal(true, Property(value, "DryRun"));
        Assert.Equal(["CommunicationManagement"], ((IEnumerable<string?>)Property(value, "RoleNames")!).ToList());
        var tenants = ((System.Collections.IEnumerable)Property(value, "Tenants")!).Cast<object>().ToList();
        Assert.Equal(2, tenants.Count);
        var planned = tenants.Single(t => (string?)Property(t, "ChildTenantId") == "plant-a");
        Assert.Equal("Planned", Property(planned, "Outcome"));
        Assert.Equal(["CommunicationManagement"], ((IEnumerable<string?>)Property(planned, "Added")!).ToList());
        Assert.Equal(["Reader"], ((IEnumerable<string?>)Property(planned, "HandSet")!).ToList());
        var missing = tenants.Single(t => (string?)Property(t, "ChildTenantId") == "plant-b");
        Assert.Equal(["CommunicationManagement"], ((IEnumerable<string?>)Property(missing, "RoleMissing")!).ToList());
        Assert.Empty((IEnumerable<string?>)Property(missing, "RoleRefused")!);

        var body = Assert.Single(_service.Bodies);
        Assert.Contains("setMirrorRoles(rtId: $rtId, input: $input, idempotencyKey: $idempotencyKey)", body, StringComparison.Ordinal);
        Assert.Contains("\"input\":{\"roleNames\":[\"CommunicationManagement\"],\"dryRun\":true}", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_SetMirrorRoles_DeniedRoleIsATypedResult()
    {
        _service.Respond(200, Recorded("client-set-mirror-roles-role-not-allowed"));

        var result = (CkMethodResult)await CallAsync("Client", "SetMirrorRolesAsync", TargetRtId,
            new List<string> { "TenantManagement" }, null, null);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsError(ErrorConstant("Client", "SetMirrorRoles", "MirrorRoleNotAllowed")));
        // The refused names, comma-separated (the method error parameters are strings).
        Assert.Equal("TenantManagement", result.Error!.Parameters["roleNames"]);
        // dryRun omitted: not sent, the handler reads it as false.
        Assert.Contains("\"input\":{\"roleNames\":[\"TenantManagement\"]}", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_SetMirrorRolesWithoutTenantManagement_Throws()
    {
        _service.Respond(200, Recorded("client-set-mirror-roles-forbidden"));

        var e = await Assert.ThrowsAsync<CkMethodCallException>(() => CallAsync("Client", "SetMirrorRolesAsync", TargetRtId,
            new List<string> { "CommunicationManagement" }, true, null));

        Assert.Equal("METHOD_FORBIDDEN", e.Code);
    }

    [Fact]
    public async Task RecordedAnswer_CreateClientWithoutTheRole_Throws()
    {
        _service.Respond(200, Recorded("client-create-forbidden"));

        var e = await Assert.ThrowsAsync<CkMethodCallException>(() => CallAsync("Client", "CreateClientAsync", "ci-deployer",
            null, "CI deployer"));

        Assert.Equal("METHOD_FORBIDDEN", e.Code);
        Assert.IsAssignableFrom<QlQueryErrorException>(e);
    }

    [Fact]
    public async Task RecordedAnswer_ApiResourceUpdateSecret_UnknownSecretIsATypedResult()
    {
        _service.Respond(200, Recorded("api-resource-update-secret-not-found"));

        var result = (CkMethodResult)await CallAsync("ApiResource", "UpdateSecretAsync", TargetRtId, "not-a-stored-secret", "rotated",
            null, null);

        Assert.True(result.IsError(ErrorConstant("ApiResource", "UpdateSecret", "SecretNotFound")));
    }

    [Fact]
    public async Task RecordedAnswer_CreateRole_ExistingNameIsATypedResult()
    {
        _service.Respond(200, Recorded("role-create-already-exists"));

        var result = (CkMethodResult<string?>)await CallAsync("Role", "CreateRoleAsync", "Reader", null);

        Assert.True(result.IsError(ErrorConstant("Role", "CreateRole", "RoleAlreadyExists")));
        Assert.Equal("Reader", result.Error!.Parameters["name"]);
    }

    [Fact]
    public async Task RecordedAnswer_SetEnforcementMode_SendsTheModeAsEnum()
    {
        _service.Respond(200, Recorded("data-policy-set-enforcement-mode-success"));

        var result = (CkMethodResult)await CallAsync("DataPolicy", "SetEnforcementModeAsync", TargetRtId,
            EnumValue("RtDataPolicyEnforcementModeEnum", "AuditOnly"), null);

        Assert.True(result.IsSuccess);
        Assert.Contains("\"input\":{\"enforcementMode\":\"AUDIT_ONLY\"}", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_CreatePolicy_ReturnsTheNewRtId()
    {
        _service.Respond(200, Recorded("data-permission-create-policy-success"));

        var result = (CkMethodResult<string?>)await CallAsync("DataPermission", "CreatePolicyAsync", TargetRtId,
            new List<string> { "System.Identity/Role" }, EnumValue("RtDataPolicyScopeEnum", "All"),
            EnumValue("RtDataPolicyEnforcementModeEnum", "Enforce"), new List<string> { "Read" }, null);

        Assert.True(result.IsSuccess);
        Assert.Equal("65f1c0ffee0000000000d001", result.Value);
        Assert.Contains("\"scope\":\"ALL\",\"enforcementMode\":\"ENFORCE\"", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_ProvisionCurrentUser_ReturnsTheRecord()
    {
        _service.Respond(200, Recorded("external-mapping-provision-current-user-success"));

        var result = await CallAsync("ExternalTenantUserMapping", "ProvisionCurrentUserAsync", "plant-a", null);

        var value = ResultValue(result);
        Assert.Equal("created", Property(value, "Outcome"));
        Assert.Equal("65f1c0ffee0000000000e001", Property(value, "MappingRtId"));
        Assert.Equal("alice", Property(value, "SourceUserName"));
    }

    [Fact]
    public async Task RecordedAnswer_CreateMapping_UnknownGroupIsATypedResult()
    {
        _service.Respond(200, Recorded("external-mapping-create-mapping-unknown-group"));

        var result = await CallAsync("ExternalTenantUserMapping", "CreateMappingAsync", "meshmakers", "65f0c0ffee65f0c0ffee6501",
            "alice", null, new List<string> { "65f1c0ffee0000000000b00b" }, null);

        var typed = (CkMethodResult)result;
        Assert.True(typed.IsError(ErrorConstant("ExternalTenantUserMapping", "CreateMapping", "UnknownGroup")));
        Assert.Equal("65f1c0ffee0000000000b00b", typed.Error!.Parameters["groupId"]);
    }

    [Fact]
    public async Task RecordedAnswer_CreateIdentityProvider_OnTheConcreteProviderType()
    {
        _service.Respond(200, Recorded("identity-provider-create-success"));

        var result = (CkMethodResult<string?>)await CallAsync("GoogleIdentityProvider", "CreateIdentityProviderAsync", "Google", true,
            null, null, null, "google-client", "DUMMY-CLIENT-SECRET-9044");

        Assert.True(result.IsSuccess);
        Assert.Equal("65f1c0ffee0000000000a001", result.Value);
        var body = Assert.Single(_service.Bodies);
        Assert.Contains("systemIdentityGoogleIdentityProviders { createIdentityProvider(", body, StringComparison.Ordinal);
        // The secret travels only in the variables of the request body, never in the URL.
        Assert.DoesNotContain("DUMMY-CLIENT-SECRET-9044", Assert.Single(_service.Paths), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_CreateEmailDomainGroupRule_ExistingPatternIsATypedResult()
    {
        _service.Respond(200, Recorded("email-domain-group-rule-create-already-exists"));

        var result = (CkMethodResult<string?>)await CallAsync("EmailDomainGroupRule", "CreateEmailDomainGroupRuleAsync", "*.example.com",
            "65f1c0ffee0000000000cafe", null, null);

        Assert.True(result.IsError(ErrorConstant("EmailDomainGroupRule", "CreateEmailDomainGroupRule", "EmailDomainGroupRuleAlreadyExists")));
    }

    [Fact]
    public async Task RecordedAnswer_DeleteApiScope_Succeeds()
    {
        _service.Respond(200, Recorded("api-scope-delete-success"));

        var result = (CkMethodResult)await CallAsync("ApiScope", "DeleteApiScopeAsync", TargetRtId, "key-9044-del");

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.InvocationId));
        Assert.Contains($"\"rtId\":\"{TargetRtId}\"", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    private static List<string> ClientMethodNames(string code) =>
        Regex.Matches(code, @"public global::System\.Threading\.Tasks\.Task<[^>]+(?:<[^>]+>)?(?:<[^>]+>)?> (\w+)Async\(")
            .Select(m => m.Groups[1].Value).ToList();

    private async Task<object> CallAsync(string type, string method, params object?[] arguments)
    {
        var clientType = Generated.Value.GetType($"{Ns}.Rt{type}MethodClient")!;
        var client = Activator.CreateInstance(clientType, RealTenantClient())!;
        var info = clientType.GetMethod(method)!;
        // Trailing optional parameters (and the idempotency key) not given by the test are passed as null.
        var padded = arguments.Concat(Enumerable.Repeat<object?>(null, info.GetParameters().Length - arguments.Length)).ToArray();
        var task = (Task)info.Invoke(client, padded)!;
        await task;
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static object ResultValue(object result)
    {
        Assert.True(((CkMethodResult)result).IsSuccess);
        return result.GetType().GetProperty("Value")!.GetValue(result)!;
    }

    private static object? Property(object value, string name) => value.GetType().GetProperty(name)!.GetValue(value);

    private static object EnumValue(string enumType, string name) =>
        Enum.Parse(Generated.Value.GetType($"{Ns}.{enumType}")!, name);

    private static string ErrorConstant(string type, string method, string name) =>
        (string)Generated.Value.GetType($"{Ns}.Rt{type}MethodClient+{method}Errors")!.GetField(name)!.GetValue(null)!;

    private static string Recorded(string name) =>
        File.ReadAllText(Path.Combine(GeneratorFixtures.FixtureDirectory, $"assetrepo-identity-answer-{name}.json"));

    private ITenantClient RealTenantClient()
    {
        var token = A.Fake<ITenantClientAccessToken>();
        A.CallTo(() => token.AccessToken).Returns(null);
        return new TenantClient(new TenantClientOptions { EndpointUri = _service.BaseUrl, TenantId = "acme" }, token);
    }
}
