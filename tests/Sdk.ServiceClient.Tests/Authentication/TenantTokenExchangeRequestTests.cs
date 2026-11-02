using System.Net;
using System.Text;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Meshmakers.Octo.Sdk.ServiceClient.Authentication;

namespace Sdk.ServiceClient.Tests.Authentication;

public class TenantTokenExchangeRequestTests
{
    [Fact]
    public void Request_CarriesGrantSubjectTokenAndTargetTenant()
    {
        var request = AuthenticatorClient.BuildTokenExchangeRequest("https://issuer.test/connect/token",
            "octo-cli", null, "subject-jwt", "child");

        Assert.Equal("subject-jwt", request.SubjectToken);
        Assert.Equal(OidcConstants.TokenTypeIdentifiers.AccessToken, request.SubjectTokenType);
        Assert.Equal("octo-cli", request.ClientId);
        Assert.Equal("tenant:child", request.Parameters.GetValues(OidcConstants.AuthorizeRequest.AcrValues).Single());
        Assert.Contains("octo_api", request.Scope);
    }

    [Theory]
    [InlineData("tenant_not_provisioned", null, TenantTokenDenialCause.NotProvisioned)]
    [InlineData("unauthorized_client", "cross-tenant access to the target tenant is denied",
        TenantTokenDenialCause.AccessToTenantDenied)]
    [InlineData("unauthorized_client", "This client application is not allowed to use the specified grant type.",
        TenantTokenDenialCause.ClientNotAllowed)]
    [InlineData("unsupported_grant_type", null, TenantTokenDenialCause.ClientNotAllowed)]
    [InlineData("invalid_grant", "subject_token is invalid or expired", TenantTokenDenialCause.SubjectTokenRejected)]
    [InlineData("invalid_grant", "failed to provision the target-tenant user", TenantTokenDenialCause.AccessToTenantDenied)]
    [InlineData("invalid_target", "target tenant could not be resolved for this request",
        TenantTokenDenialCause.TenantUnknown)]
    [InlineData("server_error", null, TenantTokenDenialCause.Unknown)]
    [InlineData("temporarily_unavailable", "a tenant database is unavailable", TenantTokenDenialCause.TenantUnavailable)]
    public void IdentityErrors_AreMappedToACause(string error, string? description, TenantTokenDenialCause expected)
    {
        Assert.Equal(expected, AuthenticatorClient.MapExchangeError(error, description));
    }

    private static async Task<TokenResponse> RespondAsync(HttpStatusCode status, string body, string contentType = "application/json")
    {
        using var message = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, contentType)
        };
        return await ProtocolResponse.FromHttpResponseAsync<TokenResponse>(message);
    }

    [Fact]
    public async Task A503WithAnOAuthErrorBody_ShowsTheServersReason_NotUnreachable()
    {
        // AB#8177: the identity service answers 503 temporarily_unavailable when a tenant database is down.
        var response = await RespondAsync(HttpStatusCode.ServiceUnavailable,
            "{\"error\":\"temporarily_unavailable\",\"error_description\":\"MongoDB rejected the credentials of the tenant database user\"}");

        var ex = AuthenticatorClient.MapErrorResponse("scratch", response);

        Assert.Equal(TenantTokenDenialCause.TenantUnavailable, ex.Cause);
        Assert.Equal("temporarily_unavailable", ex.OAuthError);
        Assert.Contains("MongoDB rejected the credentials", ex.Message);
        Assert.DoesNotContain("could not be reached", ex.Message);
    }

    [Fact]
    public async Task A400OAuthError_IsMappedAsBefore()
    {
        var response = await RespondAsync(HttpStatusCode.BadRequest,
            "{\"error\":\"invalid_grant\",\"error_description\":\"subject_token is invalid or expired\"}");

        Assert.Equal(TenantTokenDenialCause.SubjectTokenRejected, AuthenticatorClient.MapErrorResponse("child", response).Cause);
    }

    [Fact]
    public async Task AGatewayErrorWithoutAnOAuthBody_IsStillUnreachable()
    {
        var response = await RespondAsync(HttpStatusCode.BadGateway, "<html>bad gateway</html>", "text/html");

        Assert.Equal(TenantTokenDenialCause.IdentityUnreachable, AuthenticatorClient.MapErrorResponse("child", response).Cause);
    }

    [Fact]
    public async Task AnUnhandled500_IsStillUnreachable()
    {
        var response = await RespondAsync(HttpStatusCode.InternalServerError, "", "text/plain");

        Assert.Equal(TenantTokenDenialCause.IdentityUnreachable, AuthenticatorClient.MapErrorResponse("child", response).Cause);
    }
}
