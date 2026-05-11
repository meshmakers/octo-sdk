namespace Meshmakers.Octo.Communication.Contracts.DataTransferObjects;

/// <summary>
/// A single Helm value override entry shipped from the controller to the
/// operator at deploy time. Mirrors the <c>System.Communication/ValueOverride</c>
/// CK record. Secret-flagged values are decrypted server-side before they
/// are placed on the SignalR wire.
/// </summary>
public record ValueOverrideDto
{
    /// <summary>
    /// Dotted Helm values path (e.g. <c>image.tag</c>, <c>oauth.clientId</c>).
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Override value. Always a string in transport; Helm coerces to the
    /// target type defined by the chart's values schema.
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// When true, the operator must surface this value through a Kubernetes
    /// <c>Secret</c> (named <c>{releaseName}-octo-secrets</c>) and rewrite the
    /// Helm value at <see cref="Path"/> to a <c>secretKeyRef</c> reference
    /// instead of inlining the value. The chart at this path must accept
    /// either form.
    /// </summary>
    public bool IsSecret { get; init; }
}
