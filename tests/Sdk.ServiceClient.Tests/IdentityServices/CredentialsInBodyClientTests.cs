using System.Net;
using System.Text;
using System.Text.Json;
using Meshmakers.Octo.Sdk.ServiceClient.IdentityServices;

namespace Sdk.ServiceClient.Tests.IdentityServices;

/// <summary>
///     AB#8008 — the identity client never puts a credential into a request URL: the admin password reset and the
///     secret delete/lookup send the value in the JSON body. A local HTTP listener plays the identity service and records
///     method, path and query, and body of every request.
/// </summary>
public sealed class CredentialsInBodyClientTests : IDisposable
{
    // Throwaway values, generated per test run.
    private readonly string _password = "pw-" + Guid.NewGuid().ToString("N");
    private readonly string _storedHash = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

    private readonly HttpListener _listener;
    private readonly string _baseUri;
    private string? _method;
    private string? _pathAndQuery;
    private string? _body;
    private string? _contentType;
    private string _responseJson = "{}";

    public CredentialsInBodyClientTests()
    {
        // A failed Start() disposes the listener, so every attempt gets a fresh one; attempts are bounded and the last
        // failure is rethrown instead of looping on a closed listener.
        (_listener, _baseUri) = StartListener();
        _ = Task.Run(Serve);
    }

    public void Dispose() => _listener.Close();

    private static (HttpListener Listener, string BaseUri) StartListener()
    {
        const int attempts = 20;
        for (var attempt = 1; ; attempt++)
        {
            var uri = $"http://127.0.0.1:{45000 + Random.Shared.Next(0, 2000)}/";
            var listener = new HttpListener();
            listener.Prefixes.Add(uri);
            try
            {
                listener.Start();
                return (listener, uri);
            }
            catch (HttpListenerException) when (attempt < attempts)
            {
                // port taken: try another one with a new listener
                listener.Close();
            }
        }
    }

    private async Task Serve()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception)
            {
                return;
            }

            _method = context.Request.HttpMethod;
            _pathAndQuery = context.Request.Url?.PathAndQuery;
            _contentType = context.Request.ContentType;
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                _body = await reader.ReadToEndAsync();
            }

            var bytes = Encoding.UTF8.GetBytes(_responseJson);
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        }
    }

    private IdentityServicesClient CreateClient() =>
        new(new IdentityServiceClientOptions { EndpointUri = _baseUri, TenantId = "customer" },
            A.Fake<IIdentityServiceClientAccessToken>());

    [Fact]
    public async Task ResetPassword_PostsUserNameAndPasswordInTheBody_NothingInTheUrl()
    {
#pragma warning disable CS0618 // AB#8860: the obsolete REST method keeps calling REST during its REST window
        await CreateClient().ResetPassword("alice", _password);
#pragma warning restore CS0618

        Assert.Equal("POST", _method);
        Assert.Equal("/customer/v1/users/ResetPassword", _pathAndQuery);
        Assert.StartsWith("application/json", _contentType);
        using var body = JsonDocument.Parse(_body!);
        Assert.Equal("alice", body.RootElement.GetProperty("userName").GetString());
        Assert.Equal(_password, body.RootElement.GetProperty("password").GetString());
    }

    [Theory]
    [InlineData("client", "my-client")]
    [InlineData("apiResource", "my-resource")]
    public async Task DeleteApiSecret_PostsTheStoredValueInTheBody(string kind, string owner)
    {
        var client = CreateClient();
        if (kind == "client")
        {
            await client.DeleteApiSecretClient(owner, _storedHash);
        }
        else
        {
            await client.DeleteApiSecretApiResource(owner, _storedHash);
        }

        Assert.Equal("POST", _method);
        Assert.Equal($"/customer/v1/apiSecrets/{kind}/{owner}/delete", _pathAndQuery);
        AssertStoredValueOnlyInBody();
    }

    [Theory]
    [InlineData("client", "my-client")]
    [InlineData("apiResource", "my-resource")]
    public async Task GetApiSecret_PostsTheStoredValueInTheBody_AndReadsTheSecret(string kind, string owner)
    {
        _responseJson = JsonSerializer.Serialize(new { valueEncrypted = _storedHash, description = "d" });
        var client = CreateClient();

        var secret = kind == "client"
            ? await client.GetApiSecretForClient(owner, _storedHash)
            : await client.GetApiSecretForApiResource(owner, _storedHash);

        Assert.Equal("POST", _method);
        Assert.Equal($"/customer/v1/apiSecrets/{kind}/{owner}/lookup", _pathAndQuery);
        AssertStoredValueOnlyInBody();
        Assert.Equal(_storedHash, secret.ValueEncrypted);
        Assert.Equal("d", secret.Description);
    }

    [Theory]
    [InlineData("client", "my-client")]
    [InlineData("apiResource", "my-resource")]
    public async Task UpdateApiSecret_PutsTheSecret_NotPost(string kind, string owner)
    {
        // Regression: the update used ExecutePostAsync, which sends POST - the create endpoint - so octo-cli
        // UpdateApiSecret* generated a new secret instead of updating the existing one.
        var client = CreateClient();
        var update = new Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ApiSecretDto
            { ValueEncrypted = _storedHash, Description = "renamed" };
        if (kind == "client")
        {
            await client.UpdateApiSecretClient(owner, update);
        }
        else
        {
            await client.UpdateApiSecretApiResource(owner, update);
        }

        Assert.Equal("PUT", _method);
        Assert.Equal($"/customer/v1/apiSecrets/{kind}/{owner}", _pathAndQuery);
        AssertStoredValueOnlyInBody();
    }

    [Fact]
    public void RequestDtos_ToString_NeverCarriesTheValue()
    {
        var reset = new Meshmakers.Octo.Communication.Contracts.DataTransferObjects.UserPasswordResetDto
            { UserName = "alice", Password = _password };
        var reference = new Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ApiSecretReferenceDto
            { ValueEncrypted = _storedHash };

        Assert.DoesNotContain(_password, reset.ToString());
        Assert.Contains("alice", reset.ToString());
        Assert.DoesNotContain(_storedHash, reference.ToString());
    }

    private void AssertStoredValueOnlyInBody()
    {
        Assert.DoesNotContain(Uri.EscapeDataString(_storedHash), _pathAndQuery!);
        Assert.DoesNotContain(_storedHash, Uri.UnescapeDataString(_pathAndQuery!));
        using var body = JsonDocument.Parse(_body!);
        Assert.Equal(_storedHash, body.RootElement.GetProperty("valueEncrypted").GetString());
    }
}
