using System.Reflection;
using System.Text.RegularExpressions;
using GraphQL.Types;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

namespace Sdk.SourceGeneration.Tests;

/// <summary>
///     AB#8416 (identity wave 1): the User method clients a consumer generates from the released
///     <c>System.Identity</c> 3.2.0 model (<c>System.Identity-[3.2,4.0)</c> in <c>construction-kits.yaml</c>). The SDK
///     ships no System.Identity surface (decision of AB#8416, option C); these tests prove what a consumer gets:
///     the signatures, documents that the asset repository accepts (recorded schema of a tenant with System.Identity
///     3.2.0), and declared errors as typed results (answers recorded from the asset repository's method gateway).
/// </summary>
public class UserMethodClientTests : IClassFixture<MethodClientRuntimeTests.LoopbackGraphQlService>
{
    private const string Ns = "Consumer.DataTransferObjects.System.Identity.v3";
    private const string UserClientHint = $"{Ns}.Type.UserMethodClient.g.cs";
    private const string UserRtId = "65f1c0ffee0000000000abcd";
    private const string Result = "global::Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods.CkMethodResult";

    private static readonly Lazy<SortedDictionary<string, string>> Sources =
        new(() => GeneratorFixtures.Emit(GeneratorFixtures.SystemIdentity, "Consumer"));

    private static readonly Lazy<Assembly> Generated = new(() => GeneratorFixtures.CompileAndLoad(Sources.Value.Values));

    private static readonly Lazy<ISchema> Schema =
        new(() => MethodClientSchemaValidationTests.LoadSchema(GeneratorFixtures.IdentitySchemaFile));

    private readonly MethodClientRuntimeTests.LoopbackGraphQlService _service;

    public UserMethodClientTests(MethodClientRuntimeTests.LoopbackGraphQlService service)
    {
        _service = service;
        _service.Reset();
    }

    [Fact]
    public void UserClient_HasOneMethodPerUserCkMethod()
    {
        var code = Sources.Value[UserClientHint];

        Assert.Contains("public partial class RtUserMethodClient : global::Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods.CkMethodClientBase", code);
        var methods = Regex.Matches(code, @"public global::System\.Threading\.Tasks\.Task<[^>]+(?:<[^>]+>)?> (\w+)Async\(")
            .Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(["AddRole", "ChangePassword", "CreateUser", "DisableTwoFactor", "Merge", "RemoveRole", "ResetPassword",
            "SetPassword", "SetRoles"], methods);
    }

    [Fact]
    public void UserClient_Signatures_OfTheMigratedOperations()
    {
        var code = Sources.Value[UserClientHint];

        // Replacements of the obsolete REST methods (IIdentityServicesClient.CreateUser, ResetPassword, AddRoleToUser,
        // RemoveRoleFromUser): the methods address the user by rtId; CreateUser is static and returns the new rtId.
        Assert.Contains($"Task<{Result}<string?>> CreateUserAsync(string userName, string? email = null, string? firstName = null, string? lastName = null, string? password = null, string? idempotencyKey = null)", code);
        Assert.Contains($"Task<{Result}> ResetPasswordAsync(string rtId, string newPassword, string? idempotencyKey = null)", code);
        Assert.Contains($"Task<{Result}> AddRoleAsync(string rtId, string roleName, string? idempotencyKey = null)", code);
        Assert.Contains($"Task<{Result}> RemoveRoleAsync(string rtId, string roleName, string? idempotencyKey = null)", code);
        // Operations without an SDK REST method (no [Obsolete] counterpart).
        Assert.Contains($"Task<{Result}> SetRolesAsync(string rtId, global::System.Collections.Generic.IReadOnlyList<string> roleNames, string? idempotencyKey = null)", code);
        Assert.Contains($"Task<{Result}> MergeAsync(string rtId, string sourceUserName, string? idempotencyKey = null)", code);
        Assert.Contains($"Task<{Result}> ChangePasswordAsync(string rtId, string currentPassword, string newPassword, string? idempotencyKey = null)", code);
        Assert.Contains($"Task<{Result}> SetPasswordAsync(string rtId, string newPassword, string? idempotencyKey = null)", code);
    }

    [Theory]
    [InlineData("CreateUser", "USER_NAME_RESERVED,USER_ALREADY_EXISTS,PASSWORD_POLICY_VIOLATION")]
    [InlineData("ResetPassword", "PASSWORD_POLICY_VIOLATION,SHADOW_USER_PASSWORD_NOT_ALLOWED")]
    [InlineData("AddRole", "ROLE_NOT_FOUND,ROLE_ALREADY_ASSIGNED")]
    [InlineData("RemoveRole", "ROLE_NOT_FOUND,ROLE_NOT_ASSIGNED")]
    [InlineData("SetRoles", "ROLE_NOT_FOUND")]
    [InlineData("Merge", "SOURCE_USER_NOT_FOUND,CANNOT_MERGE_INTO_ITSELF,MERGE_FAILED")]
    [InlineData("ChangePassword", "PASSWORD_POLICY_VIOLATION,CURRENT_PASSWORD_INVALID,USER_LOCKED_OUT,SHADOW_USER_PASSWORD_NOT_ALLOWED")]
    [InlineData("SetPassword", "PASSWORD_POLICY_VIOLATION,PASSWORD_ALREADY_SET,SHADOW_USER_PASSWORD_NOT_ALLOWED")]
    public void UserClient_DeclaredErrors_AreConstants(string method, string codes)
    {
        var errors = Generated.Value.GetType($"{Ns}.RtUserMethodClient+{method}Errors")!;

        var values = errors.GetFields().Select(f => (string)f.GetValue(null)!).ToList();

        Assert.Equal(codes.Split(','), values);
    }

    [Fact]
    public void GeneratedSources_CompileWithoutWarnings()
    {
        Assert.NotNull(Generated.Value.GetType($"{Ns}.RtUserMethodClient"));
    }

    public static TheoryData<string, string> IdentityDocuments()
    {
        var data = new TheoryData<string, string>();
        foreach (var (hint, code) in Sources.Value.Where(s => s.Key.EndsWith("MethodClient.g.cs", StringComparison.Ordinal)))
        {
            foreach (Match match in Regex.Matches(code, "public const string (?<name>\\w+)Document = \"(?<doc>[^\"]*)\";"))
            {
                data.Add($"{hint}:{match.Groups["name"].Value}", match.Groups["doc"].Value);
            }
        }

        return data;
    }

    [Fact]
    public void IdentityDocuments_CoverEveryUserMethod()
    {
        var names = IdentityDocuments().Select(row => row.Data.Item1)
            .Where(n => n.StartsWith(UserClientHint, StringComparison.Ordinal))
            .Select(n => n[(UserClientHint.Length + 1)..])
            .ToList();

        Assert.Equal(["AddRole", "ChangePassword", "CreateUser", "DisableTwoFactor", "Merge", "RemoveRole", "ResetPassword",
            "SetPassword", "SetRoles"], names);
    }

    [Theory]
    [MemberData(nameof(IdentityDocuments))]
    public async Task IdentityDocument_ValidatesAgainstTheRecordedSchemaOfATenantWithSystemIdentity(string method, string document)
    {
        var errors = await MethodClientSchemaValidationTests.DocumentErrorsAsync(Schema.Value, document);

        Assert.True(errors.Count == 0, method + ": " + string.Join("; ", errors));
    }

    [Theory]
    // AddRole has a required parameter: its input is non-null in the schema.
    [InlineData("mutation M($rtId: OctoObjectId!, $input: SystemIdentityUserAddRoleMethodInput, $idempotencyKey: String) { runtime { systemIdentityUsers { addRole(rtId: $rtId, input: $input, idempotencyKey: $idempotencyKey) { invocationId } } } }")]
    // CreateUser is static: no rtId argument.
    [InlineData("mutation M($rtId: OctoObjectId!, $input: SystemIdentityUserCreateUserMethodInput!, $idempotencyKey: String) { runtime { systemIdentityUsers { createUser(rtId: $rtId, input: $input, idempotencyKey: $idempotencyKey) { invocationId result } } } }")]
    // ResetPassword has no result.
    [InlineData("mutation M($rtId: OctoObjectId!, $input: SystemIdentityUserResetPasswordMethodInput!, $idempotencyKey: String) { runtime { systemIdentityUsers { resetPassword(rtId: $rtId, input: $input, idempotencyKey: $idempotencyKey) { invocationId result } } } }")]
    public async Task BrokenIdentityDocument_IsRejected_SoTheGateIsReal(string document)
    {
        Assert.NotEmpty(await MethodClientSchemaValidationTests.DocumentErrorsAsync(Schema.Value, document));
    }

    /// <summary>
    ///     The answers below were recorded from the asset repository: the generated User documents executed with the
    ///     client's variables on the real schema and method gateway of a tenant with System.Identity 3.2.0 (scripted
    ///     handler; steps in docs/method-clients.md).
    /// </summary>
    [Fact]
    public async Task RecordedAnswer_CreateUser_ReturnsTheNewRtId()
    {
        _service.Respond(200, Recorded("create-user-success"));

        var result = (CkMethodResult<string?>)await CallAsync("CreateUserAsync", "alice", "alice@example.invalid", null,
            null, null, "key-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("65f1c0ffee0000000000beef", result.Value);
        Assert.False(string.IsNullOrEmpty(result.InvocationId));
        var body = Assert.Single(_service.Bodies);
        Assert.Contains("createUser(input: $input, idempotencyKey: $idempotencyKey)", body, StringComparison.Ordinal);
        Assert.Contains("\"input\":{\"userName\":\"alice\",\"email\":\"alice@example.invalid\"}", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordedAnswer_ResetPassword_DeclaredErrorIsATypedResult()
    {
        _service.Respond(200, Recorded("reset-password-declared-error"));

        var result = (CkMethodResult)await CallAsync("ResetPasswordAsync", UserRtId, "short", null);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsError(ErrorConstant("ResetPassword", "PasswordPolicyViolation")));
        Assert.Equal("12", result.Error!.Parameters["minLength"]);
        Assert.Equal("System.Identity/User.ResetPassword-1", result.Error.Parameters["methodId"]);
    }

    [Fact]
    public async Task RecordedAnswer_AddRole_DeclaredErrorIsATypedResult()
    {
        _service.Respond(200, Recorded("add-role-declared-error"));

        var result = (CkMethodResult)await CallAsync("AddRoleAsync", UserRtId, "Reader", null);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsError(ErrorConstant("AddRole", "RoleAlreadyAssigned")));
        Assert.Equal("Reader", result.Error!.Parameters["roleName"]);
    }

    [Fact]
    public async Task RecordedAnswer_ResetPasswordWithoutUserManagement_Throws()
    {
        _service.Respond(200, Recorded("reset-password-forbidden"));

        var e = await Assert.ThrowsAsync<CkMethodCallException>(() => CallAsync("ResetPasswordAsync", UserRtId, "short", null));

        Assert.Equal("METHOD_FORBIDDEN", e.Code);
        Assert.IsAssignableFrom<QlQueryErrorException>(e);
    }

    [Fact]
    public async Task RecordedAnswer_Merge_Succeeds()
    {
        _service.Respond(200, Recorded("merge-success"));

        var result = (CkMethodResult)await CallAsync("MergeAsync", UserRtId, "bob", "key-2");

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.InvocationId));
        Assert.Contains("\"input\":{\"sourceUserName\":\"bob\"}", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    private async Task<object> CallAsync(string method, params object?[] arguments)
    {
        var clientType = Generated.Value.GetType($"{Ns}.RtUserMethodClient")!;
        var client = Activator.CreateInstance(clientType, RealTenantClient())!;
        var task = (Task)clientType.GetMethod(method)!.Invoke(client, arguments)!;
        await task;
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static string ErrorConstant(string method, string name) =>
        (string)Generated.Value.GetType($"{Ns}.RtUserMethodClient+{method}Errors")!.GetField(name)!.GetValue(null)!;

    private static string Recorded(string name) =>
        File.ReadAllText(Path.Combine(GeneratorFixtures.FixtureDirectory, $"assetrepo-identity-answer-{name}.json"));

    private ITenantClient RealTenantClient()
    {
        var token = A.Fake<ITenantClientAccessToken>();
        A.CallTo(() => token.AccessToken).Returns(null);
        return new TenantClient(new TenantClientOptions { EndpointUri = _service.BaseUrl, TenantId = "acme" }, token);
    }
}
