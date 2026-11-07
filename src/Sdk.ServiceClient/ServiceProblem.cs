using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Meshmakers.Octo.Sdk.ServiceClient;

/// <summary>
///     An RFC 9457 problem answer (<c>application/problem+json</c>) of an OctoMesh service, read from a
///     <see cref="ServiceClientResultException" /> (AB#8346). Clients branch on <see cref="Code" />, the stable,
///     language-neutral <c>code</c> extension (e.g. the constants in
///     <c>Meshmakers.Octo.Communication.Contracts.DataTransferObjects.ApiErrors.CrossTenantGrantProblemCodes</c>);
///     <see cref="Title" /> and <see cref="Detail" /> are English text for humans.
/// </summary>
public sealed record ServiceProblem
{
    /// <summary>The <c>status</c> member, or the HTTP status of the answer when the body has none.</summary>
    public int Status { get; init; }

    /// <summary>The <c>code</c> extension, if any.</summary>
    public string? Code { get; init; }

    /// <summary>The <c>title</c> member, if any.</summary>
    public string? Title { get; init; }

    /// <summary>The <c>detail</c> member, if any.</summary>
    public string? Detail { get; init; }

    /// <summary>The <c>retryAfterSeconds</c> extension of a rate-limited answer (<c>429</c>), if any.</summary>
    public int? RetryAfterSeconds { get; init; }

    /// <summary>The <c>unknownGroupIds</c> extension of an <c>UnknownGroup</c> answer; empty otherwise.</summary>
    public IReadOnlyList<string> UnknownGroupIds { get; init; } = [];

    /// <summary>The <c>existingMappingId</c> extension of an <c>ExternalTenantUserMappingAlreadyExists</c> answer.</summary>
    public string? ExistingMappingId { get; init; }

    /// <summary>
    ///     Reads the problem body of <paramref name="exception" />. Returns <c>false</c> when the answer had no JSON object
    ///     body, or the body carries neither <c>code</c>, <c>title</c> nor <c>detail</c> (then it is no problem answer).
    /// </summary>
    /// <param name="exception">The exception thrown by a service client.</param>
    /// <param name="problem">The problem, when the method returns <c>true</c>.</param>
    public static bool TryParse(ServiceClientResultException exception, [NotNullWhen(true)] out ServiceProblem? problem)
    {
        ArgumentNullException.ThrowIfNull(exception);
        problem = null;

        var content = exception.ResponseContent;
        if (string.IsNullOrWhiteSpace(content))
        {
            // Exceptions built without ResponseContent keep the body in the message ("{status}: {body}").
            var start = exception.Message.IndexOf('{');
            if (start < 0)
            {
                return false;
            }

            content = exception.Message[start..];
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var code = ReadString(root, "code");
            var title = ReadString(root, "title");
            var detail = ReadString(root, "detail");
            if (code == null && title == null && detail == null)
            {
                return false;
            }

            problem = new ServiceProblem
            {
                Status = root.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.Number &&
                         status.TryGetInt32(out var statusValue)
                    ? statusValue
                    : (int)exception.HttpStatusCode,
                Code = code,
                Title = title,
                Detail = detail,
                RetryAfterSeconds = root.TryGetProperty("retryAfterSeconds", out var retry) &&
                                    retry.ValueKind == JsonValueKind.Number && retry.TryGetInt32(out var retryValue)
                    ? retryValue
                    : null,
                UnknownGroupIds = root.TryGetProperty("unknownGroupIds", out var groups) &&
                                  groups.ValueKind == JsonValueKind.Array
                    ? groups.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String)
                        .Select(e => e.GetString()!).ToList()
                    : [],
                ExistingMappingId = ReadString(root, "existingMappingId")
            };
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    ///     One readable line: title and detail, the code, and the extensions a caller acts on (wait time, unknown group
    ///     ids, existing mapping id). Contains only what the service answered.
    /// </summary>
    public string ToDisplayText()
    {
        var text = new StringBuilder();
        text.Append(Title ?? $"HTTP {Status}");
        if (!string.IsNullOrWhiteSpace(Detail))
        {
            text.Append(": ").Append(Detail);
        }

        text.Append(" (HTTP ").Append(Status.ToString(CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(Code))
        {
            text.Append(", code ").Append(Code);
        }

        text.Append(')');

        if (RetryAfterSeconds is { } seconds)
        {
            text.Append(". Retry after ").Append(seconds.ToString(CultureInfo.InvariantCulture)).Append(" s");
        }

        if (UnknownGroupIds.Count > 0)
        {
            text.Append(". Unknown group ids: ").Append(string.Join(", ", UnknownGroupIds));
        }

        if (!string.IsNullOrWhiteSpace(ExistingMappingId))
        {
            text.Append(". Existing mapping id: ").Append(ExistingMappingId);
        }

        return text.ToString();
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String &&
        !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()
            : null;
}
