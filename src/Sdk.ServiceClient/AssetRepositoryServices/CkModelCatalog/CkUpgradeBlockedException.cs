using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Meshmakers.Octo.Sdk.ServiceClient.AssetRepositoryServices.CkModelCatalog;

/// <summary>
///     The asset repository refused an import because the update set would break an installed model (tenant upgrade
///     guard, CK v2 F2.5, AB#6870): HTTP 409 with the stable code <see cref="ErrorCode" />. Nothing was imported.
///     <see cref="Blockers" /> is language-neutral (dependent, update, kind, subject, code, resolve hint); repeat the
///     request with <c>ForceBreakingUpdate = true</c> to override deliberately.
/// </summary>
[Serializable]
public class CkUpgradeBlockedException : ServiceClientResultException
{
    /// <summary>The stable error code of the refusal.</summary>
    public const string ErrorCode = "CK_UPGRADE_BLOCKED";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="detail">English explanation of the server</param>
    /// <param name="updateSet">The requested update set (full model ids)</param>
    /// <param name="blockers">The installed models the update set would break</param>
    public CkUpgradeBlockedException(string? detail, IReadOnlyList<string> updateSet,
        IReadOnlyList<CkUpgradeBlockerDto> blockers)
        : base(string.IsNullOrEmpty(detail) ? ErrorCode : $"{ErrorCode}: {detail}", HttpStatusCode.Conflict)
    {
        Detail = detail;
        UpdateSet = updateSet;
        Blockers = blockers;
    }

    /// <summary>English explanation of the server (names the dependents and how to proceed).</summary>
    public string? Detail { get; }

    /// <summary>The requested update set (full model ids).</summary>
    public IReadOnlyList<string> UpdateSet { get; }

    /// <summary>The installed models the update set would break, one entry per model and pin.</summary>
    public IReadOnlyList<CkUpgradeBlockerDto> Blockers { get; }

    /// <summary>
    ///     Reads the problem details of a 409 answer. Returns <c>null</c> when the body is not the
    ///     <see cref="ErrorCode" /> problem (another 409, or no JSON), so the caller falls back to the generic error.
    /// </summary>
    internal static CkUpgradeBlockedException? TryParse(HttpStatusCode statusCode, string? content)
    {
        if (statusCode != HttpStatusCode.Conflict || string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        try
        {
            var problem = JsonSerializer.Deserialize<Problem>(content, JsonOptions);
            if (problem == null || !string.Equals(problem.Code, ErrorCode, StringComparison.Ordinal))
            {
                return null;
            }

            return new CkUpgradeBlockedException(problem.Detail, problem.UpdateSet ?? [], problem.Blockers ?? []);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class Problem
    {
        public string? Code { get; set; }
        public string? Detail { get; set; }
        public List<string>? UpdateSet { get; set; }
        public List<CkUpgradeBlockerDto>? Blockers { get; set; }
    }
}
