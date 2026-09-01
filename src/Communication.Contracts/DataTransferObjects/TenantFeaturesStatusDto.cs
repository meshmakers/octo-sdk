namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
///     Aggregate enabled-state of the four per-tenant capabilities the tenant delete/detach guard
///     evaluates (AB#4255): Stream Data, Communication, Reporting, AI Services. Served by the asset
///     repository's <c>GET {tenantId}/v1/features/status</c> (AB#4884).
/// </summary>
/// <remarks>
///     Whether a capability's service is part of the installation at all is not answered here (except
///     for Stream Data's instance-level flag) — browser clients read that from the
///     <c>_configuration</c> discovery document, where an empty service URL means "not installed".
/// </remarks>
public class TenantFeaturesStatusDto
{
    /// <summary>Stream Data state (tenant flag plus the instance-level kill switch).</summary>
    public StreamDataFeatureStatusDto? StreamData { get; set; }

    /// <summary>Communication state.</summary>
    public TenantFeatureStatusDto? Communication { get; set; }

    /// <summary>Reporting state.</summary>
    public TenantFeatureStatusDto? Reporting { get; set; }

    /// <summary>AI Services state.</summary>
    public TenantFeatureStatusDto? AiServices { get; set; }
}

/// <summary>Enabled-state of one capability for the tenant.</summary>
public class TenantFeatureStatusDto
{
    /// <summary>True when the tenant's capability flag exists and reads enabled.</summary>
    public bool TenantEnabled { get; set; }
}

/// <summary>
///     Stream Data state: the tenant flag plus the deployment-wide <c>StreamData:Enabled</c> instance
///     flag. The tenant flag is reported regardless of the instance flag, so a tenant left enabled on
///     an installation without stream data is visible as exactly that.
/// </summary>
public class StreamDataFeatureStatusDto
{
    /// <summary>True when stream data is enabled at the instance level.</summary>
    public bool InstanceEnabled { get; set; }

    /// <summary>True when the tenant's stream data flag reads enabled.</summary>
    public bool TenantEnabled { get; set; }
}
