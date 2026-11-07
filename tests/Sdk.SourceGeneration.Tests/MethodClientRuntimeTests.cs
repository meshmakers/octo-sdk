using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using GraphQL;
using GraphQL.Client.Serializer.SystemTextJson;
using Meshmakers.Octo.Sdk.ServiceClient;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants;
using Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.Tenants.Methods;

namespace Sdk.SourceGeneration.Tests;

/// <summary>
///     AB#8423 (AC 3): the generated clients of the test model, compiled and called. A declared method error comes
///     back as a typed result (code, declared error, parameters); an undeclared error (authorization, validation)
///     throws as before. The fake tests check the request; the loopback tests go through the real
///     <see cref="TenantClient" /> and its response parsing.
/// </summary>
public class MethodClientRuntimeTests : IClassFixture<MethodClientRuntimeTests.LoopbackGraphQlService>
{
    private const string Ns = "Consumer.DataTransferObjects.MethodClientTest.v1";
    private const string RtId = "65f1c0ffee0000000000abcd";

    private static readonly Lazy<Assembly> Generated = new(() =>
        GeneratorFixtures.CompileAndLoad(GeneratorFixtures.Emit(GeneratorFixtures.MethodClientTest, "Consumer").Values));

    private const string AssetRepoNs = "Consumer.DataTransferObjects.AssetRepositoryIntegrationTest.v1";

    // Type DTOs left out: see GeneratedSources_CompileWithoutWarnings_ForBothFixtureModels.
    private static readonly Lazy<Assembly> AssetRepoGenerated = new(() => GeneratorFixtures.CompileAndLoad(
        GeneratorFixtures.Emit(GeneratorFixtures.AssetRepoIntegrationTest, "Consumer")
            .Where(s => !s.Key.Contains(".Type.", StringComparison.Ordinal) ||
                        s.Key.EndsWith("MethodClient.g.cs", StringComparison.Ordinal))
            .Select(s => s.Value)));

    private readonly LoopbackGraphQlService _service;

    public MethodClientRuntimeTests(LoopbackGraphQlService service)
    {
        _service = service;
        _service.Reset();
    }

    [Fact]
    public async Task Call_SendsTheDocumentAndTheTypedVariables()
    {
        var (tenantClient, requests) = FakeTenantClient("""{"invocationId":"6ae7c0de-0000-0000-0000-000000000001"}""");
        var state = Enum.Parse(Generated.Value.GetType($"{Ns}.RtAccountStateEnum")!, "LockedOut");

        var result = (CkMethodResult)await CallAsync(tenantClient, "Account", "ResetPassword2Async",
            RtId, "n3w-Secret", null, state, "key-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("6ae7c0de-0000-0000-0000-000000000001", result.InvocationId);
        var request = Assert.Single(requests);
        Assert.Equal(Constant("Account", "ResetPassword2Document"), request.Query);
        Assert.Equal("MethodClientTestAccountResetPassword2", request.OperationName);
        var json = new SystemTextJsonSerializer().SerializeToString(request);
        using var document = JsonDocument.Parse(json);
        var variables = document.RootElement.GetProperty("variables");
        Assert.Equal(RtId, variables.GetProperty("rtId").GetString());
        Assert.Equal("key-1", variables.GetProperty("idempotencyKey").GetString());
        var input = variables.GetProperty("input");
        Assert.Equal("n3w-Secret", input.GetProperty("newPassword").GetString());
        // Enum in the CONSTANT_CASE form of the asset repository's enum type; an omitted optional parameter is not sent.
        Assert.Equal("LOCKED_OUT", input.GetProperty("state").GetString());
        Assert.False(input.TryGetProperty("notifyUser", out _));
    }

    [Fact]
    public async Task StaticCallWithoutParameters_SendsOnlyTheIdempotencyKey_AndReadsTheResult()
    {
        var (tenantClient, requests) = FakeTenantClient("""{"invocationId":"i-1","result":42}""");

        var result = (CkMethodResult<int?>)await CallAsync(tenantClient, "Account", "CountLockedAsync", [null]);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        var json = new SystemTextJsonSerializer().SerializeToString(Assert.Single(requests));
        using var document = JsonDocument.Parse(json);
        Assert.Equal(["idempotencyKey"],
            document.RootElement.GetProperty("variables").EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public async Task DeclaredError_ComesBackAsTypedResult()
    {
        var tenantClient = ThrowingTenantClient(MethodError("PASSWORD_REUSED",
            new Map { ["methodId"] = "MethodClientTest/Account.ResetPassword-2" }));

        var result = (CkMethodResult)await CallAsync(tenantClient, "Account", "ResetPassword2Async",
            RtId, "old", null, null, null);

        Assert.False(result.IsSuccess);
        Assert.Null(result.InvocationId);
        Assert.Equal("PASSWORD_REUSED", result.Error!.Code);
        Assert.True(result.IsError(ErrorsConstant("Account", "ResetPassword2Errors", "PasswordReused")));
        Assert.Equal("MethodClientTest/Account.ResetPassword-2", result.Error.Parameters["methodId"]);
        Assert.Equal("The CK method call failed: PASSWORD_REUSED", result.Error.Message);
    }

    [Fact]
    public async Task DeclaredErrorOfAnotherMethod_IsNotTyped_ButThrown()
    {
        // ResetPassword-1 declares only PASSWORD_POLICY_VIOLATION.
        var tenantClient = ThrowingTenantClient(MethodError("PASSWORD_REUSED", new Map()));

        var e = await Assert.ThrowsAsync<CkMethodCallException>(() =>
            CallAsync(tenantClient, "Account", "ResetPasswordAsync", RtId, "old", null));

        Assert.Equal("PASSWORD_REUSED", e.Code);
    }

    [Fact]
    public async Task AuthorizationError_Throws_AsAGraphQlErrorLikeToday()
    {
        var tenantClient = ThrowingTenantClient(MethodError("METHOD_FORBIDDEN",
            new Map { ["methodId"] = "MethodClientTest/Account.Touch-1", ["reason"] = "not-authorized" }));

        var e = await Assert.ThrowsAsync<CkMethodCallException>(() =>
            CallAsync(tenantClient, "Account", "TouchAsync", RtId, null));

        Assert.IsAssignableFrom<QlQueryErrorException>(e);
        Assert.Equal("METHOD_FORBIDDEN", e.Code);
        Assert.Equal("not-authorized", e.Error.Parameters["reason"]);
        Assert.Equal("MethodClientTest/Account.Touch-1", e.MethodId);
        Assert.NotNull(e.Errors);
    }

    [Fact]
    public async Task ErrorWithoutCode_IsRethrownUnchanged()
    {
        var original = new QlQueryErrorException("Field 'x' not found", [new GraphQLError { Message = "Field 'x' not found" }]);
        var tenantClient = A.Fake<ITenantClient>();
        A.CallTo(tenantClient).Where(call => call.Method.Name == nameof(ITenantClient.SendMutationAsync))
            .WithNonVoidReturnType().Throws(original);

        var e = await Assert.ThrowsAsync<QlQueryErrorException>(() =>
            CallAsync(tenantClient, "Account", "TouchAsync", RtId, null));

        Assert.Same(original, e);
    }

    [Fact]
    public async Task UnauthorizedTransport_IsNotTurnedIntoAResult()
    {
        var tenantClient = A.Fake<ITenantClient>();
        A.CallTo(tenantClient).Where(call => call.Method.Name == nameof(ITenantClient.SendMutationAsync))
            .WithNonVoidReturnType().Throws(new UnauthorizedServiceAccessException(new Exception("401")));

        await Assert.ThrowsAsync<UnauthorizedServiceAccessException>(() =>
            CallAsync(tenantClient, "Account", "TouchAsync", RtId, null));
    }

    [Fact]
    public async Task ThroughTheRealTenantClient_RecordResultIsRead()
    {
        _service.Respond(200,
            """{"data":{"runtime":{"methodClientTestAccounts":{"findByName":{"invocationId":"i-7","result":{"name":"alice","loginCount":3}}}}}}""");

        var result = await CallAsync(RealTenantClient(), "Account", "FindByNameAsync", "alice", 5, null);

        var typed = Assert.IsAssignableFrom<CkMethodResult>(result);
        Assert.True(typed.IsSuccess);
        Assert.Equal("i-7", typed.InvocationId);
        dynamic value = result.GetType().GetProperty("Value")!.GetValue(result)!;
        Assert.Equal("alice", (string)value.Name);
        Assert.Equal(3L, (long?)value.LoginCount);
        var body = Assert.Single(_service.Bodies);
        Assert.Contains("findByName(input: $input, idempotencyKey: $idempotencyKey)", body, StringComparison.Ordinal);
        Assert.Contains("\"maxResults\":5", body, StringComparison.Ordinal);
        Assert.Equal("/tenants/acme/GraphQL", Assert.Single(_service.Paths));
    }

    [Fact]
    public async Task ThroughTheRealTenantClient_DeclaredErrorIsATypedResult()
    {
        _service.Respond(200,
            """{"errors":[{"message":"The CK method call failed: PASSWORD_POLICY_VIOLATION","path":["runtime","methodClientTestAccounts","resetPassword"],"extensions":{"code":"PASSWORD_POLICY_VIOLATION","codes":["PASSWORD_POLICY_VIOLATION"],"parameters":{"methodId":"MethodClientTest/Account.ResetPassword-1","minLength":"12"},"details":[{"code":"TOO_SHORT","parameters":{"field":"newPassword"}}]}}],"data":{"runtime":{"methodClientTestAccounts":{"resetPassword":null}}}}""");

        var result = (CkMethodResult<string?>)await CallAsync(RealTenantClient(), "Account", "ResetPasswordAsync",
            RtId, "short", null);

        Assert.False(result.IsSuccess);
        Assert.Equal("PASSWORD_POLICY_VIOLATION", result.Error!.Code);
        Assert.Equal("12", result.Error.Parameters["minLength"]);
        var detail = Assert.Single(result.Error.Details);
        Assert.Equal("TOO_SHORT", detail.Code);
        Assert.Equal("newPassword", detail.Parameters["field"]);
    }

    [Fact]
    public async Task ThroughTheRealTenantClient_ForbiddenThrows()
    {
        _service.Respond(200,
            """{"errors":[{"message":"The CK method call failed: METHOD_FORBIDDEN","extensions":{"code":"METHOD_FORBIDDEN","parameters":{"methodId":"MethodClientTest/Account.Touch-1","reason":"not-authorized"}}}],"data":{"runtime":{"methodClientTestAccounts":{"touch":null}}}}""");

        var e = await Assert.ThrowsAsync<CkMethodCallException>(() =>
            CallAsync(RealTenantClient(), "Account", "TouchAsync", RtId, null));

        Assert.Equal("METHOD_FORBIDDEN", e.Code);
    }

    [Fact]
    public async Task ThroughTheRealTenantClient_InheritedMethodUsesTheDerivedTypeField()
    {
        _service.Respond(200,
            """{"data":{"runtime":{"methodClientTestServiceAccounts":{"touch":{"invocationId":"i-9"}}}}}""");

        var result = (CkMethodResult)await CallAsync(RealTenantClient(), "ServiceAccount", "TouchAsync", RtId, null);

        Assert.Equal("i-9", result.InvocationId);
        Assert.Contains("methodClientTestServiceAccounts { touch(", Assert.Single(_service.Bodies), StringComparison.Ordinal);
    }

    /// <summary>
    ///     The answers below were recorded from the asset repository itself: the generated documents of
    ///     <c>AccessTestAccount</c> executed with the client's variables on the real schema and method gateway
    ///     (octo-asset-repo-services 603d54f, <c>MethodGatewayGraphQlTestFixture</c>, scripted handler; steps in
    ///     docs/method-clients.md).
    /// </summary>
    [Fact]
    public async Task RecordedAssetRepositoryAnswer_DeclaredError_IsATypedResult()
    {
        _service.Respond(200, Recorded("declared-error"));

        var result = (CkMethodResult)await CallAsync(AssetRepoGenerated.Value, AssetRepoNs, RealTenantClient(),
            "AccessTestAccount", "ResetPasswordAsync", RtId, "short", null, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("PASSWORD_POLICY_VIOLATION", result.Error!.Code);
        Assert.Equal("12", result.Error.Parameters["minLength"]);
        Assert.Equal("AssetRepositoryIntegrationTest/AccessTestAccount.ResetPassword-1", result.Error.Parameters["methodId"]);
    }

    [Fact]
    public async Task RecordedAssetRepositoryAnswer_Forbidden_Throws()
    {
        _service.Respond(200, Recorded("forbidden"));

        var e = await Assert.ThrowsAsync<CkMethodCallException>(() => CallAsync(AssetRepoGenerated.Value, AssetRepoNs,
            RealTenantClient(), "AccessTestAccount", "ResetPasswordAsync", RtId, "short", null, null));

        Assert.Equal("METHOD_FORBIDDEN", e.Code);
        Assert.Equal("not-authorized", e.Error.Parameters["reason"]);
    }

    [Fact]
    public async Task RecordedAssetRepositoryAnswer_Success_ReadsTheResult()
    {
        _service.Respond(200, Recorded("success"));

        var result = (CkMethodResult<string?>)await CallAsync(AssetRepoGenerated.Value, AssetRepoNs, RealTenantClient(),
            "AccessTestAccount", "ResetPassword2Async", RtId, 3, "key-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("done", result.Value);
        Assert.False(string.IsNullOrEmpty(result.InvocationId));
        var body = Assert.Single(_service.Bodies);
        Assert.Contains("\"input\":{\"count\":3}", body, StringComparison.Ordinal);
    }

    [Fact]
    public void TimeSpanConverter_WritesAndReadsWholeSeconds()
    {
        var options = new JsonSerializerOptions { Converters = { new CkMethodTimeSpanSecondsConverter() } };

        Assert.Equal("90", JsonSerializer.Serialize(TimeSpan.FromSeconds(90), options));
        Assert.Equal(TimeSpan.FromMinutes(2), JsonSerializer.Deserialize<TimeSpan>("120", options));
        Assert.Equal(TimeSpan.FromMinutes(2), JsonSerializer.Deserialize<TimeSpan?>("120", options));
    }

    private static Task<object> CallAsync(ITenantClient tenantClient, string type, string method,
        params object?[] arguments) => CallAsync(Generated.Value, Ns, tenantClient, type, method, arguments);

    private static async Task<object> CallAsync(Assembly assembly, string ns, ITenantClient tenantClient, string type,
        string method, params object?[] arguments)
    {
        var clientType = assembly.GetType($"{ns}.Rt{type}MethodClient")!;
        var client = Activator.CreateInstance(clientType, tenantClient)!;
        var task = (Task)clientType.GetMethod(method)!.Invoke(client, arguments)!;
        await task;
        return task.GetType().GetProperty("Result")!.GetValue(task)!;
    }

    private static string Recorded(string name) =>
        File.ReadAllText(Path.Combine(GeneratorFixtures.FixtureDirectory, $"assetrepo-answer-{name}.json"));

    private static string Constant(string type, string name) =>
        (string)Generated.Value.GetType($"{Ns}.Rt{type}MethodClient")!.GetField(name)!.GetValue(null)!;

    private static string ErrorsConstant(string type, string errorsClass, string name) =>
        (string)Generated.Value.GetType($"{Ns}.Rt{type}MethodClient+{errorsClass}")!.GetField(name)!.GetValue(null)!;

    private static (ITenantClient Client, List<GraphQLRequest> Requests) FakeTenantClient(string responseJson)
    {
        var requests = new List<GraphQLRequest>();
        var tenantClient = A.Fake<ITenantClient>();
        var options = SystemTextJsonSerializer.DefaultJsonSerializerOptions;
        options.PropertyNameCaseInsensitive = true;
        A.CallTo(tenantClient).Where(call => call.Method.Name == nameof(ITenantClient.SendMutationAsync))
            .WithNonVoidReturnType()
            .ReturnsLazily(call =>
            {
                requests.Add((GraphQLRequest)call.Arguments[0]!);
                var responseType = call.Method.GetGenericArguments()[0];
                var value = JsonSerializer.Deserialize(responseJson, responseType, options);
                return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(responseType)
                    .Invoke(null, [value])!;
            });
        return (tenantClient, requests);
    }

    private static ITenantClient ThrowingTenantClient(GraphQLError error)
    {
        var tenantClient = A.Fake<ITenantClient>();
        A.CallTo(tenantClient).Where(call => call.Method.Name == nameof(ITenantClient.SendMutationAsync))
            .WithNonVoidReturnType()
            .Throws(new QlQueryErrorException(error.Message, [error]));
        return tenantClient;
    }

    private static GraphQLError MethodError(string code, Map parameters) => new()
    {
        Message = $"The CK method call failed: {code}",
        Extensions = new Map { ["code"] = code, ["parameters"] = parameters }
    };

    private ITenantClient RealTenantClient()
    {
        var token = A.Fake<ITenantClientAccessToken>();
        A.CallTo(() => token.AccessToken).Returns(null);
        return new TenantClient(new TenantClientOptions { EndpointUri = _service.BaseUrl, TenantId = "acme" }, token);
    }

    /// <summary>
    ///     A loopback HTTP endpoint answering every request with one canned GraphQL response and recording the bodies.
    ///     A class fixture: registering an <see cref="HttpListener" /> prefix costs seconds on macOS.
    /// </summary>
    public sealed class LoopbackGraphQlService : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly List<string> _bodies = [];
        private readonly List<string> _paths = [];
        private readonly Lock _sync = new();
        private (int Status, string Body) _response = (200, "{}");

        public LoopbackGraphQlService()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            BaseUrl = $"http://127.0.0.1:{port}/";
            _listener.Prefixes.Add(BaseUrl);
            _listener.Start();
            _ = Task.Run(ServeAsync);
        }

        public string BaseUrl { get; }

        public IReadOnlyList<string> Bodies
        {
            get
            {
                lock (_sync)
                {
                    return _bodies.ToArray();
                }
            }
        }

        public IReadOnlyList<string> Paths
        {
            get
            {
                lock (_sync)
                {
                    return _paths.ToArray();
                }
            }
        }

        public void Reset()
        {
            lock (_sync)
            {
                _bodies.Clear();
                _paths.Clear();
                _response = (200, "{}");
            }
        }

        public void Respond(int status, string body)
        {
            lock (_sync)
            {
                _response = (status, body);
            }
        }

        public void Dispose() => _listener.Abort();

        private async Task ServeAsync()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception e) when (e is HttpListenerException or ObjectDisposedException)
                {
                    return;
                }

                using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                var body = await reader.ReadToEndAsync();
                (int Status, string Body) response;
                lock (_sync)
                {
                    _bodies.Add(body);
                    _paths.Add(context.Request.Url!.AbsolutePath);
                    response = _response;
                }

                var bytes = Encoding.UTF8.GetBytes(response.Body);
                context.Response.StatusCode = response.Status;
                context.Response.ContentType = "application/json";
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
            }
        }
    }
}
