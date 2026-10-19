using System.Net;
using System.Text.Json;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.System;

/// <summary>
///     The untouched answer of a tenant's GraphQL route (AB#7027). <see cref="Content" /> is the response body as the
///     service sent it, so a script gets exactly what a direct call would have returned.
/// </summary>
/// <param name="Content">The response body, unmodified.</param>
/// <param name="StatusCode">The HTTP status code of the answer.</param>
public sealed record GraphQlRawResponse(string Content, HttpStatusCode StatusCode)
{
    /// <summary>
    ///     True when the answer carries a non-empty top-level <c>errors</c> array (GraphQL errors arrive with
    ///     HTTP 200 as well as with 400). Never throws: a body that is not JSON has no GraphQL errors.
    /// </summary>
    public bool HasErrors => ContainsErrors(Content);

    internal static bool ContainsErrors(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                   document.RootElement.TryGetProperty("errors", out var errors) &&
                   errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
