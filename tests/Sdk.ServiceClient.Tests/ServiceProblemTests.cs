using System.Net;
using Meshmakers.Octo.Sdk.ServiceClient;

namespace Sdk.ServiceClient.Tests;

/// <summary>
///     AB#8346 — <see cref="ServiceProblem" /> reads RFC 9457 problem bodies from a <see cref="ServiceClientResultException" />.
/// </summary>
public class ServiceProblemTests
{
    [Fact]
    public void TryParse_RateLimited_ReadsRetryAfterSeconds()
    {
        var exception = new ServiceClientResultException("ignored", HttpStatusCode.TooManyRequests)
        {
            ResponseContent = "{\"status\":429,\"title\":\"Too many lookups\",\"detail\":\"Try again later.\"," +
                              "\"code\":\"SourceUserLookupRateLimited\",\"retryAfterSeconds\":240}"
        };

        Assert.True(ServiceProblem.TryParse(exception, out var problem));
        Assert.Equal(429, problem.Status);
        Assert.Equal("SourceUserLookupRateLimited", problem.Code);
        Assert.Equal(240, problem.RetryAfterSeconds);
        Assert.Equal(
            "Too many lookups: Try again later. (HTTP 429, code SourceUserLookupRateLimited). Retry after 240 s",
            problem.ToDisplayText());
    }

    [Fact]
    public void TryParse_Conflict_ReadsExistingMappingId()
    {
        var exception = new ServiceClientResultException("ignored", HttpStatusCode.Conflict)
        {
            ResponseContent = "{\"title\":\"Mapping exists\",\"code\":\"ExternalTenantUserMappingAlreadyExists\"," +
                              "\"existingMappingId\":\"6a1b2c3d4e5f60718293a4c0\"}"
        };

        Assert.True(ServiceProblem.TryParse(exception, out var problem));
        Assert.Equal(409, problem.Status);
        Assert.Equal("6a1b2c3d4e5f60718293a4c0", problem.ExistingMappingId);
        Assert.EndsWith("Existing mapping id: 6a1b2c3d4e5f60718293a4c0", problem.ToDisplayText());
    }

    [Fact]
    public void TryParse_FallsBackToTheMessage_WhenNoResponseContentIsSet()
    {
        var exception = new ServiceClientResultException(
            "{\"status\":403,\"title\":\"Forbidden\",\"code\":\"SourceTenantNotPermitted\"}", HttpStatusCode.Forbidden);

        Assert.True(ServiceProblem.TryParse(exception, out var problem));
        Assert.Equal("SourceTenantNotPermitted", problem.Code);
        Assert.Equal("Forbidden (HTTP 403, code SourceTenantNotPermitted)", problem.ToDisplayText());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("{\"message\":\"requires the role 'UserManagement'\"}")]
    public void TryParse_NoProblemBody_ReturnsFalse(string content)
    {
        var exception = new ServiceClientResultException("Forbidden", HttpStatusCode.Forbidden)
        {
            ResponseContent = content
        };

        Assert.False(ServiceProblem.TryParse(exception, out _));
    }
}
