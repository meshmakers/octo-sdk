namespace Meshmakers.Octo.Sdk.ServiceClient.AiServices;

/// <summary>
///     Answer of <c>POST system/v1/aiSpendCaps/{tenantId}/starterDefault</c> (AB#9572): the outcome of applying the
///     cluster's default monthly LLM cap to a tenant the platform operator created with the starter path.
/// </summary>
public sealed class StarterSpendCapResultDto
{
    /// <summary>The cap entry was created now.</summary>
    public const string Created = "created";

    /// <summary>The tenant had a cap entry already; nothing changed.</summary>
    public const string Exists = "exists";

    /// <summary>No default cap is configured on the cluster; nothing was written.</summary>
    public const string NotConfigured = "notConfigured";

    /// <summary>Tenant id (lower case).</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary><see cref="Created" />, <see cref="Exists" /> or <see cref="NotConfigured" />.</summary>
    public string Outcome { get; set; } = string.Empty;

    /// <summary>Key source of the tenant after the call (<c>CentralKey</c>, <c>None</c>, …).</summary>
    public string KeySource { get; set; } = string.Empty;

    /// <summary>Monthly cap in EUR after the call (0 without an entry).</summary>
    public decimal CapMonthlyEur { get; set; }

    /// <summary>What happened, in words.</summary>
    public string Message { get; set; } = string.Empty;
}
