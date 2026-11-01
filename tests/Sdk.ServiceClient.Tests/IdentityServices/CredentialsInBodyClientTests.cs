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

    private readonly HttpListener _listener = new();
    private readonly string _baseUri;
    private string? _method;
    private string? _pathAndQuery;
    private string? _body;
    private string? _contentType;
    private string _responseJson = "{}";

    public CredentialsInBodyClientTests()
    {
        var started = false;
        var uri = "";
        for (var port = 45000 + Random.Shared.Next(0, 2000); !started; port++)
        {
            try
            {
                uri = $"http://127.0.0.1:{port}/";
                _listener.Prefixes.Clear();
                _listener.Prefixes.Add(uri);
                _listener.Start();
                started = true;
            }
            catch (HttpListenerException)
            {
                // port taken, try the next one
            }
        }

        _baseUri = uri;
        _ = Task.Run(Serve);
    }

    public void Dispose() => _listener.Close();

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
        await CreateClient().ResetPassword("alice", _password);

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
