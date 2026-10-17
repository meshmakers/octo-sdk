using System.Text;
using System.Text.Json;

namespace Sdk.ServiceClient.Tests.Authentication;

/// <summary>Builds unsigned JWT-shaped strings; the SDK only reads the payload.</summary>
internal static class TestJwt
{
    public static string For(string? tenantId, string marker = "x")
    {
        var payload = new Dictionary<string, object> { ["sub"] = "user-1", ["marker"] = marker };
        if (tenantId != null)
        {
            payload["tenant_id"] = tenantId;
        }

        return $"{Encode("""{"alg":"none"}""")}.{Encode(JsonSerializer.Serialize(payload))}.sig";
    }

    private static string Encode(string json) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
