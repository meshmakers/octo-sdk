namespace Meshmakers.Octo.Sdk.ServiceClient.AiServices;

/// <summary>Spend of one tenant in one UTC month for central-key LLM calls (AI service ledger).</summary>
public sealed class AiSpendMonthDto
{
    /// <summary>Month as <c>yyyy-MM</c> (UTC).</summary>
    public string Month { get; set; } = string.Empty;

    /// <summary>Settled spend in EUR.</summary>
    public decimal SpentEur { get; set; }

    /// <summary>Reservations of calls still running, in EUR.</summary>
    public decimal ReservedEur { get; set; }

    /// <summary>Calls that got a reservation in the month.</summary>
    public long CallCount { get; set; }

    /// <summary>When the month's budget resets (first day of the next month, 00:00 UTC).</summary>
    public DateTimeOffset ResetsAt { get; set; }
}

/// <summary>
///     Key source, monthly cap and spend (current and previous UTC month) of a tenant's central-key LLM budget. The cap is
///     owned by the platform; the tenant reads it only.
/// </summary>
public sealed class AiSpendReportDto
{
    /// <summary>Tenant id (lower case).</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary><c>CentralKey</c>, <c>ByoKey</c>, <c>Subscription</c> or <c>None</c> (no cap set, no central-key budget).</summary>
    public string KeySource { get; set; } = "None";

    /// <summary>Monthly cap in EUR (0 when none is set).</summary>
    public decimal CapMonthlyEur { get; set; }

    /// <summary>When the cap was last changed (null when none is set).</summary>
    public DateTimeOffset? CapChangedAt { get; set; }

    /// <summary>Current month first, then the previous month.</summary>
    public List<AiSpendMonthDto> Months { get; set; } = [];
}

/// <summary>Body of a cap change (platform role <c>AiBudgetAdmin</c> on the system tenant).</summary>
public sealed class SetAiSpendCapRequestDto
{
    /// <summary>Monthly cap in EUR (0 or more).</summary>
    public decimal CapMonthlyEur { get; set; }

    /// <summary>Key source: <c>CentralKey</c> (default), <c>None</c>, <c>ByoKey</c> or <c>Subscription</c>.</summary>
    public string KeySource { get; set; } = "CentralKey";

    /// <summary>Why the cap changes; stored in the audit record (required, at most 500 characters).</summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Result of a cap change: old and new value.</summary>
public sealed class AiSpendCapChangeDto
{
    /// <summary>Tenant id (lower case).</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Key source before.</summary>
    public string OldKeySource { get; set; } = string.Empty;

    /// <summary>Cap before in EUR.</summary>
    public decimal OldCapMonthlyEur { get; set; }

    /// <summary>Key source after.</summary>
    public string NewKeySource { get; set; } = string.Empty;

    /// <summary>Cap after in EUR.</summary>
    public decimal NewCapMonthlyEur { get; set; }

    /// <summary>Who made the change.</summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>When (UTC).</summary>
    public DateTimeOffset ChangedAt { get; set; }
}

/// <summary>One audit record of a cap change.</summary>
public sealed class AiSpendCapAuditDto
{
    /// <summary>When (UTC).</summary>
    public DateTimeOffset At { get; set; }

    /// <summary>Who.</summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>Why.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Key source before.</summary>
    public string OldKeySource { get; set; } = string.Empty;

    /// <summary>Cap before in EUR.</summary>
    public decimal OldCapMonthlyEur { get; set; }

    /// <summary>Key source after.</summary>
    public string NewKeySource { get; set; } = string.Empty;

    /// <summary>Cap after in EUR.</summary>
    public decimal NewCapMonthlyEur { get; set; }
}
