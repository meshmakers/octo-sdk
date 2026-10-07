namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Report of the secret sweep of one tenant, as returned by bot-services
///     (<c>GET {tenantId}/v1/jobs/secret-sweep/report</c>, <c>GET system/v1/secrets/reports</c>; AB#5539).
///     Carries counts, CK type ids, runtime ids and attribute paths - never a value, never ciphertext.
/// </summary>
public class SecretSweepReportDto
{
    /// <summary>
    ///     Tenant.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    ///     The requested mode. Writing modes are followed by a <see cref="SecretSweepModeDto.Verify" /> step
    ///     that describes the state after the sweep.
    /// </summary>
    public SecretSweepModeDto Mode { get; set; }

    /// <summary>
    ///     What started the sweep.
    /// </summary>
    public SecretSweepTriggerDto Trigger { get; set; }

    /// <summary>
    ///     Outcome.
    /// </summary>
    public SecretSweepOutcomeDto Outcome { get; set; }

    /// <summary>
    ///     Why the sweep was skipped or failed, or a remark. Never contains a value.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    ///     Start (UTC).
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    ///     End (UTC).
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    ///     File name of the pre-sweep dump on the bot service, or <c>null</c> when none was taken.
    /// </summary>
    public string? BackupFileName { get; set; }

    /// <summary>
    ///     Active key id of the key ring at the time of the sweep, or <c>null</c> when no key is configured.
    /// </summary>
    public string? ActiveKeyId { get; set; }

    /// <summary>
    ///     True when strict mode was in force for the environment.
    /// </summary>
    public bool StrictModeActive { get; set; }

    /// <summary>
    ///     True when strict mode was in force and the final state still holds legacy values (clear text or
    ///     <c>enc:v1</c>).
    /// </summary>
    public bool StrictModeViolation { get; set; }

    /// <summary>
    ///     Clear-text plus <c>enc:v1</c> values in the final state (the last step).
    /// </summary>
    public long RemainingLegacyValues { get; set; }

    /// <summary>
    ///     The steps in execution order.
    /// </summary>
    public List<SecretSweepStepReportDto> Steps { get; set; } = [];

    /// <summary>
    ///     The secrets that were lost (unknown key id) and must be re-entered.
    /// </summary>
    public List<SecretValueReferenceDto> SecretsToReEnter { get; set; } = [];

    /// <summary>
    ///     Legacy clear-text placeholders (<c>TODO_SET_*</c>, <c>&lt;…&gt;</c>) converted once to "not set"
    ///     over all steps (migration only; placeholders have no meaning on any write path).
    /// </summary>
    public long PlaceholdersNormalized { get; set; }

    /// <summary>
    ///     <see cref="SecretSweepModeDto.CleanupUnreadable" /> only, summed over all steps: legacy <c>enc:v1</c>
    ///     values that were kept although unreadable, because only the legacy key
    ///     (<c>SecretEncryption:LegacyV1Key</c>) is missing - a configuration gap, not key loss. They stay in
    ///     <see cref="Unreadable" /> (key id <c>enc:v1</c>) and become readable once the legacy key is configured.
    /// </summary>
    public long SkippedLegacyV1KeyMissing { get; set; }

    /// <summary>
    ///     Stored values that cannot be read because their key id is not in the key ring - the re-entry list
    ///     (decision 2026-10-06). They are kept and become readable once the key is added to the ring; only
    ///     re-entry or <see cref="SecretSweepModeDto.CleanupUnreadable" /> removes them.
    /// </summary>
    public List<SecretUnreadableValueDto> Unreadable { get; set; } = [];
}

/// <summary>
///     One step of a secret sweep.
/// </summary>
public class SecretSweepStepReportDto
{
    /// <summary>
    ///     Mode of the step.
    /// </summary>
    public SecretSweepModeDto Mode { get; set; }

    /// <summary>
    ///     Start (UTC).
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    ///     End (UTC).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    ///     CK types scanned.
    /// </summary>
    public int CkTypesScanned { get; set; }

    /// <summary>
    ///     Entities read.
    /// </summary>
    public long EntitiesScanned { get; set; }

    /// <summary>
    ///     Entities with at least one rewritten attribute.
    /// </summary>
    public long EntitiesRewritten { get; set; }

    /// <summary>
    ///     Values changed.
    /// </summary>
    public long ValuesRewritten { get; set; }

    /// <summary>
    ///     Values converted from legacy clear text or <c>enc:v1</c> to <c>enc:v2</c> by this step (included in
    ///     <see cref="ValuesRewritten" />; AB#5534).
    /// </summary>
    public long EncryptedCount { get; set; }

    /// <summary>
    ///     Placeholders normalised to "not set".
    /// </summary>
    public long PlaceholdersNormalized { get; set; }

    /// <summary>
    ///     Attributes left alone because they changed while the sweep ran.
    /// </summary>
    public long SkippedConcurrentlyModified { get; set; }

    /// <summary>
    ///     <see cref="SecretSweepModeDto.CleanupUnreadable" /> only: legacy <c>enc:v1</c> values kept although
    ///     unreadable, because only the legacy key is missing (configuration gap, not key loss); they stay in
    ///     <see cref="Unreadable" />.
    /// </summary>
    public long SkippedLegacyV1KeyMissing { get; set; }

    /// <summary>
    ///     True when nothing failed.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Counts per form over the tenant, as found by this step (before it acted).
    /// </summary>
    public SecretFormCountsReportDto Totals { get; set; } = new();

    /// <summary>
    ///     Counts per CK type and Secret slot.
    /// </summary>
    public List<SecretSlotCountsReportDto> Slots { get; set; } = [];

    /// <summary>
    ///     Values deleted because their key id is unknown; only filled by
    ///     <see cref="SecretSweepModeDto.CleanupUnreadable" />.
    /// </summary>
    public List<SecretValueReferenceDto> Cleared { get; set; } = [];

    /// <summary>
    ///     Values found unreadable (unknown key id) and kept by this step.
    /// </summary>
    public List<SecretUnreadableValueDto> Unreadable { get; set; } = [];

    /// <summary>
    ///     Values that could not be processed.
    /// </summary>
    public List<SecretSweepFailureReportDto> Failures { get; set; } = [];
}

/// <summary>
///     Counts of Secret values per stored form.
/// </summary>
public class SecretFormCountsReportDto
{
    /// <summary>
    ///     No value.
    /// </summary>
    public long NotSet { get; set; }

    /// <summary>
    ///     Legacy placeholder or empty string.
    /// </summary>
    public long Placeholder { get; set; }

    /// <summary>
    ///     Legacy clear text.
    /// </summary>
    public long Plaintext { get; set; }

    /// <summary>
    ///     Legacy <c>enc:v1</c>.
    /// </summary>
    public long EncV1 { get; set; }

    /// <summary>
    ///     <c>enc:v2</c> with a known key id.
    /// </summary>
    public long EncV2 { get; set; }

    /// <summary>
    ///     <c>enc:v2</c> with a known key id, per key id.
    /// </summary>
    public Dictionary<string, long> EncV2ByKeyId { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     <c>enc:v2</c> with an unknown key id.
    /// </summary>
    public long UnknownKeyId { get; set; }

    /// <summary>
    ///     <c>enc:v2</c> with an unknown key id, per key id.
    /// </summary>
    public Dictionary<string, long> UnknownKeyIdByKeyId { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Values that could not be processed (counted in their form as well).
    /// </summary>
    public long Failed { get; set; }

    /// <summary>
    ///     All classified slots.
    /// </summary>
    public long Total { get; set; }

    /// <summary>
    ///     Clear text plus <c>enc:v1</c>, as computed by the service.
    /// </summary>
    public long Legacy { get; set; }
}

/// <summary>
///     Counts for one Secret slot of one CK type.
/// </summary>
public class SecretSlotCountsReportDto
{
    /// <summary>
    ///     CK type.
    /// </summary>
    public string CkTypeId { get; set; } = string.Empty;

    /// <summary>
    ///     Attribute name or record path.
    /// </summary>
    public string AttributePath { get; set; } = string.Empty;

    /// <summary>
    ///     Counts per form.
    /// </summary>
    public SecretFormCountsReportDto Counts { get; set; } = new();
}

/// <summary>
///     Reference to one Secret value (never the value itself).
/// </summary>
public class SecretValueReferenceDto
{
    /// <summary>
    ///     CK type of the entity.
    /// </summary>
    public string CkTypeId { get; set; } = string.Empty;

    /// <summary>
    ///     Runtime id of the entity.
    /// </summary>
    public string RtId { get; set; } = string.Empty;

    /// <summary>
    ///     Attribute name or record path (record elements by record key).
    /// </summary>
    public string AttributePath { get; set; } = string.Empty;

    /// <summary>
    ///     Form the value had.
    /// </summary>
    public SecretValueFormDto PreviousForm { get; set; }

    /// <summary>
    ///     Key id of the envelope, if any.
    /// </summary>
    public string? KeyId { get; set; }
}

/// <summary>
///     A stored Secret value that cannot be read because its key id is not in the key ring - an entry of
///     the re-entry list. Never the value, never ciphertext.
/// </summary>
public class SecretUnreadableValueDto
{
    /// <summary>
    ///     CK type of the entity.
    /// </summary>
    public string CkTypeId { get; set; } = string.Empty;

    /// <summary>
    ///     Runtime id of the entity.
    /// </summary>
    public string RtId { get; set; } = string.Empty;

    /// <summary>
    ///     Attribute name or record path (record elements by record key).
    /// </summary>
    public string AttributePath { get; set; } = string.Empty;

    /// <summary>
    ///     Key id of the envelope (not in the key ring).
    /// </summary>
    public string? KeyId { get; set; }
}

/// <summary>
///     A value the sweep could not process.
/// </summary>
public class SecretSweepFailureReportDto
{
    /// <summary>
    ///     CK type of the entity.
    /// </summary>
    public string CkTypeId { get; set; } = string.Empty;

    /// <summary>
    ///     Runtime id of the entity.
    /// </summary>
    public string RtId { get; set; } = string.Empty;

    /// <summary>
    ///     Attribute name or record path.
    /// </summary>
    public string AttributePath { get; set; } = string.Empty;

    /// <summary>
    ///     Value-free reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}
